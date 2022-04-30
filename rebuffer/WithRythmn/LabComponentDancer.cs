using System;
using System.Linq;
using System.Threading;
using System.Runtime.CompilerServices;

using HarmonyLib;

//REMINDER: Large portions of this code is shared with the Patcher version!

namespace Eirshy.DSP.ReBuffer.WithRythmn {

    //POSSIBLE FUTURE PLANS
    #region Lab Tier Support
    //  Currently, we use <LC>.speed as a ratio to determine hat value.
    //
    //  But there's a problem with this.
    //
    //  Vanilla doesn't set <LC>.speed for Research Mode, and completely
    //    ignores the prefabDesc values. So currently we hard-code to 100
    //    if speed isn't available, and use it if it is.

    /*==== Impl notes... ====*/
    //  RythmnKit could easily sync-down the speed value from the prefabs, 
    //    but SetFunction blows it up automatically.
    //  It didn't look like it gets Write from anywhere else that RythmnKit
    //    wouldn't pre-empt, but definitely yet another thing we'd have to
    //    watch out for in update diffs.
    //  If we do this, we're gonna need to do something wacky to figure out
    //    what lab tier we are on SetFunction, 'cause  I don't think there's
    //    currently a way to go backwards from LabComponent to the protoId.
    //  And no you can't just use the previous speed data as a key and pass
    //    it over SetFunction with __state.

    #endregion
    //BUGS:
    //- on save load without tool, jello-makers can get stuck (really weird).

    static class LabComponentDancer {
        const int LOCKED = -1;
        const int JELLO_CALORIES = 3600;//this isn't actually a buffer value
        const int JELLO_FLAVORS = 6;//if you change this you have to redo the unrolls.

        static int InpMult;
        static int OupMult;
        static int JelloPlateSize;

        public static void ApplyMe() {
            InpMult = CFG.GetInp(ERecipeType.Research);
            OupMult = CFG.GetOup(ERecipeType.Research);
            JelloPlateSize =  JELLO_CALORIES * CFG.JelloAppetite;

            //we need non-parallel by factory
            Rythmn.RythmnKit.AddLoad_PostVisitor(MoveToTheDimmsdaleSector);
            //These however we can do fully parallel
            Rythmn.RythmnKit.AddSaveClean_Visitor(SaveClean);
            Rythmn.RythmnKit.AddSaveRestore_Visitor(SaveRestore);

            ReBuffer.Harmony.PatchAll(typeof(LabComponentDancer));
        }

        #region Math helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int MIN2(int i, int j) => i < j ? i : j;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long MIN2(long i, long j) => i < j ? i : j;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static long MIN3(long i, long j, long k) => i < j ? i < k ? i : k : j < k ? j : k;

        #endregion
        #region Lab Component helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool HAS_NO_TASK(in LabComponent lab) => !lab.researchMode && lab.recipeId == 0;

        #endregion
        #region DimmmaHelpers

        //theme:
        // https://www.youtube.com/watch?v=SBxpeuxUiOA

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ref int[] DimmaDome(ref LabComponent lab) {
            if(lab.researchMode) return ref lab.requireCounts;
            else return ref lab.matrixServed;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ref int[] DimmaDome(ref LabComponent lab, bool isRes) {
            if(isRes) return ref lab.requireCounts;
            else return ref lab.matrixServed;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static int[] DimmaForceDome(ref LabComponent lab, bool isRes) {
            ref var dome = ref DimmaDome(ref lab, isRes);
            if(dome is null) dome = new int[5];
            return dome;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool DimmaIsDoug(ref LabComponent lab) {
            var dome = DimmaDome(ref lab, lab.researchMode);
            return dome is null || dome[DIMMA_TYPE] == DIMMA_TYPE_DOUG;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool DimmaIsHat(ref LabComponent lab) {
            var dome = DimmaDome(ref lab, lab.researchMode);
            return dome is null || dome[DIMMA_TYPE] == DIMMA_TYPE_HAT;
        }

        const int DIMMA_TYPE = 0;
        const int DIMMA_TYPE_UNKNOWN = 0;
        const int DIMMA_TYPE_HAT = 1;
        const int DIMMA_TYPE_DOUG = 2;
        const int DIMMA_LAST = 1;
        const int DIMMA_WORNBY = 2;
        const int DIMMA_SPEED = 3;
        const int DIMMA_LOCK = 4;
        static bool DimmaIsDoug(int[] dome) => dome[DIMMA_TYPE] == DIMMA_TYPE_DOUG;
        static bool DimmaIsHat(int[] dome) => dome[DIMMA_TYPE] == DIMMA_TYPE_HAT;
        static bool DimmaIsUnknown(int[] dome) => dome[DIMMA_TYPE] == DIMMA_TYPE_UNKNOWN;
        static ref int DimmaRefType(int[] dome) => ref dome[DIMMA_TYPE];
        static int DimmaSetType(int[] dome, int type) => dome[DIMMA_TYPE] = type;

        static uint DimmaGetLast(int[] dome) => unchecked((uint)dome[DIMMA_LAST]);
        static void DimmaSyncLast(int[] dome, int[] doug) => dome[DIMMA_LAST] = doug[DIMMA_LAST];
        static void DimmaSetLast(int[] dome, uint last) => dome[DIMMA_LAST] = unchecked((int)last);

        static ref int DimmaRefWornBy(int[] dome) => ref dome[DIMMA_WORNBY];
        static void DimmaSetWornBy(int[] dome, in LabComponent by) => dome[DIMMA_WORNBY] = by.id;

        static ref int DimmaRefSpeedPrc(int[] dome) => ref dome[DIMMA_SPEED];
        static void DimmaClearSpeedPrc(int[] dome) => dome[DIMMA_SPEED] = 0;

        static ref int DimmaRefLock(int[] dome) => ref dome[DIMMA_LOCK];
        static void DimmaUnlock(int[] dome) => dome[DIMMA_LOCK] = 0;

        static int DimmaGetRelSpeed(in LabComponent hat, in LabComponent wearer, bool isRes) {
            return wearer.speed == 0 || hat.speed == 0
                ? 100 
                : (int)Math.Ceiling(hat.speed * 100f / wearer.speed);
            //Currently there are no lab tiers, so This Is Fine .jpg
        }

        #endregion

        static void MoveToTheDimmsdaleSector(GameData gdat) {
            Rythmn.RythmnKit.LogProvider.LogStanza(typeof(LabComponentDancer), "Collapsing Labs");
            gdat.factories.AsParallel().ForAll((factory) => {
                if(factory is null) return;
                var labs = Rythmn.EntityRef.GetEntityRefs(factory)
                    .Where(er => er.HasActive_LabComponent)
                    .ToList();
                ;
                if(labs.Count < 2) return;//not enough labs for towers, let it autodiscover
                //build our hats, transfer everything down the hats
                foreach(var er in labs) {
                    ref var lab = ref er.GetLive_LabComponent();
                    if(HAS_NO_TASK(lab)) continue;//no selected action, can't add a dome
                    var isRes = lab.researchMode;
                    var dome = DimmaForceDome(ref lab, isRes);

                    if(DimmaIsHat(dome)) continue;//We've already been claimed
                    DimmaSetType(dome, DIMMA_TYPE_DOUG);
                    var nextid = lab.nextLabId;
                    while(nextid > 0) {
                        ref var hat = ref Rythmn.EntityRef.GetLive_LabComponent(er.Factory, nextid);
                        var hatDome = DimmaForceDome(ref hat, isRes);
                        if(!DimmaIsHat(hatDome)) {
                            DimmaSetType(hatDome, DIMMA_TYPE_HAT);
                            //clear needs so nobody sees any alerts.
                            for(int i = 0; i < hat.needs.Length; i++) hat.needs[i] = 0;

                            if(isRes) {
                                #region JELLO EATER

                                for(int i = 0; i < JELLO_FLAVORS; i++) {
                                    lab.matrixServed[i] += hat.matrixServed[i];
                                    lab.matrixIncServed[i] += hat.matrixIncServed[i];
                                    lab.matrixPoints[i] += hat.matrixPoints[i];
                                    hat.matrixServed[i] = 0;
                                    hat.matrixIncServed[i] = 0;
                                    hat.matrixPoints[i] = 0;
                                }
                                lab.hashBytes += hat.hashBytes;
                                hat.hashBytes = 0;
                                lab.extraHashBytes += hat.extraHashBytes;
                                hat.extraHashBytes = 0;

                                #endregion
                            } else if(!HAS_NO_TASK(hat)) {
                                #region JELLOW MAKER

                                for(int i = 0; i < hat.served.Length; i++) {
                                    lab.served[i] += hat.served[i];
                                    lab.incServed[i] += hat.incServed[i];
                                    hat.served[i] = 0;
                                    hat.incServed[i] = 0;
                                }
                                lab.produced[0] += hat.produced[0];
                                hat.produced[0] = 0;
                                lab.time += hat.time;
                                hat.time = 0;
                                lab.extraTime += hat.extraTime;
                                hat.extraTime = 0;

                                #endregion
                            }
                        }
                        //claim the hat's wearer slot and keep climbing
                        DimmaSetWornBy(hatDome, lab);
                        nextid = hat.nextLabId;
                    }
                }
                //sync replicating, since our save files remove it explicitly
                foreach(var er in labs) {
                    ref var lab = ref er.GetLive_LabComponent();
                    var dome = DimmaDome(ref lab);
                    if(dome is null) continue;
                    if(DimmaIsHat(dome)) {
                        ref var doug = ref Rythmn.EntityRef.GetLive_LabComponent(er.Factory, DimmaRefWornBy(dome));
                        lab.replicating = doug.replicating;
                    }
                }
            });
        }

        #region Hat Size Tracking

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.UpdateOutputToNext))]
        static void UpdateOutputToNext(LabComponent[] labPool, ref LabComponent __instance, ref bool __runOriginal) {
            if(!__runOriginal) return;
            __runOriginal = false;
            var isRes = __instance.researchMode;
            if(HAS_NO_TASK(__instance)) return;
            var dome = DimmaDome(ref __instance, isRes);
            if(dome is null) return;//we'll be taken care of by Doug or by our action pass if we're Doug
            switch(DimmaRefType(dome)) {
                case DIMMA_TYPE_UNKNOWN:
                    return;//do nothing
                case DIMMA_TYPE_HAT: {
                    if(__instance.nextLabId == 0) return;//We're Tophat, let our Doug take care of us.
                    //if unlocked, update wearer's count, sync visual state to last InternalUpdate, 
                    ref var locked = ref DimmaRefLock(dome);
                    if(Interlocked.Exchange(ref locked, LOCKED) == LOCKED) return;
                    ref var wearer = ref labPool[DimmaRefWornBy(dome)];
                    var doug = DimmaDome(ref wearer, isRes);//wearer links are sired by Dougs, always assume available
                    bool chaseDown = false;
                    while(!DimmaIsDoug(doug)) {
                        wearer = ref labPool[DimmaRefWornBy(doug)];
                        doug = DimmaDome(ref wearer, isRes);
                        chaseDown = true;
                    }
                    if(chaseDown) DimmaSetWornBy(dome, wearer);
                    DimmaSyncLast(dome, doug);
                    __instance.replicating = wearer.replicating;
                    __instance.extraPowerRatio = wearer.extraPowerRatio;
                    var speedCollect = DimmaRefSpeedPrc(dome);
                    Interlocked.Add(ref DimmaRefSpeedPrc(doug), DimmaGetRelSpeed(__instance, wearer, isRes) + speedCollect);
                    DimmaClearSpeedPrc(dome);
                    return;
                }
                case DIMMA_TYPE_DOUG: {
                    ref var mySpeedPrc = ref DimmaRefSpeedPrc(dome);
                    var nextid = __instance.nextLabId;
                    while(nextid > 0) {
                        //todo, include a check to make sure we haven't been locked
                        //if we have, then we're a poser-doug.

                        ref var hat = ref labPool[nextid];
                        var hatDome = DimmaForceDome(ref hat, isRes);
                        bool isHat = DimmaIsHat(hatDome);
                        bool isTophat = hat.nextLabId == 0 && isHat;
                        if(!isHat || isTophat) {//Poser-Doug, Unknown, and Tophat.
                            Interlocked.Exchange(ref DimmaRefLock(hatDome), LOCKED);
                            //does not actually respect Tophat's privacy.
                            DimmaSetType(hatDome, DIMMA_TYPE_HAT);
                            DimmaSyncLast(hatDome, dome);
                            DimmaSetWornBy(hatDome, __instance);
                            var speedCollect = DimmaRefSpeedPrc(hatDome);
                            hat.replicating = __instance.replicating;
                            hat.extraPowerRatio = __instance.extraPowerRatio;
                            Interlocked.Add(ref mySpeedPrc, DimmaGetRelSpeed(hat, __instance, isRes) + speedCollect);
                            DimmaClearSpeedPrc(hatDome);
                        }
                        nextid = hat.nextLabId;
                    }
                    return;
                }
            }

        }

        #endregion
        #region Assemble mode

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.UpdateNeedsAssemble))]
        static void UpdateNeedsAssemble(ref LabComponent __instance, ref bool __runOriginal) {
            if(!__runOriginal) return;
            __runOriginal = false;
            if(!DimmaIsDoug(ref __instance)) return;//Only confirmed dougs have needs
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
            __instance.needs[i] = __instance.served[i] < __instance.requireCounts[i] * InpMult ? __instance.requires[i] : 0;
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(LabComponent), nameof(LabComponent.InternalUpdateAssemble), new[] { typeof(float), typeof(int[]), typeof(int[]) })]
        static void InternalUpdateAssemble(float power, int[] productRegister, int[] consumeRegister,
            ref LabComponent __instance, ref bool __runOriginal, ref uint __result
        ) {
            if(!__runOriginal) return;
            __runOriginal = false;
            var dome = DimmaForceDome(ref __instance, false);
            switch(DimmaRefType(dome)) {
                case DIMMA_TYPE_HAT: {
                    DimmaUnlock(dome);
                    __result = DimmaGetLast(dome);
                    return;
                }
                case DIMMA_TYPE_UNKNOWN:
                    DimmaSetType(dome, DIMMA_TYPE_DOUG);//WELCOME NEW INITIATE
                    goto case DIMMA_TYPE_DOUG;
                case DIMMA_TYPE_DOUG: break;//just keep going
            }
            if(power < 0.1f) {
                DimmaSetLast(dome, __result = 0U);
                return;
            }
            var dimmaSpeedPrc = 1f + (DimmaRefSpeedPrc(dome) / 100f);
            DimmaClearSpeedPrc(dome);

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
                    case -1: DimmaSetLast(dome, __result = 0U); return;
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
                    case -1: __instance.time = 0; DimmaSetLast(dome, __result = 0U); return;
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
                __instance.time += (int)(power * (float)__instance.speedOverride * dimmaSpeedPrc);
                __instance.extraTime += (int)(power * (float)__instance.extraSpeed * dimmaSpeedPrc);
            }

            DimmaSetLast(dome, __result = !__instance.replicating ? 0U : (uint)(__instance.products[0] - LabComponent.matrixIds[0] + 1));
        }
        //These are basically all coppied wholesale from AssemblerComponent
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void InternalUpdate_inlineAddProd(ref LabComponent __instance, int i, int[] productRegister) {
            __instance.produced[i] += __instance.productCounts[i];
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
            if(__instance.incServed[i] < 0 || __instance.served[i] <= 0) __instance.incServed[i] = 0;
            //was: split_inc_level... which is needlessly complex lol
            int ipi = __instance.incServed[i] / __instance.served[i];
            __instance.served[i] -= __instance.requireCounts[i];
            __instance.incServed[i] -= ipi * __instance.requireCounts[i];
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
            if(!DimmaIsDoug(ref __instance)) return;
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
            float power, float speed, int[] consumeRegister, ref TechState ts, ref int techHashedThisFrame, ref long uMatrixPoint, ref long hashRegister
            , ref LabComponent __instance, ref bool __runOriginal, ref uint __result
        ) {
            if(!__runOriginal) return;
            __runOriginal = false;
            var dome = DimmaForceDome(ref __instance, true);
            switch(DimmaRefType(dome)) {
                case DIMMA_TYPE_HAT: {
                    DimmaUnlock(dome);
                    __result = DimmaGetLast(dome);
                    return;
                }
                case DIMMA_TYPE_UNKNOWN:
                    DimmaSetType(dome, DIMMA_TYPE_DOUG);//WELCOME NEW INITIATE
                    goto case DIMMA_TYPE_DOUG;
                case DIMMA_TYPE_DOUG: break;//just keep going
            }
            if(power < 0.1f) {
                DimmaSetLast(dome, __result = 0U);
                return;
            }
            var dimmaSpeedPrc = 1f + (DimmaRefSpeedPrc(dome) / 100f);
            DimmaClearSpeedPrc(dome);
            var totalSpeed = speed * dimmaSpeedPrc;

            int hashPotential = (int)(totalSpeed + 2f);
            if(InternalUpdateResearch_inlineCheckPoints(ref __instance, 0, ref hashPotential)
                || InternalUpdateResearch_inlineCheckPoints(ref __instance, 1, ref hashPotential)
                || InternalUpdateResearch_inlineCheckPoints(ref __instance, 2, ref hashPotential)
                || InternalUpdateResearch_inlineCheckPoints(ref __instance, 3, ref hashPotential)
                || InternalUpdateResearch_inlineCheckPoints(ref __instance, 4, ref hashPotential)
                || InternalUpdateResearch_inlineCheckPoints(ref __instance, 5, ref hashPotential)
            ) {
                __instance.replicating = false;
                DimmaSetLast(dome, __result = 0U);
                return;
            }
            __instance.replicating = true;

            var hashSpeed = ((totalSpeed < (float)hashPotential) ? totalSpeed : ((float)hashPotential));
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
            DimmaSetLast(dome, __result = 1U);
            return;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool InternalUpdateResearch_inlineCheckPoints(ref LabComponent __instance, int i, ref int hashPotential) {
            if(__instance.matrixPoints[i] > 0) {
                int pointed = __instance.matrixServed[i] / __instance.matrixPoints[i];
                if(pointed == 0) return true;
                else if (pointed < hashPotential) hashPotential = pointed;
            }
            return false;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void InternalUpdateResearch_inlineConsume(ref LabComponent __instance, int i, int iHashUp, ref int proli, int[] consumeRegister) {
            if(__instance.matrixServed[i] > 0 && __instance.matrixPoints[i] > 0) {
                var consume = __instance.matrixPoints[i] * iHashUp;

                int ipi = __instance.matrixIncServed[i] / __instance.matrixServed[i];
                __instance.matrixServed[i] -= consume;
                __instance.matrixIncServed[i] -= ipi * consume;
                if(ipi < proli) proli = ipi;

                //This was not originally locked, but odds are not low that LabCmp / AssembleCmp could be simul some day
                Interlocked.Add(ref consumeRegister[LabComponent.matrixIds[i]], consume / JELLO_CALORIES);
            }
        }

        #endregion

        #region Bugfix - Destroying hats gives items due to Replicating

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FactorySystem), nameof(FactorySystem.TakeBackItems_Lab))]
        static void TakeBackItems_Lab(Player player, int labId, FactorySystem __instance) {
            if(labId == 0) return;
            ref var lab = ref __instance.labPool[labId];
            if(!DimmaIsDoug(ref lab)) {
                lab.replicating = false;// we weren't really replicating, so don't give extra mats back
            }
        }

        #endregion
        #region Bugfix - Switching modes gives hats a free run due to Replicating

        static void SaveClean(Rythmn.EntityRef eref) {
            if(!eref.Has_LabComponent) return;
            ref var lab = ref eref.GetLive_LabComponent();
            if(!DimmaIsDoug(ref lab)) lab.replicating = false;
        }
        static void SaveRestore(Rythmn.EntityRef eref) {
            if(!eref.Has_LabComponent) return;
            ref var lab = ref eref.GetLive_LabComponent();
            var dome = DimmaDome(ref lab);
            if(dome is null) return;
            if(DimmaIsHat(dome)) {
                ref var doug = ref Rythmn.EntityRef.GetLive_LabComponent(eref.Factory, DimmaRefWornBy(dome));
                if(doug.replicating) lab.replicating = true;
            }
        }

        #endregion
    }
}
