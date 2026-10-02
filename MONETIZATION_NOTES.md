# Monetization notes — Juice King Tycoon

How the game earns money, where every ad and purchase lives in the code, and the rules that keep it fair.
Release steps (store ids, SDK keys, signing) are in [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md); the numbers behind rewards are in [BALANCE_NOTES.md](BALANCE_NOTES.md).

## Principles

- **Everything is optional.** A player who never watches an ad or pays can finish all three worlds. Ads and purchases only save time.
- **Rewards are worth roughly "a minute or two of play"**, measured from the player's real income (`GameManager.IncomePerMin`), so they stay meaningful in every world without replacing the core loop.
- **Show the reward first, ask, then play the ad.** No ad starts without a tap on a button that names the reward.
- **Grant once, only after confirmed completion, then save.** `Ads.ShowRewarded` and `Iap.HandlePending` both guarantee this.
- **Premium spends need an explicit tap on a labelled price.** Spending from the delivery card needs a second tap (TAP TO CONFIRM).
- **No forced ads.** The game shows optional rewarded videos only (Google AdMob): no interstitials, no banners.
- **VIP: Skip Ads** (product `jk_remove_ads`) grants every rewarded offer instantly, without a video. It is never sold
  as "Remove Ads", because there are no forced ads to remove.

## Rewarded video (`Core/Ads.cs`)

Flow: the reward is shown (OfferPopup / PremiumPopup / labelled chip) → the player taps → `Ads.ShowRewarded(placement, onReward, onFail)` → VIP grants at once; otherwise an Ad Ticket is spent if the player has one; otherwise the video plays → the network confirms completion → `onReward` runs **exactly once** → the game saves.

Network: **Google AdMob** (Google Mobile Ads Unity plugin 11.5.0, OpenUPM) through `Core/AdMobProvider.cs`, started by
`AppServices` after Google UMP consent (`Core/UmpConsentProvider.cs`). One rewarded ad is kept loaded, failed loads retry
with back-off (5 s to 2 min, only online), a loaded ad is replaced after 55 min, and the result is reported when the
video **closes**. Requests are tagged max content rating T, not child-directed. Ids: `ReleaseConfig` (Google test ids
until the real ones are pasted in).

Button states (`Ads.IsReady`, `Ads.SkipsVideo`, `Ads.Enabled`):

| State | Boost buttons / popups |
|---|---|
| Video loaded | full colour, AD badge, "WATCH" |
| VIP or Ad Ticket | full colour, no AD badge, "CLAIM" / "USE TICKET" |
| No video yet (no fill, offline) | dimmed; a tap shows a toast saying why; the Welcome Back card offers COLLECT only; the fox card hides its ad button; the FINISH chip hides |
| No ad network at all (release build whose SDK did not start) | the boost column is hidden |

Safety built into `Ads`:

| Risk | Protection |
|---|---|
| Network calls back twice | the completion delegate is single-use (`_finish` identity check) |
| Network never calls back | `Busy` times out after 120 s and reports a failure, so the buttons unlock (never while the video is on screen: `IsShowing`) |
| Button spam | `ShowRewarded` refuses while `Busy`; popups close before the ad starts |
| Provider throws | caught, treated as a failure |
| No provider in a release build | `SimulateWhenNoProvider` defaults to debug builds only; the boost column hides (`Ads.Enabled` = false) |
| Reward lost on a crash | the grant saves immediately (`GameManager.Save`) |

Placements (constants in `Ads`, also sent to analytics):

| Placement | Where | Reward |
|---|---|---|
| `boost_cash_2x` | boost column | 2x sales for 2 min of play (deliveries 1.5x) |
| `boost_turbo` | boost column | 90 s: player 1.35x, machines / helpers / customers 1.8x |
| `free_cash` | boost column | `Economy.FreeCash`: ~1.5 min of income, 3 min cooldown |
| `offline_2x` | Welcome Back card | doubles offline earnings |
| `unlock_assist` | "FINISH $X" chip on an unlock pad | pays the last stretch (≤ one Free Cash bag, ≤ half the pad), 150 s cooldown |
| `fox_restore` | fox popup / pad / HUD chip | restores a raided berry patch now |

Boost time is saved and only runs while the game is being played (not under menus or ads).

### Simulated ads

Without `Ads.Provider` (Editor, development builds) `AdOverlay` counts down 3 s, then grants. Its X button cancels before the countdown ends and keeps the reward after it.

### Interstitials (forced ads): switched off

The game ships **rewarded only**: `ReleaseConfig.InterstitialsEnabled = false` and `AdMobProvider` does not implement
`IInterstitialAdProvider`, so `Ads.TryShowInterstitial` never shows anything. The pacing code is kept in case the
decision changes; turning it on would also require changing VIP's description and the Play Console ads declaration.
If enabled, it only shows when all of these hold:

- a provider implementing `IInterstitialAdProvider` is installed and has an ad ready;
- VIP is not owned (`Ads.ForcedAdsAllowed`);
- the tutorial is finished;
- at least 5 min since launch, 4 min since the last forced ad, 2 min since the last rewarded ad (`ReleaseConfig`);
- nothing else is on screen (no popup, no camera shot).

They are requested at two natural breaks only: leaving the upgrade shop after buying something, and after a delivery truck drives off.

## Golden Apples (premium currency)

| Earn | Amount |
|---|---|
| Welcome gift (also given to old saves) | 5 |
| Entering the Tropical Farm / Berry Blast | 3 / 5 |
| Every Berry Blast delivery / premium truck | 1 / 2 |
| Completing Berry Blast (once) | 25 |
| Shop packs | 15 – 2,200 |

| Spend | Cost |
|---|---|
| Restore a fox-raided patch now | 3 |
| Call the next truck now | 1 (double tap) |
| Finish a truck order | 1 per 12 missing items (double tap) |

Rules: balances never go negative (`TrySpendApples`, `Load` clamps), every change saves immediately, the HUD counter always ends on the live balance, and the fox popup re-checks that the patch is still damaged before charging (a farm that regrew while the card was open costs nothing).

## In-app purchases (`Core/Iap.cs`, `Core/IapCatalog.cs`, `Core/UnityIapProvider.cs`)

- **Catalog** (`IapCatalog.Products`): 6 Golden Apple packs, 6 Ad Ticket packs, VIP: Skip Ads (`jk_remove_ads`). Ids are the store ids and must never be renamed once shipped. Fallback prices are only shown by the simulated store; a real store shows its localized price, or "..." while it connects.
- **Provider**: `UnityIapProvider` (Unity IAP 5.4, two-step pending → confirm) is installed by `AppServices` on Android and iOS devices. The Editor and development builds without a store use the simulated store (0.6 s, `Iap.SimulateNextResult` can force cancelled / failed / deferred for QA). Release builds without a store sell nothing.
- **Grant pipeline**: store → `Iap.HandlePending(productId, transactionId)` → goods granted → saved → only then the provider confirms the transaction.
  - **Duplicates**: every granted transaction id is kept in the save (`SaveData.iapTransactions`, last 200); a re-delivered or double-reported transaction never grants twice.
  - **Boot screen**: a transaction reported before a world is loaded is queued, then granted and confirmed once `GameManager` starts.
  - **Unknown product id**: never confirmed (Google refunds it; Apple re-delivers when a build knows it).
  - **Ledger first**: the transaction id is recorded before the goods are added, and the grant refreshes the backup file at once (`GameManager.SaveWithBackup`).
  - **VIP**: re-applied from the store's purchase list at every launch (new device, reinstall) and by Restore Purchases, which reports back only after that list was applied.
  - **Ad Tickets are not sold to VIPs** (their cards read "VIP"): VIP already claims every reward for free.
- **Shop states**: connecting, unavailable product (N/A), purchase in progress, success (celebration on the card), cancelled, failed, waiting for approval (Ask to Buy / deferred), already owned, store offline.

## Ad Tickets

Bought in the shop. When the player has one (and is not VIP), any rewarded placement spends it instead of playing the video. A toast confirms "Ad Ticket used!".

## Analytics hooks (`Core/Analytics.cs`)

Provider-agnostic: add an `IAnalyticsProvider` in `AppServices`. Events: `first_launch`, `tutorial_complete`, `unlock`, `upgrade`, `world_complete`, `world_enter`, `delivery_complete`, `delivery_expired`, `fox_raid`, `fox_recovery`, `rewarded_ad_started/completed/failed`, `interstitial_shown`, `iap_started/completed/failed/restored`, `offline_earnings_claimed`, `golden_apples_spent`. Every event carries `world`.

## Privacy and consent (`Core/Privacy.cs`)

`Privacy.RequestOnLaunch` runs `UmpConsentProvider` (Google UMP) once per launch before the ad SDK starts; the Mobile
Ads SDK reads the answer itself (TCF / GPP strings). `ServicesRunner` polls the 20 s request timeout (it never applies
while the form is on screen); a failure or timeout counts as "unknown", never as an old "not required". If ads may not
be requested yet (first launch offline), consent is asked again every 60 s once online. Settings ▸ Privacy opens the
UMP privacy-options form where the region requires it, otherwise the privacy policy URL. The answer lives in its own
PlayerPrefs key, so resetting progress does not ask again.
