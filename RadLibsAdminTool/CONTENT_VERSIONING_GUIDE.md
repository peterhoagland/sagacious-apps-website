# Content Versioning Guide

## Overview

The RadLibs Admin Tool automatically manages content versioning in Firebase to enable the app's auto-download system. Every time you upload stories, the tool updates the `/metadata` node in Firebase with version information and content statistics.

## How It Works

### Automatic Version Updates

When you upload stories using any of these commands:
- `RadLibsAdminTool upload <path>`
- `RadLibsAdminTool auto`
- `RadLibsAdminTool watch`

The tool automatically:
1. ✅ Uploads all stories to Firebase
2. ✅ Calculates content statistics (total stories, total collections)
3. ✅ Increments the content version (patch number)
4. ✅ Updates the `/metadata` node in Firebase

### Semantic Versioning

Content versions follow semantic versioning format: **Major.Minor.Patch** (e.g., `1.2.3`)

- **Major**: Breaking changes (manually set when needed)
- **Minor**: New features/collections (manually set when needed)
- **Patch**: Auto-incremented on every upload

By default, the tool increments the **patch** number automatically:
- `1.0.0` → `1.0.1` → `1.0.2` → etc.

## Firebase Metadata Structure

The `/metadata` node in Firebase contains:

```json
{
  "ContentVersion": "1.2.3",
  "LastUpdated": "2025-01-28T10:30:00.000Z",
  "TotalCollections": 8,
  "TotalStories": 160,
  "ChangeLog": "Content updated to version 1.2.3"
}
```

## App-Side Version Checking

The RadLibsApp checks for version updates:

1. **App Startup** (throttled to once per 24 hours)
2. **Store Page Entry** (respects 6-hour throttle if app just started)
3. **App Resume** (throttled to once per 6 hours)

### Throttling Logic

- Version checks are throttled to prevent excessive Firebase requests
- If app just checked on startup, Store page won't check again for 6 hours
- All checks respect a 6-hour minimum interval between checks

### Update Flow

```
App checks Firebase /metadata
 ↓
Compares local ContentVersion with server
 ↓
If server version is newer:
  ↓
  Triggers automatic sync
  ↓
  Downloads new stories silently
  ↓
  User sees "New Stories Available!" alert (Store page only)
```

## Manual Version Commands

### View Current Version

```bash
RadLibsAdminTool version
```

Shows current content version, last updated timestamp, and statistics.

### Set Specific Version

```bash
RadLibsAdminTool version 2.0.0
```

Manually sets the content version (useful for major/minor bumps).

**Example Use Cases:**
- New major collection release: `version 2.0.0`
- New feature/category: `version 1.3.0`
- Reset after testing: `version 1.0.0`

## Best Practices

### 1. Let Automatic Versioning Handle Updates

For most uploads, let the tool auto-increment:
```bash
RadLibsAdminTool auto
# Automatically increments patch: 1.0.0 → 1.0.1
```

### 2. Use Manual Versioning for Milestones

For significant releases, manually bump major/minor:
```bash
# New major collection launch
RadLibsAdminTool version 2.0.0
RadLibsAdminTool upload new-collection/

# New category/feature
RadLibsAdminTool version 1.5.0
RadLibsAdminTool upload bonus-stories/
```

### 3. Verify Version After Upload

Check that versioning worked correctly:
```bash
RadLibsAdminTool version
```

Expected output:
```
📊 Current Content Version:
════════════════════════════
Version: 1.2.3
Last Updated: 2025-01-28T10:30:00.000Z
Total Stories: 160
Total Collections: 8
Change Log: Content updated to version 1.2.3
```

### 4. Testing Version Updates

When testing the auto-download system:

1. Note current version: `RadLibsAdminTool version`
2. Upload test stories: `RadLibsAdminTool upload test-story.json`
3. Verify version incremented: `RadLibsAdminTool version`
4. Open app and check that update is detected (wait for throttle period to expire if recently checked)

## Troubleshooting

### "No metadata found" Message

If `RadLibsAdminTool version` shows no metadata:

**Cause**: Firebase `/metadata` node doesn't exist yet  
**Solution**: Upload any story to initialize:
```bash
RadLibsAdminTool upload sample-story.json
```

### App Not Detecting Updates

If the app doesn't show "New Stories Available!":

1. **Check throttling**: Has it been 6 hours since last check?
2. **Verify version**: Run `RadLibsAdminTool version` to confirm version was incremented
3. **Check Firebase**: View Firebase Console → Realtime Database → `/metadata` node
4. **Force check**: Clear app data and reopen (clears throttle timestamps)

### Version Not Incrementing

If uploads don't increment version:

**Cause**: Upload had errors (failCount > 0)  
**Solution**: Fix story JSON errors and re-upload. Version only updates on successful uploads.

## Advanced: Firebase Security Rules

The `/metadata` node requires authentication:

```json
{
  "rules": {
    "metadata": {
      ".read": "auth != null",
      ".write": "auth != null"
    }
  }
}
```

The Admin Tool uses Firebase Admin SDK with service account credentials, so it has full write access regardless of security rules.

## Integration with Auto-Upload Workflow

The version system integrates seamlessly with the auto-upload workflow:

```bash
# Start watch mode (continuous monitoring)
RadLibsAdminTool watch

# Drop story files into 'out' folder
# Tool automatically:
#   1. Uploads stories
#   2. Increments version
#   3. Updates metadata
#   4. Archives files

# App users receive updates on next check
```

## Summary

✅ **Automatic**: Version increments on every successful upload  
✅ **Manual Control**: Use `version <ver>` for major/minor bumps  
✅ **App Integration**: Auto-download system uses metadata for update detection  
✅ **Throttling**: Smart polling prevents excessive Firebase requests  
✅ **Firebase Structure**: `/metadata` node contains all version info  

**No manual Firebase edits needed** – the Admin Tool handles everything automatically.
