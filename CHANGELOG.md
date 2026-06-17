# Changelog

All notable changes to RVRepairVan are documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/), and this
project adheres to Semantic Versioning.

## [2.0.1] - 2026-06-17

### Added
- Marco now shows a persistent "What can I bring to lower the price?" reminder once you
  have earned his trust (after the package favour). It explains that bringing packaged
  product lowers the bill, so players who skipped the dialogue still know how to keep
  progressing and afford the repair. It appears whenever you are not currently holding
  product to hand over (when you are, the "give a sample" action is shown instead).

## [2.0.0] - 2026-06-17

A ground-up rebuild on S1API with a full story questline, real items, save-bound
persistence and a repair cinematic.

### Added
- "Back on the Road" questline (default `QuestMode = Questline`): the RV is wrecked in
  the intro, then Donna points you to Mrs. Ming, who sends you on a dead-drop crate
  errand, who refers you to Marco the mechanic at the docks.
- Native S1API dialogue: real in-conversation choices and replies (no more floating
  worldspace-only bubbles).
- Referral pricing: Marco charges $50,000 up front, dropping to $10,000 once you tell
  him "Mrs. Ming sent me". Optional packaged-sample discounts cut the bill further,
  down to the repair-price floor.
- Real carried items in dead drops (Ming's Crate, Marco's Package) cloned from in-game
  items so they have a proper inventory icon and in-hand model. Lose one and the NPC
  charges a $500 recovery fee to continue.
- Repair cinematic when you pay: the screen fades to black, repair sounds play, Marco
  grunts as he works, then it fades back and he tells you it is done. Plays in both
  Questline and Simple mode.
- A wrench quest icon in the journal/HUD tracker.
- Settings: `QuestMode`, `BasePriceNoReferral`, `BasePriceWithReferral`,
  `MinSampleDiscount`, `MaxSampleDiscount` (plus the existing `Enabled`, `RepairPrice`).

### Changed
- Persistence is now bound to the game save (via the S1API save system): progress is
  written when the game saves and read on load, so it stays in sync with the world.
  Finishing the quest without saving (or after a crash) correctly reverts, instead of
  leaving the quest stuck or the RV unrepairable.
- "Simple" mode (`QuestMode = Simple`) repairs directly at Marco for the flat
  `RepairPrice` and now also plays the repair cinematic.
- Repair sounds route through the game audio mixer, so they respect the in-game volume
  settings.

### Fixed
- Delivered quest items now correctly leave the inventory.
- The quest reliably starts once the RV is actually wrecked and the intro has passed,
  and never flickers on an intact RV.
