# Re-Buffer
Ever look at a smelter and think *"Why's this thing got 100 iron plates just sitting it it? I wish I could tell it to only keep like, 5... or 500."*?

Then this is the answer. ***Re*-Buffer** lets you *re*-define the buffer sizes on Assemblers, Chem Labs, *Re*-fineries, Particle Colliders, and *Re*-search labs- and possibly more in the future.

It does this by *re*-placing the highly-efficient constants in the game's original code with values loaded from a config file- and then over-paying the performance cost by *re*-writing and *re*-optimizing all of the code around it. Over all, your game should actually run at least as fast, if not *faster* by using this mod. At least, until the devs get around to *re*-writing these sections themselves.

Note that if you want to be able to tweak Miner and Pump buffers, you'll want my other mod, ***VeinityProject***. That one's potentially a lot more than just QoL, so I've split it off into a separate mod for purists.

### NOTE On Settings
By default we drop the output buffers to 5x iterations of the recipe. This is generally a very good thing- less materials sitting around converted into potentially the wrong item, 
less you have to deal with temp storage when rebuilding something, etc. However it might be bothersome in the super early game if you aren't hooking up things to chests for your 
handcrafting mini-malls. If this is a problem for you, I'd recommmend either cranking the output multiplier up (50 is likely around what you'd want), or just leaving this mod off 
until you're out of that phase. The performance gains this mod provides are rather unimportant until you're on at least two planets anyways.

Also note, output multipliers limit the buffer to nx +1, *not* to nx. So a multiplier of 5 on a recipe that produces 1 item will stop at 6 items in the output buffer, while a recipe
that produces 3 items will stop at 16 items in the output buffer. This is caused by a guard against a dirty array read I've left in to prevent an empty output buffer from being able
to ever be considered "full" without doing a second check.

## Changes
- v0.2.5 Increased the max buffer multiplier to 500 so you can actually do what the first line of this readme says.
- v0.2.4 Fixed null reference error on the lab component (oops lol)
- v0.2.3 Separated readers from writers on Labs, fixing the race condition.
- v0.2.0 DarkFog Update; Included the PowerGenerator (Ray-Reciever, etc) support I wrote months ago.
- v0.1.4 Added triage component-disablers for in case I'm slow to update something. Updated with note about *VeinityProject*.

## Mechanical changes compared to vanilla
As a note, part of the speed gain is by allowing certain mechanical edge-cases to just happen, as well as changing certain behaviors. Notably...
- ***Proliferator residue is not aggressively cleaned out of each slot.***<br />Mixing item types of different levels will still use the lowest level, but a single item kind with mixed levels won't "lose" the extra proliferator.
- ***Research Labs can no longer consume items they recieved from pass-up on the same tick they got the items.***
- ***Research Labs no longer "lose" spare hash byte upon tech completion or rate limiting.***
- ***Research Labs now prioritize keeping their own buffer satisfied before passing items up.***
- ***Research Labs may batch item send-ups instead of doing them one at a time.***<br />The batching cap is the same as the recipe's cost, so there's not much of a change with this mod by itself.

## Compatibility Notes
- ***MoreMegastructures** by JinxOAO* is NOT supported by the PowerGenerator component of this mod!
  - I'm currently wanting to actually play Dark Fog, and I didn't have the code for that already mostly-written.
  - Everything except for our PowerGenerator component works still, so just disable our PG component