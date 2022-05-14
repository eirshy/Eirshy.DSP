# Re-Buffer
Ever look at a smelter and think *"Why's this thing got 100 iron plates just sitting it it? I wish I could tell it to only keep like, 5."*?

Then this is the answer. ***Re*-Buffer** lets you re-define the buffer sizes on Assemblers, Chem Labs, *Re*-fineries, Particle Colliders, and *Re*-search labs- and possibly more in the future (probably miners).

It does this by *re*-placing the highly-efficient constants in the game's original code with values loaded from a config file- and then over-paying the performance cost by *re*-writing and *re*-optimizing all of the code around it. Over all, your game should actually run at least as fast, if not *faster* by using this mod. At least, until the devs get around to *re*-writing these sections themselves.

## Changes

- v0.1.3<br />Fixed integer rounding issues causing us to misregister how much jello we eat on research.
- v0.1.2<br />Removed some on-further-consideration-unnecessary thread guards from non-collapsed labs.
- v0.1.1<br />Fixed the readme and added the missing BepInEX dep.

## Mechanical changes compared to vanilla
As a note, part of the speed gain is by allowing certain mechanical edge-cases to just happen, as well as changing certain behaviors. Notably...
- ***Proliferator residue is not aggressively cleaned out of each slot.***<br />Mixing item types of different levels will still use the lowest level, but a single item kind with mixed levels won't "lose" the extra proliferator.
- ***Research Labs no longer "lose" spare hash byte upont tech completion or rate limiting.***
- ***Research Labs now prioritize keeping their own buffer satisfied before passing items up.***
- ***Research Labs may batch item send-ups instead of doing them one at a time.***<br />The batching cap is the same as the recipe's cost, so there's not much of a change with this mod by itself.

## RythmnKit Addon -- Collapse Lab Towers
As I'm sure you've noticed, lab towers are kind of silly, passing materials up and then passing jello back down. As an optional additional optimization, you can choose to have all of the labs on top of the base lab simply increase the base lab's speed rather than act as separate labs.

Upon loading a save with this setting enabled (and RythmnKit installed), we'll forcibly transfer all items back down to the base of the tower, and internally mark all of the higher labs as the base lab's speed-boosting hat. This trades the expensive Active-Lab code for massively cheaper Hat-Maintenance code- all without making your save game incompatible with vanilla.

## Wait, the heck's a *RythmnKit*?
RythmnKit is a modding toolkit that offers protos-modding, game-load-event, and game-save-event helpers. After all, tick-speed code is difficult to write and, due to manual optimization, even more difficult to maintain. Inspired by Factorio's modding structure, it aims to handle all of the complicated parts of a proto mod (like syncing the protoDesc into the save file) for you, while also providing a lot of convenience for other mods that need to handle entity components and *re*-interpret save files.

It also might not yet be *re*-leased. If it isn't, please look forward to it.
