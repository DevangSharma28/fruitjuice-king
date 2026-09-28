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
   - more oranges, then the upgrade shop and the watermelon field
   - the watermelon juicer
   - a waiter and more melons
   - the pineapple field, then the pineapple juicer
   - a patio
   - three farmers
7. The **upgrade shop** sells chainsaw power, backpack size and move speed.

Progress (money, unlocks, upgrades and tutorial step) is saved to PlayerPrefs.

## Regenerating content

The scene, prefabs, materials, meshes, textures and animator are all generated from code. After you change the builder code, use one of these menu items:

- **Juice King ▸ Build Everything** regenerates art, reconfigures the model imports, and rebuilds prefabs and the scene.
- **Juice King ▸ Rebuild Scene (skip art + import)** skips the art and import steps, so it is faster.

Hand edits to the generated scene are overwritten on rebuild. Put layout changes in `Scripts/Editor/Builder/JuiceKingBuilder.Scene.cs`.

## Code map

| Path | What it holds |
|---|---|
| `Scripts/Runtime/Core/Items.cs` | Item types and **all balance numbers** (`Balance`): HP, slices, prices, juice time, upgrade costs |
| `Scripts/Runtime/Core` | GameManager (money, save, unlocks), Tweener, Pool, Sfx (procedural audio), Fx (particles), CameraFollow, NavBaker |
| `Scripts/Runtime/Items` | Carrier (swaying back/hand stack), ItemPile (grid piles), LooseItems (ground pickups), StackItem |
| `Scripts/Runtime/Stations` | FruitNode/FruitField, Juicer, Counter, CashPile |
| `Scripts/Runtime/Zones` | Floor pads: Drop, Pickup, Cash, Upgrade, Unlock, plus UnlockManager |
| `Scripts/Runtime/Actors` | Player, Chainsaw, Customer + CustomerManager (queue/payment), WorkerAI (farmer/waiter on NavMesh), CharacterAnim |
| `Scripts/Runtime/UI` | HUD, UpgradePanel, InputJoystick, Tutorial, OrderBubble, FloatingText |
| `Scripts/Editor` | Builder: procedural textures (`ArtGen`), meshes, materials, Kenney import, scene and UI construction |

## Credits

- 3D models: [Kenney](https://kenney.nl) (CC0). The packs used are Mini Characters, Mini Market, Survival Kit, Food Kit, Nature Kit and Furniture Kit. Licenses are in `Art/Kenney/*/License.txt`.
- Font: Lilita One by Juan Montoreano (SIL Open Font License 1.1).
- The icons, UI sprites, textures, particle effects and sound effects are generated procedurally by the project's own code.
