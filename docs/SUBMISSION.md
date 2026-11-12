# Submitting Sysight to the Microsoft Store

Steps marked **(you)** need your own account and identity and can't be automated.

## 1. Create a developer account (you)
1. Go to https://storedeveloper.microsoft.com and choose **Individual developer**.
2. Sign in with a Microsoft account and verify your identity (a government ID and selfie check is typical).
3. Check the fee shown at sign-up. Individual registration became free in 2025.

## 2. Reserve the name (you)
Partner Center → **Apps and games** → **New product** → **MSIX or PWA app** → reserve **Sysight**.
If it's taken, pick another name and change `App.Name` in `src/Sysight/Program.cs`, `<title>`/brand in `src/Sysight/ui.html`, and `DisplayName` in `packaging/AppxManifest.template.xml`.

## 3. Copy your package identity (you)
Partner Center → your app → **Product management → Product identity**. Note:
- `Package/Identity/Name` (for example `12345YourName.Sysight`)
- `Package/Identity/Publisher` (for example `CN=1A2B3C4D-...`)
- `Package/Properties/PublisherDisplayName`

## 4. Build the Store package
```powershell
cd C:\Sysight
./packaging/build-msix.ps1 -IdentityName "<Name>" -Publisher "<Publisher>" -PublisherDisplayName "<PublisherDisplayName>" -Version 1.0.0.0
```
Output: `out\Sysight_1.0.0.0_x64.msix`. Don't sign it; the Store signs it for you.
For every later update, raise `-Version` (for example `1.0.1.0`). The last number must stay `0` for Store packages.

## 5. Host the privacy policy
Publish `docs/privacy.html` somewhere public. The simplest way is to push this repo to GitHub, then go to Settings → Pages → Deploy from branch `main`, folder `/docs`. The URL will be `https://<you>.github.io/<repo>/privacy.html`.

## 6. Fill in the submission (you)
- **Pricing and availability:** Free (or your price), all markets.
- **Properties:** Category *Utilities & tools*. Privacy policy URL from step 5. Tick "This product accesses, collects or transmits personal information" = **No**.
- **Age ratings:** answer the questionnaire (no UGC, no purchases, no data collection).
- **Packages:** upload the `.msix`.
- **Store listings:** copy from `docs/STORE_LISTING.md` and upload the 8 PNGs in `docs/screenshots/` plus `docs/store-logo-300.png`.
- **Submission options → Restricted capabilities:** paste the justification from `STORE_LISTING.md`.

Submit. Certification usually takes 1–3 business days. Apps with restricted capabilities can take longer and may get a follow-up question from Microsoft.

## 7. Also ship outside the Store (optional)
- **GitHub Releases:** attach the `.msix` and a zipped `out\layout`. Note that an unsigned build triggers a SmartScreen warning.
- **winget:** after a GitHub release exists, run `wingetcreate new <release-asset-url>` to open a PR to microsoft/winget-pkgs.

## Testing locally
```powershell
./packaging/build-msix.ps1 -Register      # needs Developer Mode (already on for this PC)
Get-AppxPackage Sysight.Local | Remove-AppxPackage   # remove the test install
```
