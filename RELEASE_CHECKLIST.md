# Release checklist — Juice King Tycoon (Android & iOS)

Everything in the project that can be finished without store accounts is done. Each item below needs something only
the owner has: a store account, a certificate, an ad-network dashboard or a legal page. Work through them in order.

Code entry points: `Assets/Game/Scripts/Runtime/Core/ReleaseConfig.cs` (ids and switches) and
`Assets/Game/Scripts/Runtime/Core/AppServices.cs` (where SDK providers are installed).

## 1. Identity and signing

| Item | Where | Notes |
|---|---|---|
| Bundle identifier (e.g. `com.yourstudio.juicekingtycoon`) | `ReleaseConfig.BundleIdentifier` | Applied to Android and iOS by **Juice King ▸ Build Everything** (`ConfigureProject`). Once a build is live it can never change. |
| Company name | Player Settings ▸ Company Name | Currently `DefaultCompany`. It changes `Application.persistentDataPath`, so set it **before** the first public build (the save backup file lives there). |
| Version / build number | Player Settings ▸ Version, Android Bundle Version Code, iOS Build | Raise the code / build number on every upload. |
| Android keystore | Player Settings ▸ Android ▸ Publishing Settings | Create an upload key and keep it plus its passwords outside the repo. Enable Play App Signing. |
| iOS signing | Xcode project or Player Settings ▸ iOS ▸ Signing Team ID | Needs an Apple Developer account. |
| App icon | Player Settings ▸ Icon | Not generated yet: export one from the loading-screen key art (`Assets/Game/UI/Loading screen/`). |

`ConfigureProject` already sets: IL2CPP, ARM64, portrait lock, incremental GC, low stripping, Android App Bundle,
Android min API ≥ 24 (Unity 6.4 default 25) and iOS 15.0 (engine minimum).

## 2. In-app purchases (Unity IAP 5.4.3 installed)

1. Create every product of `IapCatalog.Products` in **Google Play Console** and **App Store Connect** with **exactly
   the same ids** (`jk_apples_starter` ... `jk_remove_ads`). Types: apples and tickets are *consumable*, `jk_remove_ads`
   is *non-consumable*.
   - If a store forces another id, map it in `ReleaseConfig.GooglePlayProductIds` / `AppleProductIds`. Never rename a
     catalog id after release: saves and receipts refer to it.
2. Set real prices in the consoles. The catalog's `fallbackPrice` is only shown by the simulated store in the Editor
   and dev builds. Release builds show the store's localized price (or "..." while connecting).
3. Google Play: upload a signed build to an internal testing track before products become purchasable; add license
   testers.
4. App Store: fill the Paid Apps agreement, tax and banking; create a sandbox tester.
5. Optional receipt validation:
   - Google: **Services ▸ In-App Purchasing ▸ Receipt Validation Obfuscator**, then validate in
     `UnityIapProvider.OnPurchasePending` before `Iap.HandlePending`.
   - Apple: validate `order.Info.Apple?.jwsRepresentation` server-side if you add a backend.
6. Test on device: buy each pack once, kill the app mid-purchase, Restore Purchases on a second device (Remove Ads must
   come back), Ask to Buy / deferred, cancelled purchase, airplane mode.

Already handled in code: two-step pending → grant → save → confirm, duplicate transactions (ledger in the save),
purchases reported during the boot screen, Remove Ads restore on launch, shop states (connecting / unavailable /
failed / cancelled / deferred / owned).

## 3. Rewarded and interstitial ads

1. Choose the mediation network (LevelPlay recommended for Unity; AdMob or AppLovin MAX also fit). Install its UPM
   package.
2. Create the apps and ad units in its dashboard. Put the keys in `ReleaseConfig` (`AndroidAdAppKey`,
   `IosAdAppKey`, `*RewardedUnitId`, `*InterstitialUnitId`).
3. Write one class implementing `IRewardedAdProvider` (and `IInterstitialAdProvider` on the same object):
   - `IsReady`: a rewarded ad is loaded;
   - `Show(placement, done)`: call `done(true)` **only** from the network's "reward granted" callback, `done(false)`
     on close-without-reward or a show failure. Extra calls are ignored by `Ads`;
   - preload the next ad after each show.
4. Install it in `AppServices.StartAds` (it runs after consent). Pass `Privacy.PersonalizedAds` and
   `Privacy.ChildDirected` to the SDK.
5. Placement names to register (optional, for reporting): `boost_cash_2x`, `boost_turbo`, `free_cash`, `offline_2x`,
   `unlock_assist`, `fox_restore`; interstitials `upgrades_closed`, `delivery_done`.
6. Release builds without a provider hide every ad button (`Ads.SimulateWhenNoProvider` is debug-only). Do not ship
   that way unless ads are intentionally off.
7. Test on device: reward granted once; closing early gives nothing; airplane mode hides the buttons; Remove Ads stops
   interstitials but keeps rewarded ads.

## 4. Privacy, consent and store forms

| Item | Where |
|---|---|
| Privacy policy URL (required by both stores, shown in Settings) | `ReleaseConfig.PrivacyPolicyUrl` |
| Terms of service URL (optional) | `ReleaseConfig.TermsOfServiceUrl` |
| GDPR / CMP consent form (EEA/UK) | implement `IConsentProvider` (Google UMP or the network's CMP) and set `Privacy.Provider` in `AppServices.Boot` |
| iOS App Tracking Transparency | request inside the consent provider; add `NSUserTrackingUsageDescription` in Player Settings ▸ iOS |
| SKAdNetwork ids (iOS) | from the ad network's docs into Info.plist |
| Child-directed? | `ReleaseConfig.ChildDirected` (also the Play "Target audience" and Families policy) |
| Google Play Data safety / App Store privacy labels | declare: purchase history, device ids (ads), analytics if added |
| Content rating questionnaire | Play Console and App Store Connect (in-app purchases, ads, no violence) |

Settings shows "Privacy" once a policy URL or a consent provider with privacy options exists.

## 5. Analytics (optional)

Implement `IAnalyticsProvider` (Firebase, GameAnalytics...) and add it in `AppServices.Boot`. The game already sends
the events listed in [MONETIZATION_NOTES.md](MONETIZATION_NOTES.md). Set `Analytics.LogInEditor = true` to watch them
in the Editor console.

## 6. Build and smoke test

1. **Juice King ▸ Build Everything** (regenerates art, scenes, settings).
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

Screenshots (portrait 1080×1920; capture from the Game view), feature graphic, short / long description, support
e-mail, category (Casual / Simulation).

## Known limitations at release time

- Ads and purchases were verified in the Editor with the simulated store / ad overlay and the Unity IAP code path was
  compiled against the real package; a device test with real store accounts is still required.
- Nice Vibrations haptics are only active on device (the Editor plugin is unavailable on this Mac).
- Web portals (Poki / CrazyGames) need a landscape HUD pass and their SDK bridge (`Core/Platform.cs`).
