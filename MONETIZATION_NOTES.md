# Monetization notes — Juice King Tycoon

How the game earns money, where every ad and purchase lives in the code, and the rules that keep it fair.
Release steps (store ids, SDK keys, signing) are in [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md); the numbers behind rewards are in [BALANCE_NOTES.md](BALANCE_NOTES.md).

## Principles

- **Everything is optional.** A player who never watches an ad or pays can finish all three worlds. Ads and purchases only save time.
- **Rewards are worth roughly "a minute or two of play"**, measured from the player's real income (`GameManager.IncomePerMin`), so they stay meaningful in every world without replacing the core loop.
- **Show the reward first, ask, then play the ad.** No ad starts without a tap on a button that names the reward.
- **Grant once, only after confirmed completion, then save.** `Ads.ShowRewarded` and `Iap.HandlePending` both guarantee this.
- **Premium spends need an explicit tap on a labelled price.** Spending from the delivery card needs a second tap (TAP TO CONFIRM).
- **Remove Ads removes forced ads only.** Rewarded ads stay because the player chooses them.

## Rewarded video (`Core/Ads.cs`)

Flow: the reward is shown (OfferPopup / PremiumPopup / labelled chip) → the player taps → `Ads.ShowRewarded(placement, onReward, onFail)` → an Ad Ticket is spent if the player has one, otherwise the video plays → the network confirms completion → `onReward` runs **exactly once** → the game saves.

Safety built into `Ads`:

| Risk | Protection |
|---|---|
| Network calls back twice | the completion delegate is single-use (`_finish` identity check) |
| Network never calls back | `Busy` times out after 120 s and reports a failure, so the buttons unlock |
| Button spam | `ShowRewarded` refuses while `Busy`; popups close before the ad starts |
| Provider throws | caught, treated as a failure |
| No provider in a release build | `SimulateWhenNoProvider` defaults to debug builds only; ad buttons hide (`Ads.IsReady` = false) |
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

### Interstitials (forced ads)

`Ads.TryShowInterstitial(placement)` exists so that **Remove Ads has a real effect** once a network is plugged in. It only shows when all of these hold:

- a provider implementing `IInterstitialAdProvider` is installed and has an ad ready;
- Remove Ads is not owned (`Ads.ForcedAdsAllowed`);
- the tutorial is finished;
- at least 5 min since launch, 4 min since the last forced ad, 2 min since the last rewarded ad (`ReleaseConfig`);
- nothing else is on screen (no popup, no camera shot).

They are requested at two natural breaks only: leaving the upgrade shop after buying something, and after a delivery truck drives off. With no provider (current state) they never show.

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

- **Catalog** (`IapCatalog.Products`): 6 Golden Apple packs, 6 Ad Ticket packs, Remove Ads. Ids are the store ids and must never be renamed once shipped. Fallback prices are only shown by the simulated store; a real store shows its localized price, or "..." while it connects.
- **Provider**: `UnityIapProvider` (Unity IAP 5.4, two-step pending → confirm) is installed by `AppServices` on Android and iOS devices. The Editor and development builds without a store use the simulated store (0.6 s, `Iap.SimulateNextResult` can force cancelled / failed / deferred for QA). Release builds without a store sell nothing.
- **Grant pipeline**: store → `Iap.HandlePending(productId, transactionId)` → goods granted → saved → only then the provider confirms the transaction.
  - **Duplicates**: every granted transaction id is kept in the save (`SaveData.iapTransactions`, last 200); a re-delivered or double-reported transaction never grants twice.
  - **Boot screen**: a transaction reported before a world is loaded is queued, then granted and confirmed once `GameManager` starts.
  - **Unknown product id**: never confirmed (Google refunds it; Apple re-delivers when a build knows it).
  - **Remove Ads**: re-applied from the store's purchase list at every launch (new device, reinstall) and by Restore Purchases.
- **Shop states**: connecting, unavailable product (N/A), purchase in progress, success (celebration on the card), cancelled, failed, waiting for approval (Ask to Buy / deferred), already owned, store offline.

## Ad Tickets

Bought in the shop. When the player has one, any rewarded placement spends it instead of playing the video. A toast confirms "Ad Ticket used!".

## Analytics hooks (`Core/Analytics.cs`)

Provider-agnostic: add an `IAnalyticsProvider` in `AppServices`. Events: `first_launch`, `tutorial_complete`, `unlock`, `upgrade`, `world_complete`, `world_enter`, `delivery_complete`, `delivery_expired`, `fox_raid`, `fox_recovery`, `rewarded_ad_started/completed/failed`, `interstitial_shown`, `iap_started/completed/failed/restored`, `offline_earnings_claimed`, `golden_apples_spent`. Every event carries `world`.

## Privacy and consent (`Core/Privacy.cs`)

`Privacy.RequestOnLaunch` runs an `IConsentProvider` (Google UMP, the mediation CMP, iOS ATT) once per launch before the ad SDK starts. Ad providers must read `Privacy.PersonalizedAds` and `Privacy.ChildDirected`. With no provider the state is "not required". The answer lives in its own PlayerPrefs key, so resetting progress does not ask again.
