# RVRepairVan - Repair Your RV

> 🛟 **Need help or found a bug?** Get support at [support.doodesch.de](https://support.doodesch.de).

> Your RV got wrecked? Get it fixed. Talk to Ming to pick up the job, head to Marco the mechanic, pay the bill, and your RV is back on the road - guided by a proper tracked quest with journal and HUD.

![Version](https://img.shields.io/badge/version-2.0.0-blue)
![Game](https://img.shields.io/badge/game-Schedule%20I-purple)
![MelonLoader](https://img.shields.io/badge/MelonLoader-0.7.3+-green)
![S1API](https://img.shields.io/badge/S1API-required-orange)
![Status](https://img.shields.io/badge/status-working-brightgreen)

## Features

- Repair your destroyed RV by talking to **Marco** the mechanic - he charges a configurable fee and restores the wrecked van to working order.
- A guided **quest**: **Ming** hands you the job ("My RV got wrecked - what should I do?"), the objective points you to Marco, and finishing the repair completes the quest. Shows up in the journal/phone and the on-screen HUD tracker (built on [S1API](https://github.com/ifBars/S1API)).
- **Configurable repair price** (default `$1,500`), read live - change it in settings and Marco's offer updates without a restart.
- **Per-save persistence**: a repaired RV stays repaired across reloads, scoped to the individual save game.
- Pays from your **cash** balance and only offers the repair while the RV is actually destroyed.
- **Debug helpers** for testing without waiting for the story event: a one-shot *Destroy RV* toggle and a one-shot *Add $10,000* toggle.
- Settings are exposed through the **Mod Manager & Phone App** UI (in-game) or `UserData/MelonPreferences.cfg`.

## Requirements

| Component | Version / Source |
|-----------|------------------|
| Schedule I | `0.4.5f2` (IL2CPP, current Steam public build) |
| MelonLoader | `0.7.3+` |
| S1API | [ifBars/S1API_Forked](https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/) `3.0.22+` (quest/money layer) |
| Mod Manager & Phone App | [Prowiler, Nexus mods/397](https://www.nexusmods.com/schedule1/mods/397) - for the in-game settings UI |

## Installation

### Recommended: Thunderstore mod manager

Install through a Thunderstore mod manager such as [r2modman](https://thunderstore.io/package/ebkr/r2modman/) or [Gale](https://thunderstore.io/package/Kesomannen/GaleModManager/). It resolves the MelonLoader and S1API dependencies automatically. Install **Mod Manager & Phone App** from Nexus for the in-game settings UI.

### Manual

1. Install [MelonLoader 0.7.3](https://melonwiki.xyz/#/) into Schedule I.
2. Install [S1API](https://thunderstore.io/c/schedule-i/p/ifBars/S1API_Forked/) (extract its `Mods/` and `Plugins/` into your `Schedule I` folder).
3. (Recommended) Install [Mod Manager & Phone App](https://www.nexusmods.com/schedule1/mods/397).
4. Download the latest `RVRepairVan.dll` from the [releases page](../../releases) and drop it into `Schedule I/Mods/`.
5. Launch the game once to generate the config at `UserData/MelonPreferences.cfg`.

## Configuration

Stored in `UserData/MelonPreferences.cfg` under the `[RVRepairVan_01_Main]` category (display name **RV Repair Van**). Also editable in-game via the Mod Manager & Phone App.

| Option | Description | Default | Values |
|--------|-------------|---------|--------|
| `Enabled` | Enable the RV repair feature. | `true` | `true` / `false` |
| `RepairPrice` | Cash Marco charges to repair the RV. Applied live - Marco's offer updates on save. | `1500` | any non-negative integer |
| `DestroyRvDebug` | One-shot debug: wreck the RV so you can test the repair without the story event. Auto-resets to off. | `false` | `true` / `false` |
| `AddCashDebug` | One-shot debug: give yourself `$10,000` (only while a save is loaded). Auto-resets to off. | `false` | `true` / `false` |

## Usage

1. When your RV is destroyed, talk to **Ming** and pick *"My RV got wrecked - what should I do?"* to start the **Repair the RV** quest.
2. Follow the quest marker to **Marco** the mechanic and choose *"Repair my RV ($…)"*.
3. The fee is deducted from your cash and the RV is restored; the quest completes. The repair persists across reloads for that save.

To test quickly: toggle `AddCashDebug` for funds and `DestroyRvDebug` to wreck the RV on demand.

## Compatibility

Built for Schedule I `0.4.5f2` (IL2CPP) / MelonLoader `0.7.3`. A Mono build (`alternate` Steam branch) is planned. Disable other RV-repair mods (e.g. PrimoBuddy's *RV Repair*) to avoid duplicate Marco dialogue.

## Building (developers)

```
dotnet build -c Release
```

References are resolved from a sibling `../Workspace/lib/<backend>/` folder (game + MelonLoader + S1API assemblies); the backend (`il2cpp` / `mono`) is derived from the active TargetFramework. The PostBuild step copies `RVRepairVan.dll` into the game's `Mods/`. The project is structured after [Mimesis-InventoryExpansion](https://github.com/DooDesch/Mimesis-InventoryExpansion).

## Credits / License

Author: **DooDesch**. Built on [S1API](https://github.com/ifBars/S1API) (KaBooMa, ifBars & contributors) and integrates with **Mod Manager & Phone App** (Prowiler). Provided as-is under the MIT License. Contributions welcome via pull requests on the [repository](https://github.com/DooDesch/RVRepairVan).
