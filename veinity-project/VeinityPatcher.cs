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

    class VeinityPatcher {
		const int ARBITRARY_LARGE_NUMBER = 500_000;
		static long logmax = 5;

		static void _transcludedDependencies() {
			PlanetFactory pf = null;
			MinerComponent mc = new MinerComponent();

			_ = nameof(DiminishingMiningRateModifier);
			pf.factorySystem.GameTick(0L, false);//the oil modifier calc to miningRate in this

			_ = nameof(_safeAddFlagsToFactory_inline);
			pf.AddMiningFlagUnsafe(EVeinType.None);
			pf.AddVeinMiningFlagUnsafe(EVeinType.None);

			_ = nameof(_DetermineState_inline);
			mc.DetermineState();

			_ = nameof(_getupdDamper);
			pf.factorySystem.GameTickBeforePower(0, false);
			pf.factorySystem.ParallelGameTickBeforePower(0, false, 0, 0, 0);

			_ = nameof(_iu_prune_inline);
			mc.RemoveVeinFromArray(0);

			_ = nameof(InternalUpdate) + "..VeinScanner";
			mc.GetMinimumVeinAmount(pf, (VeinData[])null);
		}


		public static void SetUp(){
            VeinityProject.Harmony.PatchAll(typeof(VeinityPatcher));
		}


		internal static int MkRemapKey(int ProtoID, int OreProdID) => ProtoID << 16 ^ OreProdID;
		internal static void AddRemapFor(int proto, int oreProto, int makesProto, int orePer) {
			if(Remap == null) Remap = new Dictionary<int, (int to, int ratio)>();
			Remap.Add(MkRemapKey(proto, oreProto), (makesProto, orePer));
        }
		internal static Dictionary<int, (int to, int ratio)> Remap = null;



		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameSave), nameof(GameSave.LoadCurrentGame), new[] { typeof(string) })]
		public static void Precalc() {
			DiminishingMiningRateModifier = GameMain.data.gameDesc.resourceMultiplier * 0.40111667f;
		}
		static float DiminishingMiningRateModifier { get; set; }


		[HarmonyPrefix]
		[HarmonyPatch(typeof(MinerComponent), nameof(MinerComponent.SetPCState))]
		public static void SetPCState(PowerConsumerComponent[] pcPool
			, ref MinerComponent __instance, ref bool __runOriginal
		) {
			if(!__runOriginal) return;
			__runOriginal = false;

			_DetermineState_inline(ref __instance);
			pcPool[__instance.pcId].SetRequiredEnergy(
				__instance.workstate > EWorkState.Idle//how vanilla does it
				? _getupdDamper(ref __instance) * __instance.speed * __instance.speed / 100_000_000.0
				: 0
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void _DetermineState_inline(ref MinerComponent __instance) {
			//switched to buffer space checking instead of time sniffing
			switch(__instance.type) {
				case EMinerType.None: return;//"this isn't actually a minercomponent".
				case EMinerType.Oil:
				case EMinerType.Vein: {
					if(__instance.veinCount == 0) {
						__instance.time = 0;
						__instance.workstate = EWorkState.Idle;
					} else if(__instance.productCount < Config.Buffer) {
						__instance.workstate = EWorkState.Running;
					} else __instance.workstate = EWorkState.Full;
					return;
				}
				case EMinerType.Water: {
					if(__instance.productCount < Config.Buffer) {
						__instance.workstate = EWorkState.Running;
					} else __instance.workstate = EWorkState.Full;
					return;
				}
				//We don't actually know what this is.
				default: __instance.DetermineState(); return;
			}
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float _getupdDamper(ref MinerComponent __instance) {
			if(__instance.productCount >= Config.Buffer) {
				__instance.speedDamper = 0f;
            } else if(Config.DisableDampers) {//Above is how we get zero power on full
				__instance.speedDamper = 1f;
            } else if(__instance.productCount > 0) {
				//this is clunky but there's no other way to check if we have a station component
				float rate50 = 2.47f - 2.45f * ( __instance.productCount / 50f );
				rate50 = rate50 > 1f ? 1f : rate50 < 0f ? 0f : rate50;
				if(__instance.speedDamper == rate50) {
					float rateNew = 2.47f - 2.45f * (__instance.productCount / (float)Config.Buffer);
					__instance.speedDamper = rateNew > 1f ? 1f : rateNew;
				}
			}
			return __instance.speedDamper;
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
			//Time ticking and Internal Buffer filling (shortcut if both are capped)
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
					default: //fail gracefully- we literally don't know what to do for this.
						__runOriginal = true;
						return;
					case EMinerType.None: return;//"this isn't actually a minercomponent".
					case EMinerType.Water:
						vcnt = __instance.veinCount == 0
							? Config.WaterPumpVeinCount
							: __instance.veinCount
						;
						source = Config.OceanSourceType;
						prodID = factory.planet.waterItemId;
						break;
					case EMinerType.Oil:
						source = Config.OilSourceType;
						goto case EMinerType.Vein;
					case EMinerType.Vein: {
						#region Scan veins array, populate values

						switch(__instance.veinCount) {
							case < 0:
							case 0: break;
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
									source = Config.OceanSourceType;
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
						#region Handle __instnace.minimumVeinAmount updating (Vein-type Only)

						//this value is only really set on Vein types in vanilla.
						if(__instance.type == EMinerType.Vein) {
                            switch(source) {
								case ESourceType.FiniteDepleting:
                                    switch(Config.FiniteSourceTargeting) {
										case EFiniteSourceConsumptionTarget.Fullest:
											//If we're on Fullest that means we run out all at once, so min is actually max (as weird as that sounds).
											__instance.minimumVeinAmount = vpiFullest == -1 ? 0 : vmax;
											break;
										default:
											__instance.minimumVeinAmount = vpiLowest == -1 ? 0 : vmin;
											break;
									}
									break;
								case ESourceType.Diminishing:
									//go with our average, since that's sort-of correct.
									__instance.minimumVeinAmount = vcnt > 0 ? vtot / vcnt : 0;
									break;
								case ESourceType.Infinite:
									__instance.minimumVeinAmount = __instance.veinCount > 0 ? ARBITRARY_LARGE_NUMBER : 0;
									break;
								default:
									__instance.minimumVeinAmount = vpiLowest == -1 ? 0 : vmin;
									break;
							}
							
						}

                        #endregion
                        break;
					}
				}

				#endregion
				if(vcnt > 0) {
					//Time tick and mark as running
					if(__instance.time <= __instance.period) {
						__instance.time += (int)GetSingleTickTime_inline(
							source, vcnt, vtot, __instance.speed, miningSpeed, power, __instance.speedDamper
						);
						__result = 1U;
					}
					//Move from Source to Internal Buffer
					if(__instance.time >= __instance.period && __instance.productCount < Config.Buffer && prodID > 0) {
						int potential = __instance.time / __instance.period;
						__instance.productId = prodID;
						switch(source) {
							#region case ESourceType.Infinite: { ... } break;
							case ESourceType.Infinite: {
								__instance.productCount += potential;
								Interlocked.Add(ref productRegister[prodID], potential);
								__instance.time -= __instance.period * potential;
								break;
							}
							#endregion
							#region case ESourceType.FiniteDepleting: { ... } break;
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
											for(int i = potential; i-- > 0;) {
												if(_iu_cycleSeed_inline(ref curSeed, in miningRate)) {
													if(_iu_tryConsume_inline(ref vtarg, ref realized, ref consume)) break;
												} else realized++;//no consumption this seed
											}
											__instance.seed = curSeed;
										} else {
											for(int i = potential; i-- > 0;) {
												if(_iu_tryConsume_inline(ref vtarg, ref realized, ref consume)) break;
											}
										}
										if(consume > 0) {//skip reducing if nothing to reduce
											int groupIndex = vtarg.groupIndex;
											//don't update min-vein, we'll do that next pass.
											Interlocked.Add(ref factory.veinGroups[groupIndex].amount, -consume);
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
									__instance.productCount += realized;
									Interlocked.Add(ref productRegister[__instance.productId], realized);
									_safeAddFlagsToFactory_inline(ref factory, veinType);
									__instance.time -= __instance.period * realized;
								}
								break;
							}
							#endregion
							#region case ESourceType.Diminishing: { ... } break;
							case ESourceType.Diminishing: {
								var consRate = miningRate * DiminishingMiningRateModifier;
								if(consRate > 0f && vmax > Config.DiminishLimit) {
									int consume = 0;
									var curSeed = __instance.seed;
									for(int j = potential; j-- > 0;) {
										if(_iu_cycleSeed_inline(ref curSeed, in consRate)) consume++;
									}
									__instance.seed = curSeed;
									if(consume > 0) {
										//Always hitting Fullest works mostly just fine.
										vpi = vpiFullest;
										ref var vtarg = ref veinPool[vpi];
										var eaten = _iu_tryConsumeBulk_inline(ref vtarg, consume, Config.DiminishLimit);
										if(eaten > 0) {
											ref var vgrp = ref factory.veinGroups[vtarg.groupIndex];
											Interlocked.Add(ref factory.veinGroups[vtarg.groupIndex].amount, -eaten);
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
							//variations only on tick rate
							case ESourceType.InfiniteDiminished: goto case ESourceType.Infinite;
						}
					}
				}
			}
			_iu_export_inline(ref __instance, ref factory);
			_iu_prune_inline(ref __instance, in vcnt);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float GetSingleTickTime_inline(in ESourceType source
			, in int veinCount, in int veinAmountTotal
			, in int speed, in float miningSpeed
			, in float power, in float speedDamper
		) {
			switch(source) {
				default: throw new NotImplementedException();
				case ESourceType.Infinite:
				case ESourceType.FiniteDepleting:
					return power * speed * speedDamper * miningSpeed * veinCount;
				case ESourceType.InfiniteDiminished:
				case ESourceType.Diminishing:
					return power * speed * speedDamper * miningSpeed * veinAmountTotal * VeinData.oilSpeedMultiplier + veinCount * 0.5f;
			}
		}

		/// <summary>
		/// Returns if consumption should occur
		/// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool _iu_cycleSeed_inline(ref uint curSeed, in float miningRate) {
			curSeed = (uint)((ulong)(curSeed % 2147483646U + 1U) * 48271UL % 2147483647UL) - 1U;
			return (curSeed / 2147483646.0 < (double)miningRate);
		}

		/// <summary>
		/// Decrements vein by 1, increments realized & consume, returns if vein is now empty.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static bool _iu_tryConsume_inline(ref VeinData vtarg, ref int realized, ref int consume) {
            switch(Interlocked.Decrement(ref vtarg.amount)) {
				case < 0: return true;//nothing left to eat.
				case 0://we ate but we're out now
					realized++;
					consume++;
                    return true;
				case > 0://there's more to eat
					realized++;
					consume++;
					return false;
			}
		}
		/// <summary>
		/// Reduces vd.amount down to minRemaining, by at most reduceBy. Returns the amount actually reduced.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static int _iu_tryConsumeBulk_inline(ref VeinData vd, in int reduceBy, in int minRemaining) {
			int cur, to;
			do {
				cur = vd.amount;
				if(cur <= minRemaining) return 0;//lte for hardening
				to = cur - reduceBy;
				if(to < minRemaining) to = minRemaining;
			} while(Interlocked.CompareExchange(ref vd.amount, to, cur) != cur);
			return cur - to;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void _iu_export_inline(ref MinerComponent mc, ref PlanetFactory factory) {
			if(mc.productCount > 0) {
                if(mc.insertTarget > 0) {
					#region Direct belt output on mk1 miners
					//Just always pile up to 4. Avoids needing to calc a delta.
					byte toOut;
					int outputted;
					if(Remap != null) {
						//compat catch
						int pid = factory.entityPool[mc.entityId].protoId;
						if(!Remap.TryGetValue(MkRemapKey(pid, mc.productId), out var rm2)) {
							rm2 = (mc.productId, 1);
						}
						int adjusted = mc.productCount / rm2.ratio;
						toOut = (byte)((adjusted < 4) ? adjusted : 4);
						if(toOut > 0) {
							outputted = factory.InsertInto(mc.insertTarget, 0, rm2.to, toOut, 0, out _);
							mc.productCount -= outputted * rm2.ratio;
						}
					} else {
						toOut = (byte)((mc.productCount < 4) ? mc.productCount : 4);
						outputted = factory.InsertInto(mc.insertTarget, 0, mc.productId, toOut, 0, out _);
						mc.productCount -= outputted;
					}
					#endregion
				}
			}
            //Don't think this is actually *necessary*, so don't do it.
            //if(__instance.productCount == 0 && __instance.type == EMinerType.Vein) __instance.productId = 0;
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
