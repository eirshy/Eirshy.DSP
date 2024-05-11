using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace Eirshy.DSP.Staurolite {
    internal class StauroliteJet {
        struct Splinput(int splindex, CargoPath path, int cargoID, in Cargo cargo) {
            public int Splindex = splindex;
            public CargoPath Path = path;
            public int CargoID = cargoID;
            public Cargo Cargo = cargo;
            public bool Picked = false;
        }
        struct Sploutput(int splindex, CargoPath path, int blank) {
            public int Splindex = splindex;
            public CargoPath Path = path;
            public int Blank = blank;
            public bool Picked = false;
        }

        /// <summary>
        /// Also unrolls CargoTraffic.UpdateSplitter, SplitterComponent.InputAlternate, and SplitterComponent.OutputAlternate
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CargoTraffic), nameof(CargoTraffic.SplitterGameTick))]
        static void SplitterGameTick(long time, ref CargoTraffic __instance, ref bool __runOriginal) {
            if(!__runOriginal) return;
            __runOriginal = false;

            //todo: figure out how to hijack their threading system and async this
            PerformanceMonitor.BeginSample(ECpuWorkEntry.Splitter);

            var splins = new Splinput[4];
            int inTail = 0;
            var splouts = new Sploutput[4];
            int outTail = 0;
            bool doFilter = false;
            int piling = 
                Config.Spile 
                ? Config.SpileAlways4 
                    ? 4 
                    : GameMain.history.stationPilerLevel 
                : 1
            ;
            var arrange = new int[4];
            var back = new int[4];
            var used = new bool[4];

            for(int i = __instance.splitterCursor; i-- > 1;) {
                ref var sp = ref __instance.splitterPool[i];
                if(sp.id != i) continue;

                sp.CheckPriorityPreset();
                var inCount =
                      sp.input0 <= 0 ? 0
                    : sp.input1 <= 0 ? 1
                    : sp.input2 <= 0 ? 2
                    : sp.input3 <= 0 ? 3
                    : 4
                ;
                var outCount = //not just a negation of inputs, unusued is valid!
                      sp.output0 <= 0 ? 0
                    : sp.output1 <= 0 ? 1
                    : sp.output2 <= 0 ? 2
                    : sp.output3 <= 0 ? 3
                    : 4
                ;
                var hasTop = sp.topId != 0;

                if((inCount | outCount) == 0
                    || !hasTop && (inCount == 0 || outCount == 0)
                ) continue;

                //PREP SPLINS
                inTail = 0;
                switch(inCount) {
                    case 4: _spltr_loadRear_inline(3, sp.input3, splins, __instance, ref inTail); goto case 3;
                    case 3: _spltr_loadRear_inline(2, sp.input2, splins, __instance, ref inTail); goto case 2;
                    case 2: _spltr_loadRear_inline(1, sp.input1, splins, __instance, ref inTail); goto case 1;
                    case 1: _spltr_loadRear_inline(0, sp.input0, splins, __instance, ref inTail); break;
                }
                if(!hasTop && inTail == 0) continue;//nothing can be done, immediate bail

                //PREP SPLOUTS
                outTail = 0;
                switch(outCount) {
                    case 4: _ = _spltr_loadBlank_inline(3, sp.output3, splouts, __instance, ref outTail); goto case 3;
                    case 3: _ = _spltr_loadBlank_inline(2, sp.output2, splouts, __instance, ref outTail); goto case 2;
                    case 2: _ = _spltr_loadBlank_inline(1, sp.output1, splouts, __instance, ref outTail); goto case 1;
                    case 1: doFilter = _spltr_loadBlank_inline(0, sp.output0, splouts, __instance, ref outTail); break;
                }
                if(doFilter && sp.outFilter <= 0) doFilter = false;//filter isn't actually set

                //filter and priority always are the "topmost" index of our spl*s

                bool cycleIn = false;
                bool cycleOut = false;
                if(!hasTop) {
                    #region No Hat: Match Inputs To Outputs

                    if(outTail == 0) continue;//we can't output

                    var outAt = outTail -1;
                    ref var splout = ref splouts[outAt];
                    if(doFilter) {
                        switch(inTail) {
                            //case 4: impossible, one is out
                            case 3: {
                                ref var splinf = ref splins[2];
                                if(splinf.Cargo.item == sp.outFilter) {
                                    _spltr_moveCargo_inline(ref splinf, ref splout);
                                    cycleIn = true;
                                    break;
                                }
                                goto case 2;
                            }
                            case 2: {
                                ref var splinf = ref splins[1];
                                if(splinf.Cargo.item == sp.outFilter) {
                                    _spltr_moveCargo_inline(ref splinf, ref splout);
                                    cycleIn = true;
                                    break;
                                }
                                goto case 1;
                            }
                            case 1: {
                                ref var splinf = ref splins[0];
                                if(splinf.Cargo.item == sp.outFilter) {
                                    _spltr_moveCargo_inline(ref splinf, ref splout);
                                    cycleIn = true;
                                    break;
                                }
                                break;
                            }
                        }
                        //cycle splout, filter mode prevents other item types
                        if(outAt > 0) splout = ref splouts[--outAt];
                        else splout.Picked = true;//pretend we were picked, so we skip
                    }
                    //fitler done, pair rest
                    if(!splout.Picked) {
                        switch(inTail) {
                            //case 4: impossible, one is out
                            case 3: {
                                ref var splin = ref splins[2];
                                if(!splin.Picked) {
                                    _spltr_moveCargo_inline(ref splin, ref splout);
                                    cycleIn = true;
                                    cycleOut = true;
                                    if(outAt > 0) splout = ref splouts[--outAt];
                                    else break;
                                }
                                goto case 2;
                            }
                            case 2: {
                                ref var splin = ref splins[1];
                                if(!splin.Picked) {
                                    _spltr_moveCargo_inline(ref splin, ref splout);
                                    cycleIn = true;
                                    cycleOut = true;
                                    if(outAt > 0) splout = ref splouts[--outAt];
                                    else break;
                                }
                                goto case 1;
                            }
                            case 1: {
                                ref var splin = ref splins[0];
                                if(!splin.Picked) {
                                    _spltr_moveCargo_inline(ref splin, ref splout);
                                    cycleIn = true;
                                    cycleOut = true;
                                    //no out cycle necessary
                                }
                                break;
                            }
                        }
                    }

                    #endregion
                } else {
                    #region Hat: IN -> Hat, Hat -> OUT; Pile

                    switch(inTail) {
                        //we can't bail early on failed insert- we don't know what we're inserting, might be filter-limits
                        case 4: _spltr_buffered_inline(in sp, ref splins[3], in __instance, ref cycleIn); goto case 3;
                        case 3: _spltr_buffered_inline(in sp, ref splins[2], in __instance, ref cycleIn); goto case 2;
                        case 2: _spltr_buffered_inline(in sp, ref splins[1], in __instance, ref cycleIn); goto case 1;
                        case 1: _spltr_buffered_inline(in sp, ref splins[0], in __instance, ref cycleIn); break;
                    }
                    int filter;
                    int outAt = outTail;
                    if(doFilter) {
                        filter = sp.outFilter;
                        ref var splout = ref splouts[--outAt];//reserve it, we filter out only
                        bool ignored = false;
                        _ = _spltr_buffered_inline(in sp, ref splout, ref filter, in piling, in __instance, ref ignored);
                    }
                    switch(outAt) {
                        //we can bail early on failed output- we're out of things to output
                        case 4:
                            filter = -sp.outFilter;
                            if(!_spltr_buffered_inline(in sp, ref splouts[3], ref filter, in piling, in __instance, ref cycleOut)) break;
                            goto case 3;
                        case 3:
                            filter = -sp.outFilter;
                            if(!_spltr_buffered_inline(in sp, ref splouts[2], ref filter, in piling, in __instance, ref cycleOut)) break;
                            goto case 2;
                        case 2:
                            filter = -sp.outFilter;
                            if(!_spltr_buffered_inline(in sp, ref splouts[1], ref filter, in piling, in __instance, ref cycleOut)) break;
                            goto case 1;
                        case 1:
                            filter = -sp.outFilter;
                            _ = _spltr_buffered_inline(in sp, ref splouts[0], ref filter, in piling, in __instance, ref cycleOut);
                            break;
                    }

                    #endregion
                }

                //cyclers
                #region Cycle Inputs

                if(cycleIn) {
                    var arrTail = 0;
                    int deltaAll = 0;
                    int deltaNoPri = 0;
                    Array.Clear(used, 0, 4);
                    switch(inTail) {
                        case 4: {
                            ref var splx = ref splins[3];
                            if(splx.Picked) {
                                deltaAll++;
                                if(!sp.inPriority || splx.Splindex != 0) {
                                    used[splx.Splindex] = true;
                                    deltaNoPri++;
                                }
                            }
                            goto case 3;
                        }
                        case 3: {
                            ref var splx = ref splins[2];
                            if(splx.Picked) {
                                deltaAll++;
                                if(!sp.inPriority || splx.Splindex != 0) {
                                    used[splx.Splindex] = true;
                                    deltaNoPri++;
                                }
                            }
                            goto case 2;
                        }
                        case 2: {
                            ref var splx = ref splins[1];
                            if(splx.Picked) {
                                deltaAll++;
                                if(!sp.inPriority || splx.Splindex != 0) {
                                    used[splx.Splindex] = true;
                                    deltaNoPri++;
                                }
                            }
                            goto case 1;
                        }
                        case 1: {
                            ref var splx = ref splins[0];
                            if(splx.Picked) {
                                deltaAll++;
                                if(!sp.inPriority || splx.Splindex != 0) {
                                    used[splx.Splindex] = true;
                                    deltaNoPri++;
                                }
                            }
                            break;
                        }
                    }

                    //no cycle needed if only pri delta'd or everything delta'd
                    if(deltaNoPri > 0 && deltaAll <= inCount) {
                        //load unused (or Priority on 0)
                        var offset = inCount - deltaNoPri -1;//inverts the write index in the switchfall
                        switch(inCount) {
                            case 4: if(!used[3]) arrange[offset - arrTail++] = sp.input3; goto case 3;
                            case 3: if(!used[2]) arrange[offset - arrTail++] = sp.input2; goto case 2;
                            case 2: if(!used[1]) arrange[offset - arrTail++] = sp.input1; goto case 1;
                            case 1: if(!used[0]) arrange[offset - arrTail++] = sp.input0; break;
                        }
                        //load used (in reverse order so things shuffle)
                        switch(inCount) {
                            case 4: if(used[3]) arrange[arrTail++] = sp.input3; goto case 3;
                            case 3: if(used[2]) arrange[arrTail++] = sp.input2; goto case 2;
                            case 2: if(used[1]) arrange[arrTail++] = sp.input1; goto case 1;
                            case 1: if(used[0]) arrange[arrTail++] = sp.input0; break;
                        }
                        //write updated order
                        switch(arrTail) {
                            case 4: sp.input3 = arrange[3]; goto case 3;
                            case 3: sp.input2 = arrange[2]; goto case 2;
                            case 2: sp.input1 = arrange[1]; goto case 1;
                            case 1: sp.input0 = arrange[0]; break;
                        }
                    }
                }

                #endregion
                #region Cycle Outputs

                if(cycleOut) {
                    var arrTail = 0;
                    int deltaAll = 0;
                    int deltaNoPri = 0;
                    Array.Clear(used, 0, 4);
                    switch(outTail) {
                        case 4: {
                            ref var splx = ref splouts[3];
                            if(splx.Picked) {
                                deltaAll++;
                                if(!sp.outPriority || splx.Splindex != 0) {
                                    used[splx.Splindex] = true;
                                    deltaNoPri++;
                                }
                            }
                            goto case 3;
                        }
                        case 3: {
                            ref var splx = ref splouts[2];
                            if(splx.Picked) {
                                deltaAll++;
                                if(!sp.outPriority || splx.Splindex != 0) {
                                    used[splx.Splindex] = true;
                                    deltaNoPri++;
                                }
                            }
                            goto case 2;
                        }
                        case 2: {
                            ref var splx = ref splouts[1];
                            if(splx.Picked) {
                                deltaAll++;
                                if(!sp.outPriority || splx.Splindex != 0) {
                                    used[splx.Splindex] = true;
                                    deltaNoPri++;
                                }
                            }
                            goto case 1;
                        }
                        case 1: {
                            ref var splx = ref splouts[0];
                            if(splx.Picked) {
                                deltaAll++;
                                if(!sp.outPriority || splx.Splindex != 0) {
                                    used[splx.Splindex] = true;
                                    deltaNoPri++;
                                }
                            }
                            break;
                        }
                    }

                    //no cycle needed if only pri delta'd or everything delta'd
                    if(deltaNoPri > 0 && deltaAll <= outCount) {
                        //load unused (or Priority on 0)
                        var offset = outCount - deltaNoPri -1;//inverts the write index in the switchfall
                        switch(outCount) {
                            case 4: if(!used[3]) arrange[offset - arrTail++] = sp.output3; goto case 3;
                            case 3: if(!used[2]) arrange[offset - arrTail++] = sp.output2; goto case 2;
                            case 2: if(!used[1]) arrange[offset - arrTail++] = sp.output1; goto case 1;
                            case 1: if(!used[0]) arrange[offset - arrTail++] = sp.output0; break;
                        }
                        //load used (in reverse order so things shuffle)
                        switch(outCount) {
                            case 4: if(used[3]) arrange[arrTail++] = sp.output3; goto case 3;
                            case 3: if(used[2]) arrange[arrTail++] = sp.output2; goto case 2;
                            case 2: if(used[1]) arrange[arrTail++] = sp.output1; goto case 1;
                            case 1: if(used[0]) arrange[arrTail++] = sp.output0; break;
                        }
                        //write updated order
                        switch(arrTail) {
                            case 4: sp.output3 = arrange[3]; goto case 3;
                            case 3: sp.output2 = arrange[2]; goto case 2;
                            case 2: sp.output1 = arrange[1]; goto case 1;
                            case 1: sp.output0 = arrange[0]; break;
                        }
                    }
                }

                #endregion

                //DONE!
            }
            PerformanceMonitor.EndSample(ECpuWorkEntry.Splitter);

        }
        #region Splitter Inlines - Load Spl*s

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _spltr_loadRear_inline(in int splindex, in int pathID, in Splinput[] splins, in CargoTraffic traffic, ref int tail) {
            var path = traffic.GetCargoPath(traffic.beltPool[pathID].segPathId);
            var cargoID = path.GetCargoIdAtRear();
            if(cargoID != -1) {
                ref var splin = ref splins[tail];
                splin.Splindex = splindex;
                splin.Path = path;
                splin.CargoID = cargoID;
                splin.Cargo = traffic.container.cargoPool[cargoID];
                splin.Picked = false;
                tail++;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool _spltr_loadBlank_inline(in int splindex, in int pathID, in Sploutput[] splouts, in CargoTraffic traffic, ref int tail) {
            var path = traffic.GetCargoPath(traffic.beltPool[pathID].segPathId);
            var blank = path.TestBlankAtHead();
            if(path.pathLength > 10 && blank >= 0) {
                ref var splout = ref splouts[tail];
                splout.Splindex = splindex;
                splout.Path = path;
                splout.Blank = blank;
                splout.Picked = false;
                tail++;
                return true;
            }
            return false;
        }

        #endregion
        #region Splitter Inlines - Move Cargo

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _spltr_moveCargo_inline(ref Splinput splin, ref Sploutput splout) {
            int pickedID = splin.Path.TryPickCargoAtEnd();
            splout.Path.InsertCargoAtHeadDirect(pickedID, splout.Blank);
            splin.Picked = true;
            splout.Picked = true;
        }

        #endregion
        #region Splitter Inlines - Buffered

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void _spltr_buffered_inline(in SplitterComponent sp, ref Splinput splin, in CargoTraffic traffic, ref bool cycleInputs) {
            var success = traffic.factory.InsertCargoIntoStorage(sp.topId, ref traffic.container.cargoPool[splin.CargoID], useBan: true);
            if(!success) return;
            var pickedID = splin.Path.TryPickCargoAtEnd();
            traffic.container.RemoveCargo(pickedID);
            splin.Picked = true;
            cycleInputs = true;
        }

        /// <param name="filter">send -2 to not filter; will pass back up the id of the item type passed out</param>
        /// <returns>If we successfully filled Splout's blank</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool _spltr_buffered_inline(in SplitterComponent sp, ref Sploutput splout, ref int filter, in int piling, in CargoTraffic traffic, ref bool cycleOutputs) {
            var got = traffic.factory.PickFromStorageFiltered(sp.topId, ref filter, piling, out var inc);
            if(got <= 0) return false;//no items acquired
            var cargoID = traffic.container.AddCargo((short)filter, (byte)got, (byte)inc);
            splout.Path.InsertCargoAtHeadDirect(cargoID, splout.Blank);
            splout.Picked = true;
            cycleOutputs = true;
            return true;
        }

        #endregion

    }
}
