# Back on the Road - Exact Quest Script (technical trace)

The **precise, code-faithful** spec of the RVRepairVan questline (`QuestMode = Questline`), mirroring
`Quests/Questline.cs` 1:1 - stages, triggers, dialogue (text + show-condition + reply + effect), the price
tiers, the dead-drop pickups, the closing "check the RV" beat, and persistence. For traceability/debugging,
not for players (player narrative: `docs/QUESTLINE.md`).

**Source of truth:** `RVRepairVan/Quests/Questline.cs` (+ `RepairQuest.cs`, `RVManager.cs`,
`RepairStateStore.cs`, `Config/RVRepairVanPreferences.cs`). If this doc and the code disagree, the code wins.

Quest title (journal header): **"Back on the Road"**. Description: *"Your RV's wrecked. Someone in Hyland
Point has to know a guy."* Icon: a bundled wrench PNG (embedded resource `RVRepairVan.quest_icon.png`).

---

## 1. State machine

One persisted integer `Stage` drives both the objective AND the price tier (no separate referral/trust flags -
they're derived from the stage, since each is only reachable through its own action):

| Const | Value | Objective | Notes |
|---|--:|---|---|
| `None` | 0 | (no quest) | start / not started (reverts here if a completed run was never saved) |
| `Started` | 1 | Ask the motel manager about the RV | |
| `AskedDonna` | 2 | Talk to Mrs. Ming at the Chinese restaurant | |
| `MingErrand` | 3 | Pick up Ming's crate from the dead drop | errand accepted |
| `MingCrate` | 4 | Bring Ming's crate back to Mrs. Ming | crate collected |
| `Referred` | 5 | Talk to Marco at the body shop | back from Ming; may now drop Ming's name to Marco |
| `MarcoMet` | 6 | Tell Marco Mrs. Ming sent you | greeted Marco (got the $50k quote) |
| `ReadyToPay` | 7 | Pay Marco for the repair | said "Mrs. Ming sent me"; **`ReferralUsed`** -> price $10k |
| `Trusted` | 8 | Pay Marco for the repair | did Marco's pickup; **`Trusted_`** -> packaged samples |
| `Paid` | 9 | Check on the RV | paid, RV shell restored |
| `Done` | 10 | (complete) | checked the RV |

Derived: `MarcoGreeted = Stage>=6`, `ReferralUsed = Stage>=7`, `Trusted_ = Stage>=8`.
The Marco pickup (objectives "Pick up / Bring Marco's package") is an optional detour from `ReadyToPay`,
tracked by transient `_pickupActive` / `_hasPackage`.

## 2. Global gate `Active`

`Active = Enabled && RVManager.IsDestroyed()`. Every injected choice is gated on `Active`.

## 3. Start + completion (`ProximityCoroutine`, 1 Hz)

0. **Wait for load:** skip until `RepairSave.Loaded` (10s fallback), so we never act on stale/unloaded state. No
   desync reconciliation is needed - the state is save-bound (see section 7), hence always consistent with the RV.
1. **Auto-start:** `if (Enabled && Stage==None && !repaired && IsDestroyed() && ExplosionBeatPassed())` ->
   `Stage=Started`, create quest. Requiring `IsDestroyed()` (an actual wrecked RV) means an intact/repaired RV
   never spawns a flickering quest, and a fresh install on a post-explosion save (wrecked RV) still picks it up.
2. **Closing beat:** `if (Stage==Paid && near RV (<14m))` -> `Stage=Done`, complete the quest, Marco line
   *"There she is. Standing again. Interior's your problem. Try not to piss off whoever torched it the first
   time."* (Checked **before** the `if(!Active)` guard, because the RV is no longer destroyed after the repair.)

`ExplosionBeatPassed()` = `Quest.GetQuest("Getting Started").State in {Active,Completed}` OR
`Quest.GetQuest("Welcome to Hyland Point").State == Completed`.

## 4. Price (`CurrentPrice()`)

```
basePrice = ReferralUsed (Stage>=ReadyToPay) ? 10000 : 50000
price     = max(1500 floor, basePrice - DiscountTotal)
```
The "Repair my RV ($...)" choice text refreshes live via `RefreshRepairChoice()`.

## 5. Dialogue - exact choices

Player choice | shown when (`Active &&` ...) | reply | effect. "node" = in-dialogue S1API container;
"worldspace" = floating bubble (live price / dynamic / flavour).

### Donna (`donna_martin`)
| Choice | Shown | Reply | Effect |
|---|---|---|---|
| "My RV got blown up. Know anyone who can fix it?" | `Stage==Started` | node: *"Do I look like a mechanic, sweetheart? Go ask Mrs. Ming over at the Chinese place. She knows people."* | `Stage=AskedDonna` |

### Mrs. Ming (`ming`)
| Choice | Shown | Reply / sub-menu | Effect |
|---|---|---|---|
| "Donna said you might know someone who can fix my RV." | `Stage==AskedDonna` | node `rv_ming_offer`: *"Marco at the docks can fix almost anything. But favors move both ways. I have a crate waiting at a dead drop nearby. Bring it back, and I'll put in a word."* sub-choices: `MING_ACCEPT` "I'll grab it." -> *"Good. Pick it up, bring it here, and don't open it."*; `MING_DEFER` "Not right now." -> *"Then your RV can stay where it is."* | `MING_ACCEPT` -> `OnAcceptErrand` |
| (sub) `MING_ACCEPT` | in the offer | (accepted node) | `if Stage==AskedDonna`: `Stage=MingErrand`, **reserve a real dead drop** near Ming -> `_crateDrop`/`_cratePoint`, **place item** `rv_ming_crate` in it (`_cratePlaced`) |
| "Here's your crate." | `Stage==MingCrate && (!_cratePlaced || holding rv_ming_crate)` | node `rv_ming_deliver`: *"Good. Go see Marco at the body shop down by the docks. Tell him Mrs. Ming sent you."* | `OnDeliverCrate`: remove the carried crate, `Stage=Referred` |
| "I lost your crate." | `Stage==MingCrate && _cratePlaced && !holding rv_ming_crate` | node `rv_ming_lost` (ENTRY=`MingAngry`): sub-choices `MING_PAY` "Pay $500" -> (no node); `MING_DEFER_LOSS` "I'll get the money." -> node `MING_LOSS_DEFER` (`MingShort`) | `MING_PAY` -> `OnMingPayLoss` |
| (sub) `MING_PAY` | in `rv_ming_lost` | - | `OnMingPayLoss`: cash<500 -> worldspace `MingShort`; else charge $500, `Stage=Referred`, worldspace `MingPaid` |

### Marco (`marco_baron`)
| Choice | Shown | Reply | Effect |
|---|---|---|---|
| "Can you fix my RV?" | `Stage==Referred` | worldspace: *"Yeah, I can fix it. Fifty grand."* | `Stage=MarcoMet` |
| "Fifty grand?" | `Stage==MarcoMet` | worldspace: *"You brought me a burnt-out shell. That's not a repair, that's a resurrection."* | (flavour, none) |
| "Mrs. Ming sent me." | `Stage==MarcoMet` | worldspace: *"Mrs. Ming sent you? Yeah, alright. Should've opened with that. Ten grand."* | `Stage=ReadyToPay` (price 50k->10k) |
| "Repair my RV ($X)" | `MarcoGreeted && Stage<Paid` | worldspace (dynamic) | `OnMarcoRepair` (below) |
| "Anything I can do to bring the price down?" | `Stage==ReadyToPay && !_pickupActive` | node `rv_marco_favour`: *"Maybe. I left a package at a dead drop nearby. Pick it up, bring it back, and don't make it weird."* | `_pickupActive=true`; **reserve a real dead drop** near Marco -> `_drop`/`_dropPoint`, **place item** `rv_marco_package` (`_pkgPlaced`) |
| "Got your package." | `_pickupActive && _hasPackage && (!_pkgPlaced || holding rv_marco_package)` | node `rv_marco_gotpkg`: *"Good. You can follow instructions. Bring me some of that good stuff now and then, and I'll keep shaving down the bill."* | `OnGotPackage`: remove the carried package, `_pickupActive=false`, `Stage=Trusted` |
| "I lost your package." | `_pickupActive && _hasPackage && _pkgPlaced && !holding rv_marco_package` | node `rv_marco_lost` (ENTRY=`MarcoAngry`): sub-choices `MARCO_PAY` "Pay $500" -> (no node); `MARCO_DEFER_LOSS` "I'll get the money." -> node `MARCO_LOSS_DEFER` (`MarcoShort`) | `MARCO_PAY` -> `OnMarcoPayLoss` |
| (sub) `MARCO_PAY` | in `rv_marco_lost` | - | `OnMarcoPayLoss`: cash<500 -> worldspace `MarcoShort`; else charge $500, `_pickupActive=false`, `Stage=Trusted`, worldspace `MarcoPaid` |
| "Give Marco a packaged sample" | `Trusted_ && Stage<Paid && HoldingPackaged() && CurrentPrice()>floor` | worldspace (dynamic) | `OnGiveSample` (below) |
| "What can I bring to lower the price?" | `Trusted_ && Stage<Paid && !HoldingPackaged() && CurrentPrice()>floor` | node `rv_marco_bring`: *"Bring me packaged product - sealed stuff, not raw. Every piece I take knocks its value off the bill, up to five hundred a pop, right down to my floor."* | (info reminder, none) |

**`OnMarcoRepair`:** cash < price -> *"You're short. Come back when you've got the cash."* | else: charge `price`
synchronously, then **play the repair cinematic** (`Effects/RepairCinematic.Play`): fade to black (game
`BlackOverlay`), lock input, hold ~2.5s while a bundled repair sound (`Assets/repair.wav`, synthesised clanks +
drill, embedded + decoded to an `AudioClip` at runtime) plays, do `RVManager.Repair()` + `repaired=true` +
`Stage=Paid` WHILE black, fade back, unlock, then Marco's line *"There she is - back from the dead..."*. Objective ->
"Check on the RV".

**`OnGiveSample`:** held item must be a `ProductItemInstance` with `AppliedPackaging != null` - else *"That
ain't packaged. Hand me something sealed."* Discount = `clamp(round(GetMonetaryValue()), 100, 500)`. Marco takes
and consumes it like a free sample via `marco.Behaviour.ConsumeProductBehaviour.SendProduct(product,
removeFromInventory:FALSE)` (runs the consume animation/effects) and we then remove **exactly one** unit ourselves
via `RemoveOneFromSlot(equippedSlot)` (`ChangeQuantity(-1)`, or `ClearStoredInstance` for the last unit). Why false + manual: `SendProduct`'s native removal quantity is unverifiable
(body is in `GameAssembly.dll`) and could wipe a whole stack > 1; removing one deterministically protects the
player's stack. A before/after equipped-quantity line is logged (`expected -1`). Then `Samples++`,
`DiscountTotal += discount`; *"Appreciate it. Knocked $D off the bill."* The choice only appears while you're
actually **holding** packaged product (`HoldingPackaged()` peek).

## 6. Quest items + dead-drop pickups (`ProximityCoroutine`, while `Active`)

Two custom items are registered once (`EnsureItems`, lazy on first placement). Each is **cloned from an existing
carry-only storable item** so it inherits a real inventory icon + in-hand model + world mesh (a code-built item
with no model shows a blank icon, nothing in-hand, and risks an NRE on equip). `RegisterItem` tries a small list of
explicit candidate base IDs in order - crate: `grainbag` (burlap sack), `trashbag`, `flashlight`; package:
`trashbag`, `grainbag`, `flashlight` - via
`ItemCreator.CloneFrom(baseId).WithBasicInfo(id, name, desc, ItemCategory.Tools).WithStackLimit(1).Build()`
(`Build()` auto-registers). `CloneFrom` throws if a base isn't a `StorableItemDefinition`, so the loop skips to the
next candidate; if none clone, it falls back to a plain model-less `CreateItem(...)` so registration never fails
(logged as a warning). Bases are deliberately carry items (not furniture) so the player carries, not deploys, them.
`PreserveRuntimeItem` keeps it across save/reload. DEBUG builds also emit a one-time `[ITEMDUMP]` of every item
id/name/category/hasIcon to the log. Items:

| ID | Name | Placed in |
|---|---|---|
| `rv_ming_crate` | "Ming's Crate" | Ming's reserved drop (`_crateDrop`), on accepting the errand |
| `rv_marco_package` | "Marco's Package" | Marco's reserved drop (`_drop`), on the "bring the price down" favour |

Placement: `drop.Storage.AddItem(ItemManager.GetDefinition(id).CreateInstance(1))`. `_cratePlaced`/`_pkgPlaced`
record whether the real item went in (drops can fail -> proximity fallback).

Collection (the player took the item out of the drop's storage):
- **Ming's crate** (`Stage==MingErrand`): if `_cratePlaced` -> `Stage=MingCrate` when `_crateDrop.IsEmpty`;
  else proximity within 5 m of `_cratePoint`. Re-reserves + re-places after a reload (lost `_cratePoint`).
- **Marco's package** (`_pickupActive && !_hasPackage`): if `_pkgPlaced` -> `_hasPackage=true` when
  `_drop.IsEmpty`; else proximity within 5 m of `_dropPoint`.

Holding check (`PlayerHasItem`/`RemovePlayerItem`): scan `PlayerInventory.hotbarSlots`, match
`((BaseItemInstance)slot.ItemInstance).ID`; remove one via `RemoveOneFromSlot` - `ChangeQuantity(-1)` if qty > 1,
else `ItemSlot.ClearStoredInstance(false)` for the last unit (`ChangeQuantity(-1)` on a qty-1 item leaves an empty
ghost slot - the item never disappears). The sample consume uses the same helper.

Dead drop = farthest of `DeadDropManager.Empty` (fallback `.All`), or the **nearest** under `#if DEBUG` for
quick testing. If no drop is found, a fixed offset near the NPC is used and the item simply isn't placed
(`_cratePlaced`/`_pkgPlaced` false -> proximity reach counts as the pickup).

**Loss fee:** `LostPackageFee = 500`. If the player admits losing a delivered item, the NPC's loss node
(`rv_ming_lost` / `rv_marco_lost`) lets them pay $500 (via `S1API.Money.Money.ChangeCashBalance`) to advance,
or defer. Lines: `MingAngry`/`MingPaid`/`MingShort`, `MarcoAngry`/`MarcoPaid`/`MarcoShort`.

## 7. Persistence (save-bound, via S1API `Saveable`)

State (`repaired` bool, `stage`/`samples`/`discount` ints) lives in `RepairSave : S1API.Internal.Abstraction.Saveable`
with `[SaveableField]` fields. S1API writes them ONLY during the game's save pipeline and reads them on load, so the
state lives **inside the game save** and reverts in lockstep: completing the quest but not saving (or a crash)
reloads to the last-saved state, so the quest comes back instead of being stuck "done". `RepairStateStore` is a thin
in-memory accessor over `RepairSave.Instance` (no immediate disk writes). There is NO desync reconciliation - the
state is consistent with the game's RV/quest world by construction.

Load lifecycle: `Core.OnSceneWasLoaded` calls `RepairSave.BeginLoad()` (marks not-loaded + zeroes fields - the
instance is a process-wide singleton, so this stops one save's state leaking into the next). Then S1API calls
`OnLoaded` (save had mod data) or `OnCreated` (fresh save / no mod data) - both set `Loaded=true`. The
`ProximityCoroutine` (auto-start) and `RestoreRepairCoroutine` both wait for `Loaded` (10s fallback). On reload the
game ALWAYS spawns the RV wrecked; if `repaired` is set, `RestoreRepairCoroutine` re-applies the repair for free.
Log proof the pipeline engaged: `[State] loaded:` / `[State] created (fresh save)` / `[State] saved:`.

## 8. Player path (one line)

RV blows up -> "Getting Started" begins -> quest auto-starts -> **Donna** -> **Ming** (accept errand) ->
crate dead drop -> back to **Ming** (`Referred`) -> **Marco** ("Can you fix my RV?" -> $50k) ->
**"Mrs. Ming sent me."** (`ReadyToPay`, $10k) -> (optional) "bring the price down" -> package dead drop ->
back to Marco (`Trusted`) -> (optional) packaged samples (-value, clamped 100..500, floor 1500) ->
"Repair my RV ($X)" -> pay (`Paid`) -> check the RV (`Done`, complete).

## 9. Simple mode + settings

`QuestMode = Simple` skips this questline entirely (Marco repairs directly at the flat `RepairPrice`, default
$1,500). Settings (`RVRepairVan_01_Main`): `Enabled`, `QuestMode`, `RepairPrice` (1500), `BasePriceNoReferral`
(50000), `BasePriceWithReferral` (10000), `MinSampleDiscount` (100), `MaxSampleDiscount` (500). Debug-only:
`DestroyRvDebug`, `AddCashDebug`, `DumpRvStateDebug`, `TestRepairCinematicDebug` (plays the fade+sound where you
stand, no need to reach Marco).
