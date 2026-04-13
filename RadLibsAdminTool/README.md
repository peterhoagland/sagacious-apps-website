# RadLibs Admin Tool

Single-source documentation for creating collection JSON files and uploading them to Firebase.

## 1) Collection JSON Structure

Each upload file must be a collection object like this:

- `category` (string)
  - Must include a numeric suffix for series naming (example: `Peter Pan 1`, `Aladdin 1`).
- `packId` (string)
  - Stable identifier for the collection (example: `pack_peter_pan_1`).
- `isPremium` (boolean)
  - Collection-level premium flag.
- `creditCost` (number)
  - Cost metadata for the collection.
- `tags` (string[])
  - Collection-level tags used for collection/theme context.
- `stories` (array of 20 story objects)

### Story object fields

Each story must include:

- `Id` (string)
- `Title` (string)
- `Story` (string with placeholder tokens like `{adjective}`)
- `Placeholders` (string[])
- `CreatedDate` (ISO timestamp string)
- `IsFavorite` (boolean)
- `PackId` (string, should match collection `packId`)

### Content requirements

- Exactly **20 stories per collection**.
- Each story must contain **15–20 blanks**.
- `Placeholders` count must match placeholder tokens in `Story`.
- Story-level `IsPremium` is deprecated and should not be present.

## 2) Example Skeleton

```json
{
  "category": "Example Theme 1",
  "packId": "pack_example_theme_1",
  "isPremium": true,
  "creditCost": 1,
  "tags": ["example", "theme"],
  "stories": [
    {
      "Id": "example1-1",
      "Title": "Story Title",
      "Story": "A {adjective} story with {number} blanks...",
      "Placeholders": ["adjective", "number"],
      "CreatedDate": "2026-04-13T00:00:00.000Z",
      "IsFavorite": false,
      "PackId": "pack_example_theme_1"
    }
  ]
}
```

## 3) Firebase Target Shape

Uploader writes collection data to:

- `/radlibs/<category>/creditCost`
- `/radlibs/<category>/packId`
- `/radlibs/<category>/isPremium`
- `/radlibs/<category>/tags`
- `/radlibs/<category>/stories/<storyId>`

Uploads are keyed by `category`.

## 4) Admin Tool Commands

Run from repo root:

```bash
dotnet run --project RadLibsAdminTool/RadLibsAdminTool.csproj --configuration Release -- <command>
```

Common commands:

- `upload <file-or-directory>`: Upload one file or all JSON files in a folder.
- `list`: List collections in Firebase with metadata.
- `migrate`: Normalize DB schema to collection-based format, move premium to collection level, and ensure tags exist.
- `version` / `version <x.y.z>`: View or set content version metadata.

## 5) Upload Process (Repo Workflow)

GitHub workflow: `.github/workflows/upload-radlibs-collections.yml`

Behavior:

1. Trigger: commits touching `uploads/pending/**/*.json` (or manual run).
2. Discovers all pending JSON files.
3. Validates schema + content rules.
4. Uploads each file.
5. Moves successfully uploaded files to `uploads/processed`.
6. Commits the file move automatically.

## 6) Authoring Checklist

Before commit:

- [ ] Collection name includes number suffix (e.g., `Theme 1`).
- [ ] `packId` is correct and intentional.
- [ ] `tags` exists and is meaningful.
- [ ] 20 stories exactly.
- [ ] Every story has 15–20 blanks.
- [ ] Placeholders align with story tokens.
- [ ] Story-level `IsPremium` is not present.

That is the authoritative documentation for future collection creation and uploads.
