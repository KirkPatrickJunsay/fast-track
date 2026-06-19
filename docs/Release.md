# Fast Track — Release build & upload

Steps to produce a signed AAB for the Play Console.

## 1. One-time keystore generation

Generate an **upload keystore**. With Play App Signing, Google holds the actual app-signing key in the cloud; we only manage this upload key. If it ever leaks or is lost, Google can issue a reset.

```bash
keytool -genkey -v \
  -keystore fasttrack-upload.keystore \
  -alias fasttrack-upload \
  -keyalg RSA \
  -keysize 2048 \
  -validity 36500 \
  -storepass "<STORE_PASSWORD>" \
  -keypass "<KEY_PASSWORD>" \
  -dname "CN=Codes & Chips, OU=Fast Track, O=Codes and Chips, L=Manila, ST=NCR, C=PH"
```

- Store the keystore file outside the repo (1Password vault item, encrypted backup).
- Both passwords go into 1Password too. The repo's `.gitignore` blocks `*.keystore` and `signing.local.props` so even an accidental drop won't end up on GitHub.

## 2. signing.local.props (gitignored)

Create `signing.local.props` at the repo root:

```xml
<Project>
  <PropertyGroup>
    <AndroidKeyStore>true</AndroidKeyStore>
    <AndroidSigningKeyStore>/absolute/path/to/fasttrack-upload.keystore</AndroidSigningKeyStore>
    <AndroidSigningStorePass><STORE_PASSWORD></AndroidSigningStorePass>
    <AndroidSigningKeyAlias>fasttrack-upload</AndroidSigningKeyAlias>
    <AndroidSigningKeyPass><KEY_PASSWORD></AndroidSigningKeyPass>
  </PropertyGroup>
</Project>
```

`FastTrack.csproj` auto-imports it for Release builds.

## 3. Bump versions before each upload

In `FastTrack.csproj`:

- `ApplicationDisplayVersion` — user-facing SemVer (`1.0.0` → `1.0.1` → `1.1.0`).
- `ApplicationVersion` — monotonic integer. Convention: `yyMMdd0` (e.g. `2605190` for 2026-05-19 build #0). Play Store rejects re-used or lower numbers.

## 4. Build the signed AAB

```bash
dotnet publish FastTrack.csproj \
  -f net9.0-android \
  -c Release \
  -p:AndroidPackageFormat=aab
```

Output lands at:
`bin/Release/net9.0-android/publish/com.codesandchips.fasttrack-Signed.aab`

## 5. Smoke-test the Release build before uploading

Release builds run the linker + AOT — behaviour can diverge from Debug. Install on a real device via `bundletool`:

```bash
bundletool build-apks --bundle=publish/com.codesandchips.fasttrack-Signed.aab \
                      --output=fasttrack.apks --mode=universal
bundletool install-apks --apks=fasttrack.apks --device-id=<tablet-serial>
```

Verify:
- Cold launch reaches Home (BootPage routes correctly).
- Stages roadmap renders SVGs (SkiaSharp wasn't shrunk away).
- A test fast can be started / ended (SQLite reflection survived R8).
- Notification fires (Plugin.LocalNotification BroadcastReceivers were kept).

## 6. Upload to Play Console

Internal testing → Closed → Open → Production. Each track lets you iterate without affecting the public release.
