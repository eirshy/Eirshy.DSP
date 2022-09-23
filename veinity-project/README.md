# Veinity Project

Want to change the buffer sizes on miners? Ever say "oops, I meant to put resources on infinite." Want to experiment with having all veins work like Oil ones- or want to actually be able to run out of oil?

Or maybe you just want that Texan fever dream where a single mkII miner can harvest every oil node on a planet when combined with with my other mod, ***LazyOutposting***.

This and maybe more is all possible when you completely rewrite MinerComponent's internal update to support a more loose definition of everything- and stop locking the entire vein pool in the process. 

That's right, if you don't touch our settings, we're actually a QoL optimization mod like *Re*-Buffer! In particular, planets with a *lot* of miners and oil pumps will see a... well, like, 0.05ms increase. But they're also a lot more friendly to heavier multithreading and thus *could* see more of a gain in the future.

## Changes
- v0.2.1 More-Verbose Readme
- v0.2.0 
  - Actual support for JinxOAO's SmelterMiner
  - Theoretical support for similar "mining x produce y" concepts
  - Integrated Push-To-Station into our InternalUpdate code
- v0.1.6 Triage tier fix of compat with JinxOAO's SmleterMiner

## Mechanical changes compared to vanilla
- ***Item output ALWAYS piles up to 4 if available*** rather than batching to a tier 3 belt's speed.
- ***Vein pruning is done the tick after a vein is emptied*** rather than only when the vein is selected as the target vein and found empty.
- ***Vein count/amount speed contributions are calculated based on a rolling snapshot*** rather than a forced-serialization for any shared veins.
- ***StationComponent is pushed to by MiningComponent*** rather than pulling from later in the tick.

## Compatibility Notes
- ***SmelterMiner** by JinxOAO* is fully supported.
  - Uses current smelting recipe ratios.
  - Type C supports all of Kimberlite, Frac-Si, Fire Ice, and Spin-Cry "smelting"
  - Type A/B/C mk2 all support Oil refining if LazyOutposting enables mk2 Miners to "mine" oil.

## Known Issues
- ***Miner UI does not "update" to an N/s value when in Diminishing Mode*** It's roughly **25000** vein value per 1/s output. At some point I'll dig into how to change the UI for it. That is what's needed to hit v1.