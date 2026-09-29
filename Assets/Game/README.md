# Juice King Tycoon

3D arcade-idle game in the style of *Chainsaw Juice King: Idle Shop*.

## Play

1. Open `Assets/Game/Scenes/JuiceKing.unity`.
2. Set the Game view to a portrait resolution (for example 1080x1920).
3. Press Play.

**Controls**

- Touch or click anywhere and drag to use the floating joystick.
- WASD and the arrow keys also move the player.

**Debug keys** (Editor and development builds only)

- `M` adds $500.
- `F9` resets progress.
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

Progress (money, unlocks, upgrades, tutorial step and last-seen time) is saved to PlayerPrefs.

## Rewarded ads and boosts

Once the tutorial reaches its first sale, a column of boost buttons appears on the right:

| Boost | Reward | Where to tune |
|---|---|---|
| **2x CASH** | Doubles every sale for 2 minutes | `Balance.CashBoostSeconds`, `CashBoostMult` |
| **TURBO** | 90 s: player 1.35x faster; juicers, fruit regrowth, helpers and customer arrivals 1.8x faster | `Balance.TurboSeconds`, `TurboMoveMult`, `TurboWorkMult` |
| **FREE CASH** | A cash bag that grows with progress, on a 2 minute cooldown | `Balance.FreeCash`, `FreeCashCooldown` |
| **FINISH $X** | Shown when you stand on an unlock pad with no money left and at most half the price remaining. Completes the unlock | `Balance.UnlockAssistCooldown`, `BoostBar.AssistTarget` |
| **Offline earnings** | "Welcome back" popup when you've hired helpers. Collect, or watch for 2x | `Balance.OfflineRate`, `OfflineMaxSeconds` |

Watching again while a boost is active adds time (capped at 10 minutes).

**Ads are simulated.** `Core/Ads.cs` is the single entry point. When no network is plugged in, a test overlay counts down for 3 seconds and then grants the reward. The game pauses meanwhile, and the X button tests the "ad failed" path. To ship real ads:

1. Implement `IRewardedAdProvider` (`IsReady`, `Show(placement, done)`) on top of your SDK (for example LevelPlay or Unity Ads).
2. At startup, assign it: `Ads.Provider = new MyProvider();`.
3. For release builds without ads, set `Ads.SimulateWhenNoProvider = false`. This hides the ad buttons because `Ads.IsReady` becomes false.

Placement names are the `Ads.Placement*` constants.

## Feel and audio

- **Particles** (`Core/Fx.cs`): juice droplets, lingering ground splats, shockwave rings, stars, hearts, leaves, coins, glints, bubbles, footstep dust and a turbo trail. They all come from 16 shared, looping particle systems (emission rate 0) that are emitted on demand.
- **Animation**: the stack bounces on every landing and items squash; characters lean into their run; customers hop and cheer (the `emote-yes` clip is on the `Cheer` trigger); fruit sways idly and regrows with an elastic pop; juicers wobble and bubble; the bin lid springs open; UI coins swoop to the money counter; buttons squish; boost buttons wiggle; a "NEW!" banner drops in after each unlock.
- **Ambient life** (`Decor/`): `Ambient` drives the wind sway on trees, flowers, grass, reeds and bunting, the windmill sails, drifting cloud shadows and the scrolling pond water in a single Update. `Wanderer` drives the chickens, dog, cat and ducks, which scatter when you run at them. `Butterflies` flutters over the flower beds.
- **ASMR audio** (`Core/Sfx.cs`): all sounds are synthesised at 44.1 kHz with soft attacks and rounded tails: squelchy fruit bursts, crunchy chops, wooden "tok"s into the hopper, glass "tink"s on the counter, metallic coin clinks that rise in pitch during a collection streak, a marimba unlock chime, bubbling pours, bin thunks, footstep taps, and a looping breeze-and-birdsong ambience.

## Platforms and performance

The mobile build (Android and iOS, portrait) comes first. Web portals (itch.io, CrazyGames, Poki) come later from the same code.

- **Render settings.** `Mobile_RPAsset` is used on Android, iOS and WebGL: HDR off, MSAA 2x, render scale 0.9, 1024 shadow map, 32 m shadow distance, no additional lights. `PC_RPAsset` is the editor/desktop profile. `ConfigureProject()` sets both on every full build.
- **Player settings.** IL2CPP, ARM64 on Android, incremental GC, low managed stripping, portrait lock, and the screen never sleeps. **Set your own bundle identifier** (Player Settings ▸ Other) before a store build.
- **Draw calls.** Juicers, counter and bin are merged into one mesh per material at build time (`JuiceKingBuilder.Optimize.cs`). Beds, trees, bushes, rocks and grass are static-batched. Only flowers, reeds, lily pads and bunting sway. Decor sits on layers 8 and 9 with camera cull distances of 70 m and 44 m. Small props cast no shadows. In the editor this took the scene from ~1,600 to ~750 effective draws, with about 65 SetPass calls.
- **UI.** All sprites are packed into `Generated/UIAtlas.spriteatlasv2`. HUD elements sit under a `SafeArea` root, which keeps them clear of notches and home bars.
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
## Platforms and performance

The mobile build (Android and iOS, portrait) comes first. Web portals (itch.io, CrazyGames, Poki) come later from the same code.

- **Render settings.** `Mobile_RPAsset` is used on Android, iOS and WebGL: HDR off, MSAA 2x, render scale 0.9, 1024 shadow map, 32 m shadow distance, no additional lights. `PC_RPAsset` is the editor/desktop profile. `ConfigureProject()` sets both on every full build.
- **Player settings.** IL2CPP, ARM64 on Android, incremental GC, low managed stripping, portrait lock, and the screen never sleeps. **Set your own bundle identifier** (Player Settings ▸ Other) before a store build.
- **Draw calls.** Juicers, counter and bin are merged into one mesh per material at build time (`JuiceKingBuilder.Optimize.cs`). Beds, trees, bushes, rocks and grass are static-batched. Only flowers, reeds, lily pads and bunting sway. Decor sits on layers 8 and 9 with camera cull distances of 70 m and 44 m. Small props cast no shadows. In the editor this took the scene from ~1,600 to ~750 effective draws, with about 65 SetPass calls.
- **UI.** All sprites are packed into `Generated/UIAtlas.spriteatlasv2`. HUD elements sit under a `SafeArea` root, which keeps them clear of notches and home bars.
- **GC.** Tweens are pooled per type, and customers, items and floating text come from `Pool`.
- **Web later.** `Core/Platform.cs` already reports `GameplayStart`/`GameplayStop` (popups, ads, settings) and `LoadingFinished`. A Poki or CrazyGames SDK bridge only needs to subscribe to those events and implement `IRewardedAdProvider`. The layout is portrait: desktop web will need a landscape HUD pass, and `CameraFollow` already widens the FOV for landscape.

## UI art pipeline

The UI uses the art in `Assets/Game/UI/Atlas1-3.png` plus a few hi-res pieces from the *2D Mobile Game UI Kit*. `Scripts/Editor/Gen/UIKit.cs` lists each sprite by a seed point (the pixel coordinate of the item in the atlas). `UIAtlasCutter` flood-fills the item by alpha and writes it to `Generated/UI/<name>.png` as a sprite, with 9-slice borders where needed. Buttons with baked text ("Play", "Start"...) are rebuilt as **blank** 9-slice buttons by repeating a text-free column, so they can hold any label.

To add a sprite:

1. Add a `P(atlas, "name", x, y)` line in `UIKit.Define()`.
2. Run **Build Everything**.
3. Use it with `UIKit.Get("name")` in the builder.

`UIKit.ContactSheet(path)` renders every cut sprite into a grid for review.

## Regenerating content

The scene, prefabs, materials, meshes, textures and animator are all generated from code. After you change the builder code, use one of these menu items:

- **Juice King ▸ Build Everything** regenerates art, reconfigures the model imports, and rebuilds prefabs and the scene.
- **Juice King ▸ Rebuild Scene (skip art + import)** skips the art and import steps, so it is faster.

Hand edits to the generated scene are overwritten on rebuild. Put layout changes in `Scripts/Editor/Builder/JuiceKingBuilder.Scene.cs`.

## Code map

| Path | What it holds |
|---|---|
| `Scripts/Runtime/Core/Items.cs` | Item types and **all balance numbers** (`Balance`): HP, slices, prices, juice time, upgrade costs |
| `Scripts/Runtime/Core` | GameManager (money, save, unlocks, offline earnings), Boosts (timed ad boosts), Ads (rewarded-ad entry point), Tweener, Pool, Sfx (procedural audio), Fx (particles), CameraFollow, NavBaker |
| `Scripts/Runtime/Items` | Carrier (swaying back/hand stack), ItemPile (grid piles), LooseItems (ground pickups), StackItem |
| `Scripts/Runtime/Stations` | FruitNode/FruitField, Juicer, Counter, CashPile, TrashBin |
| `Scripts/Runtime/Zones` | Floor pads: Drop, Pickup, Cash, Upgrade, Unlock, Trash, plus UnlockManager |
| `Scripts/Runtime/Actors` | Player, Chainsaw, Customer + CustomerManager (queue/payment), WorkerAI (farmer/waiter on NavMesh), CharacterAnim |
| `Scripts/Runtime/UI` | HUD (money, shop progress, flying coins), BoostBar, OfferPopup, AdOverlay, UnlockBanner, UpgradePanel, InputJoystick, Tutorial, OrderBubble, FloatingText, UIPress, UISpin |
| `Scripts/Runtime/Decor` | Ambient (wind, spinners, clouds, water), Wanderer (animals), Butterflies |
| `Scripts/Editor` | Builder: procedural textures and icons (`ArtGen`), meshes, materials, Kenney import, scene (`JuiceKingBuilder.Scene.cs`), environment dressing (`JuiceKingBuilder.Env.cs`) and UI construction |

## Credits

- 3D models: [Kenney](https://kenney.nl) (CC0). The packs used are Mini Characters, Mini Market, Survival Kit, Food Kit, Nature Kit and Furniture Kit. Licenses are in `Art/Kenney/*/License.txt`.
- UI: the project's own atlases in `Assets/Game/UI`, and hero coin/money-bag art from the *2D Mobile Game UI Kit* by 300Mind (Unity Asset Store, Standard EULA). Only its two sprite sheets are in `Assets/ThirdParty/300Mind`.
- Animals: chicken, dog and cat from [Animals FREE by ithappy](https://assetstore.unity.com/) (Unity Asset Store, Standard EULA), in `Assets/ThirdParty/ithappy`. Only the needed meshes, animations, controllers and texture were extracted. The package's own `Packages/manifest.json` was deliberately not imported.
- Font: Lilita One by Juan Montoreano (SIL Open Font License 1.1).
- The icons, UI sprites, textures, particle effects and sound effects are generated procedurally by the project's own code.
