# Staurolite

Turns out if you just add random hard rocks and some water to your splitters, they split faster!

Staurolite is a rewrite of how Splitters are processed, making them significantly faster for the 
game to handle. Additionally, it enables Spiling- Splitter Piling! Just turn it on in the settings
(it's off by default) and slap a box on top of the splitter. This serves as an indirect buff to
Distributors- so if you really like the fidget spinners, this one's definitely for you.

At some point I may also enable multithreading on splitters, but for now I've only optimized them.

## Changes
- v1.0.1 Fix for filters being ignored if unfiltered was backed up;
         Fix for unfiltered mixed output from boxes;
- v1.0.0 Initial

## Mechanical changes compared to vanilla
- Splitters with a storage chest now use the chest as a buffer, meaning there must be a valid
  automation slot for any items passing through the splitter in the chest for them to be
  outputted by the splitter.
- Splitters without a storage chest on top will have mixed output on unfiltered lines.
  This is due to filtering that output being expensive, and bunnyhops being canonical means
  you should just be using those instead of having a splitter do that task.

## Compatibility Notes
- ***DSP_Smooth_Splitter** by GreyHak* is compatible!
  - GreyHak only changed CargoPath.TestBlankAtHead
  - Staurolite only touches the caller of it, and calls it as well. 