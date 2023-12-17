using System;
using System.Threading;
using System.Runtime.CompilerServices;

using HarmonyLib;

namespace Eirshy.DSP.ReBuffer.AnyBeat {
    internal class PowerGeneratorComponentPatcher {
        const int CATA_VALUE = 3600;//this isn't a buffer value
        const byte PILE_QTY = 4;

        static int CataMax;
        static float ProdMax;
        static int CataPassAt;
        static byte QtyOut;

        public static void ApplyMe() {
            CataMax = CATA_VALUE * Config.RayrCataIn;
            ProdMax = Config.RayrProdOut;
            QtyOut = Config.RayrPiling ? PILE_QTY : (byte)1;
            var CataPassMin = CATA_VALUE * (2 + QtyOut);
            CataPassAt = Config.RayrCataPass && CataMax >= CataPassMin ? CataPassMin : int.MaxValue;

            ReBuffer.Harmony.PatchAll(typeof(PowerGeneratorComponentPatcher));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float MIN2(float i, float j) => i < j ? i : j;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float MAX2(float i, float j) => i < j ? j : i;


        [HarmonyPrefix]
        [HarmonyPatch(typeof(PowerGeneratorComponent), nameof(PowerGeneratorComponent.GameTick_Gamma))]
        static void GameTick_Gamma(
            bool useIon, bool useCata, bool keyFrame, PlanetFactory factory, int[] productRegister, int[] consumeRegister,
            ref PowerGeneratorComponent __instance, ref bool __runOriginal
        ) {
            __runOriginal = false;

            //Needs compatibility patching too ._.
            // https://github.com/jinxOAO/DSPmod_MoreMegaStructures/blob/df95f75c31e5b6dd25a94691193c0e753c9d7bdc/MoreMegaStructure/MoreMegaStructure/MoreMegaStructure.cs#L712
            //Just needs to validate the mega is able to produce for this rayr

            ref var sign = ref factory.entitySignPool[__instance.entityId];

            #region Consume Catalyst

            if(useCata && __instance.catalystPoint > 0) {
                int rem = __instance.catalystPoint % CATA_VALUE;
                int ipi = __instance.catalystIncPoint / __instance.catalystPoint;
                __instance.catalystPoint -= 1;
                __instance.catalystIncPoint -= ipi;
                if(rem == 0) _ = Interlocked.Decrement(ref consumeRegister[__instance.catalystId]);
                //actual cata usage is handled in deciding how much power we got this tick.
                //no need to toggle off our consumption here if we didn't do anything.
            }

            #endregion
            #region Produce Product & set sign

            if(__instance.productId > 0 && __instance.productCount <= ProdMax) {
                float gain = (float)(__instance.capacityCurrentTick / (double)__instance.productHeat);
                int before = (int)__instance.productCount;
                __instance.productCount += gain;
                int produced = ((int)__instance.productCount) - before;
                if(produced > 0) {
                    _ = Interlocked.Add(ref productRegister[__instance.productId], produced);
                }
                sign.iconId0 = (uint)__instance.productId;
                sign.iconType = 1U;
                //we skip the round-down because why?
            }
            if(__instance.productId == 0) {
                sign.iconId0 = 0U;
                sign.iconType = 0U;
            }

            #endregion
            #region Warmup/down

            //bleh
            if(__instance.warmupSpeed > 0 && __instance.warmup != 1f) {
                __instance.warmup = MIN2(__instance.warmup + __instance.warmupSpeed, 1f);
            } else if (__instance.warmupSpeed < 0 && __instance.warmup != 0f) {
                __instance.warmup = MAX2(__instance.warmup + __instance.warmupSpeed, 0f);
            }

            #endregion

            //Vanilla keyframes shipProd at ProdMax, and needCata always
            //Since we're a semi-optimization, we just always keyframe. It's only messy on init.
            if(!keyFrame) return;

            #region Handle Belts

            var needCata = useIon && __instance.catalystPoint < CataMax;
            var hasProdId = __instance.productId > 0;
            var shipCata = __instance.catalystPoint >= CataPassAt && !hasProdId && __instance.productCount == 0f;
            var shipProd = hasProdId && __instance.productCount >= 1f;
            if(needCata || shipCata || shipProd) {
                factory.ReadObjectConn(__instance.entityId, 0, out var czOut, out var cz, out _);
                factory.ReadObjectConn(__instance.entityId, 1, out var coOut, out var co, out _);

                if(needCata) {
                    //Normal people will only ever hook up one of these, so don't merge them
                    byte cata;
                    byte ink;
                    if(!czOut && factory.PickFrom(cz, 0, __instance.catalystId, null, out cata, out ink) == __instance.catalystId) {
                        __instance.catalystPoint += CATA_VALUE * (int)cata;
                        __instance.catalystIncPoint += CATA_VALUE * (int)ink;
                    }
                    if(!coOut && factory.PickFrom(co, 0, __instance.catalystId, null, out cata, out ink) == __instance.catalystId) {
                        __instance.catalystPoint += CATA_VALUE * (int)cata;
                        __instance.catalystIncPoint += CATA_VALUE * (int)ink;
                    }
                }

                //bail if no shipping allowed or no shipping possible
                if((!shipCata && !shipProd) || (!czOut && !coOut)) return;

                var lastZero = __instance.fuelHeat == 0;
                __instance.fuelHeat = lastZero ? 1 : 0;
                var shipTo =
                      czOut && (!coOut || lastZero) ? cz
                    : coOut && (!czOut || !lastZero) ? co
                    : 0//cannot be reached
                ;
                if(shipProd) {
                    var shipped = factory.InsertInto(shipTo, 0, __instance.productId, QtyOut, 0, out _);
                    if(shipped > 0) {
                        __instance.productCount -= shipped;
                    }
                } else if(shipCata) {
                    var ink = __instance.catalystIncPoint / __instance.catalystPoint;
                    var shipInk = QtyOut * ink;
                    var shipped = factory.InsertInto(shipTo, 0, __instance.catalystId, QtyOut, (byte)shipInk, out _);
                    if(shipped > 0) {
                        __instance.catalystId -= CATA_VALUE * shipped;
                        __instance.catalystIncPoint -= CATA_VALUE * ink * shipped;
                    }
                } 
            }

            #endregion
        }

    }
}
