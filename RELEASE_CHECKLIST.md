# Release checklist — Juice King Tycoon (Android first, iOS later)

Play Console steps, policy answers and store listing drafts: [PLAY_STORE_SUBMISSION.md](PLAY_STORE_SUBMISSION.md).

Everything in the project that can be finished without store accounts is done. Each item below needs something only
the owner has: a store account, a certificate, an ad-network dashboard or a legal page. Work through them in order.

Code entry points: `Assets/Game/Scripts/Runtime/Core/ReleaseConfig.cs` (ids and switches) and
`Assets/Game/Scripts/Runtime/Core/AppServices.cs` (where SDK providers are installed).

## 1. Identity and signing

| Item | State |
|---|---|
| Package name `com.ionixgames.juicekingtycoon`, company "Ionix Games" | **done**: `ReleaseConfig.BundleIdentifier` / `CompanyName`, applied by `ConfigureRelease` (Build Everything or **Juice King ▸ Release ▸ Apply Play Store Settings**). Never change either after the first upload. |
| Version 1.0.0, Android version code 1, iOS build 1 | **done**. Raise the version code on every upload (Player Settings ▸ Android ▸ Bundle Version Code). |
| Target API 36, min API 25, ARM64, IL2CPP, App Bundle | **done** (`ConfigureProject` + `ConfigureRelease`). |
| Android adaptive icon | layers generated (`Assets/Game/UI/Icon/IconAdaptiveBack/Fore.png`); assigned by `ConfigureRelease` once Android Build Support is installed. Play Store icon: `StoreAssets/play_icon_512.png`. |
| **Android Build Support** | **open**: not installed on this Mac (needs ~10 GB free on the system disk). |
| **Upload keystore** | **open**: Player Settings ▸ Android ▸ Publishing Settings ▸ Keystore Manager. Keep it and its passwords outside the repo; enroll in Play App Signing. |
| iOS signing | later (Android first). |

`PlayStoreBuildCheck` refuses a non-development Android build with a template package name, DefaultCompany, no AdMob
app id or no ARM64, and warns about test ad ids, a missing privacy URL and a missing keystore. **Juice King ▸ Release ▸
Check Play Store Readiness** prints the same list.

## 2. In-app purchases (Unity IAP 5.4.3, Google Play Billing 9)

1. Create every product of `IapCatalog.Products` in Play Console with **exactly the same ids**, names and descriptions
   from PLAY_STORE_SUBMISSION.md §3. `jk_remove_ads` is **VIP: Skip Ads** (non-consumable); never describe it as
   "Remove Ads".
2. Set real prices there. The catalog's `fallbackPrice` is only shown by the simulated store.
3. Upload a signed build to the internal testing track (products become purchasable only then); add license testers.
4. Optional receipt validation: **Services ▸ In-App Purchasing ▸ Receipt Validation Obfuscator**, then validate in
   `UnityIapProvider.OnPurchasePending` before `Iap.HandlePending`.
5. Device tests: PLAY_STORE_SUBMISSION.md §3.

Already handled in code: pending → ledger → grant → save (+ backup) → confirm; duplicates; purchases during the boot
screen; VIP restore at launch and via Restore Purchases (reports after the purchase list is applied); 5-minute purchase
lock for slow payment sheets; shop states.

## 3. Rewarded ads (Google AdMob): integrated

- Google Mobile Ads Unity plugin 11.5.0 + External Dependency Manager 1.2.190 (OpenUPM scoped registry in
  `Packages/manifest.json`). `AdMobProvider` (rewarded), `UmpConsentProvider` (consent), installed by `AppServices`.
- **Open:** replace Google's **test ids** in `ReleaseConfig` (`AndroidAdAppKey`, `AndroidRewardedUnitId`) with the real
  ones, then **Apply Play Store Settings** (writes the app id into `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`).
  A build without an app id crashes at launch; the build check refuses it.
- **Open:** AdMob GDPR message (Privacy & messaging) and app-ads.txt: PLAY_STORE_SUBMISSION.md §2.
- First Android build: **Assets ▸ External Dependency Manager ▸ Android Resolver ▸ Resolve** (accept the Gradle templates).
- No interstitials, no banners (`ReleaseConfig.InterstitialsEnabled = false`).
- Device tests: reward granted once; closing early gives nothing; airplane mode → dimmed buttons and a toast; EEA consent
  form (VPN or `UmpConsentProvider.DebugForceEea` in a development build with your test device id); Settings ▸ Privacy.

## 4. Privacy, consent and store forms

| Item | Where |
|---|---|
| Privacy policy | draft `PRIVACY_POLICY.md` → host it → `ReleaseConfig.PrivacyPolicyUrl` (**open**) |
| GDPR / US-state consent | **done** in code (Google UMP); create the message in AdMob |
| Child-directed? | No: 13+ (`ReleaseConfig.ChildDirected = false`), Play target audience 13–15, 16–17, 18+ |
| Data safety, content rating, ads declaration, advertising ID | answers in PLAY_STORE_SUBMISSION.md §4 |
| iOS ATT / SKAdNetwork | later, with the iOS release |

## 5. Analytics (optional)

Implement `IAnalyticsProvider` (Firebase, GameAnalytics...) and add it in `AppServices.Boot`. The game already sends
the events listed in [MONETIZATION_NOTES.md](MONETIZATION_NOTES.md). Set `Analytics.LogInEditor = true` to watch them
in the Editor console.

## 6. Build and smoke test

1. **Juice King ▸ Release ▸ Apply Play Store Settings** (not Build Everything: it regenerates the hand-tuned Boot scene).
2. Switch platform, build an `.aab` (Android) / Xcode project (iOS).
3. On a phone (1080×1920 class), a tall phone (≥ 19.5:9) and a tablet:
   - fresh install → loading screen → tutorial to the first unlock;
   - force-close during play, relaunch: money, cash on the counter's pile, boosts and fox timers are kept;
   - leave for 10 min, relaunch: Welcome Back card (only once helpers are hired);
   - one rewarded ad of each placement, one purchase, Restore Purchases;
   - world switch (debug Settings row is hidden in release builds; use a save that finished world 0).
4. In a development build the log line `[Release] not configured yet: ...` lists anything still missing from
   `ReleaseConfig`.

## 7. Store listing

Drafts and specs: PLAY_STORE_SUBMISSION.md §5 (icon ready in `StoreAssets/`; feature graphic and screenshots still to make).

## Known limitations at release time

- Verified in the Editor: simulated purchases (VIP grant, shop states), the AdMob plugin's placeholder rewarded ad through
  the real `AdMobProvider` path (reward once after close, preload of the next ad), the no-video and no-network UI states.
  The Android code paths (Play Billing, AdMob on device, UMP form) still need a device test from the internal track.
- Nice Vibrations haptics are only active on device (the Editor plugin is unavailable on this Mac).
- Web portals (Poki / CrazyGames) need a landscape HUD pass and their SDK bridge (`Core/Platform.cs`).
