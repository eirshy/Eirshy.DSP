using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using HarmonyLib;

using Eirshy.DSP.VeinityProject.Enums;

namespace Eirshy.DSP.VeinityProject {

    class MinerComponentPatcher {
        public static void ApplyMe(){
            VeinityProject.Harmony.PatchAll(typeof(MinerComponentPatcher));
		}


		static void _transcludedDependencies() {
			PlanetFactory pf = null;
			MinerComponent mc = new MinerComponent();

			_ = nameof(_safeAddFlagsToFactory_inline);
			pf.AddMiningFlagUnsafe(EVeinType.None);
			pf.AddVeinMiningFlagUnsafe(EVeinType.None);

			_ = nameof(_iu_prune_inline);
			mc.RemoveVeinFromArray(0);

			_ = nameof(InternalUpdate) + "..VeinScanner";
			mc.GetMinimumVeinAmount(pf, (VeinData[])null);
        }


		[HarmonyPrefix]
		[HarmonyPatch(typeof(MinerComponent), nameof(MinerComponent.InternalUpdate))]
		public static void InternalUpdate(
			PlanetFactory factory, VeinData[] veinPool, float power, float miningRate, float miningSpeed, int[] productRegister
			, ref MinerComponent __instance, ref bool __runOriginal, ref uint __result
		) {
			if(!__runOriginal) return;
			__runOriginal = false;
			__result = 0U;
			if(power < 0.1f) return;

			int vcnt = -1;
			#region Time ticking and Internal Buffer filling (shortcut if both are capped)

			if(__instance.productCount < Config.Buffer || __instance.time < __instance.period) {
				vcnt = 0;//we'll have a value;
				int vtot = 0;
				int vmin = int.MaxValue, vmax = int.MinValue;
				int vpi, vpiLowest = -1, vpiFullest = -1;
				var veinType = EVeinType.None;
				var source = ESourceType._UNSET;
				int prodID = 0;
				#region Scan VeinArray (or fake it)

				switch(__instance.type) {
					case EMinerType.None:
					default: //fail gracefully- we literally don't know what to do for this.
						__runOriginal = true;
						return;
					case EMinerType.Water:
						vcnt = 1;
						//veinType = EVeinType.None;
						source = Config.WaterSourceType;
						prodID = factory.planet.waterItemId;
						break;
					case EMinerType.Oil:
						source = Config.OilSourceType;
						goto case EMinerType.Vein;
					case EMinerType.Vein: {
						#region Scan veins array, populate values

						switch(__instance.veinCount) {
							case < 0:
							case 0: vpi = 0; break;
							case 1: {
								vpi = __instance.veins[0];
								if(vpi > 0) {
									var amnt = veinPool[vpi].amount;
									var type = veinPool[vpi].type;
									var prod = veinPool[vpi].productId;
									if(veinPool[vpi].id == vpi && amnt > 0) {
										vcnt = 1;
										vtot = amnt;
										vmin = amnt;
										vmax = amnt;
										vpiLowest = vpi;
										vpiFullest = vpi;
										veinType = type;
										prodID = prod;
									} else __instance.veins[0] = 0;
								}
								break;
							}
							default: {//case > 1:
								for(int i = __instance.veinCount; i-- > 0;) {
									vpi = __instance.veins[i];
									if(vpi > 0) {
										var amnt = veinPool[vpi].amount;
										var type = veinPool[vpi].type;
										var prod = veinPool[vpi].productId;
										if(veinPool[vpi].id == vpi && amnt > 0) {
											vcnt++;
											vtot += amnt;
											if(amnt < vmin) {
												vpiLowest = vpi;
												vmin = amnt;
											}
											if(amnt > vmax) {
												vpiFullest = vpi;
												vmax = amnt;
											}
											veinType = type;
											prodID = prod;
										} else __instance.veins[i] = 0;
									}
								}
								break;
							}
						}
						#endregion
						#region Pick depletion mode if not set

						if(source == ESourceType._UNSET) {
							switch(veinType) {
								case EVeinType.None:
									source = Config.WaterSourceType;
									break;
								case EVeinType.Oil:
									source = Config.OilSourceType;
									break;
								default:
									source = Config.VeinSourceType;
									break;
							}
						}

						#endregion
						//this value is only really set on Vein types in vanilla.
						if(__instance.type == EMinerType.Vein) {
							__instance.minimumVeinAmount = vpiLowest == -1 ? 0 : vmin;
						}
						break;
					}
				}

				#endregion
				if(vcnt > 0) {
					#region Time tick and mark as running

					if(__instance.time <= __instance.period) {
						switch(source) {
							case ESourceType.Infinite:
								__instance.time += (int)(power * __instance.speed * miningSpeed * vcnt);
								break;
							case ESourceType.FiniteDepleting:
								__instance.time += (int)(power * __instance.speed * miningSpeed * vcnt);
								break;
							case ESourceType.Diminishing:
								__instance.time += (int)(power * __instance.speed * miningSpeed * vtot * VeinData.oilSpeedMultiplier + vcnt * 0.5f);
								break;
						}
						__result = 1U;
					}

					#endregion
					#region Move from Source to Internal Buffer

					if(__instance.time > __instance.period && __instance.productCount < Config.Buffer && prodID > 0) {
						int potential = __instance.time / __instance.period;
						__instance.productId = prodID;

						switch(source) {
							#region case EGatheringDepletionMode.Infinite: { ... } break;
							case ESourceType.Infinite: {
								__instance.productCount += potential;
								Interlocked.Add(ref productRegister[prodID], potential);
								__instance.time -= __instance.period * potential;
								break;
							}
							#endregion
							#region case EGatheringDepletionMode.FiniteDepleting: { ... } break;
							case ESourceType.FiniteDepleting: {
								switch(Config.FiniteSourceTargeting) {
									default: throw new NotImplementedException();
									case EFiniteSourceConsumptionTarget.Cyclic: {
										var ci = __instance.currentVeinIndex % __instance.veinCount;
										vpi = __instance.veins[ci];
										for(int i = __instance.veinCount; i-- > 0;) {
											ci = (ci + 1) % __instance.veinCount;
											if(vpi <= 0) vpi = __instance.veins[ci];
											else break;
										}
										__instance.currentVeinIndex = ci;
										break;
									}
									case EFiniteSourceConsumptionTarget.Random: {
										var ci = __instance.currentVeinIndex % __instance.veinCount;
										vpi = __instance.veins[ci];
										for(int i = __instance.veinCount; i-- > 0;) {
											ci = (ci + 1) % __instance.veinCount;
											if(vpi <= 0) vpi = __instance.veins[ci];
											else break;
										}
										__instance.currentVeinIndex = unchecked((int)(__instance.seed & 0x7FFFF_FFFF)) % vcnt;
										break;
									}
									case EFiniteSourceConsumptionTarget.Lowest: {
										__instance.currentVeinIndex = 0;
										vpi = vpiLowest;
										break;
									}
									case EFiniteSourceConsumptionTarget.Fullest: {
										__instance.currentVeinIndex = 0;
										vpi = vpiFullest;
										break;
									}
								}
								ref var vtarg = ref veinPool[vpi];
								if(vtarg.amount > 0) {
									int realized = 0;
									if(miningRate <= 0f) realized = potential;
									else {
										int consume = 0;
										if(miningRate < 0.99999f) {
											var curSeed = __instance.seed;
											for(int i = 0; i < potential; i++) {
												_iu_seed_inline(ref curSeed, in miningRate, ref consume);
												realized++;
												if(consume >= vtarg.amount) break;
											}
											__instance.seed = curSeed;
										} else {
											realized = potential;
											consume = potential;
										}
										if(vtarg.id != vpi) realized = 0;//pass entirely if cleared
										else if(consume > 0) {//skip reducing if nothing to reduce
											int groupIndex = vtarg.groupIndex;
											int eaten = _safeVeinAmountReduction_inline(ref vtarg, consume, 0);
											if(eaten != consume) realized = realized * eaten / consume;
											if(vtarg.amount < __instance.minimumVeinAmount) {
												__instance.minimumVeinAmount = vtarg.amount;
											}
											if(eaten > 0) {
												Interlocked.Add(ref factory.veinGroups[groupIndex].amount, -eaten);
												_safeVeinAnim_largest_inline(ref factory.veinAnimPool[vpi]
													, vtarg.amount >= 20000 ? 0f : (1f - (float)vtarg.amount * 5E-05f)
												);
												//clean up if we're the ones that ate the last of the vein.
												if(vtarg.amount <= 0) {
													//these are *very not worth* reimplementing to be thread safe...
													//  so use the same lock that original uses.
													lock(veinPool) {
														if(vtarg.id == vpi) {//make sure nobody else killed it
															factory.RemoveVeinWithComponents(vpi);
															factory.RecalculateVeinGroup(groupIndex);
															factory.NotifyVeinExhausted();
														}
													}
													//Pruning happens on the pass after the vein is zero'd.
													//  plus we only for-sure know our VPI, not our VI.
												}
											}
										}
									}
									__instance.productCount += realized;
									Interlocked.Add(ref productRegister[__instance.productId], realized);
									_safeAddFlagsToFactory_inline(ref factory, veinType);
									__instance.time -= __instance.period * realized;
								}
								break;
							}
							#endregion
							#region case EGatheringDepletionMode.Diminishing: { ... } break;
							case ESourceType.Diminishing: {
								if(miningRate > 0f && vmax > Config.DiminishLimit) {
									int consume = 0;
									var curSeed = __instance.seed;
									for(int j = potential; j-- > 0;) {
										_iu_seed_inline(ref curSeed, in miningRate, ref consume);
									}
									__instance.seed = curSeed;
									if(consume > 0) {
										//Always hitting Fullest works mostly just fine.
										vpi = vpiFullest;
										ref var vtarg = ref veinPool[vpi];
										var reduced = _safeVeinAmountReduction_inline(ref vtarg, consume, Config.DiminishLimit);
										if(reduced > 0) {
											ref var vgrp = ref factory.veinGroups[vtarg.groupIndex];
											Interlocked.Add(ref factory.veinGroups[vtarg.groupIndex].amount, -reduced);
										}
										var rem = vtarg.amount;
										_safeVeinAnim_largest_inline(ref factory.veinAnimPool[vpi]
											, (rem >= 25000) ? 0f : (1f - (float)rem * VeinData.oilSpeedMultiplier)
										);
									}
								}
								__instance.productCount += potential;
								Interlocked.Add(ref productRegister[prodID], potential);
								__instance.time -= __instance.period * potential;
								break;
							}
							#endregion
						}
					}
					#endregion
				}
			}
			#endregion
			_iu_export_inline(ref __instance, ref factory);
			_iu_prune_inline(ref __instance, in vcnt);
		}


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void _iu_seed_inline(ref uint curSeed, in float miningRate, ref int consume) {
			curSeed = (uint)((ulong)(curSeed % 2147483646U + 1U) * 48271UL % 2147483647UL) - 1U;
			consume += (curSeed / 2147483646.0 < (double)miningRate) ? 1 : 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void _iu_export_inline(ref MinerComponent mc, ref PlanetFactory factory) {
			if(mc.productCount > 0 && mc.insertTarget > 0) {
				//Just always pile as high as possible. Avoids needing to calc a delta
				byte toOut = (byte)((mc.productCount < 4) ? mc.productCount : 4);
				int outputted = factory.InsertInto(mc.insertTarget, 0, mc.productId, toOut, 0, out _);
				mc.productCount -= outputted;
				//Don't think this is actually *necessary*, so don't do it.
				//if(__instance.productCount == 0 && __instance.type == EMinerType.Vein) __instance.productId = 0;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void _iu_prune_inline(ref MinerComponent mc, in int vcnt) {
			if(vcnt < mc.veinCount && vcnt >= 0) {
				if(vcnt == 0) {
					Array.Clear(mc.veins, 0, mc.veins.Length);
					mc.veinCount = 0;
				} else {
					for(int i = mc.veinCount; i-- > 0;) {
						int vpi = mc.veins[i];
						if(vpi == 0) {
							for(int j = i + 1; j < mc.veinCount; j++) {
								mc.veins[j - 1] = mc.veins[j];
							}
							mc.veinCount--;
						}
					}
				}
			}
		}



        /// <summary>
        /// Reduces vd.amount down to minRemaining, by at most reduceBy. Returns the amount actually reduced.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int _safeVeinAmountReduction_inline(ref VeinData vd, in int reduceBy, in int minRemaining = 0) {
			int cur, to;
			do {
				cur = vd.amount;
				if(cur <= minRemaining) return 0;//lte for hardening
				to = cur - reduceBy;
				if(to < minRemaining) to = minRemaining;
			} while(Interlocked.CompareExchange(ref vd.amount, to, cur) != cur);
			return cur - to;
		}
		/// <summary>
		/// keeps largest
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void _safeVeinAnim_largest_inline(ref AnimData anim, in float newTime) {
			float curTime;
			do {
				curTime = anim.time;
				if(curTime >= newTime) return;
			} while(Interlocked.CompareExchange(ref anim.time, newTime, curTime) != curTime);
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
        [SuppressMessage("Member Access", "Publicizer001:Accessing a member that was not originally public", Justification = "Interlocked")]
        static void _safeAddFlagsToFactory_inline(ref PlanetFactory factory, in EVeinType addVeinType) {
			int flagToSet = 1 << (int)addVeinType;
			int cur;
			do {
				cur = factory._miningFlag;
				if((cur & flagToSet) == flagToSet) break;
			} while(Interlocked.CompareExchange(ref factory._miningFlag, cur | flagToSet, cur) != cur);
			do {
				cur = factory._veinMiningFlag;
				if((cur & flagToSet) == flagToSet) break;
			} while(Interlocked.CompareExchange(ref factory._veinMiningFlag, cur | flagToSet, cur) != cur);
		}
	}
}
