using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RadLibsAdminTool
{
    class Program
    {
        private const string FIREBASE_URL = "https://radlibsapp-default-rtdb.firebaseio.com";
        private static FirebaseApp? _firebaseApp;
        private static GoogleCredential? _credential;

        static async Task<int> Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("========================================");
            Console.WriteLine("  RadLibs Admin Tool v2.0");
            Console.WriteLine("  Firebase Collection Upload Manager");
            Console.WriteLine("========================================");
            Console.ResetColor();
            Console.WriteLine();

            try
            {
                if (!InitializeFirebase())
                {
                    return 1;
                }

                if (args.Length > 0)
                {
                    return await ExecuteCommandAsync(args);
                }

                return await InteractiveModeAsync();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nFatal Error: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        static bool InitializeFirebase()
        {
            try
            {
                var serviceAccountPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "radlibsapp-service-account.json");

                if (!File.Exists(serviceAccountPath))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Service account file not found!");
                    Console.WriteLine($"Expected: {serviceAccountPath}");
                    Console.ResetColor();
                    return false;
                }

                _credential = GoogleCredential.FromFile(serviceAccountPath)
                    .CreateScoped("https://www.googleapis.com/auth/firebase.database", "https://www.googleapis.com/auth/userinfo.email");

                _firebaseApp = FirebaseApp.Create(new AppOptions
                {
                    Credential = _credential,
                    ProjectId = "radlibsapp"
                });

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Connected to Firebase (Admin SDK)");
                Console.ResetColor();
                Console.WriteLine();
                return true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Firebase initialization failed: {ex.Message}");
                Console.ResetColor();
                return false;
            }
        }

        static async Task<string> GetAccessTokenAsync()
        {
            if (_credential == null)
                throw new InvalidOperationException("Firebase not initialized");

            return await ((ITokenAccess)_credential).GetAccessTokenForRequestAsync();
        }

        static async Task<int> ExecuteCommandAsync(string[] args)
        {
            var command = args[0].ToLowerInvariant();

            switch (command)
            {
                case "upload":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: RadLibsAdminTool upload <file-or-directory>");
                        return 1;
                    }
                    return await UploadCollectionsAsync(args[1]);

                case "auto":
                case "auto-upload":
                    return await AutoUploadFromOutFolderAsync();

                case "watch":
                    return await WatchOutFolderAsync();

                case "list":
                    return await ListCollectionsAsync();

                case "delete":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: RadLibsAdminTool delete <story-id>");
                        return 1;
                    }
                    return await DeleteStoryAsync(args[1]);

                case "clear":
                    return await ClearAllStoriesAsync();

                case "migrate":
                case "migrate-schema":
                case "fix":
                    return await MigrateToCollectionSchemaAsync();

                case "version":
                case "update-version":
                    if (args.Length > 1)
                    {
                        return await SetContentVersionAsync(args[1]);
                    }
                    return await ShowCurrentVersionAsync();

                case "help":
                case "--help":
                case "-h":
                    ShowHelp();
                    return 0;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    Console.WriteLine("Run 'RadLibsAdminTool help' for usage information.");
                    return 1;
            }
        }

        static async Task<int> InteractiveModeAsync()
        {
            while (true)
            {
                Console.WriteLine("\nWhat would you like to do?");
                Console.WriteLine("  1. Upload collection file or directory");
                Console.WriteLine("  2. Auto-upload from 'out' folder");
                Console.WriteLine("  3. Watch 'out' folder (continuous)");
                Console.WriteLine("  4. List all collections in database");
                Console.WriteLine("  5. Delete a specific story");
                Console.WriteLine("  6. Clear all stories (dangerous!)");
                Console.WriteLine("  7. Migrate DB to collection schema");
                Console.WriteLine("  8. Show current content version");
                Console.WriteLine("  9. Exit");
                Console.Write("\nChoice (1-9): ");

                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        Console.Write("Enter file or directory path: ");
                        var path = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            await UploadCollectionsAsync(path);
                        }
                        break;
                    case "2":
                        await AutoUploadFromOutFolderAsync();
                        break;
                    case "3":
                        await WatchOutFolderAsync();
                        break;
                    case "4":
                        await ListCollectionsAsync();
                        break;
                    case "5":
                        Console.Write("Enter story ID to delete: ");
                        var storyId = Console.ReadLine();
                        if (!string.IsNullOrWhiteSpace(storyId))
                        {
                            await DeleteStoryAsync(storyId);
                        }
                        break;
                    case "6":
                        await ClearAllStoriesAsync();
                        break;
                    case "7":
                        await MigrateToCollectionSchemaAsync();
                        break;
                    case "8":
                        await ShowCurrentVersionAsync();
                        break;
                    case "9":
                        Console.WriteLine("\nGoodbye!");
                        return 0;
                    default:
                        Console.WriteLine("Invalid choice. Please enter 1-9.");
                        break;
                }
            }
        }

        static async Task<int> UploadCollectionsAsync(string path)
        {
            return await UploadCollectionsAsync(path, moveToArchive: false);
        }

        static async Task<int> UploadCollectionsAsync(string path, bool moveToArchive)
        {
            try
            {
                var files = GetStoryFiles(path);
                if (files.Count == 0)
                {
                    Console.WriteLine($"No JSON files found in: {path}");
                    return 1;
                }

                Console.WriteLine($"\nFound {files.Count} collection file(s)");
                Console.WriteLine();

                int successCollections = 0;
                int successStories = 0;
                int failCount = 0;
                var processedFiles = new List<string>();

                using var httpClient = CreateAuthorizedHttpClient(await GetAccessTokenAsync());

                foreach (var file in files)
                {
                    Console.Write($"Uploading {Path.GetFileName(file)}... ");

                    try
                    {
                        var collection = await ParseCollectionFileAsync(file);
                        var uploadBody = BuildFirebaseCollectionNode(collection);

                        var url = $"{FIREBASE_URL}/radlibs/{Uri.EscapeDataString(collection.Category)}.json";
                        var response = await httpClient.PutAsync(url, CreateJsonContent(uploadBody));
                        response.EnsureSuccessStatusCode();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"OK ({collection.Stories.Count} stories -> {collection.Category})");
                        Console.ResetColor();

                        successCollections++;
                        successStories += collection.Stories.Count;
                        processedFiles.Add(file);
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"FAILED: {ex.Message}");
                        Console.ResetColor();
                        failCount++;
                    }

                    await Task.Delay(100);
                }

                if (moveToArchive && processedFiles.Count > 0)
                {
                    MoveFilesToArchive(processedFiles);
                }

                Console.WriteLine();
                Console.WriteLine("===================================");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Upload Complete");
                Console.ResetColor();
                Console.WriteLine($"Collections uploaded: {successCollections}");
                Console.WriteLine($"Stories uploaded: {successStories}");
                if (failCount > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Failed files: {failCount}");
                    Console.ResetColor();
                }

                if (successCollections > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Updating content metadata...");
                    await UpdateContentMetadataAsync(httpClient, incrementVersion: true);
                }

                return failCount > 0 ? 1 : 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nUpload failed: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        static async Task<CollectionPayload> ParseCollectionFileAsync(string filePath)
        {
            var json = await File.ReadAllTextAsync(filePath);
            var node = JsonNode.Parse(json) ?? throw new InvalidOperationException($"Invalid JSON in {filePath}");

            string? category = null;
            string? packId = null;
            bool? isPremium = null;
            int? creditCost = null;
            var tags = new List<string>();
            var stories = new List<JsonObject>();

            if (node is JsonArray array)
            {
                stories = array.Select(s => s?.AsObject() ?? throw new InvalidOperationException("Story entry must be an object")).ToList();
            }
            else if (node is JsonObject obj)
            {
                category = obj["Category"]?.GetValue<string>() ?? obj["category"]?.GetValue<string>();
                packId = obj["PackId"]?.GetValue<string>() ?? obj["packId"]?.GetValue<string>();
                isPremium = obj["IsPremium"]?.GetValue<bool>() ?? obj["isPremium"]?.GetValue<bool>();
                creditCost = obj["creditCost"]?.GetValue<int>() ?? obj["CreditCost"]?.GetValue<int>();
                tags = ExtractTagsFromNode(obj["tags"] ?? obj["Tags"]);

                if (obj["stories"] is JsonArray storiesArray)
                {
                    stories = storiesArray.Select(s => s?.AsObject() ?? throw new InvalidOperationException("Story entry must be an object")).ToList();
                }
                else if (obj["stories"] is JsonObject storyMap)
                {
                    stories = storyMap.Select(kvp => kvp.Value?.AsObject() ?? throw new InvalidOperationException("Story entry must be an object")).ToList();
                }
                else if (obj["Id"] != null)
                {
                    stories.Add(obj);
                }
            }

            if (stories.Count == 0)
            {
                throw new InvalidOperationException($"No stories found in {filePath}");
            }

            category ??= stories[0]["Category"]?.GetValue<string>() ?? Path.GetFileNameWithoutExtension(filePath);
            category = category.Trim();
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new InvalidOperationException("Collection category is required");
            }

            packId ??= stories[0]["PackId"]?.GetValue<string>() ?? SlugifyPackId(category);
            packId = packId.Trim();
            if (string.IsNullOrWhiteSpace(packId))
            {
                throw new InvalidOperationException("Collection PackId is required");
            }

            isPremium ??= stories[0]["IsPremium"]?.GetValue<bool>() ?? false;
            creditCost ??= 1;

            var normalizedStories = new List<JsonObject>();
            foreach (var story in stories)
            {
                normalizedStories.Add(NormalizeStory(story, packId));
            }

            if (tags.Count == 0)
            {
                tags = BuildDefaultTags(category, normalizedStories);
            }

            return new CollectionPayload(category, packId, isPremium.Value, creditCost.Value, tags, normalizedStories);
        }

        static JsonObject NormalizeStory(JsonObject inputStory, string packId)
        {
            var required = new[] { "Id", "Title", "Story", "Placeholders" };
            foreach (var field in required)
            {
                if (inputStory[field] == null)
                {
                    throw new InvalidOperationException($"Story missing required field '{field}'");
                }
            }

            var normalized = new JsonObject
            {
                ["Id"] = inputStory["Id"]!.DeepClone(),
                ["Title"] = inputStory["Title"]!.DeepClone(),
                ["Story"] = inputStory["Story"]!.DeepClone(),
                ["Placeholders"] = inputStory["Placeholders"]!.DeepClone(),
                ["CreatedDate"] = inputStory["CreatedDate"]?.DeepClone() ?? DateTime.UtcNow.ToString("o"),
                ["IsFavorite"] = inputStory["IsFavorite"]?.GetValue<bool>() ?? false,
                ["PackId"] = inputStory["PackId"]?.GetValue<string>() ?? packId
            };

            return normalized;
        }

        static JsonObject BuildFirebaseCollectionNode(CollectionPayload collection)
        {
            var storiesObject = new JsonObject();
            foreach (var story in collection.Stories)
            {
                var storyId = story["Id"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(storyId))
                {
                    throw new InvalidOperationException("Story Id cannot be null or empty");
                }

                storiesObject[storyId] = story.DeepClone();
            }

            var tagsArray = new JsonArray(collection.Tags.Select(t => (JsonNode?)t).ToArray());

            return new JsonObject
            {
                ["creditCost"] = collection.CreditCost,
                ["packId"] = collection.PackId,
                ["isPremium"] = collection.IsPremium,
                ["stories"] = storiesObject,
                ["tags"] = tagsArray
            };
        }

        static async Task<int> AutoUploadFromOutFolderAsync()
        {
            try
            {
                var outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "out");
                if (!Directory.Exists(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }

                Console.WriteLine($"Scanning 'out' folder: {outDir}");
                return await UploadCollectionsAsync(outDir, moveToArchive: true);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nAuto-upload failed: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        static async Task<int> WatchOutFolderAsync()
        {
            try
            {
                var outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "out");
                if (!Directory.Exists(outDir))
                {
                    Directory.CreateDirectory(outDir);
                }

                Console.WriteLine("Watching 'out' folder for new files...");
                Console.WriteLine($"Location: {Path.GetFullPath(outDir)}");
                Console.WriteLine("Press Ctrl+C to stop watching");

                using var watcher = new FileSystemWatcher(outDir)
                {
                    Filter = "*.json",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite
                };

                var fileQueue = new HashSet<string>();
                var processingLock = new object();

                async Task ProcessFileAsync(string filePath)
                {
                    await Task.Delay(500);
                    lock (processingLock)
                    {
                        if (!fileQueue.Contains(filePath)) return;
                        fileQueue.Remove(filePath);
                    }

                    Console.WriteLine($"\nNew file detected: {Path.GetFileName(filePath)}");
                    await UploadCollectionsAsync(filePath, moveToArchive: true);
                }

                watcher.Created += (_, e) =>
                {
                    lock (processingLock)
                    {
                        if (!fileQueue.Contains(e.FullPath))
                        {
                            fileQueue.Add(e.FullPath);
                            _ = Task.Run(async () => await ProcessFileAsync(e.FullPath));
                        }
                    }
                };

                watcher.EnableRaisingEvents = true;
                await AutoUploadFromOutFolderAsync();
                await Task.Delay(Timeout.Infinite);
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nWatch failed: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        static List<string> GetStoryFiles(string path)
        {
            var files = new List<string>();
            if (File.Exists(path) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                files.Add(path);
            }
            else if (Directory.Exists(path))
            {
                files.AddRange(Directory.GetFiles(path, "*.json", SearchOption.TopDirectoryOnly));
            }

            return files;
        }

        static async Task<int> ListCollectionsAsync()
        {
            try
            {
                using var httpClient = CreateAuthorizedHttpClient(await GetAccessTokenAsync());
                var response = await httpClient.GetAsync($"{FIREBASE_URL}/radlibs.json");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json) || json == "null")
                {
                    Console.WriteLine("No collections found.");
                    return 0;
                }

                var node = JsonNode.Parse(json) as JsonObject;
                if (node == null)
                {
                    Console.WriteLine("Unexpected radlibs structure.");
                    return 1;
                }

                Console.WriteLine("\nCollections in Database:");
                Console.WriteLine("===================================");

                var collectionCount = 0;
                var totalStories = 0;
                foreach (var collection in node)
                {
                    if (collection.Value is not JsonObject collectionObj)
                    {
                        continue;
                    }

                    var stories = collectionObj["stories"] as JsonObject;
                    var storyCount = stories?.Count ?? 0;
                    totalStories += storyCount;

                    var packId = collectionObj["packId"]?.GetValue<string>() ?? "(missing)";
                    var isPremium = collectionObj["isPremium"]?.GetValue<bool>() ?? false;
                    var creditCost = collectionObj["creditCost"]?.GetValue<int>() ?? 1;

                    collectionCount++;
                    Console.WriteLine($"{collectionCount}. {collection.Key}");
                    Console.WriteLine($"   packId: {packId}");
                    Console.WriteLine($"   isPremium: {isPremium}");
                    Console.WriteLine($"   creditCost: {creditCost}");
                    Console.WriteLine($"   stories: {storyCount}");
                }

                Console.WriteLine();
                Console.WriteLine($"Total collections: {collectionCount}");
                Console.WriteLine($"Total stories: {totalStories}");
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nFailed to list collections: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        static async Task<int> DeleteStoryAsync(string storyId)
        {
            try
            {
                Console.Write($"Are you sure you want to delete '{storyId}'? (yes/no): ");
                var confirm = Console.ReadLine();
                if (!string.Equals(confirm, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Cancelled.");
                    return 0;
                }

                using var httpClient = CreateAuthorizedHttpClient(await GetAccessTokenAsync());
                var response = await httpClient.GetAsync($"{FIREBASE_URL}/radlibs.json");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var root = JsonNode.Parse(json) as JsonObject;

                if (root == null)
                {
                    Console.WriteLine("No collections available.");
                    return 1;
                }

                foreach (var collection in root)
                {
                    if (collection.Value is not JsonObject collectionObj) continue;
                    if (collectionObj["stories"] is not JsonObject stories) continue;
                    if (!stories.ContainsKey(storyId)) continue;

                    var deleteUrl = $"{FIREBASE_URL}/radlibs/{Uri.EscapeDataString(collection.Key)}/stories/{Uri.EscapeDataString(storyId)}.json";
                    var deleteResponse = await httpClient.DeleteAsync(deleteUrl);
                    deleteResponse.EnsureSuccessStatusCode();

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Deleted story '{storyId}' from collection '{collection.Key}'.");
                    Console.ResetColor();
                    await UpdateContentMetadataAsync(httpClient, incrementVersion: true);
                    return 0;
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Story '{storyId}' not found.");
                Console.ResetColor();
                return 1;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nFailed to delete story: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        static async Task<int> ClearAllStoriesAsync()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("\nWARNING: This will delete ALL collections and stories from /radlibs!");
                Console.ResetColor();
                Console.Write("Type 'DELETE ALL' to confirm: ");
                var confirm = Console.ReadLine();
                if (confirm != "DELETE ALL")
                {
                    Console.WriteLine("Cancelled.");
                    return 0;
                }

                using var httpClient = CreateAuthorizedHttpClient(await GetAccessTokenAsync());
                var response = await httpClient.DeleteAsync($"{FIREBASE_URL}/radlibs.json");
                response.EnsureSuccessStatusCode();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("All collections deleted.");
                Console.ResetColor();
                await UpdateContentMetadataAsync(httpClient, incrementVersion: true);
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nFailed to clear database: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        static async Task<int> MigrateToCollectionSchemaAsync()
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("\nSchema Migration - Collection Structure + collection-level isPremium");
                Console.WriteLine("==============================================================");
                Console.ResetColor();

                using var httpClient = CreateAuthorizedHttpClient(await GetAccessTokenAsync());
                var response = await httpClient.GetAsync($"{FIREBASE_URL}/radlibs.json");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(json) || json == "null")
                {
                    Console.WriteLine("No data found under /radlibs.");
                    return 0;
                }

                var root = JsonNode.Parse(json) as JsonObject;
                if (root == null)
                {
                    Console.WriteLine("Unexpected /radlibs payload.");
                    return 1;
                }

                var transformed = TransformToCollectionSchema(root);
                var writeResponse = await httpClient.PutAsync($"{FIREBASE_URL}/radlibs.json", CreateJsonContent(transformed));
                writeResponse.EnsureSuccessStatusCode();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Migration completed. /radlibs is now collection-based with collection-level isPremium.");
                Console.ResetColor();

                await UpdateContentMetadataAsync(httpClient, incrementVersion: true);
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nMigration failed: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        static JsonObject TransformToCollectionSchema(JsonObject sourceRoot)
        {
            var target = new JsonObject();

            bool isAlreadyCollectionShape = sourceRoot.Any(kvp => kvp.Value is JsonObject obj && obj["stories"] is JsonObject);
            if (isAlreadyCollectionShape)
            {
                foreach (var collectionEntry in sourceRoot)
                {
                    if (collectionEntry.Value is not JsonObject collectionObj) continue;
                    var collectionName = collectionEntry.Key;
                    var storyMap = collectionObj["stories"] as JsonObject ?? new JsonObject();

                    var packId = collectionObj["packId"]?.GetValue<string>()
                        ?? storyMap.FirstOrDefault(s => s.Value?["PackId"] != null).Value?["PackId"]?.GetValue<string>()
                        ?? SlugifyPackId(collectionName);

                    var collectionPremium = collectionObj["isPremium"]?.GetValue<bool>()
                        ?? storyMap.Any(s => s.Value?["IsPremium"]?.GetValue<bool>() == true);

                    var creditCost = collectionObj["creditCost"]?.GetValue<int>() ?? 1;

                    var normalizedStories = new JsonObject();
                    var normalizedStoryList = new List<JsonObject>();
                    foreach (var storyEntry in storyMap)
                    {
                        if (storyEntry.Value is not JsonObject storyObj) continue;
                        var normalized = NormalizeStory(storyObj, packId);
                        normalizedStories[storyEntry.Key] = normalized;
                        normalizedStoryList.Add(normalized);
                    }

                    var tags = ExtractTagsFromNode(collectionObj["tags"]);
                    if (tags.Count == 0)
                    {
                        tags = BuildDefaultTags(collectionName, normalizedStoryList);
                    }

                    target[collectionName] = new JsonObject
                    {
                        ["creditCost"] = creditCost,
                        ["packId"] = packId,
                        ["isPremium"] = collectionPremium,
                        ["stories"] = normalizedStories,
                        ["tags"] = new JsonArray(tags.Select(t => (JsonNode?)t).ToArray())
                    };
                }

                return target;
            }

            var grouped = new Dictionary<string, CollectionPayloadBuilder>(StringComparer.OrdinalIgnoreCase);
            foreach (var storyEntry in sourceRoot)
            {
                if (storyEntry.Value is not JsonObject storyObj) continue;
                var category = storyObj["Category"]?.GetValue<string>()?.Trim();
                if (string.IsNullOrWhiteSpace(category))
                {
                    category = "Uncategorized";
                }

                if (!grouped.TryGetValue(category, out var group))
                {
                    group = new CollectionPayloadBuilder
                    {
                        Category = category,
                        PackId = storyObj["PackId"]?.GetValue<string>() ?? SlugifyPackId(category),
                        CreditCost = 1,
                        IsPremium = storyObj["IsPremium"]?.GetValue<bool>() ?? false
                    };
                    grouped[category] = group;
                }
                else if (storyObj["IsPremium"]?.GetValue<bool>() == true)
                {
                    group.IsPremium = true;
                }

                var normalized = NormalizeStory(storyObj, group.PackId);
                var storyId = normalized["Id"]?.GetValue<string>() ?? storyEntry.Key;
                group.Stories[storyId] = normalized;
                group.StoryList.Add(normalized);
            }

            foreach (var group in grouped.Values)
            {
                var tags = BuildDefaultTags(group.Category, group.StoryList);
                target[group.Category] = new JsonObject
                {
                    ["creditCost"] = group.CreditCost,
                    ["packId"] = group.PackId,
                    ["isPremium"] = group.IsPremium,
                    ["stories"] = group.Stories,
                    ["tags"] = new JsonArray(tags.Select(t => (JsonNode?)t).ToArray())
                };
            }

            return target;
        }

        static void ShowHelp()
        {
            Console.WriteLine("\nRadLibs Admin Tool - Usage");
            Console.WriteLine("===================================");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  upload <path>       Upload collection file or directory");
            Console.WriteLine("  auto                Upload all files from 'out' folder and archive them");
            Console.WriteLine("  watch               Continuously watch 'out' folder for new files");
            Console.WriteLine("  list                List all collections in database");
            Console.WriteLine("  delete <id>         Delete a specific story by ID");
            Console.WriteLine("  clear               Clear all collections (dangerous!)");
            Console.WriteLine("  migrate             Convert database to collection schema and move isPremium to collection level");
            Console.WriteLine("  version             Show current content version");
            Console.WriteLine("  version <ver>       Set content version (e.g., version 1.2.0)");
            Console.WriteLine("  help                Show this help message");
            Console.WriteLine();
            Console.WriteLine("Upload JSON supports:");
            Console.WriteLine("  - array of stories");
            Console.WriteLine("  - object with stories array");
            Console.WriteLine("  - object with stories map (export-like)");
            Console.WriteLine("Collection-level isPremium is written to /radlibs/<Collection>/isPremium.");
            Console.WriteLine();
        }

        static async Task UpdateContentMetadataAsync(HttpClient httpClient, bool incrementVersion)
        {
            try
            {
                var response = await httpClient.GetAsync($"{FIREBASE_URL}/radlibs.json");
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();

                if (string.IsNullOrWhiteSpace(json) || json == "null")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No stories found, skipping metadata update");
                    Console.ResetColor();
                    return;
                }

                var root = JsonNode.Parse(json) as JsonObject;
                if (root == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Unexpected /radlibs schema, skipping metadata update");
                    Console.ResetColor();
                    return;
                }

                var normalizedRoot = TransformToCollectionSchema(root);
                var totalCollections = normalizedRoot.Count;
                var totalStories = 0;
                foreach (var collection in normalizedRoot)
                {
                    if (collection.Value is JsonObject obj && obj["stories"] is JsonObject stories)
                    {
                        totalStories += stories.Count;
                    }
                }

                var currentVersion = "1.0.0";
                try
                {
                    var metadataResponse = await httpClient.GetAsync($"{FIREBASE_URL}/metadata.json");
                    if (metadataResponse.IsSuccessStatusCode)
                    {
                        var metadataJson = await metadataResponse.Content.ReadAsStringAsync();
                        if (!string.IsNullOrWhiteSpace(metadataJson) && metadataJson != "null")
                        {
                            var metadataNode = JsonNode.Parse(metadataJson) as JsonObject;
                            currentVersion = metadataNode?["ContentVersion"]?.GetValue<string>() ?? "1.0.0";
                        }
                    }
                }
                catch
                {
                }

                var newVersion = incrementVersion ? IncrementVersion(currentVersion) : currentVersion;

                var metadata = new JsonObject
                {
                    ["ContentVersion"] = newVersion,
                    ["LastUpdated"] = DateTime.UtcNow.ToString("o"),
                    ["TotalCollections"] = totalCollections,
                    ["TotalStories"] = totalStories,
                    ["ChangeLog"] = $"Content updated to version {newVersion}"
                };

                var updateResponse = await httpClient.PutAsync($"{FIREBASE_URL}/metadata.json", CreateJsonContent(metadata));
                updateResponse.EnsureSuccessStatusCode();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Metadata updated: v{newVersion} ({totalStories} stories, {totalCollections} collections)");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed to update metadata: {ex.Message}");
                Console.ResetColor();
            }
        }

        static string IncrementVersion(string currentVersion)
        {
            var parts = currentVersion.Split('.');
            if (parts.Length != 3) return "1.0.1";

            if (int.TryParse(parts[2], out var patch))
            {
                patch++;
                return $"{parts[0]}.{parts[1]}.{patch}";
            }

            return "1.0.1";
        }

        static async Task<int> ShowCurrentVersionAsync()
        {
            try
            {
                using var httpClient = CreateAuthorizedHttpClient(await GetAccessTokenAsync());
                var response = await httpClient.GetAsync($"{FIREBASE_URL}/metadata.json");

                if (!response.IsSuccessStatusCode || response.Content == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No metadata found. Upload stories to initialize version tracking.");
                    Console.ResetColor();
                    return 0;
                }

                var json = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(json) || json == "null")
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No metadata found. Upload stories to initialize version tracking.");
                    Console.ResetColor();
                    return 0;
                }

                var root = JsonNode.Parse(json) as JsonObject;
                if (root == null)
                {
                    Console.WriteLine("Metadata format invalid.");
                    return 1;
                }

                Console.WriteLine("\nCurrent Content Version:");
                Console.WriteLine("====================================");
                Console.WriteLine($"Version: {root["ContentVersion"]?.GetValue<string>()}");
                Console.WriteLine($"Last Updated: {root["LastUpdated"]?.GetValue<string>()}");
                Console.WriteLine($"Total Stories: {root["TotalStories"]?.GetValue<int>()}");
                Console.WriteLine($"Total Collections: {root["TotalCollections"]?.GetValue<int>()}");
                Console.WriteLine($"Change Log: {root["ChangeLog"]?.GetValue<string>()}");
                Console.WriteLine();
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nFailed to retrieve version: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        static async Task<int> SetContentVersionAsync(string version)
        {
            try
            {
                var parts = version.Split('.');
                if (parts.Length != 3 || !int.TryParse(parts[0], out _) || !int.TryParse(parts[1], out _) || !int.TryParse(parts[2], out _))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid version format. Use semantic versioning (e.g., 1.2.0)");
                    Console.ResetColor();
                    return 1;
                }

                using var httpClient = CreateAuthorizedHttpClient(await GetAccessTokenAsync());
                Console.WriteLine($"\nSetting content version to: {version}");
                await UpdateContentMetadataAsync(httpClient, incrementVersion: false);

                var versionResponse = await httpClient.PutAsync(
                    $"{FIREBASE_URL}/metadata/ContentVersion.json",
                    CreateJsonContent(version));
                versionResponse.EnsureSuccessStatusCode();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Version set to: {version}");
                Console.ResetColor();
                return 0;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nFailed to set version: {ex.Message}");
                Console.ResetColor();
                return 1;
            }
        }

        static HttpClient CreateAuthorizedHttpClient(string accessToken)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            return httpClient;
        }

        static StringContent CreateJsonContent(object value)
        {
            return new StringContent(JsonSerializer.Serialize(value), System.Text.Encoding.UTF8, "application/json");
        }

        static string SlugifyPackId(string category)
        {
            var safe = new string(category.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
            while (safe.Contains("__")) safe = safe.Replace("__", "_");
            return $"pack_{safe.Trim('_')}";
        }

        static List<string> ExtractTagsFromNode(JsonNode? node)
        {
            if (node is not JsonArray arr)
            {
                return new List<string>();
            }

            return arr
                .Select(x => x?.GetValue<string>()?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()!;
        }

        static List<string> BuildDefaultTags(string category, List<JsonObject> stories)
        {
            var tags = new List<string>();
            var categoryParts = category
                .ToLowerInvariant()
                .Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Length > 1)
                .ToList();
            tags.AddRange(categoryParts);

            foreach (var story in stories.Take(6))
            {
                var title = story["Title"]?.GetValue<string>() ?? string.Empty;
                foreach (var word in title
                    .ToLowerInvariant()
                    .Split(new[] { ' ', '-', '_', '\'', '"', ',', '.', ':', ';', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 3))
                {
                    tags.Add(word);
                }
            }

            return tags.Distinct(StringComparer.OrdinalIgnoreCase).Take(16).ToList();
        }

        static void MoveFilesToArchive(List<string> processedFiles)
        {
            Console.WriteLine("\nMoving processed files to archive...");
            var archiveDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "archived");
            Directory.CreateDirectory(archiveDir);

            foreach (var file in processedFiles)
            {
                try
                {
                    var fileName = Path.GetFileName(file);
                    var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                    var archiveName = $"{Path.GetFileNameWithoutExtension(fileName)}-{timestamp}{Path.GetExtension(fileName)}";
                    var archivePath = Path.Combine(archiveDir, archiveName);

                    File.Move(file, archivePath, overwrite: true);
                    Console.WriteLine($"  Archived: {archiveName}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"  Could not archive {Path.GetFileName(file)}: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
    }

    sealed record CollectionPayload(string Category, string PackId, bool IsPremium, int CreditCost, List<string> Tags, List<JsonObject> Stories);

    sealed class CollectionPayloadBuilder
    {
        public string Category { get; set; } = string.Empty;
        public string PackId { get; set; } = string.Empty;
        public bool IsPremium { get; set; }
        public int CreditCost { get; set; } = 1;
        public JsonObject Stories { get; } = new();
        public List<JsonObject> StoryList { get; } = new();
    }
}
