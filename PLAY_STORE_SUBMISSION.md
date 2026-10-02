# Google Play submission guide: Juice King Tycoon

How to get the game from this project onto Google Play without policy problems. The code side is done; this file
covers what only the account owner can do (Play Console, AdMob, legal pages) and the answers to give in each form.
Engineering details live in [RELEASE_CHECKLIST.md](RELEASE_CHECKLIST.md) and [MONETIZATION_NOTES.md](MONETIZATION_NOTES.md).

| | Value |
|---|---|
| Package name | `com.ionixgames.juicekingtycoon` (permanent after the first upload) |
| Developer / company | Ionix Games |
| Version | 1.0.0, version code 1 (raise the code by 1 on every upload) |
| Target API | 36 (Android 16), min API 25, ARM64, IL2CPP, App Bundle (.aab) |
| Monetisation | optional rewarded ads (Google AdMob) + in-app purchases (Google Play Billing 9 via Unity IAP 5.4.3) |
| Audience | 13+ (not designed for children) |

---

## 1. Still open before the first upload

| # | Item | Where | Notes |
|---|---|---|---|
| 1 | **Install Android Build Support** | Unity Hub ▸ Installs ▸ 6000.4.8f1 ▸ Add modules ▸ Android (+ SDK/NDK, OpenJDK) | ~3.4 GB download, ~9.6 GB on the system disk. The system disk had only 4.2 GB free. |
| 2 | **Upload keystore** | Player Settings ▸ Android ▸ Publishing Settings ▸ Keystore Manager | Create it once, keep the file and both passwords **outside the repo** and backed up. Enroll in **Play App Signing** (default for new apps). |
| 3 | **Privacy policy URL** | `PRIVACY_POLICY.md` → host it → `ReleaseConfig.PrivacyPolicyUrl` | Public page, no login. Also goes into Play Console and the AdMob consent message. |
| 4 | **Real AdMob ids** | `ReleaseConfig.AndroidAdAppKey`, `AndroidRewardedUnitId` | Google test ids are in place now (correct for internal testing; they earn nothing). Then **Juice King ▸ Release ▸ Apply Play Store Settings**. |
| 5 | **app-ads.txt** | root of the developer website listed on the store page | One line from AdMob ▸ Apps ▸ app-ads.txt, e.g. `google.com, pub-XXXXXXXXXXXXXXXX, DIRECT, f08c47fec0942fa0`. Without it AdMob limits ad serving. |
| 6 | **Support e-mail** | Play Console ▸ Store settings | Required; shown publicly. |

When all of these are done, **Juice King ▸ Release ▸ Check Play Store Readiness** logs "ready". A non-development Android
build is refused while the package name is a template id, the company is DefaultCompany, the AdMob app id is missing or
ARM64 is off (`PlayStoreBuildCheck`).

### Build
1. Unity: **File ▸ Build Profiles ▸ Android ▸ Switch Platform** (first time only; this takes a while).
2. **Juice King ▸ Release ▸ Apply Play Store Settings** (do not run Build Everything: it would regenerate the hand-tuned Boot scene).
3. External Dependency Manager: **Assets ▸ External Dependency Manager ▸ Android Resolver ▸ Resolve**. Accept enabling the
   custom Gradle templates if asked (it then patches `mainTemplate.gradle` with the AdMob / UMP libraries).
4. Build Profiles ▸ Android: **Build App Bundle** on, Development Build **off** → Build → `JuiceKing-1.0.0-1.aab`.

---

## 2. AdMob setup (admob.google.com)

1. **Apps ▸ Add app ▸ Android**, "Is the app listed on a supported store?" → No for now (link it after the Play listing goes live).
2. **Ad units ▸ Rewarded**: one unit, e.g. "Rewarded – all placements". Reward settings in AdMob are ignored by the game
   (rewards are computed in code), so any value is fine.
3. Copy the **App ID** (`ca-app-pub-…~…`) and the **rewarded unit id** (`ca-app-pub-…/…`) into `ReleaseConfig`.
4. **Privacy & messaging ▸ European regulations (GDPR)**: create a message for the app, select the consent options
   you need, enter the **privacy policy URL**, publish. Required for EEA / UK / Switzerland. The game shows the form
   through Google UMP before any ad request (`UmpConsentProvider`) and offers **Settings ▸ Privacy** to change it later.
5. Optional: **US state regulations** message (the same code shows it automatically).
6. **Blocking controls ▸ Content**: max ad content rating **T** (the code also requests T).
7. While testing on your own phone with real ids, add the phone as a **test device** (AdMob ▸ Settings ▸ Test devices,
   or `ReleaseConfig.AdMobTestDeviceIds`). Never tap live ads on your own device.

---

## 3. In-app products (Play Console ▸ Monetize ▸ Products ▸ In-app products)

Create every product with **exactly** these ids (they are in `IapCatalog`, and saves refer to them). In Play Console they
are all "one-time products"; whether they are consumed or kept is decided by the game.

| Product id | Name (Play) | Description (Play) | Suggested price (USD) | Type in game |
|---|---|---|---|---|
| `jk_apples_starter` | Starter Golden Apples | 15 Golden Apples | 0.99 | consumable |
| `jk_apples_small` | Small Golden Apples | 85 Golden Apples (+10% bonus) | 4.99 | consumable |
| `jk_apples_medium` | Medium Golden Apples | 180 Golden Apples (+20% bonus) | 9.99 | consumable |
| `jk_apples_large` | Large Golden Apples | 375 Golden Apples (+25% bonus) | 19.99 | consumable |
| `jk_apples_mega` | Mega Golden Apples | 1,000 Golden Apples (+30% bonus) | 49.99 | consumable |
| `jk_apples_ultimate` | Ultimate Golden Apples | 2,200 Golden Apples (+45% bonus) | 99.99 | consumable |
| `jk_tickets_starter` | Starter Ad Tickets | 5 Ad Tickets: claim a video reward without watching | 0.99 | consumable |
| `jk_tickets_small` | Small Ad Tickets | 16 Ad Tickets (+5% bonus) | 2.99 | consumable |
| `jk_tickets_medium` | Medium Ad Tickets | 30 Ad Tickets (+20% bonus) | 4.99 | consumable |
| `jk_tickets_large` | Large Ad Tickets | 70 Ad Tickets (+40% bonus) | 9.99 | consumable |
| `jk_tickets_mega` | Mega Ad Tickets | 160 Ad Tickets (+60% bonus) | 19.99 | consumable |
| `jk_tickets_ultimate` | Ultimate Ad Tickets | 450 Ad Tickets (+80% bonus) | 49.99 | consumable |
| `jk_remove_ads` | VIP: Skip Ads | Get every video reward instantly, without watching a video. Permanent. | 9.99 | non-consumable |

Notes:
- **VIP: Skip Ads** keeps the old product id `jk_remove_ads` on purpose. The game has **no forced ads**, so it must
  never be described as "Remove Ads" (that would promise something that does not exist, which is a deceptive-behaviour
  rejection risk). Use the name and description above.
- Prices: set them in Play Console (Pricing templates convert to every country). The game always shows the store's
  localized price.
- Products become purchasable only after a signed build has been uploaded to a testing track. Add your Google account
  under **Settings ▸ License testing** to buy without being charged.
- Test on a device from the internal track: buy one pack of each kind, cancel a purchase, kill the app during a
  purchase (the goods must arrive on the next launch), uninstall and reinstall (VIP must come back on its own, and via
  Settings ▸ Restore Purchases), and buy with airplane mode on.

---

## 4. App content (Play Console ▸ Policy ▸ App content)

| Section | Answer |
|---|---|
| **Privacy policy** | the hosted URL from `PRIVACY_POLICY.md` |
| **Ads** | **Yes, my app contains ads** (rewarded videos) |
| **App access** | All functionality is available without special access (no login) |
| **Content rating** | see below |
| **Target audience and content** | Age groups **13–15, 16–17, 18+**. Do **not** select any under-13 group. "Could your store listing unintentionally appeal to children?" → answer honestly; keep the listing (text, screenshots) aimed at a general audience. |
| **News app** | No |
| **COVID-19, Health, Financial features, Government** | No / None |
| **Data safety** | see below |
| **Advertising ID** | **Yes**, the app uses the advertising ID. Purposes: **Advertising or marketing**, **Fraud prevention, security and compliance**. (The AdMob SDK adds the `AD_ID` permission to the manifest.) |
| **Families policy** | not applicable (no under-13 age group selected) |

### Content rating questionnaire (IARC)
Category: **Game**. Answers that match the current game:
- Violence: **No**. A chainsaw cuts fruit; foxes are cartoon animals that run off. No violence against characters, no blood.
- Fear, sexuality, language, controlled substances, crude humour, gambling: **No**.
- Simulated gambling / loot boxes: **No**. Every purchase shows exactly what you get.
- Users can interact or exchange content: **No**.
- Shares user location with other users: **No**.
- Digital purchases: **Yes** (in-app purchases).
- Contains ads: **Yes**.

Expected result: Everyone / PEGI 3 / USK 0 class. The store also adds "Contains ads · In-app purchases".

### Data safety form
Data collection and security:
- Does your app collect or share any of the required user data types? **Yes** (through the AdMob SDK).
- Is all user data encrypted in transit? **Yes**.
- Do you provide a way for users to request that their data is deleted? **No**: the game has no accounts and keeps
  progress only on the device (uninstalling deletes it). If you add a support-mail deletion process, answer Yes.

Data types (tick "Collected" and "Shared" where the AdMob row says so):

| Data type | Collected | Shared | Optional? | Purposes | Source |
|---|---|---|---|---|---|
| Location ▸ **Approximate location** | Yes | Yes | No | Advertising or marketing; Analytics; Fraud prevention, security, compliance | AdMob (from IP address) |
| App activity ▸ **App interactions** | Yes | Yes | No | Advertising or marketing; Analytics | AdMob (ad views and taps) |
| App info and performance ▸ **Crash logs**, **Diagnostics** | Yes | Yes | No | Analytics; Fraud prevention, security, compliance | AdMob |
| Device or other IDs ▸ **Device or other IDs** | Yes | Yes | No | Advertising or marketing; Analytics; Fraud prevention, security, compliance | AdMob (advertising ID, app set ID) |
| Financial info ▸ **Purchase history** | Yes | No | No | App functionality | Google Play Billing transaction ids, kept on device |

Ephemeral processing: No. Check Google's current AdMob guidance before submitting
(<https://developers.google.com/admob/android/privacy/play-data-disclosure>) in case the SDK's list has changed.
If you add analytics (Firebase, GameAnalytics...) later, update this form and the privacy policy first.

---

## 5. Store listing (Play Console ▸ Grow ▸ Store presence ▸ Main store listing)

**App name** (max 30): `Juice King Tycoon`

**Short description** (max 80):
`Harvest fruit with your chainsaw, squeeze juice and grow a booming juice empire!`

**Full description** (draft, edit freely; no keyword stuffing, no claims about other games, no fake reviews):

```
Fire up your chainsaw and build the juiciest business in the countryside!

🍉 HARVEST — Slice oranges, watermelons, mangoes, berries and more. Fruit drops straight into your backpack.
🥤 SQUEEZE — Feed the juicers, stock the counter and serve a queue of thirsty customers.
💰 EXPAND — Spend your earnings on new farms, juicers, helpers, decorations and upgrades.
🚚 DELIVER — Fill delivery trucks with orders for big payouts.
🌴 EXPLORE — Finish the farm and move on to the Tropical Island and the Berry Blast cake bakery.
🦊 PROTECT — Clever foxes raid your berry patches. Restore them and keep the bakery running!

• Relaxing one-thumb controls
• Hire farmers, waiters and loaders who work for you
• Earn even while you are away
• Play offline: no account needed

Juice King Tycoon is free to play. It contains optional ads that you can watch for in-game rewards, and optional
in-app purchases (Golden Apples, Ad Tickets and VIP: Skip Ads). You can finish every world without paying.
```

**Graphics**
| Asset | Spec | Source |
|---|---|---|
| App icon | 512×512 PNG, 32-bit, ≤1 MB | `StoreAssets/play_icon_512.png` (made from `Assets/Game/UI/Icon/GameIcon.png`) |
| Feature graphic | 1024×500 JPG/PNG, no alpha | to make: key art from `Assets/Game/UI/Loading screen/starterBG.png` + `GameLogo.png` |
| Phone screenshots | 2–8, 9:16 portrait, 1080×1920 recommended | capture from the Game view at 1080×1920 (gameplay in each world, upgrade panel, delivery, shop) |
| 7" / 10" tablet screenshots | optional, recommended | capture at 1536×2048 |

Screenshots must show real gameplay. No "#1" or "Best game" text, and no prices or rewards that the game does not give.

**Category:** Game ▸ Simulation (or Casual). **Tags:** Idle, Tycoon, Farming, Simulation, Casual.
**Contact details:** support e-mail (required), website (needed for app-ads.txt), privacy policy URL.

---

## 6. Testing tracks and going live

1. **Internal testing**: upload the first .aab, add testers by e-mail, install from the opt-in link. Check ads (test
   ads with test ids), consent (use a VPN to an EU country, or `UmpConsentProvider.DebugForceEea` in a dev build),
   purchases with license testers, Restore Purchases, reinstall.
2. **Closed testing**: personal developer accounts created after 13 Nov 2023 must run a closed test with **at least 12
   testers opted in for 14 days in a row** before they can apply for production access. Organisation accounts are exempt.
3. **Pre-launch report** (runs automatically on uploaded builds): fix any crash, ANR or accessibility error it lists.
4. **Production**: swap in the real AdMob ids, raise the version code, build, upload, start with a staged rollout (e.g. 20%).
5. After it is live: link the app in AdMob (Apps ▸ App settings ▸ Add store) and check app-ads.txt is verified.

---

## 7. Policy self-check (what the game already does)

| Policy | How the game complies |
|---|---|
| **Ads: no disruptive or forced ads** | Rewarded only: no interstitials, no banners (`ReleaseConfig.InterstitialsEnabled = false`). |
| **Rewarded ads opt-in and clearly labelled** | Every video starts only after a tap on a button that names the reward (offer popup / labelled chip). Closing early gives nothing; the reward is granted once after completion. |
| **Ads not shown to unconsented EEA users** | Google UMP form before the first ad request; Privacy options in Settings. |
| **Ad content suitable for the audience** | Max ad content rating T, not child-directed (`AdMobProvider`). |
| **Payments** | Digital goods are sold only through Google Play Billing (Billing Library 9). |
| **Deceptive behaviour** | VIP describes exactly what it does ("skip videos"), not "remove ads". Prices come from the store. |
| **Restore purchases** | VIP is re-applied automatically on every launch and via Settings ▸ Restore Purchases. |
| **No gambling / loot boxes** | Every pack shows its exact content. |
| **User data** | No accounts; progress stays on the device; privacy policy in the app (Settings ▸ Privacy) and on the listing. |
| **Target API / 64-bit** | API 36, ARM64. 16 KB memory pages: supported by Unity 6.4 and the current AdMob SDK. |
| **Families** | Not targeted at children (13+); no child-directed content in the listing. |
| **Debug features** | Debug keys, the world switcher, simulated purchases and simulated ads are gated to Editor / development builds. |
