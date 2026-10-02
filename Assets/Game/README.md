# Juice King Tycoon

3D arcade-idle game in the style of *Chainsaw Juice King: Idle Shop*.

Developers: see [`CLAUDE.md`](../../CLAUDE.md) in the project root for architecture, build workflow and rules for working on the code (with or without Claude Code).

## Play

1. Open `Assets/Game/Scenes/Boot.unity` (build index 0). It shows the loading screen: the key art, the logo popping in, and a progress bar with a percentage and tips. It loads the world your save is in, then fades into the game.
2. Set the Game view to a portrait resolution (for example 1080x1920).
3. Press Play.

**Controls**

- Touch or click anywhere and drag to use the floating joystick.
- WASD and the arrow keys also move the player.

**Debug keys** (Editor and development builds only)

- `M` adds $500.
- `F9` resets progress (back to the original farm).
- `F10` opens the world-complete popup without finishing the world.
- **Settings ▸ DEBUG: SWITCH WORLD** jumps to Farm, Tropical or Berry. Each world's progress is parked and comes back when you return; Golden Apples, Ad Tickets, Remove Ads and stats are shared. A world you haven't visited starts fresh. The row is hidden in release builds.
- **Juice King ▸ Reset Save Data** clears the save from the menu.

## Core loop

1. Walk up to a giant fruit and your chainsaw cuts it automatically. The fruit bursts into slices that land in the basket on your back.
2. Stand on the juicer's **yellow pad** to unload the slices. Every 2 slices make 1 juice cup.
3. Stand on the **green pad** at the juicer's tray to pick up the cups.
4. Stand on the **blue pad** behind the counter to stock it. Customers queue up, order a flavour, and pay.
5. Stand on the **cash pad** to collect the money.
6. Stand on the **dashed pads** to spend money. Each unlock reveals the next pads:
   - more oranges, then the upgrade shop and the watermelon juicer
   - the watermelon field
   - a waiter and more melons
   - the pineapple juicer, then the pineapple field
   - a patio
   - three farmers

   Every juicer unlocks **before** its field, so harvested fruit always has somewhere to go. Customers only order a flavour once both its juicer and its field are open.
7. The **upgrade shop** sells chainsaw power, backpack size, move speed, a juice-price **Recipe** upgrade and a **Counter** upgrade (+12 cups of stack height per level, shown by the `12/24` readout over the counter).
8. The green **trash bin** next to the upgrade shop takes anything you can't use. Stand on its pad for half a second and the bag empties, unusable slices first. If you're carrying fruit that has no juicer yet, the objective banner and guide arrow point you to the bin.

**The waiter** serves the queue in order. It covers each order from the cups already on the counter. For the first customer still short, it fetches exactly that flavour, and only that flavour, from the matching juicer. If that juicer's tray is empty, the waiter waits beside it with a ⚠ bubble, and the HUD shows "Waiter needs Orange juice!" (at most every 12 s). It does not grab other juice. Farmers never pick up cups. Helpers use `Carrier.pickupFilter` and `maxPickup` for this.

**Guide markers.** A glowing, outlined 3D arrow bounces over the current target, and a ring pulses on the ground there. Bold chevrons slide out from the player toward it. When the target is off-screen, or hidden under the HUD, a green arrow on the screen edge points to it.

**Customers** arrive every 0.35–0.9 s whenever the 7-slot queue has room, so the line stays full. Each order is random: any flavour whose juicer *and* field are open, from 1 up to N cups, where N grows from 1 to 4 as you sell more (`Balance.MaxOrder`). The customer at the front has a patience bar. After 40 s without receiving a cup, they pay for what they got and leave grumpy, so one unmet order never blocks the line. Customers are pooled, not instantiated.

Progress (money, unlocks, upgrades, tutorial step, last-seen time, cash still lying on the cash piles, boost time) is saved to PlayerPrefs, with a backup copy in the app's data folder that is used if the PlayerPrefs entry is ever unreadable.

**Settings** has Sound, Ambience and Vibration toggles, Restore Purchases, a Privacy link (once a privacy policy URL is configured) and the version number.

## Rewarded ads and boosts

Once the tutorial reaches its first sale, a column of boost buttons appears on the right (all on the same brown wooden tile):

| Boost | Reward | Where to tune |
|---|---|---|
| **2x CASH** | Doubles every sale for 2 minutes | `Balance.CashBoostSeconds`, `CashBoostMult` |
| **TURBO** | 90 s: player 1.35x faster; juicers, fruit regrowth, helpers and customer arrivals 1.8x faster | `Balance.TurboSeconds`, `TurboMoveMult`, `TurboWorkMult` |
| **FREE CASH** | A cash bag worth about 1.5 minutes of your measured income, on a 3 minute cooldown | `Economy.FreeCash`, `Balance.FreeCashCooldown` |
| **FINISH $X** | Shown when you stand on an unlock pad with no money left and only the last stretch (about one Free Cash bag) remaining. Completes the unlock | `Economy.UnlockAssistMax`, `Balance.UnlockAssistCooldown` |
| **Offline earnings** | "Welcome back" popup when you've hired helpers: a share of your income that grows with helpers hired, up to 30 min / 1 h / 2 h per world. Collect, or watch for 2x. An unclaimed card comes back on the next launch | `Economy.OfflinePerSecond`, `Economy.OfflineMaxSeconds` |

Watching again while a boost is active adds time (capped at 10 minutes). Boost time only runs while you play (not under menus or ads) and is saved, so it survives a restart or a world change.

**Ads are simulated** in the Editor and development builds. `Core/Ads.cs` is the single entry point. When no network is plugged in, a test overlay counts down for 3 seconds and then grants the reward. The game pauses meanwhile; the X button tests the "ad failed" path before the countdown ends. Release builds without a network hide the ad buttons. Every reward is granted once, only after the ad completed, and saved at once. To ship real ads, implement `IRewardedAdProvider` (and optionally `IInterstitialAdProvider`) and install it in `Core/AppServices.cs` (see `RELEASE_CHECKLIST.md` and `MONETIZATION_NOTES.md` in the project root).

Placement names are the `Ads.Placement*` constants.

## Expansion 1: Tropical Farm

**Unlocking it.** When every pad of the original farm is bought and every upgrade is maxed, a **JUICE KING!** popup appears. It shows lifetime stats: total earnings, juice produced, fruit harvested and customers served. It offers:
- **ENTER TROPICAL FARM**
- **Stay a bit longer**, which leaves a "NEW WORLD" button on the HUD.

Entering does the following (`GameManager.BeginExpansion`):
- archives a snapshot of the finished farm in the save (`world0Archive`, `world0Complete`);
- resets per-world progress: money, unlocks, upgrades, tutorial and delivery;
- keeps lifetime stats and settings;
- fades to `Tropical.unity`.

On the first visit a skippable fly-over (`ExpansionIntro`) shows "Welcome to the Tropical Juice Empire." and captions the groves, mixers, bay and hut. The tutorial then restarts with world-aware wording.

**The island.** A rounded island with organic grass patches that feather into the sand, packed-sand walkways, turquoise shallows, shore foam and a scrolling ocean. It has four orchards, each with its own trees, harvest break and dressing:

| Orchard | Plant | Harvest | Drink |
|---|---|---|---|
| Coconut Grove | leaning palms | coconuts drop and crack | coconut milk in a shell with an umbrella |
| Mango Orchard | round-crowned trees | mangoes split | tall mango tumbler |
| Banana Plantation | broad-leaf plants | bunches thump and scatter | banana shake with cream and a cherry |
| Papaya Grove | slender trunks with fan leaves | papayas burst with seeds | papaya goblet |

`TropicalFruitNode` shakes the tree on each chainsaw bite. The fruit tumbles from the tree and breaks open where it lands.

**Living world.** Gulls over the sea, parrots over the jungle, butterflies, sailboats, a lagoon with a waterfall, tiki torches, beach umbrellas, loungers and surfboards, and the trucks on the causeway.

**Delivery loop** (`Runtime/Delivery/`). Unlock the **Delivery Bay**. A truck then drives in over the causeway about every 90 s (`Economy.TruckInterval`) and parks by the loading pad.

- **Trucks.** Van 8–14 cups, Juice Truck 16–24, Resort 26–40 (two flavours unlocked), Premium 30–45 at 1.8x reward (needs **Premium Contracts**).
- **Clients.** Each order has a client such as Hotel Mango, Beach Bar or Tropical Cafe.
- **Reward.** About 2x the shop value, before truck type and upgrades.
- **Loading.** Stand on the pad with the ordered juice: cups arc into the side door, the cargo fills, and the board over the truck and the HUD card count up. When the order is full you get "DELIVERY COMPLETE!", fanfare, confetti, coins flying to the counter, the payout, and the truck drives off.
- **Timeout.** After 3 minutes an unfinished truck leaves and pays shop price for what it got.
- **HUD card.** Shows client, juice, x/y, reward and time left, or the countdown to the next truck. Tap it to glance at the bay.
- **Helpers.** The **Loader** fills trucks automatically, the **Runner** serves the hut, and one farmer per orchard.
- **Saving.** The current order is saved: a parked truck is still there after a restart.

**Economy and upgrades** (`Core/Economy.cs`). Every system reads its numbers from `Economy`. World 0 keeps the classic `Balance` formulas. World 1 has its own scale: juice from $40 to $100, unlocks from $150 to $20,000, and a data-driven tree of 18 upgrades in 6 tabs (`Upgrades.ForWorld`):

| Tab | Upgrades |
|---|---|
| FARM | Harvest Speed, Fruit Yield, Regrowth |
| MIXER | Mixer Speed, Bonus Cup, Tray Size |
| DELIVERY | Truck Reward, Big Orders, Truck Frequency, Premium Orders |
| PLAYER | Backpack, Speed, Quick Hands |
| WORKERS | Helper Speed, Helper Carry |
| BUSINESS | Juice Price, Counter, Night Shift |

Boosts carry over:
- **2x CASH** pays 1.5x on deliveries.
- **TURBO** speeds loading.
- **FREE CASH** scales to the new economy.
- **FINISH** works on the glowing unlock pads.

**Unlock → reveal → discover.** Tropical pads are soft glowing rings with a radial progress bar. On purchase, the NEW! ribbon adds a discovery line ("Juicy mangoes, juicier profits"). For big unlocks the camera glides over to what appeared (`CameraFollow.Peek`, which also drives the intro).

**Saves.** Old saves load unchanged. `saveVersion` 0 → 2 adds lifetime stats, estimated from cups sold. Unknown fields default safely, and the redirect in `GameManager.Awake` sends a Tropical save straight to its scene.

## Expansion 2: Berry Blast

**Unlocking it.** Finish the Tropical Farm (every pad bought, every upgrade maxed). A **TROPICAL TYCOON!** popup offers **ENTER BERRY BLAST**, or a NEW WORLD button if you stay. Entering archives the island (`world1Archive`), resets per-world progress, starts you with $1,500 and a gift of 5 Golden Apples, and plays a fly-over: patches, presses, Cake Hall, delivery desks, and the fox's den.

**The village** (`Berry.unity`). Everything sits in one column per berry, so walks stay short:

| Row (south → north) | What is there |
|---|---|
| Cobbled square | the **Berry Bar** juice counter, till, workshop, trash bin |
| Presses | four **berry presses** (input pad north, cups on the tray south) |
| Patches | Strawberry Patch, Raspberry Patch, Blueberry Patch, Cranberry Bog (6 bushes each) |
| Fox lane | the path the fox sneaks along; farmhand pads |
| Cake Hall | per berry: **cake mixer** → conveyor → **oven**, with a cooling rack |
| North street | the **pastry case**, cake customers queuing from the street |
| East | two **delivery desks** on a lay-by road |

Around it: pastel cottages, a bakery with a smoking chimney, blossom trees, a forest ring, flower beds, beehives with bees, rabbits, a lily pond, butterflies, birds and drifting blossom petals.

**Berries.** Bushes shake and rustle on every chainsaw bite, then the ripe berries pop off in a spray of leaves and roll onto the soil. Each berry is packed its own way (strawberries on a leaf, raspberries and blueberries in punnets, cranberries in a scoop), and each has its own drink:

| Berry | Drink | Cake |
|---|---|---|
| Strawberry | Strawberry Smoothie (cream and a berry on top) | Strawberry Shortcake |
| Raspberry | Raspberry Fizz (mason jar) | Raspberry Velvet |
| Blueberry | Blueberry Shake (milk bottle) | Blueberry Cheesecake |
| Cranberry | Cranberry Cooler (tumbler with ice and lime) | Cranberry Tart |

**Berry crushers** (`BerryPress`, a `Juicer` with a richer show). The hopper is heaped with the berries waiting in the crate. Two spiked rollers spin against each other and thump four times per batch, juice sprays from the nip and pulses through a glass pipe into a tank that fills with the batch, a row of lights counts the progress, and the spout pours a stream into every bottle. The gold crown on the sign (back corner) bounces on every bottle.

**Berry Cake Shop** (a second business with its own queue):
1. Bring berries to the **cake mixer** (yellow pad south of it). Three berries make one tin of batter: flour puffs from the sack, the whisk spins, the bowl turns.
2. The tin rides the **conveyor** into the **oven** by itself. The window glows, a timer bar fills, the chimney steams, and with a *ding* the cake slides out onto the cooling rack.
3. Pick cakes up on the green pad by the rack and stock the **pastry case** from its south side.
4. Cake customers (party hats, pink order bubbles) queue on the north side and pay about 3.5x a juice. Cake orders are small: 1–3 cakes.

The **Baker** helper serves the cake queue from the ovens; **farmhands** alternate trips between their berry's press and its cake mixer.

**Fox raids** (`FoxRaid`, `FoxActor`). Now and then (every 4–7 minutes, rarer with the **Fox Fence** upgrade) a fox slips out of its den in the woods, runs along the fox lane, pounces into three bushes and escapes with the berries. A toast and a yip warn you a few seconds before. The fox never wrecks your only open patch. The patch wilts: it stops producing, and customers who wanted that berry switch to something else (juice or cakes already on the counter still sell).
- It regrows by itself in 5 minutes (faster with **Garden Care**). A glowing pad with a countdown appears on the lane in front of it, and a fox chip with the timer shows on the HUD.
- Step on the pad (or tap the chip) to **RESTORE** it now for **3 Golden Apples**, or **WATCH AD**.
- The first raid is a short cutscene ("Uh oh... a sneaky fox!") that ends on this choice. It happens about 50 s after the tutorial.
- Raided patches and their timers are saved; regrowth keeps ticking while the game is closed.

**Delivery desks.** Two desks (Desk B unlocks later), each with its own trucks and order board. Trucks are new too: rounded cabs with headlight "eyes" and a smiling grille, mirrors, fenders, brake lights, a berry mural and a roof mascot per type (Berry Van, Smoothie Co., Party Time, Royal Berry). They brake smoothly into the lay-by, the body dips and settles.
1. The truck stops and an **empty box** pops up on the pad beside it, flaps open.
2. Stand on the pad with the ordered juice (or cakes, once the Cake Shop bakes): items fly in, the box fills, the label counts up.
3. When it is full the flaps fold shut, tape seals it, and the box is lifted into the truck's open rear doors (Berry Blast trucks park square in the bay, swing their barn doors round flat against the sides and show the parcel stack growing in the hold); the truck dips under the weight, you get the payout and a **Golden Apple**, and the truck drives away.
- The first truck is always an easy juice order. Later trucks may order cakes (fewer items, bigger rewards).
- The **Loader** fills whichever desk needs it.

**Upgrades.** 24 upgrades in 6 tabs: FARM (Harvest Speed, Berry Yield, Regrowth, Fox Fence, Garden Care), MIXER (Press Speed, Bonus Cup, Tray Size), BAKERY (Oven Heat, Cooling Rack, Cake Recipe, Pastry Case), DELIVERY (4), PLAYER (3), BUSINESS (Juice Price, Juice Counter, Helper Speed, Helper Carry, Night Shift).

**Decor pads.** The Cozy Patio, Beach Cabana, Flower Gazebo and Picnic Garden each make customers pay +10% in their world ("Charm").

**Completing the Berry Blast** (every pad, every upgrade) shows a **BERRY CAKE EMPIRE!** celebration with a one-time reward of 25 Golden Apples.

## Golden Apples (premium currency)

Golden Apples are shown next to the settings gear in every world and are kept when you move to a new world. Tap the counter (its green **+**) to open the shop.

| Earn | Spend |
|---|---|
| 5 as a welcome gift (existing saves get them too) | **3** to restore a fox-raided patch at once |
| 3–5 when entering a new world | **1** to call the next truck now (delivery card) |
| 1 per Berry Blast delivery, 2 per premium truck | **1 per 12 missing items** to finish a truck order at once |
| 25 for completing the Berry Blast (once) | Spending from the delivery card needs a second tap (TAP TO CONFIRM) |

Rewards fly up to the counter with a chime; spending sparkles. Numbers live in `Economy` (`StartingApples`, `ApplesRestoreFarm`, `ApplesCallTruck`, `ApplesFinishOrder`). The **WATCH AD** button is the free alternative wherever a restore is offered.

## Shop (in-app purchases)

The **+** on the Golden Apple counter opens the shop on its apple packs; the **+** on the smaller **Ad Ticket** counter under it opens it on the ticket packs. The shop is one scrolling popup:

- **GOLDEN APPLES:** six packs (Starter, Small, Medium, Large, Mega, Ultimate) with a growing pile of apples, a bonus line and a price button. Medium is tagged **POPULAR**, Ultimate **BEST VALUE** (gold glow and rays).
- **AD TICKETS:** six packs. One ticket claims a rewarded-ad reward (boosts, free cash, offline x2, fox restore...) instantly, without the video. Tickets are used automatically whenever you have one; a toast says "Ad Ticket used!".
- **REMOVE ADS:** removes forced ads for good and shows **OWNED** afterwards. Rewarded ads stay available as optional rewards.
- **Restore Purchases** at the bottom (required by the App Store for Remove Ads).

Products, quantities, bonus lines, badges and fallback prices are data in `Core/IapCatalog.cs` (`IapCatalog.Products`); the cards read them at runtime, so retuning needs no rebuild (adding, removing or reordering products does: rebuild the scenes). Product ids (`jk_apples_starter`...) must match the store and must never be renamed once shipped.

**Purchases.** `Core/Iap.cs` is the single entry point. On Android and iOS devices it talks to the stores through Unity IAP 5 (`Core/UnityIapProvider.cs`); in the Editor and development builds a simulated store completes a purchase after 0.6 s. Each store transaction is granted once (its id is kept in the save), saved, and only then confirmed with the store, so nothing is lost or doubled if the app is killed mid-purchase. The shop shows the store's localized prices and its states (connecting, unavailable, cancelled, failed, waiting for approval, owned). Store setup: `RELEASE_CHECKLIST.md`.

Forced ads (interstitials, banners) don't exist yet; when they are added they must check `Ads.ForcedAdsAllowed`.

## Loading screen

`Boot.unity` / `Runtime/UI/LoadingScreen.cs`. The art comes from `Assets/Game/UI/Loading screen/`:
- `starterBG.png` fills the portrait screen, cropping the sides, and drifts in slowly.
- `GameLogo.png` pops in with a bounce, then breathes and bobs, with a shine sweeping across it.

At the bottom are a rotating tip, "LOADING…" and a juice-orange bar with a percentage.

The bar follows real work:
1. synthesising the sound effects;
2. loading the saved world in the background.

It stays up for at least 2.6 s so the logo animation reads, and it stays on screen while the world's first frames run. It then fades out as the logo floats up. Entering a new world, or pressing F9, comes back through the same screen.

To change the art, replace the two PNGs (keep the names) and run **Juice King ▸ Rebuild Boot (Loading) Scene**. The tips are listed in `JuiceKingBuilder.Boot.cs`.

## Feel and audio

- **Particles** (`Core/Fx.cs`): juice droplets, lingering ground splats, shockwave rings, stars, hearts, leaves, coins, glints, bubbles, footstep dust and a turbo trail. They all come from 16 shared, looping particle systems (emission rate 0) that are emitted on demand.
- **Animation**: the stack bounces on every landing and items squash; characters lean into their run; customers hop and cheer (the `emote-yes` clip is on the `Cheer` trigger); fruit sways idly and regrows with an elastic pop; juicers wobble and bubble; the bin lid springs open; UI coins swoop to the money counter; buttons squish; boost buttons wiggle; a "NEW!" banner drops in after each unlock.
- **Ambient life** (`Decor/`): `Ambient` drives the wind sway on trees, flowers, grass, reeds and bunting, the windmill sails, drifting cloud shadows, the scrolling foam and the falling waterfall in a single Update. `Wanderer` drives the chickens, dog, cat and ducks, which scatter when you run at them. `Butterflies` flutters over the flower beds.
- **ASMR audio** (`Core/Sfx.cs`): all sounds are synthesised at 44.1 kHz with soft attacks and rounded tails: squelchy fruit bursts, crunchy chops, wooden "tok"s into the hopper, glass "tink"s on the counter, metallic coin clinks that rise in pitch during a collection streak, a marimba unlock chime, bubbling pours, bin thunks, footstep taps, and a looping breeze-and-birdsong ambience.

## Look

Everything opaque is drawn with one hand-written shader, `Assets/Game/Shaders/Stylized.shader`, and the water with `Water.shader`. The look is soft and storybook-like:

- **Shading.** Light wraps softly around shapes, shade drifts to a cool lilac instead of grey, and a warm rim light outlines everything from the high camera. The sides of props darken a little near the ground. Faint world-space noise keeps big surfaces from looking flat. Shiny and metal parts get a cheap fake reflection.
- **Foliage.** Palm fronds, broad leaves and canopies are shaded from a darker base to sunlit tips (vertex colours), and they sway in the wind in the vertex shader, shadows included. Kenney tree leaves and grass sway too.
- **Water.** The pond, lagoon and ocean have drifting caustics, sun sparkles and a shallow-to-deep colour shift. The lagoon's waterfall is a curved, streaming sheet with foam at the lip and the splash, plus a little mist.
- **Contact shadows.** Every prop that stands on the ground gets a soft oval shadow sized to its footprint (a trunk for a tree, the whole base for a hut). Delivery trucks carry theirs down the road.
- **See-through.** Tall decor (trees, umbrellas, the windmill, huts, signs) dithers away when it stands between the camera and the player.
- **Ambient particles.** Pollen motes float and leaves drift down around the player in both worlds.
- **Details.** Striped patio and beach umbrellas, striped swim rings (ProBuilder) in the lagoon and on the sand, a pad you can afford twinkles, and a glint sweeps across the progress bar when it grows.
- **Colour grading.** Neutral tonemapping with extra contrast and saturation, cool shadows and warm highlights (`BuildLighting`).

## Platforms and performance

The mobile build (Android and iOS, portrait) comes first. Web portals (itch.io, CrazyGames, Poki) come later from the same code.

- **Render settings.** `Mobile_RPAsset` is used on Android, iOS and WebGL: HDR off, MSAA 2x, render scale 0.9, 1024 shadow map, 32 m shadow distance, no additional lights. `PC_RPAsset` is the editor/desktop profile. `ConfigureProject()` sets both on every full build.
- **Player settings.** IL2CPP, ARM64 on Android, incremental GC, low managed stripping, portrait lock, and the screen never sleeps. **Set your own bundle identifier** (Player Settings ▸ Other) before a store build.
- **Draw calls.** Juicers, counter and bin are merged into one mesh per material at build time (`JuiceKingBuilder.Optimize.cs`). Beds, trees, bushes, rocks and grass are static-batched. Only flowers, reeds, lily pads and bunting sway. Decor sits on layers 8 and 9 with camera cull distances of 70 m and 44 m. Small props cast no shadows. In the editor this took the scene from ~1,600 to ~750 effective draws, with about 65 SetPass calls.
- **UI.** All sprites are packed into `Generated/UIAtlas.spriteatlasv2`. HUD elements sit under a `SafeArea` root, which keeps them clear of notches and home bars. Berry Blast's popups, ribbons and buttons use the berry-themed `UI/Atlas4.png`.
- **GC.** Tweens are pooled per type, and customers, items and floating text come from `Pool`.
- **Web later.** `Core/Platform.cs` already reports `GameplayStart`/`GameplayStop` (popups, ads, settings) and `LoadingFinished`. A Poki or CrazyGames SDK bridge only needs to subscribe to those events and implement `IRewardedAdProvider`. The layout is portrait: desktop web will need a landscape HUD pass, and `CameraFollow` already widens the FOV for landscape.

## UI art pipeline

The UI uses the art in `Assets/Game/UI/Atlas1-3.png` plus a few hi-res pieces from the *2D Mobile Game UI Kit*. `Scripts/Editor/Gen/UIKit.cs` lists each sprite by a seed point (the pixel coordinate of the item in the atlas). `UIAtlasCutter` flood-fills the item by alpha and writes it to `Generated/UI/<name>.png` as a sprite, with 9-slice borders where needed. Buttons with baked text ("Play", "Start"...) are rebuilt as **blank** 9-slice buttons by repeating a text-free column, so they can hold any label.

The atlases also contain blank buttons, square buttons, ribbons, planks and speech bubbles; these are cut as-is, and their 9-slice borders are detected automatically. Panels have a close X painted on. `UIKit.CloseUV(panel)` finds it by flood-filling from a seed point on its red circle, and the builder places an invisible button exactly over it.

To add a sprite:

1. Add a `P(atlas, "name", x, y)` line in `UIKit.Define()`.
2. Run **Build Everything**.
3. Use it with `UIKit.Get("name")` in the builder.

`UIKit.ContactSheet(path)` renders every cut sprite into a grid for review.

## Regenerating content

The scene, prefabs, materials, meshes, textures and animator are all generated from code. After you change the builder code, use one of these menu items:

- **Juice King ▸ Build Everything** regenerates art, reconfigures the model imports, and rebuilds prefabs and the scene.
- **Juice King ▸ Rebuild Scene (skip art + import)** skips the art and import steps, so it is faster. It rebuilds both worlds.
- **Juice King ▸ Rebuild Tropical Scene** rebuilds only the island.

Hand edits to the generated scenes are overwritten on rebuild. Put layout changes in `Scripts/Editor/Builder/JuiceKingBuilder.Scene.cs` (original farm) or `JuiceKingBuilder.Tropical.cs` (island). Merged meshes are written per scene to `Generated/Meshes/Combined/<Scene>/`, so rebuilding one world never breaks the other.

## Code map

| Path | What it holds |
|---|---|
| `Scripts/Runtime/Core/Items.cs` | Item types and **all balance numbers** (`Balance`): HP, slices, prices, juice time, upgrade costs |
| `Scripts/Runtime/Core` | GameManager (money, save, unlocks, offline earnings), Boosts (timed ad boosts), Ads (rewarded-ad entry point), Iap + IapCatalog (store products and purchase flow), Tweener, Pool, Sfx (procedural audio), Fx (particles), CameraFollow, NavBaker |
| `Scripts/Runtime/Items` | Carrier (swaying back/hand stack), ItemPile (grid piles), LooseItems (ground pickups), StackItem |
| `Scripts/Runtime/Stations` | FruitNode/FruitField, Juicer, Counter, CashPile, TrashBin |
| `Scripts/Runtime/Zones` | Floor pads: Drop, Pickup, Cash, Upgrade, Unlock, Trash, plus UnlockManager |
| `Scripts/Runtime/Actors` | Player, Chainsaw, Customer + CustomerManager (queue/payment), WorkerAI (farmer/waiter on NavMesh), CharacterAnim |
| `Scripts/Runtime/UI` | HUD (money, shop progress, Golden Apple and Ad Ticket counters, flying coins), ShopPopup (in-app purchase shop), BoostBar, OfferPopup, AdOverlay, UnlockBanner, UpgradePanel, InputJoystick, Tutorial, OrderBubble, FloatingText, UIPress, UISpin |
| `Scripts/Runtime/Core/Economy.cs` | World-aware numbers (`Economy`), `UpgradeDef` and the per-world upgrade trees (`Upgrades`) |
| `Scripts/Runtime/Delivery` | DeliveryManager (schedule, orders, payout, save), DeliveryOrder (truck types, clients, rewards), DeliveryTruck (drive, park, door, cargo fill), DeliveryBay, DeliveryZone (loading pad), TruckBoard, DeliveryHUD |
| `Scripts/Runtime/Expansion` | ExpansionManager (world-complete check, scene switch), CompletionPopup, ExpansionIntro, ScreenFader |
| `Scripts/Runtime/Decor` | Ambient (wind, spinners, clouds, water), Wanderer (animals), Butterflies, Birds, PathMover (boats) |
| `Scripts/Editor` | Builder: procedural textures and icons (`ArtGen`, `ArtGen.Tropical`), meshes, materials, Kenney import, scenes (`JuiceKingBuilder.Scene.cs`, `JuiceKingBuilder.Tropical.cs`), tropical plants, trucks and props (`JuiceKingBuilder.TropicalProps.cs`), environment dressing (`JuiceKingBuilder.Env.cs`), visual polish (`JuiceKingBuilder.Polish.cs`) and UI construction (`JuiceKingBuilder.UI.cs`, `JuiceKingBuilder.UIExpansion.cs`) |
| `Shaders` | `Stylized.shader` (every opaque material) and `Water.shader`: hand-written URP HLSL, SRP Batcher compatible |

## Credits

- 3D models: [Kenney](https://kenney.nl) (CC0). The packs used are Mini Characters, Mini Market, Survival Kit, Food Kit, Nature Kit and Furniture Kit. Licenses are in `Art/Kenney/*/License.txt`.
- UI: the project's own atlases in `Assets/Game/UI`, and hero coin/money-bag art from the *2D Mobile Game UI Kit* by 300Mind (Unity Asset Store, Standard EULA). Only its two sprite sheets are in `Assets/ThirdParty/300Mind`.
- Animals: chicken, dog and cat from [Animals FREE by ithappy](https://assetstore.unity.com/) (Unity Asset Store, Standard EULA), in `Assets/ThirdParty/ithappy`. Only the needed meshes, animations, controllers and texture were extracted. The package's own `Packages/manifest.json` was deliberately not imported.
- Font: Lilita One by Juan Montoreano (SIL Open Font License 1.1).
- The icons, UI sprites, textures, particle effects and sound effects are generated procedurally by the project's own code.
