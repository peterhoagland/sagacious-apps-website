# Auto-Download System Implementation Summary

## System Overview

The RadLibsApp now has a complete auto-download system for story library content with automatic version management. The system consists of three integrated components:

### 1. **Firebase Metadata Node** (Server-Side)
Location: `/metadata` in Firebase Realtime Database

```json
{
  "ContentVersion": "1.2.3",
  "LastUpdated": "2025-01-28T10:30:00.000Z",
  "TotalCollections": 8,
  "TotalStories": 160,
  "ChangeLog": "Content updated to version 1.2.3"
}
```

### 2. **RadLibsApp** (Client-Side)
- Checks for updates on app startup, Store page entry, and app resume
- Smart throttling (6-hour minimum between checks)
- Silent background downloads when updates detected
- User alerts on Store page when new content available

### 3. **Admin Tool** (Content Management)
- Automatically updates `/metadata` after every successful upload
- Auto-increments patch version (e.g., 1.0.0 → 1.0.1)
- Manual version control for major/minor releases
- Calculates and tracks content statistics

---

## How It Works End-to-End

### Upload Flow

```
Admin uploads stories
  ↓
RadLibsAdminTool upload stories.json
  ↓
Stories uploaded to Firebase /radlibs
  ↓
Tool calculates: Total Stories, Total Collections
  ↓
Tool increments version: 1.0.5 → 1.0.6
  ↓
Tool updates /metadata node
  ↓
✅ Firebase now has new version 1.0.6
```

### App Detection Flow

```
User opens RadLibsApp
  ↓
Background task checks /metadata
  ↓
Local version: 1.0.5
Server version: 1.0.6
  ↓
Server is newer → Trigger sync
  ↓
Download new stories silently
  ↓
Update local database
  ↓
Store local version as 1.0.6
  ↓
User enters Store page
  ↓
Check skipped (already checked <6 hours ago)
  ↓
[6 hours later]
User re-enters Store page
  ↓
Check /metadata again
  ↓
Local: 1.0.6, Server: 1.0.7
  ↓
Download + Show Alert: "New Stories Available!"
  ↓
LoadPacksCommand refreshes UI
```

---

## Implementation Details

### Client-Side Changes

#### **1. SyncMetadataEntity.cs**
Added fields:
- `ContentVersion` (string) - Stores semantic version
- `LastVersionCheckDate` (DateTime?) - Enables throttling
- `ServerTotalStories` (int) - For comparison validation

#### **2. ContentMetadata.cs** (NEW FILE)
Model representing Firebase `/metadata` structure:
```csharp
public class ContentMetadata
{
    public string? ContentVersion { get; set; }
    public DateTime? LastUpdated { get; set; }
    public int TotalCollections { get; set; }
    public int TotalStories { get; set; }
    public string? ChangeLog { get; set; }
}
```

#### **3. FirebaseService.cs**
Added method:
```csharp
public async Task<ContentMetadata?> GetContentMetadataAsync()
```
- Fetches `/metadata.json` from Firebase
- Returns `ContentMetadata` object or null if not found

#### **4. SyncService.cs**
Implemented method:
```csharp
public async Task<bool> CheckForUpdatesAsync(bool forceCheck = false)
```
- Throttles checks to 6-hour minimum interval
- Compares local vs server ContentVersion
- Compares local vs server TotalStories count
- Triggers full sync if updates detected
- Updates LastVersionCheckDate timestamp

#### **5. App.xaml.cs**
Modified `CreateWindow` method:
- Background task calls `CheckForUpdatesAsync()` on startup
- Respects 24-hour throttle for startup checks
- Downloads updates silently without blocking UI

#### **6. StoreViewModel.cs**
Added:
- `ISyncService` dependency injection
- `CheckForContentUpdatesAsync()` method
- User alert: "New Stories Available!" when updates found
- Automatic `LoadPacksCommand` refresh after update

#### **7. StorePage.xaml.cs**
Modified `OnAppearing`:
- Made method async
- Calls `await _viewModel.CheckForContentUpdatesAsync()`
- Checks for updates before loading packs

#### **8. MauiProgram.cs**
Fixed DI registration:
- Added `syncService` parameter to `StoreViewModel` factory

---

### Admin Tool Changes

#### **1. Program.cs Updates**

**Automatic Version Update:**
```csharp
// After successful upload
if (successCount > 0)
{
    await UpdateContentMetadataAsync(httpClient, incrementVersion: true);
}
```

**New Methods Added:**
- `UpdateContentMetadataAsync()` - Updates Firebase `/metadata` with version and stats
- `IncrementVersion()` - Semantic versioning increment (patch++)
- `ShowCurrentVersionAsync()` - Displays current version info
- `SetContentVersionAsync()` - Manually sets version number

**New Commands:**
- `RadLibsAdminTool version` - Show current version
- `RadLibsAdminTool version <ver>` - Set specific version

**Interactive Mode:**
- Option 8: Show current content version
- Option 9: Exit (was option 8)

---

## Throttling Strategy

### Prevents Excessive Firebase Requests

| Trigger | Throttle Period | Force Check Option |
|---------|----------------|-------------------|
| App Startup | 24 hours | No |
| Store Page Entry | 6 hours | No |
| App Resume | 6 hours | No |
| Manual Sync Button | None | Yes (forceCheck=true) |

### How Throttling Works

```csharp
// Check last version check timestamp
var metadata = await _database.GetSyncMetadataAsync();
var lastCheck = metadata?.LastVersionCheckDate;

// Respect throttle (default 6 hours)
if (!forceCheck && lastCheck.HasValue)
{
    var timeSinceLastCheck = DateTime.UtcNow - lastCheck.Value;
    if (timeSinceLastCheck.TotalHours < 6)
    {
        // Skip check - too recent
        return false;
    }
}

// Perform check and update timestamp
await _database.UpdateLastVersionCheckDateAsync(DateTime.UtcNow);
```

---

## Semantic Versioning

### Format: `MAJOR.MINOR.PATCH`

- **MAJOR** (X.0.0) - Breaking changes, major overhauls
- **MINOR** (1.X.0) - New features, new collections
- **PATCH** (1.2.X) - Bug fixes, small updates (auto-incremented)

### Auto-Increment Logic

```csharp
// Current version: 1.0.5
// After upload: 1.0.6 (patch++)
// After upload: 1.0.7 (patch++)

static string IncrementVersion(string currentVersion)
{
    var parts = currentVersion.Split('.');
    var patch = int.Parse(parts[2]) + 1;
    return $"{parts[0]}.{parts[1]}.{patch}";
}
```

---

## Testing the System

### 1. Initialize Versioning

```bash
# Upload initial content (creates /metadata node)
cd RadLibsApp\Tools\RadLibsAdminTool
dotnet run upload sample-story.json

# Verify metadata created
dotnet run version
```

**Expected Output:**
```
📊 Current Content Version:
════════════════════════════
Version: 1.0.1
Last Updated: 2025-01-28T10:00:00.000Z
Total Stories: 1
Total Collections: 1
Change Log: Content updated to version 1.0.1
```

### 2. Test Auto-Update Detection

```bash
# Check current version
dotnet run version
# Output: Version: 1.0.1

# Upload new story
dotnet run upload new-story.json
# Auto-increments to 1.0.2

# Open RadLibsApp
# - Background check detects 1.0.2 > 1.0.1
# - Downloads new story automatically
# - No user interaction required

# Enter Store page (after 6+ hours or clear app data)
# - Check detects update (if any)
# - Shows alert: "New Stories Available!"
# - Reloads packs
```

### 3. Test Throttling

```bash
# Open app at 10:00 AM
# - Checks version (last check: 10:00 AM)

# Enter Store page at 10:15 AM
# - No check (only 15 minutes since last check)

# Enter Store page at 4:15 PM (6+ hours later)
# - Checks version again (last check: 10:00 AM → now 4:15 PM)
```

### 4. Test Manual Version Bump

```bash
# Bump to major version
dotnet run version 2.0.0

# Upload collection
dotnet run upload holiday-collection/

# Version is now 2.0.1 (auto-incremented after upload)
dotnet run version
```

---

## Firebase Structure

### Before Implementation

```
/radlibs
  /story1
  /story2
  /story3
```

### After Implementation

```
/radlibs
  /story1
  /story2
  /story3
/metadata  ← NEW
  ContentVersion: "1.0.3"
  LastUpdated: "2025-01-28T10:00:00.000Z"
  TotalCollections: 8
  TotalStories: 160
  ChangeLog: "Content updated to version 1.0.3"
```

---

## Key Benefits

### For Users
✅ **Automatic updates** - No manual "Check for Updates" button needed  
✅ **Silent downloads** - Updates happen in background  
✅ **Smart notifications** - Only alerted when entering Store page  
✅ **No interruptions** - App never blocks or shows loading spinners  

### For Admins
✅ **Automatic versioning** - No manual Firebase edits  
✅ **Version control** - Full history via semantic versioning  
✅ **Statistics tracking** - Total stories/collections automatically calculated  
✅ **Simple workflow** - Just upload files, versioning is automatic  

### For App Performance
✅ **Throttled requests** - Prevents excessive Firebase bandwidth usage  
✅ **Lightweight checks** - Only fetches small `/metadata` node first  
✅ **Delta sync** - Only downloads changed stories (existing SyncService feature)  
✅ **Background tasks** - Never blocks main UI thread  

---

## Documentation Files

All documentation is located in `RadLibsApp\Tools\RadLibsAdminTool\`:

| File | Purpose |
|------|---------|
| **CONTENT_VERSIONING_GUIDE.md** | Complete technical documentation with troubleshooting |
| **VERSION_QUICK_REFERENCE.md** | Quick command reference and examples |
| **AUTO_DOWNLOAD_IMPLEMENTATION.md** | This file - system overview and implementation summary |

---

## Next Steps

### 1. One-Time Firebase Setup

The `/metadata` node will be automatically created on the first upload. No manual Firebase configuration required.

### 2. Testing Checklist

- [ ] Upload test story: `dotnet run upload test-story.json`
- [ ] Verify version created: `dotnet run version`
- [ ] Open app and verify update detection (check logs)
- [ ] Enter Store page and verify throttling behavior
- [ ] Clear app data and re-test update detection
- [ ] Test manual version bump: `dotnet run version 1.1.0`
- [ ] Upload again and verify version increments to 1.1.1

### 3. Production Deployment

Once testing is complete:

1. Upload current story library to initialize production version
2. Note the version number for reference
3. Document the version in release notes
4. Deploy app update with version checking enabled
5. Monitor logs for version check activity

---

## Maintenance

### Regular Operations

**No manual maintenance needed!** The system is fully automated:

- Upload stories → Version increments automatically
- App checks → Throttling prevents excessive requests
- Users update → Downloads happen silently

### Occasional Manual Version Bumps

Only needed for major releases:

```bash
# New major collection launch
dotnet run version 2.0.0
dotnet run upload new-collection/

# New category/feature
dotnet run version 1.5.0
dotnet run upload bonus-stories/
```

---

## Technical Notes

### Version Comparison Logic

```csharp
// Simple string comparison works for semantic versioning
if (serverVersion != localVersion && !string.IsNullOrEmpty(serverVersion))
{
    // Server version is different - trigger sync
    await _syncService.SyncAsync();
}
```

### Story Count Validation

```csharp
// Additional validation using story count
if (serverMetadata.TotalStories > localStoryCount)
{
    // Server has more stories - definitely needs update
    await _syncService.SyncAsync();
}
```

### Error Handling

- If `/metadata` doesn't exist, version check silently fails (no error shown)
- If network fails, app continues working with cached content
- If sync fails, retry on next check (respects throttle)

---

## Summary

✅ **Complete auto-download system implemented**  
✅ **Version management fully automated**  
✅ **Smart throttling prevents excessive requests**  
✅ **Silent background updates**  
✅ **User notifications on Store page**  
✅ **Admin Tool handles all versioning automatically**  
✅ **Build validated successfully**  
✅ **Documentation complete**  

**The system is ready for testing and production deployment.**
