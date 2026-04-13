# RadLibs Admin Tool

Firebase story upload and content management tool for RadLibsApp.

## Overview

The RadLibs Admin Tool is a command-line utility for managing story content in Firebase Realtime Database. It handles secure uploads, automatic version management, and content statistics tracking.

## Features

✅ **Secure Firebase Admin SDK** - Server-side authentication with service account  
✅ **Automatic Version Management** - Semantic versioning with auto-increment  
✅ **Content Statistics** - Tracks total stories and collections  
✅ **Auto-Upload Workflow** - Batch upload from 'out' folder  
✅ **Watch Mode** - Continuous monitoring for new files  
✅ **Migration Tools** - Add missing fields to existing stories  
✅ **Interactive CLI** - Menu-driven interface for all operations  

## Quick Start

### 1. Setup Service Account

1. Go to [Firebase Console](https://console.firebase.google.com)
2. Select **RadLibsApp** project
3. Go to **Project Settings** → **Service Accounts**
4. Click **Generate New Private Key**
5. Save as `radlibsapp-service-account.json` in this tool's directory

### 2. Run the Tool

```bash
# Interactive mode
dotnet run

# Command mode
dotnet run upload story.json
dotnet run auto
dotnet run version
```

## Commands

### Upload Stories

```bash
# Upload single file
dotnet run upload story.json

# Upload directory
dotnet run upload ./stories/

# Auto-upload from 'out' folder (with archiving)
dotnet run auto
```

**Version Management:**  
✅ Automatically increments patch version (1.0.0 → 1.0.1)  
✅ Updates Firebase `/metadata` node with stats  

### Version Management

```bash
# Show current version
dotnet run version

# Set specific version
dotnet run version 2.0.0
```

**Semantic Versioning:**  
- **Major** (X.0.0) - Breaking changes, major overhauls  
- **Minor** (1.X.0) - New features, new collections  
- **Patch** (1.2.X) - Bug fixes, small updates (auto-incremented)  

See **VERSION_QUICK_REFERENCE.md** for detailed examples.

### Watch Mode

```bash
# Continuously watch 'out' folder for new files
dotnet run watch
```

**Behavior:**
- Monitors `out/` folder for new `.json` files
- Automatically uploads on file creation
- Auto-increments version on each upload
- Archives processed files to `archived/` folder
- Press Ctrl+C to stop watching

### Database Operations

```bash
# List all stories
dotnet run list

# Delete specific story
dotnet run delete story-id

# Clear all stories (⚠️ Dangerous!)
dotnet run clear

# Migrate - Add missing IsPremium/PackId fields
dotnet run migrate
```

### Help

```bash
dotnet run help
```

## Documentation

| File | Description |
|------|-------------|
| **CONTENT_VERSIONING_GUIDE.md** | Complete versioning documentation with technical details |
| **VERSION_QUICK_REFERENCE.md** | Quick command reference and workflow examples |
| **AUTO_DOWNLOAD_IMPLEMENTATION.md** | System overview and implementation summary |
| **AUTO_UPLOAD_GUIDE.md** | Auto-upload workflow documentation |
| **MIGRATION_GUIDE.md** | Database migration instructions |

## Workflows

### Standard Upload Workflow

```bash
# 1. Place story files in 'out' folder
cp new-stories/*.json out/

# 2. Run auto-upload
dotnet run auto

# 3. Stories uploaded, version incremented, files archived
```

**Result:**
- Stories uploaded to Firebase `/radlibs`
- Version incremented (e.g., 1.0.5 → 1.0.6)
- Metadata updated in `/metadata` node
- Files moved to `archived/` folder with timestamps

### Continuous Upload (Watch Mode)

```bash
# Start watch mode
dotnet run watch

# Drop files into 'out' folder as needed
# Tool automatically uploads, versions, and archives

# Press Ctrl+C when done
```

### Major Release Workflow

```bash
# 1. Set major version
dotnet run version 2.0.0

# 2. Upload new collection
dotnet run upload holiday-collection/

# 3. Version is now 2.0.1 (auto-incremented after upload)
dotnet run version
```

## Interactive Mode

Run without arguments for menu-driven interface:

```bash
dotnet run
```

**Menu Options:**
1. Upload story file or directory
2. Auto-upload from 'out' folder
3. Watch 'out' folder (continuous)
4. List all stories in database
5. Delete a specific story
6. Clear all stories (⚠️ Dangerous!)
7. Migrate stories - Add missing IsPremium/PackId fields
8. Show current content version
9. Exit

## Firebase Structure

### Stories Node
```
/radlibs
  /story-id-1
    Id: "story-id-1"
    Title: "Adventure Story"
    Category: "Sports"
    PackId: "pack_short_stories_1"
    IsPremium: true
    Prompts: [...]
    ...
  /story-id-2
  /story-id-3
```

### Metadata Node (Auto-Updated)
```
/metadata
  ContentVersion: "1.2.3"
  LastUpdated: "2025-01-28T10:00:00.000Z"
  TotalCollections: 8
  TotalStories: 160
  ChangeLog: "Content updated to version 1.2.3"
```

## Version Management Integration

The Admin Tool integrates with RadLibsApp's auto-download system:

### Upload Flow
```
Admin uploads stories
  ↓
Tool uploads to Firebase /radlibs
  ↓
Tool calculates statistics
  ↓
Tool increments version (1.0.5 → 1.0.6)
  ↓
Tool updates /metadata node
  ↓
✅ Version 1.0.6 available to apps
```

### App Detection Flow
```
User opens RadLibsApp
  ↓
App checks Firebase /metadata
  ↓
Server version 1.0.6 > Local version 1.0.5
  ↓
App downloads updates silently
  ↓
User sees "New Stories Available!" (Store page)
```

**Throttling:** App checks are throttled to 6-hour intervals to prevent excessive Firebase requests.

## Troubleshooting

### "Service account file not found"
**Problem:** `radlibsapp-service-account.json` missing  
**Solution:** Download from Firebase Console (see Setup section above)

### "No metadata found"
**Problem:** First-time use, `/metadata` node doesn't exist  
**Solution:** Upload any story to initialize:
```bash
dotnet run upload sample-story.json
```

### Upload fails with authentication error
**Problem:** Service account credentials invalid or expired  
**Solution:** Re-download service account JSON from Firebase Console

### Version not incrementing
**Problem:** Upload had errors (failCount > 0)  
**Solution:** Fix story JSON errors and re-upload. Version only updates on successful uploads.

## Story JSON Format

Stories must follow this structure:

```json
{
  "Id": "unique-story-id",
  "Title": "Story Title",
  "Category": "Sports",
  "PackId": "pack_short_stories_1",
  "IsPremium": true,
  "Prompts": [
    {
      "Order": 1,
      "Text": "a place",
      "PartOfSpeech": "Noun",
      "IsPlural": false
    }
  ],
  "Template": "Once upon a time, there was {0}..."
}
```

**Required Fields:**
- `Id` - Unique identifier (lowercase with hyphens)
- `Title` - Story display name
- `Category` - Collection name
- `PackId` - Product ID for billing (e.g., `sample_stories`, `pack_short_stories_1`)
- `IsPremium` - `true` for paid packs, `false` for free
- `Prompts` - Array of word prompts with `Order`, `Text`, `PartOfSpeech`
- `Template` - Story text with `{0}`, `{1}`, etc. placeholders

## Migration Command

If stories are missing `PackId` or `IsPremium` fields:

```bash
dotnet run migrate
```

**What it does:**
- Fetches all stories from Firebase
- Checks which ones are missing fields
- Adds fields based on category mappings
- Updates only stories that need fixing

**Category Mappings:**
```
Sample → sample_stories (free)
Sports → pack_short_stories_1 (premium)
Fairy Tales → pack_short_stories_1 (premium)
Bonus Set 1 → pack_bonus_set_1 (free)
Rapunzel → pack_rapunzel (premium)
```

## Best Practices

### ✅ Do's

- Use `auto` command for regular uploads (automatic versioning)
- Use `watch` mode for continuous development
- Verify version after upload: `dotnet run version`
- Keep service account JSON file secure (never commit to git)
- Test story JSON format before uploading

### ❌ Don'ts

- Don't manually edit Firebase `/metadata` node (tool handles it)
- Don't commit service account JSON to version control
- Don't use `clear` command in production (dangerous!)
- Don't bypass version management (always use tool for uploads)

## Security Notes

### Service Account Security

The `radlibsapp-service-account.json` file contains admin credentials:

⚠️ **Never commit this file to version control**  
⚠️ **Keep it secure on your local machine**  
⚠️ **Regenerate if compromised**  

### Firebase Admin SDK

The tool uses Firebase Admin SDK with full database access. This bypasses all Firebase security rules, so use responsibly.

## Examples

### Example 1: Upload Single Story
```bash
dotnet run upload bonus-story-1.json
```
**Output:**
```
📦 Found 1 story file(s)
Uploading bonus-story-1.json... ✅ (1 stories)

════════════════════════════
✅ Upload Complete!
   Successful: 1 stories

📊 Updating content version metadata...
✅ Metadata updated: v1.0.6 (161 stories, 8 collections)
```

### Example 2: Auto-Upload Batch
```bash
# Place files in out/ folder
cp *.json out/

# Run auto-upload
dotnet run auto
```
**Output:**
```
📂 Scanning 'out' folder: /path/to/out

📦 Found 3 story file(s)
Uploading story1.json... ✅ (5 stories)
Uploading story2.json... ✅ (5 stories)
Uploading story3.json... ✅ (5 stories)

📁 Moving processed files to archive...
  ✓ Archived: story1-20250128-103045.json
  ✓ Archived: story2-20250128-103046.json
  ✓ Archived: story3-20250128-103047.json

════════════════════════════
✅ Upload Complete!
   Successful: 15 stories
   Archived: 3 files

📊 Updating content version metadata...
✅ Metadata updated: v1.0.7 (175 stories, 9 collections)
```

### Example 3: Check Version
```bash
dotnet run version
```
**Output:**
```
📊 Current Content Version:
════════════════════════════
Version: 1.0.7
Last Updated: 2025-01-28T10:30:45.000Z
Total Stories: 175
Total Collections: 9
Change Log: Content updated to version 1.0.7
```

### Example 4: Major Release
```bash
# Set major version for new collection launch
dotnet run version 2.0.0

# Upload collection
dotnet run upload holiday-collection/

# Check final version
dotnet run version
# Output: Version: 2.0.1 (patch auto-incremented)
```

## System Requirements

- .NET 8.0 SDK or later
- Firebase Admin SDK NuGet packages
- Service account JSON file from Firebase Console

## Project Structure

```
RadLibsAdminTool/
├── Program.cs                          # Main tool code
├── RadLibsAdminTool.csproj             # Project file
├── radlibsapp-service-account.json     # Firebase credentials (DO NOT COMMIT)
├── out/                                # Drop story files here for auto-upload
├── archived/                           # Auto-created, stores uploaded files
├── CONTENT_VERSIONING_GUIDE.md         # Complete versioning documentation
├── VERSION_QUICK_REFERENCE.md          # Quick command reference
├── AUTO_DOWNLOAD_IMPLEMENTATION.md     # System implementation summary
├── AUTO_UPLOAD_GUIDE.md                # Auto-upload workflow docs
├── MIGRATION_GUIDE.md                  # Migration instructions
└── README.md                           # This file
```

## Related Documentation

- **RadLibsApp** - Main app project (uses version checking)
- **DEVELOPER_REFERENCE.md** - Complete app technical reference
- **BUILD_CONFIGURATION_GUIDE.md** - Build configurations and deployment

## Support

For issues or questions:
1. Check documentation files in this folder
2. Review Firebase Console for database state
3. Check Admin Tool logs for error details
4. Verify service account credentials are valid

---

**Admin Tool Version:** 1.0  
**Last Updated:** January 2025  
**Firebase Project:** radlibsapp  
**Database:** Firebase Realtime Database
