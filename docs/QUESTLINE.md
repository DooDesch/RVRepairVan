# Back on the Road - RV Repair Questline

A reference for the RV-repair side quest added by **RVRepairVan**. Keep it handy if you get stuck on where to
go next. *(Technical trace for modders: `docs/QUEST-SCRIPT.md`.)*

## Two ways to repair (`QuestMode` setting)

- **Questline** (default) - the full story below: Donna -> Ming -> Marco. The price starts high, but you can
  bring it right back down through favors and packaged product.
- **Simple** - no story. As soon as the RV is destroyed, Marco at the docks repairs it for a flat price
  (`RepairPrice`, default **$1,500**).

## When it starts

During the opening, your RV is blown up; you report it to Uncle Nelson by payphone and **"Getting Started"**
sends you to the motel. The quest **"Back on the Road"** appears the moment **"Getting Started"** begins,
pointing you to the **motel manager** - exactly where the game is already sending you. *(Uncle Nelson's report
is a payphone call, so there's no in-person Nelson step.)*

## The path at a glance

```
RV blown up -> reported to Uncle Nelson (payphone) -> "Getting Started" begins -> "Back on the Road" starts
   1. Ask the motel manager (Donna) about the RV      -> she points you to Mrs. Ming
   2. Talk to Mrs. Ming at the Chinese restaurant      -> she offers an errand (accept now, or later)
   3. Pick up Ming's crate from the dead drop          -> a real item you carry; no cash involved
   4. Bring Ming's crate back to Mrs. Ming             -> she refers you to Marco (lost it? owe $500)
   5. Talk to Marco at the body shop                   -> "Can you fix my RV?" -> $50,000
   6. Tell Marco "Mrs. Ming sent me."                  -> price drops to $10,000
   7. (optional) "bring the price down" -> Marco's dead-drop pickup -> unlocks packaged samples
      (optional) give packaged samples                -> each cuts the price by its value ($100-$500)
   8. Pay Marco for the repair                         -> RV shell restored
   9. Check on the RV                                  -> quest complete
```

## Step by step

*(Player choices in **bold**; NPC replies in italics. These are the exact in-game lines.)*

### 1. Donna - the motel manager
- **You:** **"My RV got blown up. Know anyone who can fix it?"**
- **Donna:** *"Do I look like a mechanic, sweetheart? Go ask Mrs. Ming over at the Chinese place. She knows people."*
- **Next:** *Talk to Mrs. Ming at the Chinese restaurant.*

### 2. Mrs. Ming - the errand offer
- **Where:** Mrs. Ming, at the Chinese restaurant (Northtown).
- **You:** **"Donna said you might know someone who can fix my RV."**
- **Ming:** *"Marco at the docks can fix almost anything. But favors move both ways. I have a crate waiting at a
  dead drop nearby. Bring it back, and I'll put in a word."*
- **Your call (right in the dialogue):** **"I'll grab it."** -> *"Good. Pick it up, bring it here, and don't
  open it."*  |  or **"Not right now."** -> *"Then your RV can stay where it is."* (the offer stays open).
- **No cash changes hands** - the crate is already paid for.

### 3. Ming's dead drop
- Follow the quest marker to a real **dead drop** near Ming and take **Ming's Crate** out of it (it's a real
  item you now carry in your inventory), then head back to Ming. The drop is the **farthest** available one
  (in debug builds: the nearest, for quick testing).

### 4. Back to Ming
- **You:** **"Here's your crate."** (only while you're actually holding it)
- **Ming:** *"Good. Go see Marco at the body shop down by the docks. Tell him Mrs. Ming sent you."*
- **Lost it?** If you no longer have the crate, the option becomes **"I lost your crate."**
  - **Ming:** *"You lost it? I don't lose things, and people who lose my things lose teeth. Five hundred buys
    you both back. Now."*
  - **[Pay $500]** -> *"Smart. We're square. Now go see Marco at the body shop down by the docks, and tell him
    Mrs. Ming sent you."* (you're referred, same as handing over the crate).
  - **[I'll get the money.]** -> *"Then don't come back until your hands are full."* (come back when you can pay).
  - Short on cash when you pick Pay: same *"Then don't come back..."* brush-off until you can cover it.

### 5. Marco - first contact (the full price)
- **Where:** Marco, at the body shop / auto shop in the Docks.
- **You:** **"Can you fix my RV?"**
- **Marco:** *"Yeah, I can fix it. Fifty grand."*
- *(Flavor:* **"Fifty grand?"** *->* *"You brought me a burnt-out shell. That's not a repair, that's a resurrection."*)
- You *can* just pay the full **"Repair my RV ($50,000)"** right here - or knock the price down below.

### 6. Drop Mrs. Ming's name ($50k -> $10k)
- **You:** **"Mrs. Ming sent me."**
- **Marco:** *"Mrs. Ming sent you? Yeah, alright. Should've opened with that. Ten grand."*
- The repair option updates to **"Repair my RV ($10,000)"**.

### 7. (optional) Earn Marco's trust + free samples
- Pick **"Anything I can do to bring the price down?"**
- **Marco:** *"Maybe. I left a package at a dead drop nearby. Pick it up, bring it back, and don't make it weird."*
- Follow the marker to the dead drop, take **Marco's Package** (a real item you carry), then return and pick
  **"Got your package."**
- **Marco:** *"Good. You can follow instructions. Bring me some of that good stuff now and then, and I'll keep shaving down the bill."*
- **Lost it?** If you no longer have the package, the option becomes **"I lost your package."**
  - **Marco:** *"You did what? You walk in here empty-handed and waste my time. Five hundred, or the next thing
    that goes missing is you."*
  - **[Pay $500]** -> *"Good. Mess like that gets forgotten when the cash shows up. Bring me some of that good
    stuff now and then, and I'll keep shaving down the bill."* (trust earned, same as delivering it).
  - **[I'll get the money.]** -> *"Clock's running. Come back with it."* (and again if you're short when you
    pick Pay).
- Now, while **holding a PACKAGED product**, pick **"Give Marco a packaged sample."** Marco actually takes it and
  consumes it on the spot (just like handing a free sample to anyone in town). Each accepted sample cuts the price
  by **its own value, clamped to $100-$500** (down to the $1,500 floor).
  - Raw / unpackaged / nothing -> *"That ain't packaged. Hand me something sealed."*
  - Accepted -> *"Appreciate it. Knocked $X off the bill."*

### 8. Pay + check the RV
- Pick **"Repair my RV ($X)"** and pay. (Short on cash: *"You're short. Come back when you've got the cash."*)
- The screen fades to black, you hear Marco work on it for a few seconds, then it fades back - the RV shell is
  restored. **Marco:** *"There she is - back from the dead. Go take a look, and try not to total her again."*
- **Next:** *Check on the RV.* Head back to it; when you reach it, the quest completes:
  *"There she is. Standing again. Interior's your problem. Try not to piss off whoever torched it the first time."*
- The repair stays fixed across reloads. *(Only the shell is restored - looted starter gear does not come back.)*

## Price summary

| Situation | Price |
|---|---|
| Simple mode | $1,500 (flat) |
| Questline, before saying "Mrs. Ming sent me" (incl. straight to Marco) | $50,000 |
| Questline, after dropping Ming's name | $10,000 |
| Each PACKAGED free sample (after earning Marco's trust) | -(its value, clamped $100..$500) |
| Lost Ming's crate / Marco's package (to continue) | $500 each |
| Minimum possible price | $1,500 (the `RepairPrice` floor) |

## Stuck? The quick path
Donna -> Ming (accept the errand) -> crate dead drop -> back to Ming -> Marco ("Can you fix my RV?") ->
**"Mrs. Ming sent me."** [$50k -> $10k] -> (optional: "bring the price down" -> dead drop -> packaged samples)
-> **"Repair my RV"** -> check the RV -> done.
