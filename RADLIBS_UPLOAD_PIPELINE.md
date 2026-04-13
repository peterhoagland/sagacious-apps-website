# Rad Libs Collection Upload Pipeline

This repository contains the upload pipeline for publishing new Rad Libs story collections to Firebase.

## What is automated

The workflow file `.github/workflows/upload-radlibs-collections.yml` runs when:
- JSON files are pushed under `uploads/pending/**/*.json`
- the workflow is started manually (`workflow_dispatch`)

For each run, the workflow:
1. Builds `RadLibsAdminTool`
2. Discovers target JSON files in `uploads/pending`
3. Validates JSON format and required fields (`Id`, `Title`, `Category`, `Story`, `Placeholders`)
4. Materializes Firebase credentials from a GitHub secret at runtime
5. Uploads each file via `RadLibsAdminTool upload <file>`

## Required GitHub secret

Create this repository secret (already configured):
- `FIREBASE_SERVICE_ACCOUNT_JSON`

Value requirements:
- Paste the full Firebase service account JSON (entire object from `{` to `}`)
- Do **not** store this JSON file in source control

Runtime behavior:
- The workflow writes this secret to `RadLibsAdminTool/bin/Release/net10.0/radlibsapp-service-account.json` during the job
- The file exists only in the Actions runner environment

## Operational usage

1. Add new collection `.json` files to `uploads/pending/`
2. Create a PR and merge to `main` (or push directly if allowed)
3. Wait for the workflow run to complete
4. Verify the app syncs and the new collection appears

## Manual run option

You can run the workflow manually from GitHub Actions.
- Manual runs will process all `.json` files currently in `uploads/pending/`

## Notes

- Keep upload files scoped to `uploads/pending/`
- If validation fails, the workflow stops before upload
- If an upload fails for a file, the workflow fails and reports the file-level error
