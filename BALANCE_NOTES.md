# Balance notes — Juice King Tycoon

How the economy is meant to feel, what was measured, and what changed in the release balance pass.
All numbers live in `Assets/Game/Scripts/Runtime/Core/Economy.cs` (world-aware formulas and the upgrade trees) and
`Core/Items.cs` (`Balance`: per-fruit tables, world 0 classic formulas). Unlock prices are in the scene builders
(`JuiceKingBuilder.Scene.cs` / `Tropical.cs` / `Berry.cs`, the `Unlock(...)` / `TUnlock(...)` calls).

## Target curve

| Stage | Feel | Target |
|---|---|---|
| Early (first pads of a world) | fast: an unlock every 20–60 s | the player's own loop pays for it |
| Mid (helpers, delivery, bakery) | satisfying: a new machine or helper every 1–3 min | helpers start carrying part of the income |
| Late (expensive pads, premium trucks) | strategic: choose between upgrades and pads | helpers ≈ half the income |
| End (all pads, max upgrades) | completion: a visible "x / y" goal | upgrades multiply income ~7x, no wall |

Session length targets: Juice Farm ~40 min, Tropical Farm ~60–70 min, Berry Blast (flagship) ~90 min.

## Measurements (release pass)

Helper-only income (the player parked away from every pad), all pads of the world unlocked, 4x time scale,
~3.5 game minutes per run. Player activity comes on top of this.

| World | Upgrades | Income / min | Notes |
|---|---|---|---|
| Juice Farm | none | $85 | hands-on world: the player is the engine |
| Juice Farm | all maxed | $211 | |
| Tropical Farm | none | $2.3K | |
| Tropical Farm | all at level 3 | $10.4K | |
| Tropical Farm | all maxed | $49K | |
| Berry Blast | none | $23K | cakes + two desks + seven helpers |
| Berry Blast | all at level 3 | $71K | |
| Berry Blast | all maxed | $164K | |
| Tropical, first 6 pads | none | $0 | no farmer yet: all income is the player's |
| Berry Blast, first 10 pads | none | $232 | one farmer: still player-driven |

Upgrade-phase simulation (greedy cheapest upgrade, income interpolated from the table + 40% player share):
Tropical ≈ 34 min, Berry Blast ≈ 29 min before the pass → ≈ 41 min after the x1.4 Berry upgrade costs (same model, costs x1.4).
Income grows faster than costs, so there is no wall at the last levels.

### Totals per world

| | Pads | Upgrades | Pad cost | Upgrade cost |
|---|---|---|---|---|
| Juice Farm | 12 | 25 levels | $2.9K | $9.6K |
| Tropical Farm | 18 | 77 levels | $85.5K | $854K |
| Berry Blast | 28 | 101 levels | $392K | $5.69M |

## Changes in the release pass

### Rewarded cash no longer replaces playing

| | Before | After |
|---|---|---|
| FREE CASH bag | world 1: `300 + 650 × pads`, world 2: `1200 + 1800 × pads` (Berry Blast mid-game: **$17.4K per ad**, about two pads) | `Economy.FreeCash`: 1.5 min of measured income, never below a small floor (`40+25p` / `200+200p` / `400+300p`), never above 6 floors |
| FREE CASH cooldown | 120 s | 180 s |
| FINISH $X (ad on an unlock pad) | finishes up to **half of any pad** | at most one Free Cash bag (or 1/6 of the pad), never more than half; cooldown 60 → 150 s |
| Offline earnings | flat per-helper rate, 30 min cap | share of measured income (6% + 24% × helpers hired / helper slots, × Night Shift), cap 30 min / 1 h / 2 h per world |

The measured income (`GameManager.IncomePerMin`) is a 3-minute moving average of customer payments and truck
payouts. It is saved, so offline earnings use the player's real pace. Old saves without it fall back to the previous
formula once.

### Berry Blast is the longest world

- Pads from $7,000 up cost x1.3 (Blueberry Press $7K → $9K ... Premium Contracts $45K → $58.5K). Early pads are
  unchanged, so the opening stays fast.
- Upgrade base costs x1.4 (growth rates unchanged).
- Players mid-way keep everything they paid: a pad's paid amount is kept, and a pad already paid in full opens on the
  next step (`UnlockZone`).

### Decor pads now do something

Cozy Patio, Beach Cabana, Flower Gazebo and Picnic Garden each add **+10% to every sale** in their world
(`Economy.CharmMult`, "Charm: customers pay +10%" on the unlock banner). Before, they were cosmetic only.

### Tips scale with the price

A happy customer tips 5–15% of the order 30% of the time (it was a flat $1–3, meaningless after the first world).

### Fox raids are fair

- The fox never takes the last healthy farm (a raid with one open farm stopped every sale for 5 min). It waits for a
  second farm.
- 7 s warning (toast, yip, vibration) before every regular raid; the raid clock stops while a menu is open.
- Customers who wanted a raided berry switch to something available; the stock already on the counter still sells.

## Golden Apples and Ad Tickets

| | Value |
|---|---|
| Starting gift | 5 |
| Fox restore | 3 apples (≈ 5 min of a damaged patch) |
| Call truck now | 1 |
| Finish truck | 1 per 12 missing items |
| Berry Blast delivery | +1 (premium +2) |
| Final completion | +25 once |

Shop value per dollar rises 12% → 45% (apples) and 6% → 78% (tickets) from the smallest to the biggest pack; the
"+X% BONUS" labels match these real ratios.

## How to rebalance

1. Change the numbers (`Economy`, `Balance`, builder prices).
2. Rebuild the affected scene if a pad price changed.
3. Measure: the QA helper used for the table above sets a save with N pads and every upgrade at level L, parks the
   player, runs at 4x and reads `GameManager.SessionIncome` (counts every sale and truck payout since the scene
   started).
4. Never rename unlock or upgrade ids; lowering a price is safe (overpaid pads open), raising it is safe (paid amounts
   are kept).
