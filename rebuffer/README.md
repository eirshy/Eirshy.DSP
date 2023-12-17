# Re-Buffer
Ever look at a smelter and think *"Why's this thing got 100 iron plates just sitting it it? I wish I could tell it to only keep like, 5... or 500."*?

Then this is the answer. ***Re*-Buffer** lets you *re*-define the buffer sizes on Assemblers, Chem Labs, *Re*-fineries, Particle Colliders, and *Re*-search labs- and possibly more in the future.

It does this by *re*-placing the highly-efficient constants in the game's original code with values loaded from a config file- and then over-paying the performance cost by *re*-writing and *re*-optimizing all of the code around it. Over all, your game should actually run at least as fast, if not *faster* by using this mod. At least, until the devs get around to *re*-writing these sections themselves.

Note that if you want to be able to tweak Miner and Pump buffers, you'll want my other mod, ***VeinityProject***. That one's potentially a lot more than just QoL, so I've split it off into a separate mod for purists.

## Changes
- v0.2.0 DarkFog Update; Included the PowerGenerator (Ray-Reciever, etc) support I wrote months ago.
- v0.1.4 Added triage component-disablers for in case I'm slow to update something. Updated with note about *VeinityProject*.
- v0.1.3 Fixed integer rounding issues causing us to misregister how much jello we eat on research.

## Mechanical changes compared to vanilla
As a note, part of the speed gain is by allowing certain mechanical edge-cases to just happen, as well as changing certain behaviors. Notably...
- ***Proliferator residue is not aggressively cleaned out of each slot.***<br />Mixing item types of different levels will still use the lowest level, but a single item kind with mixed levels won't "lose" the extra proliferator.
- ***Research Labs no longer "lose" spare hash byte upont tech completion or rate limiting.***
- ***Research Labs now prioritize keeping their own buffer satisfied before passing items up.***
- ***Research Labs may batch item send-ups instead of doing them one at a time.***<br />The batching cap is the same as the recipe's cost, so there's not much of a change with this mod by itself.

## Compatibility Notes
- ***MoreMegastructures** by JinxOAO* is NOT supported by the PowerGenerator component of this mod!
  - I'm currently wanting to actually play Dark Fog, and I didn't have the code for that already mostly-written.
  - Everything except for our PowerGenerator component works still, so just disable our PG component