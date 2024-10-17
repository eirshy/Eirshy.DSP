# Lazy Outposting
Man, I need MORE iron ore? Fiiine I'll go find a planet for it... After this drink.

Allows your mining facilities to apply to every vein on a planet (or just veins a bit wider than their normal scan radius
, if you don't want to cheat too much), and your ocean extractors to work regardless of if you put their feed pipe in some
water when you first built them.

Additionally can outfit your contracted mining dwarfves with buckets so they can fetch oil for you....
but only if you also install my other mod, ***VeinityProject***.

All options are toggleable, so you can freely decide just how lazy you want to be.

### But didn't you also optimize some of the code? What if I just want that!

Each config section with more than one option will have an off-by-default "Optimizations Only" setting.
If you turn that on, we'll run as close to vanilla as possible, but without, for example, thrashing over 8kb of memory every
17 miliseconds because you're hovering a Miner mk2 over nothing.

## Controls
#### HAULERS
- Press *PageUp* to cycle forwards in the hauler resource list.
- Press *PageDown* to cycle backwards in the hauler resource list.

## Changes
- v1.4.5 Fix for Genesis Book and most other EVeinType editors
- v1.4.4 Fix for game version 0.10.30.23310
- v1.4.3 Fix for miner prebuilds not getting the memo about vein depletion.
- v1.4.2 Fix for rebuilding miners, more probably
- v1.4.1 Fix for rebuilding miners, probably
- v1.3.5 Fix for miners (broke last version lol)

## AUTOMATED BUGFIXES
- **v1.3.0 through v1.3.2**:<br />
For if you placed any Miner mk2s (or other Vein Collector Stations, *not* Gas Giant Collectors) while using those versions of this mod.<br />
We will reconnect all your Vein Collectors' Mine/Station components, making the mining speed slider work and resolving any other
, less-obvious issues caused by the Station Component not having bookmarked its paired Miner Component.
This issue is also resolved on affected buildings if you just deconstruct and reconstruct them, regardless of if this bugfixer is enabled.
The fix is permanent once you save, and can thus be turned off after being run once on each affected savegame.<br />
This automated fix is more for "completeness", as this is a bug I introduced into save files.

## Future...
Possibly support for miner blueprint forcing for advanced miners, as well as more proper lazy-oil and better lazy-ocean.