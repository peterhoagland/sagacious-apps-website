# RadLibs Admin Tool - Complete Guide

## Overview

Command-line tool for managing RadLibs Firebase database. Uses Firebase Admin SDK for secure, permanent authentication.

**Key Features:**
- Auto-upload from `out/` folder with archiving
- Story overwriting (same ID = update)
- Batch processing multiple files
- Watch mode for continuous monitoring
- Database management (list, delete, clear)

---

## Setup (One Time)

### 1. Install Dependencies
```powershell
cd C:\Users\peter\source\repos\RadLibsApp\RadLibsApp\Tools\RadLibsAdminTool
dotnet restore
dotnet build
```

### 2. Get Firebase Service Account
1. Go to: https://console.firebase.google.com/project/radlibsapp/settings/serviceaccounts
2. Click **"Generate new private key"**
3. Save as: `bin/Debug/net10.0/radlibsapp-service-account.json`

**?? Security:** Never commit this file to Git (already in .gitignore)

---

## Usage

### Auto-Upload Workflow (Recommended)

**Quick Upload:**
```powershell
# 1. Drop JSON files in out/ folder
# 2. Run:
dotnet run -- auto
# Files uploaded and moved to archived/
```

**Watch Mode (Continuous):**
```powershell
dotnet run -- watch
# Drop files in out/ anytime - auto-uploads
# Press Ctrl+C to stop
```

### Manual Commands

```powershell
# Upload specific file/folder (no archive)
dotnet run -- upload path/to/file.json
dotnet run -- upload stories/

# List all stories in database
dotnet run -- list

# Delete specific story
dotnet run -- delete story-id

# Clear all stories (dangerous!)
dotnet run -- clear

# Interactive menu
dotnet run

# Help
dotnet run -- help
```

---

## Folder Structure

```
RadLibsAdminTool/
??? out/              ? Place new JSON files here
??? archived/         ? Successfully uploaded files (timestamped)
??? stories/          ? Example templates
??? bin/Debug/net10.0/
    ??? radlibsapp-service-account.json  ? Firebase credentials
```

---

## Story Format

```json
[
  {
    "Id": "unique-story-id",
    "Title": "Story Title",
    "Category": "Funny",
    "Difficulty": "Easy",
    "IsPremium": false,
    "StoryPackId": null,
    "Tags": ["tag1", "tag2"],
    "PlaceholderCount": 5,
    "Placeholders": [
      {"Type": "Noun", "Position": 0},
      {"Type": "Verb", "Position": 1},
      {"Type": "Adjective", "Position": 2},
      {"Type": "Adverb", "Position": 3},
      {"Type": "Noun", "Position": 4}
    ],
    "StoryTemplate": "The [2] [0] decided to [1] [3] to find the [4].",
    "CreatedDate": "2026-02-26T16:45:00Z"
  }
]
```

**Important:**
- File can contain array of stories or single object
- `Id` must be unique (or will overwrite existing)
- `Position` in Placeholders starts at 0
- Use `[0]`, `[1]`, etc. in StoryTemplate

---

## How It Works

### Auto-Upload Flow
1. Scans `out/` for *.json files
2. Uploads each story by ID to Firebase: `PUT /radlibs/{Id}.json`
3. On success: moves file to `archived/` with timestamp
4. On failure: file stays in `out/` for retry

### Story Overwriting
- Same `Id` = overwrites existing story
- Different `Id` = creates new story
- Uses HTTP PUT, so overwrites are automatic

### Archiving
- Format: `filename-yyyyMMdd-HHmmss.json`
- Example: `bonus-set-3-20260226-164233.json`
- Only successful uploads are archived

---

## Common Workflows

### Add New Story Collection
```powershell
# Create bonus-set-3.json with 10 stories
# Drop in out/
dotnet run -- auto
# Result: 10 stories added, file archived
```

### Update Existing Story
```powershell
# Edit existing story, keep same Id
# Drop in out/
dotnet run -- auto
# Result: Story overwritten with updates
```

### Development Session
```powershell
# Start watch mode
dotnet run -- watch
# Create stories throughout the day
# Drop each in out/ when done
# Each auto-uploads immediately
```

### Batch Update
```powershell
# Fix typos in 20 stories
# Drop all 20 JSON files in out/
dotnet run -- auto
# All uploaded and archived in seconds
```

---

## Troubleshooting

**401 Unauthorized:**
- Service account JSON missing or invalid
- Check file location: `bin/Debug/net10.0/radlibsapp-service-account.json`

**No files found:**
- Check files are in `out/` folder
- Verify files have .json extension

**Upload failed:**
- Check JSON format is valid
- Verify Firebase rules allow writes
- Failed files stay in `out/` - fix and retry

**Archive not working:**
- Check write permissions on `archived/` folder
- Only successful uploads are archived

---

## Technical Details (For Maintenance)

### Authentication
```csharp
// Uses Firebase Admin SDK with scoped credentials
_credential = GoogleCredential.FromFile(serviceAccountPath)
    .CreateScoped("https://www.googleapis.com/auth/firebase.database", 
                  "https://www.googleapis.com/auth/userinfo.email");

// Adds Bearer token to all HTTP requests
var accessToken = await GetAccessTokenAsync();
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", accessToken);
```

### Firebase Endpoints
- Upload/Update: `PUT https://radlibsapp-default-rtdb.firebaseio.com/radlibs/{storyId}.json`
- List all: `GET https://radlibsapp-default-rtdb.firebaseio.com/radlibs.json`
- Delete: `DELETE https://radlibsapp-default-rtdb.firebaseio.com/radlibs/{storyId}.json`

### Dependencies
```xml
<PackageReference Include="FirebaseAdmin" Version="3.0.1" />
<PackageReference Include="System.Text.Json" Version="10.0.1" />
```

### Watch Mode Implementation
- Uses `FileSystemWatcher` on `out/` folder
- Filter: `*.json` files
- 500ms delay before processing (ensures file fully written)
- Async processing with Task.Run
- Duplicate detection via HashSet

---

## Quick Reference

### Daily Use
```powershell
cd C:\Users\peter\source\repos\RadLibsApp\RadLibsApp\Tools\RadLibsAdminTool
dotnet run -- auto        # Upload from out/, archive
dotnet run -- watch       # Continuous monitoring
dotnet run -- list        # Show database contents
```

### File Locations
- **Drop here:** `out/`
- **Backup here:** `archived/`
- **Examples:** `stories/example-bonus-set-3.json`
- **Credentials:** `bin/Debug/net10.0/radlibsapp-service-account.json`

### Story ID Rules
- Must be unique across database
- Format: lowercase with hyphens (e.g., `bonus1-1`, `fairytales-2`)
- Same ID = overwrite, Different ID = new story

---

## Security Notes

- Service account JSON has full database access
- Never commit to Git (protected by .gitignore)
- Regenerate key if compromised
- Store securely when not in use

---

**Tool Version:** 1.0  
**Last Updated:** February 2026  
**Firebase Project:** radlibsapp
