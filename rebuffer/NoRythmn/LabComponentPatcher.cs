using System;
using System.Threading;
using System.Runtime.CompilerServices;

using HarmonyLib;

//A large part of this code made bad thread safety assumptions.
//We need to correct those at some point.
//Needs always goes first, but IU and UO2N can interleve, so while Needs
// gives us some safety it can break if timings are bad.
//I've sprinkled in a ton of extra Interlocked and snapshot most of my
// guard values, but this really needs to just be reimplemented entirely.
//Having to update Inc and Cnt separately is a big pain lol
//Maybe have the at-tick-start values copied over to the unused side during needs,
// then execute all changes on actual referencing those?
//-eir

namespace Eirshy.DSP.ReBuffer.NoRythhmn {
    static class LabComponentPatcher {
        const int LOCKED = -1;
        const int JELLO_CALORIES = 3600;//this isn't a buffer value
        const int JELLO_FLAVORS = 6;//if you change this you have to redo the unrolls.

        static int InpMult;
        static int OupMult;
        static int JelloPlateSize;

        public static void ApplyMe() {
            InpMult = Config.GetInp(ERecipeType.Research);
            OupMult = Config.GetOup(ERecipeType.Research);
            JelloPlateSize = JELLO_CALORIES * Config.JelloAppetite;

            ReBuffer.Harmony.PatchAll(typeof(LabComponentPatcher));
        }

        #region Math helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int MIN2(int i, int j) => i < j ? i : j;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long MIN2(long i, long j) => i < j ? i : j;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long MIN3(long i, long j, long k) => i < j ? i < k ? i : k : j < k ? j : k;

        #endregion
        #region Tower Up/Down

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.UpdateOutputToNext))]
        static void UpdateOutputToNext(LabComponent[] labPool, ref LabComponent __instance, ref bool __runOriginal) {
            if(!__runOriginal) return;
            __runOriginal = false;
            //this is the same guard in the decomp src
            if(labPool[__instance.nextLabId].id == 0 || labPool[__instance.nextLabId].id != __instance.nextLabId) {
                Assert.CannotBeReached();
                __instance.nextLabId = 0;
                return;
            }
            //convenience/readability
            ref var next = ref labPool[__instance.nextLabId];
            //Matrix Mode is literally !researchMode && recipeId != 0.
            if(__instance.researchMode) {
                if(UpdateOutputToNext_inlineWillUp(ref __instance, ref next, 0)) {
                    UpdateOutputToNext_inlineJelloUp(ref __instance, ref next, 0);
                }
                if(UpdateOutputToNext_inlineWillUp(ref __instance, ref next, 1)) {
                    UpdateOutputToNext_inlineJelloUp(ref __instance, ref next, 1);
                }
                if(UpdateOutputToNext_inlineWillUp(ref __instance, ref next, 2)) {
                    UpdateOutputToNext_inlineJelloUp(ref __instance, ref next, 2);
                }
                if(UpdateOutputToNext_inlineWillUp(ref __instance, ref next, 3)) {
                    UpdateOutputToNext_inlineJelloUp(ref __instance, ref next, 3);
                }
                if(UpdateOutputToNext_inlineWillUp(ref __instance, ref next, 4)) {
                    UpdateOutputToNext_inlineJelloUp(ref __instance, ref next, 4);
                }
                if(UpdateOutputToNext_inlineWillUp(ref __instance, ref next, 5)) {
                    UpdateOutputToNext_inlineJelloUp(ref __instance, ref next, 5);
                }
            } else if(__instance.recipeId != 0) {
                //transfer from inst to next
                switch(__instance.served.Length) {
                    case 6:
                        if(UpdateOutputToNext_inlineWillUp(ref __instance, ref next, 5)) {
                            UpdateOutputToNext_inlineServeUp(ref __instance, ref next, 5);
                        }
                        goto case 5;
                    case 5:
                        if(UpdateOutputToNext_inlineWillUp(ref __instance, ref next, 4)) {
                            UpdateOutputToNext_inlineServeUp(ref __instance, ref next, 4);
                        }
                        goto case 4;
                    case 4:
                        if(UpdateOutputToNext_inlineWillUp(ref __instance, ref next, 3)) {
                            UpdateOutputToNext_inlineServeUp(ref __instance, ref next, 3);
                        }
                        goto case 3;
                    case 3:
                        if(UpdateOutputToNext_inlineWillUp(ref __instance, ref next, 2)) {
                            UpdateOutputToNext_inlineServeUp(ref __instance, ref next, 2);
                        }
                        goto case 2;
                    case 2:
                        if(UpdateOutputToNext_inlineWillUp(ref __instance, ref next, 1)) {
                            UpdateOutputToNext_inlineServeUp(ref __instance, ref next, 1);
                        }
                        goto case 1;
                    case 1:
                        if(UpdateOutputToNext_inlineWillUp(ref __instance, ref next, 0)) {
                            UpdateOutputToNext_inlineServeUp(ref __instance, ref next, 0);
                        }
                        break;
                }
                //This was originally runnable in research mode for... raisins.
                //Research mode doesn't even define/load a produced[] array.
                var shouldTransfer = __instance.produced[0] < __instance.productCounts[0] && next.produced[0] > 0;
                if(shouldTransfer) {
                    var transfer = Interlocked.Exchange(ref next.produced[0], 0);
                    Interlocked.Add(ref __instance.produced[0], transfer);
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool UpdateOutputToNext_inlineWillUp(ref LabComponent __instance, ref LabComponent next, int i) {
            //no locking needed, needs is immutable at this step if we don't mutate it, so if we require
            //  needs to be met by __inst and not-met by next, we have guaranteed safety thanks to sniffing
            //  a this-point-in-the-tick immutable aspect of the state
            return __instance.needs[i] == 0 && next.needs[i] > 0;
            //old hijack-for-lock based for matrixMode
            /*
            if(__instance.served[i] <= __instance.requireCounts[i] || next.needs[i] <= 0) return false;//wont transfer
            if(0 != Interlocked.CompareExchange(ref __instance.needs[i], LOCKED, 0)) return false;
            if(0 >= Interlocked.CompareExchange(ref next.needs[i], LOCKED, __instance.requires[i])) {
                //release self, we failed to get next.
                __instance.needs[i] = 0;
                return false;
            }
            return true;
            /**/
            //old hijack-for-lock based researchMode
            /*
            if(__instance.matrixServed[i] <= JELLO_CALORIES || next.needs[i] <= 0) return false;//wont transfer 
            if(0 != Interlocked.CompareExchange(ref __instance.needs[i], LOCKED, 0)) return false;
            if(0 >= Interlocked.CompareExchange(ref next.needs[i], LOCKED, 6001 + i)) {
                //release self, we failed to get next.
                __instance.needs[i] = 0;
                return false;
            }
            return true;
            /**/
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void UpdateOutputToNext_inlineServeUp(ref LabComponent __instance, ref LabComponent next, int i) {
            int transCnt = next.requireCounts[i];
            //^^^ guaranteed available by how Needs works, as we always have at least 1x this if our needs are met
            if(transCnt > 0) {
                var ipi_served = __instance.served[i];
                int ipi = ipi_served <= 0 ? 0 : __instance.incServed[i] / ipi_served;
                //was: split_inc ... which is needlessly complex lol
                var incTrans = ipi * transCnt;
                //---------
                Interlocked.Add(ref __instance.served[i], -transCnt);
                Interlocked.Add(ref __instance.incServed[i], -incTrans);
                //---------
                Interlocked.Add(ref next.served[i], transCnt);
                Interlocked.Add(ref next.incServed[i], incTrans);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void UpdateOutputToNext_inlineJelloUp(ref LabComponent __instance, ref LabComponent next, int i) {
            int transCnt = JELLO_CALORIES;
            //^^^ guaranteed available by how Needs works, as we always have at least 1x this if our needs are met
            if(transCnt > 0) {
                var ipi_served = __instance.matrixServed[i];
                //was: split_inc ... which is needlessly complex lol
                int ipi = ipi_served <= 0 ? 0 : __instance.matrixIncServed[i] / ipi_served;
                var incTrans = ipi * transCnt;
                //---------
                Interlocked.Add(ref __instance.matrixServed[i], -transCnt);
                Interlocked.Add(ref __instance.matrixIncServed[i], -incTrans);
                //---------
                Interlocked.Add(ref next.matrixServed[i], transCnt);
                Interlocked.Add(ref next.matrixIncServed[i], incTrans);
            }
        }

        #endregion
        #region Assemble mode

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.UpdateNeedsAssemble))]
        static void UpdateNeedsAssemble(ref LabComponent __instance, ref bool __runOriginal) {
            if(!__runOriginal) return;
            __runOriginal = false;
            switch(__instance.served.Length) {
                case 6: UpdateNeedsAssemble_inline(ref __instance, 5); goto case 5;
                case 5: UpdateNeedsAssemble_inline(ref __instance, 4); goto case 4;
                case 4: UpdateNeedsAssemble_inline(ref __instance, 3); goto case 3;
                case 3: UpdateNeedsAssemble_inline(ref __instance, 2); goto case 2;
                case 2: UpdateNeedsAssemble_inline(ref __instance, 1); goto case 1;
                case 1: UpdateNeedsAssemble_inline(ref __instance, 0); break;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void UpdateNeedsAssemble_inline(ref LabComponent __instance, int i) {
            __instance.needs[i] = __instance.served[i] < (__instance.requireCounts[i] * InpMult) ? __instance.requires[i] : 0;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateAssemble), new[] { typeof(float), typeof(int[]), typeof(int[]) })]
        static void InternalUpdateAssemble(float power, int[] productRegister, int[] consumeRegister,
            ref LabComponent __instance, ref bool __runOriginal, ref uint __result
        ) {
            if(!__runOriginal) return;
            __runOriginal = false;

            if(power < 0.1f) {
                __result = 0U;
                return;
            }

            //reorderd to skip a single jge, because why not, it's a free ops-1.

            if(__instance.time >= __instance.timeSpend) {
                var prods = __instance.products.Length;
                __instance.replicating = false;
                switch(prods) {
                    case 6: if(InternalUpdate_inlineIsOutFull(ref __instance, 5)) goto case -1; goto case 5;
                    case 5: if(InternalUpdate_inlineIsOutFull(ref __instance, 4)) goto case -1; goto case 4;
                    case 4: if(InternalUpdate_inlineIsOutFull(ref __instance, 3)) goto case -1; goto case 3;
                    case 3: if(InternalUpdate_inlineIsOutFull(ref __instance, 2)) goto case -1; goto case 2;
                    case 2: if(InternalUpdate_inlineIsOutFull(ref __instance, 1)) goto case -1; goto case 1;
                    case 1: if(InternalUpdate_inlineIsOutFull(ref __instance, 0)) goto case -1; break;
                    case -1: __result = 0U; return;
                }
                switch(prods) {
                    case 6: InternalUpdate_inlineAddProd(ref __instance, 5, productRegister); goto case 5;
                    case 5: InternalUpdate_inlineAddProd(ref __instance, 4, productRegister); goto case 4;
                    case 4: InternalUpdate_inlineAddProd(ref __instance, 3, productRegister); goto case 3;
                    case 3: InternalUpdate_inlineAddProd(ref __instance, 2, productRegister); goto case 2;
                    case 2: InternalUpdate_inlineAddProd(ref __instance, 1, productRegister); goto case 1;
                    case 1: InternalUpdate_inlineAddProd(ref __instance, 0, productRegister); break;
                }
                __instance.extraSpeed = 0;
                __instance.speedOverride = __instance.speed;
                __instance.extraPowerRatio = 0;
                __instance.time -= __instance.timeSpend;
            }

            if(__instance.extraTime >= __instance.extraTimeSpend) {
                switch(__instance.products.Length) {
                    case 6: InternalUpdate_inlineAddProd(ref __instance, 5, productRegister); goto case 5;
                    case 5: InternalUpdate_inlineAddProd(ref __instance, 4, productRegister); goto case 4;
                    case 4: InternalUpdate_inlineAddProd(ref __instance, 3, productRegister); goto case 3;
                    case 3: InternalUpdate_inlineAddProd(ref __instance, 2, productRegister); goto case 2;
                    case 2: InternalUpdate_inlineAddProd(ref __instance, 1, productRegister); goto case 1;
                    case 1: InternalUpdate_inlineAddProd(ref __instance, 0, productRegister); break;
                }
                __instance.extraTime -= __instance.extraTimeSpend;
            }

            if(!__instance.replicating) {
                int reqs = __instance.requireCounts.Length;
                switch(reqs) {
                    case 6: if(InternalUpdate_inlineIsLacking(ref __instance, 5)) goto case -1; goto case 5;
                    case 5: if(InternalUpdate_inlineIsLacking(ref __instance, 4)) goto case -1; goto case 4;
                    case 4: if(InternalUpdate_inlineIsLacking(ref __instance, 3)) goto case -1; goto case 3;
                    case 3: if(InternalUpdate_inlineIsLacking(ref __instance, 2)) goto case -1; goto case 2;
                    case 2: if(InternalUpdate_inlineIsLacking(ref __instance, 1)) goto case -1; goto case 1;
                    case 1: if(InternalUpdate_inlineIsLacking(ref __instance, 0)) goto case -1; break;
                    case -1: __instance.time = 0; __result = 0U; return;
                }
                int proli = ((reqs > 0) ? Cargo.kIncLevelMax : 0);
                switch(reqs) {
                    case 6: InternalUpdate_inlineConsume(ref __instance, 5, ref proli, consumeRegister); goto case 5;
                    case 5: InternalUpdate_inlineConsume(ref __instance, 4, ref proli, consumeRegister); goto case 4;
                    case 4: InternalUpdate_inlineConsume(ref __instance, 3, ref proli, consumeRegister); goto case 3;
                    case 3: InternalUpdate_inlineConsume(ref __instance, 2, ref proli, consumeRegister); goto case 2;
                    case 2: InternalUpdate_inlineConsume(ref __instance, 1, ref proli, consumeRegister); goto case 1;
                    case 1: InternalUpdate_inlineConsume(ref __instance, 0, ref proli, consumeRegister); break;
                }

                if(proli < 0) proli = 0;
                if(__instance.productive && !__instance.forceAccMode) {
                    __instance.extraSpeed = (int)((double)__instance.speed * Cargo.incTableMilli[proli] * 10.0 + 0.1);
                    __instance.speedOverride = __instance.speed;
                    __instance.extraPowerRatio = Cargo.powerTable[proli];
                } else {
                    __instance.extraSpeed = 0;
                    __instance.speedOverride = (int)((double)__instance.speed * (1.0 + Cargo.accTableMilli[proli]) + 0.1);
                    __instance.extraPowerRatio = Cargo.powerTable[proli];
                }
                __instance.replicating = true;
            }

            if(__instance.replicating && __instance.time < __instance.timeSpend && __instance.extraTime < __instance.extraTimeSpend) {
                __instance.time += (int)(power * (float)__instance.speedOverride);
                __instance.extraTime += (int)(power * (float)__instance.extraSpeed);
            }

            __result = !__instance.replicating ? 0U : (uint)(__instance.products[0] - LabComponent.matrixIds[0] + 1);
        }
        //These are basically all coppied wholesale from AssemblerComponent
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void InternalUpdate_inlineAddProd(ref LabComponent __instance, int i, int[] productRegister) {
            Interlocked.Add(ref __instance.produced[i], __instance.productCounts[i]);
            Interlocked.Add(ref productRegister[__instance.products[i]], __instance.productCounts[i]);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool InternalUpdate_inlineIsOutFull(ref LabComponent __instance, int i) {
            return __instance.produced[i] > __instance.productCounts[i] * OupMult;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool InternalUpdate_inlineIsLacking(ref LabComponent __instance, int i) {
            return __instance.served[i] < __instance.requireCounts[i] || __instance.served[i] == 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void InternalUpdate_inlineConsume(ref LabComponent __instance, int i, ref int proli, int[] consumeRegister) {
            int ipi;
            if(__instance.incServed[i] < 0 || __instance.served[i] <= 0) {
                __instance.incServed[i] = 0;
                ipi = 0;
            } else ipi = __instance.incServed[i] / __instance.served[i];
            //was: split_inc_level... which is needlessly complex lol
            Interlocked.Add(ref __instance.served[i], -__instance.requireCounts[i]);
            Interlocked.Add(ref __instance.incServed[i], -(ipi * __instance.requireCounts[i]));
            //--------
            if(ipi < proli) proli = ipi;
            Interlocked.Add(ref consumeRegister[__instance.requires[i]], __instance.requireCounts[i]);
        }

        #endregion
        #region Research Mode

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.UpdateNeedsResearch))]
        static void UpdateNeedsResearch(ref LabComponent __instance, ref bool __runOriginal) {
            if(!__runOriginal) return;
            __runOriginal = false;
            __instance.needs[0] = __instance.matrixServed[0] < JelloPlateSize ? 6001 : 0;
            __instance.needs[1] = __instance.matrixServed[1] < JelloPlateSize ? 6002 : 0;
            __instance.needs[2] = __instance.matrixServed[2] < JelloPlateSize ? 6003 : 0;
            __instance.needs[3] = __instance.matrixServed[3] < JelloPlateSize ? 6004 : 0;
            __instance.needs[4] = __instance.matrixServed[4] < JelloPlateSize ? 6005 : 0;
            __instance.needs[5] = __instance.matrixServed[5] < JelloPlateSize ? 6006 : 0;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateResearch))]
        static void InternalUpdateResearch(
            float power, float research_speed, int[] consumeRegister, ref TechState ts, ref int techHashedThisFrame
            , ref long uMatrixPoint, ref long hashRegister
            , ref LabComponent __instance, ref bool __runOriginal, ref uint __result
        ) {
            if(!__runOriginal) return;
            __runOriginal = false;

            if(power < 0.1f) {
                __result = 0U;
                return;
            }
            int hashPotential = (int)(research_speed + 2f);
            //exit on fail, so no need to switch-waterfall these
            if(InternalUpdateResearch_inlineCheckPoints(ref __instance, 0, ref hashPotential)
                || InternalUpdateResearch_inlineCheckPoints(ref __instance, 1, ref hashPotential)
                || InternalUpdateResearch_inlineCheckPoints(ref __instance, 2, ref hashPotential)
                || InternalUpdateResearch_inlineCheckPoints(ref __instance, 3, ref hashPotential)
                || InternalUpdateResearch_inlineCheckPoints(ref __instance, 4, ref hashPotential)
                || InternalUpdateResearch_inlineCheckPoints(ref __instance, 5, ref hashPotential)
            ) {
                __instance.replicating = false;
                __result = 0U;
                return;
            }
            __instance.replicating = true;

            var hashSpeed = ((research_speed < (float)hashPotential) ? research_speed : ((float)hashPotential));
            long hashRemain = ts.hashNeeded - ts.hashUploaded;
            var curBytes = __instance.hashBytes + (int)(power * 10000f * hashSpeed + 0.5f);
            var hashUp = MIN3(curBytes / 10000L, hashRemain, hashPotential);
            __instance.hashBytes = curBytes - (int)hashUp * 10000;
            //Above also saves a few hashBytes; original didn't lim to Remain/Potential until after consuming bytes.

            if(hashUp > 0) {
                int iHashUp = (int)hashUp;
                int proli = Cargo.kIncLevelMax;
                switch(__instance.matrixServed.Length) {
                    case 6: InternalUpdateResearch_inlineConsume(ref __instance, 5, iHashUp, ref proli, consumeRegister); goto case 5;
                    case 5: InternalUpdateResearch_inlineConsume(ref __instance, 4, iHashUp, ref proli, consumeRegister); goto case 4;
                    case 4: InternalUpdateResearch_inlineConsume(ref __instance, 3, iHashUp, ref proli, consumeRegister); goto case 3;
                    case 3: InternalUpdateResearch_inlineConsume(ref __instance, 2, iHashUp, ref proli, consumeRegister); goto case 2;
                    case 2: InternalUpdateResearch_inlineConsume(ref __instance, 1, iHashUp, ref proli, consumeRegister); goto case 1;
                    case 1: InternalUpdateResearch_inlineConsume(ref __instance, 0, iHashUp, ref proli, consumeRegister); break;
                    case 0: proli = 0; break;//Not sure why this guard would be necessary, but here we are.
                }
                //if(proli < 0) proli = 0; //changed calc of proli guarantees this
                __instance.extraSpeed = (int)(10000.0 * Cargo.incTableMilli[proli] * 10.0 + 0.1);
                __instance.extraPowerRatio = Cargo.powerTable[proli];

                var extraBytes = __instance.extraHashBytes + (int)(power * (float)__instance.extraSpeed * hashSpeed + 0.5f);
                long lExtraUp = MIN2(extraBytes / 100000L, hashRemain - hashUp);//hashUp is at most hashRemain
                int iExtraUp = (int)lExtraUp;
                __instance.extraHashBytes = extraBytes - (int)lExtraUp * 100000;
                //above saves extraBytes that aren't consumeable this pass.

                ts.hashUploaded += hashUp + lExtraUp;
                hashRegister += hashUp + lExtraUp;
                uMatrixPoint += (long)ts.uPointPerHash * hashUp;
                techHashedThisFrame += iHashUp + iExtraUp;

                if(ts.hashUploaded >= ts.hashNeeded) {
                    TechProto techProto = LDB.techs.Select(__instance.techId);
                    if(ts.curLevel >= ts.maxLevel) {
                        ts.curLevel = ts.maxLevel;
                        ts.hashUploaded = ts.hashNeeded;
                        ts.unlocked = true;
                    } else {
                        ts.curLevel++;
                        ts.hashUploaded = 0L;
                        ts.hashNeeded = techProto.GetHashNeeded(ts.curLevel);
                    }
                }
            } else {
                __instance.extraSpeed = 0;
                __instance.extraPowerRatio = 0;
            }
            __result = 1U;
            return;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool InternalUpdateResearch_inlineCheckPoints(ref LabComponent __instance, int i, ref int hashPotential) {
            var points = __instance.matrixPoints[i];
            if(points > 0) {
                int pointed = __instance.matrixServed[i] / points;
                if(pointed == 0) return true;
                else if(pointed < hashPotential) hashPotential = pointed;
            }
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void InternalUpdateResearch_inlineConsume(ref LabComponent __instance, int i, int iHashUp, ref int proli, int[] consumeRegister) {
            var served = __instance.matrixServed[i];
            var points = __instance.matrixPoints[i];
            if(points > 0) {
                var consume = points * iHashUp;

                int ipi = __instance.matrixIncServed[i] / served;
                Interlocked.Add(ref __instance.matrixIncServed[i], -(ipi * consume));
                __instance.matrixIncServed[i] -= ipi * consume;
                if(ipi < proli) proli = ipi;

                //can't avoid this, consume register doesn't track partials
                var jelloStart = served / JELLO_CALORIES;
                var remain = Interlocked.Add(ref __instance.matrixServed[i], -consume);
                var jelloEnd = remain / JELLO_CALORIES;
                
                //This was not originally locked, but odds are not low that LabCmp / AssembleCmp / etc will probably be simul'd
                Interlocked.Add(ref consumeRegister[LabComponent.matrixIds[i]], jelloStart - jelloEnd);
            }
        }

        #endregion

    }
}
