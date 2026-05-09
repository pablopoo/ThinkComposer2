# ThinkComposer WinUI Release

This is the supported release path for the WinUI migration branch.

## Portable Release

```powershell
powershell -ExecutionPolicy Bypass -File scripts\release-winui.ps1 `
  -Configuration Release `
  -RuntimeIdentifiers win-x86,win-x64 `
  -UpdateChannel stable `
  -UpdateBaseUrl https://downloads.example.com/thinkcomposer
```

The script publishes each runtime, creates portable zip artifacts, and writes:

- `artifacts\winui\release-index.json`
- one `release-manifest.json` per runtime
- one `artifact-hashes.json` per runtime directory
- SHA-256 hashes for every primary zip artifact and every file in each runtime directory

Use `-NoZip` only for local validation or staging directories. In that mode, the release index points `primaryArtifact` at the full runtime directory with `primaryArtifactType: "directory"` and validates it through `artifact-hashes.json`; it does not publish a standalone exe as a distributable artifact.

## Signed Release

Use a PFX certificate outside the repository. Do not commit certificates or passwords.

```powershell
$env:THINKCOMPOSER_SIGNING_PASSWORD = "<pfx password>"

powershell -ExecutionPolicy Bypass -File scripts\release-winui.ps1 `
  -Configuration Release `
  -RuntimeIdentifiers win-x86,win-x64 `
  -CertificatePath C:\certs\ThinkComposer.pfx `
  -UpdateChannel stable `
  -UpdateBaseUrl https://downloads.example.com/thinkcomposer

powershell -ExecutionPolicy Bypass -File scripts\verify-winui-release.ps1 `
  -IndexPath artifacts\winui\release-index.json `
  -RequireSigned
```

Signing uses `signtool.exe` from PATH or the Windows SDK. Pass `-SignToolPath` if it is installed elsewhere.

## Verification

```powershell
powershell -ExecutionPolicy Bypass -File scripts\verify-winui-release.ps1 `
  -IndexPath artifacts\winui\release-index.json
```

The verifier checks the product id, update channel, artifact existence, primary artifact SHA-256 hashes, and directory hash manifests. With `-RequireSigned`, it also verifies Authenticode signatures for signed artifacts.
