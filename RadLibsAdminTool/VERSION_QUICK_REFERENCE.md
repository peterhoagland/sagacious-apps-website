# Version Management Quick Reference

## Commands

### View Current Version
```bash
RadLibsAdminTool version
```
**Output:**
- Current content version (e.g., 1.2.3)
- Last updated timestamp
- Total stories and collections
- Change log message

---

### Set Specific Version
```bash
RadLibsAdminTool version <version>
```

**Examples:**
```bash
# Major release (breaking changes)
RadLibsAdminTool version 2.0.0

# Minor release (new features/collections)
RadLibsAdminTool version 1.5.0

# Patch release (bug fixes/small updates)
RadLibsAdminTool version 1.2.7
```

---

### Automatic Version Increment

All upload commands automatically increment the **patch** version:

```bash
# Upload single file - auto-increments version
RadLibsAdminTool upload story.json

# Upload from 'out' folder - auto-increments version
RadLibsAdminTool auto

# Watch mode - auto-increments on each upload
RadLibsAdminTool watch
```

**Version Progression:**
- Before upload: `1.0.0`
- After first upload: `1.0.1`
- After second upload: `1.0.2`
- After third upload: `1.0.3`

---

## When to Use Manual vs Automatic

### ✅ Use Automatic (Default)
- Regular content updates
- Adding new stories to existing collections
- Bug fixes to story content
- Testing/development uploads

**Action:** Just run upload command, version increments automatically

---

### 🎯 Use Manual Version Setting

#### Major Version Bump (X.0.0)
- Complete content overhaul
- Breaking changes to data structure
- New app version requirement

```bash
RadLibsAdminTool version 2.0.0
```

#### Minor Version Bump (1.X.0)
- New story collection/category
- New premium pack launch
- Significant content expansion

```bash
RadLibsAdminTool version 1.3.0
```

#### Patch Version (Manual)
- Rarely needed (auto-increment handles this)
- Use when you need specific version number

```bash
RadLibsAdminTool version 1.2.10
```

---

## Workflow Examples

### Example 1: Regular Content Update
```bash
# Place stories in 'out' folder
# Run auto-upload (version increments automatically)
RadLibsAdminTool auto

# Verify version updated
RadLibsAdminTool version
```

**Expected Output:**
```
📊 Current Content Version:
════════════════════════════
Version: 1.0.1  ← Auto-incremented
Last Updated: 2025-01-28T12:00:00.000Z
Total Stories: 165
Total Collections: 8
Change Log: Content updated to version 1.0.1
```

---

### Example 2: Major Collection Launch
```bash
# Set major version before upload
RadLibsAdminTool version 2.0.0

# Upload new collection
RadLibsAdminTool upload holiday-collection/

# Version is now 2.0.1 (auto-incremented patch after upload)
RadLibsAdminTool version
```

**Expected Output:**
```
📊 Current Content Version:
════════════════════════════
Version: 2.0.1  ← Patch incremented after upload
Last Updated: 2025-01-28T14:30:00.000Z
Total Stories: 185
Total Collections: 9
Change Log: Content updated to version 2.0.1
```

---

### Example 3: Testing Version Detection
```bash
# Check current version
RadLibsAdminTool version
# Output: Version: 1.2.5

# Upload test story
RadLibsAdminTool upload test-story.json

# Verify version incremented
RadLibsAdminTool version
# Output: Version: 1.2.6

# Open app (after throttle period)
# App detects 1.2.6 > 1.2.5 and downloads updates
```

---

## Version Format

### Semantic Versioning: `MAJOR.MINOR.PATCH`

- **MAJOR** (1.x.x) - Breaking changes, major overhauls
- **MINOR** (x.1.x) - New features, new collections
- **PATCH** (x.x.1) - Bug fixes, small updates (auto-incremented)

### Valid Examples
✅ `1.0.0`  
✅ `1.5.3`  
✅ `2.10.42`  

### Invalid Examples
❌ `1.0` (missing patch)  
❌ `v1.2.3` (no 'v' prefix)  
❌ `1.2.3-beta` (no suffixes)  

---

## App-Side Integration

When you update content versions, the app automatically:

1. **Checks for updates** on startup (throttled to 24 hours)
2. **Checks on Store page** entry (respects 6-hour throttle)
3. **Compares versions** (server vs local)
4. **Downloads updates** silently in background if newer version found
5. **Shows alert** "New Stories Available!" (Store page only)

### Throttling Behavior

```
App opened at 10:00 AM
  ↓
Version check: 1.0.5 (local) vs 1.0.6 (server)
  ↓
Download updates automatically
  ↓
Store page opened at 10:15 AM
  ↓
No check (throttled - only 15 minutes since last check)
  ↓
Store page opened at 4:30 PM (6+ hours later)
  ↓
Version check: 1.0.6 (local) vs 1.0.7 (server)
  ↓
Download updates + show alert
```

---

## Quick Troubleshooting

### "No metadata found"
**Problem:** Firebase `/metadata` node doesn't exist  
**Solution:** Upload any story to initialize versioning

```bash
RadLibsAdminTool upload sample-story.json
```

---

### App not detecting updates
**Problem:** Throttling or version not incremented  
**Check:**
1. Verify version incremented: `RadLibsAdminTool version`
2. Check Firebase Console → `/metadata` node exists
3. Wait for throttle period (6 hours)
4. Or clear app data to reset throttle

---

### Version shows old number
**Problem:** Upload failed (errors present)  
**Solution:** Version only updates on **successful** uploads (successCount > 0)

```bash
# Fix JSON errors in story files
# Re-run upload
RadLibsAdminTool auto
```

---

## Interactive Mode

In interactive mode, choose option **8** to view current version:

```
📋 What would you like to do?
  1. Upload story file or directory
  2. Auto-upload from 'out' folder
  3. Watch 'out' folder (continuous)
  4. List all stories in database
  5. Delete a specific story
  6. Clear all stories (⚠️ Dangerous!)
  7. Migrate stories - Add missing IsPremium/PackId fields
  8. Show current content version  ← NEW
  9. Exit

Choice (1-9): 8
```

---

## See Also

- **CONTENT_VERSIONING_GUIDE.md** - Complete documentation with technical details
- **AUTO_UPLOAD_GUIDE.md** - Auto-upload workflow and watch mode
- **MIGRATION_GUIDE.md** - Database migration commands

---

**Remember:** Version management is automatic. Manual versioning is only needed for major releases or specific version control needs.
