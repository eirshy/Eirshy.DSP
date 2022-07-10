using System;
using System.Threading;
using System.Runtime.CompilerServices;

using HarmonyLib;

namespace Eirshy.DSP.ReBuffer.AnyBeat {
    static class AssemblerComponentPatcher {
		
		
		public static void ApplyMe() {
			ReBuffer.Harmony.PatchAll(typeof(AssemblerComponentPatcher));
		}



		//SetRecipe doesn't need any additional cleanup, needs and incServed both get nuked already.
		//[HarmonyPostfix]
		//[HarmonyPatch(typeof(AssemblerComponent), nameof(AssemblerComponent.SetRecipe)]
		//static void SetRecipe_clean() { }

		[HarmonyPrefix]
		[HarmonyPatch(typeof(AssemblerComponent), nameof(AssemblerComponent.UpdateNeeds))]
		static void UpdateNeeds(ref AssemblerComponent __instance, ref bool __runOriginal) {
			if(!__runOriginal) return;
			__runOriginal = false;
			int mult = Config.GetInp(__instance.recipeType);
            switch(__instance.requires.Length) {
				case 6: UpdateNeeds_inline(ref __instance, 5, mult); goto case 5;
				case 5: UpdateNeeds_inline(ref __instance, 4, mult); goto case 4;
				case 4: UpdateNeeds_inline(ref __instance, 3, mult); goto case 3;
				case 3: UpdateNeeds_inline(ref __instance, 2, mult); goto case 2;
				case 2: UpdateNeeds_inline(ref __instance, 1, mult); goto case 1;
				case 1: UpdateNeeds_inline(ref __instance, 0, mult); break;
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void UpdateNeeds_inline(ref AssemblerComponent __instance, int i, int buffm) {
			__instance.needs[i] = __instance.served[i] < __instance.requireCounts[i] * buffm ? __instance.requires[i] : 0;
        }


		[HarmonyPrefix]
		[HarmonyPatch(typeof(AssemblerComponent), nameof(AssemblerComponent.InternalUpdate))]
		static void InternalUpdate(float power, int[] productRegister, int[] consumeRegister,
			ref AssemblerComponent __instance, ref bool __runOriginal, ref uint __result
		) {
			if(!__runOriginal) return;
			__runOriginal = false;

			if(power < 0.1f) {
				__result = 0U;
				return;
			}
			var buffm_oup = Config.GetOup(__instance.recipeType);

			//reorderd to skip a single jge, because why not, it's a free ops-1.

			if(__instance.time >= __instance.timeSpend) {
				var prods = __instance.products.Length;
				__instance.replicating = false;
				switch(prods) {
					case 6: if(InternalUpdate_inlineIsOutFull(ref __instance, 5, buffm_oup)) goto case -1; goto case 5;
					case 5: if(InternalUpdate_inlineIsOutFull(ref __instance, 4, buffm_oup)) goto case -1; goto case 4;
					case 4: if(InternalUpdate_inlineIsOutFull(ref __instance, 3, buffm_oup)) goto case -1; goto case 3;
					case 3: if(InternalUpdate_inlineIsOutFull(ref __instance, 2, buffm_oup)) goto case -1; goto case 2;
					case 2: if(InternalUpdate_inlineIsOutFull(ref __instance, 1, buffm_oup)) goto case -1; goto case 1;
					case 1: if(InternalUpdate_inlineIsOutFull(ref __instance, 0, buffm_oup)) goto case -1; break;
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
				//if(proli < 0) proli = 0;//guaranteed unless served or incServed are negative somehow
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

			__result = !__instance.replicating ? 0U : 1U;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void InternalUpdate_inlineAddProd(ref AssemblerComponent __instance, int i, int[] productRegister) {
			__instance.produced[i] += __instance.productCounts[i];
			Interlocked.Add(ref productRegister[__instance.products[i]], __instance.productCounts[i]);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool InternalUpdate_inlineIsOutFull(ref AssemblerComponent __instance, int i, int buffm_oup) {
			return __instance.produced[i] > __instance.productCounts[i] * buffm_oup;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool InternalUpdate_inlineIsLacking(ref AssemblerComponent __instance, int i) {
			return __instance.served[i] < __instance.requireCounts[i] || __instance.served[i] == 0;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void InternalUpdate_inlineConsume(ref AssemblerComponent __instance, int i, ref int proli, int[] consumeRegister) {
			//was: split_inc_level... which is needlessly complex lol
			int ipi = __instance.incServed[i] / __instance.served[i];
			__instance.served[i] -= __instance.requireCounts[i];
			__instance.incServed[i] -= ipi * __instance.requireCounts[i];
			//--------
			if(ipi < proli) proli = ipi;
			Interlocked.Add(ref consumeRegister[__instance.requires[i]], __instance.requireCounts[i]);
		}
	}
}
