# 🔧 Migrate Stories - Add Missing Fields

## What This Does

The Admin Tool now has a `migrate` command that will:

1. ✅ Read ALL stories from your Firebase database
2. ✅ Check which ones are missing `IsPremium` or `PackId` fields
3. ✅ Add the correct fields based on story category
4. ✅ Update ONLY the stories that need fixing (doesn't touch stories that already have the fields)
5. ✅ Show you detailed progress and summary

## How to Run

### Option 1: Command Line (Quick)

```powershell
cd C:\Users\peter\source\repos\RadLibsApp\RadLibsApp\Tools\RadLibsAdminTool
dotnet run -- migrate
```

### Option 2: Interactive Menu

```powershell
cd C:\Users\peter\source\repos\RadLibsApp\RadLibsApp\Tools\RadLibsAdminTool
dotnet run
# Then select option 7
```

## Category to Pack Mappings

The migration uses these mappings:

| Category | Pack ID | IsPremium |
|----------|---------|-----------|
| Sample | sample_stories | false |
| Sports | pack_short_stories_1 | true |
| Fairy Tales | pack_short_stories_1 | true |
| Short Stories Set 1 | pack_short_stories_1 | true |
| Short Stories Set 2 | pack_short_stories_2 | true |
| Bonus Set 1 | pack_bonus_set_1 | false |
| Bonus Set 2 | pack_bonus_set_2 | false |
| Bonus Set 3 | pack_bonus_set_3 | false |
| Rapunzel | pack_rapunzel | true |

## Example Output

```
🔧 Story Migration - Add Missing IsPremium/PackId Fields
=========================================================

📥 Fetching all stories from Firebase...
✅ Found 85 stories

⏭️  Space Adventure - Already has fields
⏭️  Pizza Party - Already has fields

🔄 Updating: The Costume Party
   Category: Short Stories Set 2
   PackId: pack_short_stories_2
   IsPremium: True
   ✅ Updated successfully

🔄 Updating: The Magic Garden
   Category: Fairy Tales
   PackId: pack_short_stories_1
   IsPremium: True
   ✅ Updated successfully

...

=========================================================
✅ Migration Complete!

📊 Summary:
   Total stories: 85
   ✅ Updated: 80
   ⏭️  Skipped (already had fields): 5

🎉 Your stories now have pack information!

Next steps:
1. Clear app data on your device
2. Restart the app
3. Perform a sync
4. All collections should now appear!
```

## What Happens for Unknown Categories

If a story has a category that's not in the mapping list, the tool will:
- Show a warning
- Default to `sample_stories` pack (free)
- Continue processing
- You can manually update those stories later if needed

## Safety Features

- ✅ Only updates stories that are missing fields
- ✅ Skips stories that already have both fields
- ✅ Shows detailed progress for each story
- ✅ Uses your Firebase service account (secure)
- ✅ Adds small delay between updates to avoid overwhelming Firebase

## After Migration

1. **Verify in Firebase Console**:
   - Go to: https://console.firebase.google.com/project/radlibsapp/database
   - Navigate to `/radlibs`
   - Click on any story
   - Should now have `IsPremium` and `PackId` fields

2. **Test in App**:
   - Clear app data: Settings → Apps → RadLibs → Storage → Clear Data
   - Restart app
   - Perform sync
   - Check Story Library - all collections should appear!

## Troubleshooting

### "Migration failed" error
→ Check that your service account JSON file is in the correct location:  
`RadLibsApp\Tools\RadLibsAdminTool\bin\Debug\net10.0\radlibsapp-service-account.json`

### Some stories still missing after migration
→ Check the category name matches exactly (case-sensitive)  
→ Run `dotnet run -- list` to see all stories and their categories  
→ Add new category mappings if needed

### Stories updated but still not showing in app
→ Make sure you cleared app data  
→ Make sure you performed a sync  
→ Check that the PackId matches what's in `StoryPackDataService.cs`

## Quick Commands Reference

```powershell
# Run migration
dotnet run -- migrate

# List all stories to verify
dotnet run -- list

# Upload new stories
dotnet run -- auto

# Help
dotnet run -- help
```

---

**Ready to run? Execute this command:**

```powershell
cd C:\Users\peter\source\repos\RadLibsApp\RadLibsApp\Tools\RadLibsAdminTool
dotnet run -- migrate
```

This will fix all your existing stories! 🚀
