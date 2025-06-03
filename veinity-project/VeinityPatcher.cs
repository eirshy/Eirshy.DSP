using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

using Eirshy.DSP.VeinityProject.Enums;
using Eirshy.DSP.VeinityProject.Helpers;

using HarmonyLib;

namespace Eirshy.DSP.VeinityProject {

	class VeinityPatcher {
		const int ARBITRARY_LARGE_NUMBER = 500_000;//custom(?) but not sure if there's a reason I didn't use int.MaxValue - eir

    const int VEIN_ANIM_ORE_AMOUNT_LOW_TRIGGER = 20000;
    const int VEIN_ANIM_OIL_AMOUNT_LOW_TRIGGER = 25000;
    const float VEIN_ANIM_FLOAT_MULT = 5E-05f;


    static void _transcludedDependencies() {
			PlanetFactory pf = null;
			MinerComponent mc = new MinerComponent();

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

			_ = nameof(Precalc);
			pf.factorySystem.GameTick(0, false);
			//
		}


		public static void SetUp() {
			VeinityProject.Harmony.PatchAll(typeof(VeinityPatcher));
		}


		//Since we're handling the original behavior entirely internally, just bulldoze the whole
		//  method. This should *technically* be more performant than a prefix __runOriginal -> false
		//  in the event that nobody else has patches on it.
		[HarmonyTranspiler]
		[HarmonyPatch(typeof(StationComponent), nameof(StationComponent.UpdateVeinCollection))]
		static IEnumerable<CodeInstruction> DisableIL() {
			yield return new CodeInstruction(OpCodes.Ret);
		}
		//[HarmonyPrefix]
		static bool DisableRO() => false;


		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameSave), nameof(GameSave.LoadCurrentGame), new[] { typeof(string) })]
		public static void Precalc() {
			//we promote to doubles for rounding reasons
			double resMult = Math.Max(GameMain.data.gameDesc.resourceMultiplier, 5.0 / 12.0);//orig used floats
			double floatValue = MinerComponent.kOilAmountInvMultiplier;//decomp may transclude instead of reference
      DiminishingMiningRateModifier = floatValue / resMult;
    }
    static double DiminishingMiningRateModifier { get; set; }


    [HarmonyPrefix]
		[HarmonyPatch(typeof(MinerComponent), nameof(MinerComponent.SetPCState))]
		public static void SetPCState(PowerConsumerComponent[] pcPool
			, ref MinerComponent __instance, ref bool __runOriginal
		) {
			if(!__runOriginal)
				return;
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
				case EMinerType.None:
					return;//"this isn't actually a minercomponent".
				case EMinerType.Oil:
				case EMinerType.Vein: {
					if(__instance.veinCount == 0) {
						__instance.time = 0;
						__instance.workstate = EWorkState.Idle;
					} else if(__instance.productCount < Config.Buffer) {
						__instance.workstate = EWorkState.Running;
					} else
						__instance.workstate = EWorkState.Full;
					return;
				}
				case EMinerType.Water: {
					if(__instance.productCount < Config.Buffer) {
						__instance.workstate = EWorkState.Running;
					} else
						__instance.workstate = EWorkState.Full;
					return;
				}
				//We don't actually know what this is.
				default:
					__instance.DetermineState();
					return;
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
				float rate50 = 2.47f - 2.45f * (__instance.productCount / 50f);
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
			if(!__runOriginal)
				return;
			__runOriginal = false;
			__result = 0U;
			if(power < 0.1f)
				return;

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
					default: //fail vaguely gracefully- we literally don't know what to do for this.
						__runOriginal = true;
						return;
					case EMinerType.None:
						return;//"this isn't actually a minercomponent".
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
							case 0:
								break;
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
									} else
										__instance.veins[0] = 0;
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
										} else
											__instance.veins[i] = 0;
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
          #region Time Tick & Mark as Running
          if(__instance.time <= __instance.period && __instance.time < int.MaxValue) {

						int addTime;
            switch(source) {
              default: throw new NotImplementedException();
              case ESourceType.Infinite:
              case ESourceType.FiniteDepleting:
								addTime = (int)(power * __instance.speed * __instance.speedDamper * miningSpeed * vcnt);
								break;
              case ESourceType.InfiniteDiminished:
              case ESourceType.Diminishing:
                addTime = (int)(power * __instance.speed * __instance.speedDamper * miningSpeed * vtot * VeinData.oilSpeedMultiplier + vcnt * 0.5f);
								break;
            }

            //original lacks unchecked, but includes the overflow safety still, lol
            unchecked { __instance.time += addTime; }

						//exact overflow safety as they implemented it
            if(__instance.time < -2_000_000_000) {
              __instance.time = int.MaxValue;
            } else if(__instance.time < 0) {
              __instance.time = 0;
            }

            __result = 1U;
					}
					#endregion

					//need promotion to maybe deal with rounding issues...
					double miningRateDbl = miningRate;

					//Move from Source to Internal Buffer
					if(__instance.time >= __instance.period && __instance.productCount < Config.Buffer && prodID > 0) {
						int potential = __instance.time / __instance.period;
						__instance.productId = prodID;
						switch(source) {
							#region case ESourceType.Infinite: { ... } break;
							case ESourceType.Infinite: {
								__instance.productCount += potential;
								_ = Interlocked.Add(ref productRegister[prodID], potential);
								__instance.time -= __instance.period * potential;
								break;
							}
              #endregion

              case ESourceType.InfiniteDiminished: goto case ESourceType.Infinite; //GetSingleTickTime_inline did what this needs to do

              #region case ESourceType.FiniteDepleting: { ... } break;
              case ESourceType.FiniteDepleting: {
                #region Ensure Non-"oil" miningRate
                if(__instance.type == EMinerType.Oil) {
									miningRateDbl /= DiminishingMiningRateModifier;//this was baked into us at by the caller
								}
                #endregion
                #region pick finite target
                switch(Config.FiniteSourceTargeting) {
									default: throw new NotImplementedException();

									case EFiniteSourceConsumptionTarget.Cyclic: {
										var ci = __instance.currentVeinIndex % __instance.veinCount;
										vpi = __instance.veins[ci];
										for(int i = __instance.veinCount; i-- > 0;) {
											ci = (ci + 1) % __instance.veinCount;
											if(vpi <= 0)
												vpi = __instance.veins[ci];
											else
												break;
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
                #endregion
                #region harvest-from
                ref var vtarg = ref veinPool[vpi];
								if(vtarg.amount > 0) {
									int realized = potential;
									if(miningRateDbl > 0f) {
                    int eaten = 0;
                    #region calc Consumption, interpolate Realized, grow costFrac by remainder
                    double hunger = __instance.costFrac + ((double)miningRateDbl * potential);
                    int maxEat = (int)hunger;

                    if(maxEat > 0) {
                      eaten = _iu_tryConsumeBulk_inline(ref vtarg, maxEat, __instance.minimumVeinAmount);
                      realized =  eaten >= maxEat
												? potential
                        : eaten <= 0
													? 0
													: (int)Math.Ceiling(potential * (eaten / (double)maxEat) + 0.01)
                        ;
                    }

										__instance.costFrac = hunger - eaten;
                    #endregion
                    #region VeinGroup consume, Vein Anim, Factory Prune -- NOT Miner Prune!
                    if(eaten > 0) {
                      var rem = vtarg.amount;
											int groupIndex = vtarg.groupIndex;

                      Interlocked.Add(ref factory.veinGroups[groupIndex].amount, -eaten);
											_safeVeinAnim_largest_inline(ref factory.veinAnimPool[vpi]
												, rem >= VEIN_ANIM_ORE_AMOUNT_LOW_TRIGGER ? 0f : (1f - rem * VEIN_ANIM_FLOAT_MULT)
											);

                      //clean up node -- prune done later
                      if(rem <= 0) {
												//lock and < for safety; would be better if we could lock on marking it in-progress
												// so the others can early-exit waiting, but thats Hard without taking control of fac.RVWC.
												lock(veinPool) {
													if(vtarg.id == vpi) {//make sure nobody else killed it
														var type = (int)vtarg.type;
														var pos = vtarg.pos;
														factory.RemoveVeinWithComponents(vpi);
														factory.RecalculateVeinGroup(groupIndex);
														factory.NotifyVeinExhausted(type, pos);
													}
												}
											}
										}
                    #endregion
                  }

                  __instance.productCount += realized;
									_ = Interlocked.Add(ref productRegister[__instance.productId], realized);
									_safeAddFlagsToFactory_inline(ref factory, veinType);
									__instance.time -= __instance.period * realized;
								}
                #endregion
                break;
							}
							#endregion

							#region case ESourceType.Diminishing: { ... } break;
							case ESourceType.Diminishing: {
								if(miningRateDbl > 0f && vmax > Config.DiminishLimit) {
                  #region Ensure "oil" miningRate
                  if(__instance.type != EMinerType.Oil) {
                    miningRateDbl *= DiminishingMiningRateModifier;//this was baked into us at by the caller
                  }
                  #endregion
                  
                  double hunger = __instance.costFrac + ((double)miningRateDbl * potential);
									int maxEat = (int)hunger;
									if(maxEat > 0) {
										//Always hitting Fullest works mostly just fine.
										vpi = vpiFullest;
										ref var vtarg = ref veinPool[vpi];
										var eaten = _iu_tryConsumeBulk_inline(ref vtarg, maxEat, Config.DiminishLimit);
										__instance.costFrac = hunger - eaten;

										if(eaten > 0) {
											ref var vgrp = ref factory.veinGroups[vtarg.groupIndex];
											Interlocked.Add(ref factory.veinGroups[vtarg.groupIndex].amount, -eaten);
										}
										var rem = vtarg.amount;
										_safeVeinAnim_largest_inline(ref factory.veinAnimPool[vpi]
											, (rem >= VEIN_ANIM_OIL_AMOUNT_LOW_TRIGGER) ? 0f : (1f - (float)rem * VeinData.oilSpeedMultiplier)
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
				}
			}
			_iu_export_inline(ref __instance, ref factory, ref productRegister);
			_iu_prune_inline(ref __instance, in vcnt);
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
				to = Math.Max(cur - reduceBy, minRemaining);
			} while(Interlocked.CompareExchange(ref vd.amount, to, cur) != cur);
			return cur - to;
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void _iu_export_inline(ref MinerComponent mc, ref PlanetFactory factory, ref int[] prodReg) {
			if(mc.productCount > 0) {
				int moved;
				if(mc.insertTarget > 0) {
					#region Direct belt output for non-Station miners
					byte toOut;
					if(OreRemap.HasRemap) {
						var pid = factory.entityPool[mc.entityId].protoId;
						var orm = OreRemap.Get(pid, mc.productId);
						if(orm.IsDefault) {
							toOut = (byte)((mc.productCount < 4) ? mc.productCount : 4);
							moved = factory.InsertInto(mc.insertTarget, 0, mc.productId, toOut, 0, out _);
							mc.productCount -= moved;
						} else {
							int adjusted = orm.Ore2Prod(mc.productCount);
							toOut = (byte)((adjusted < 4) ? adjusted : 4);
							if(toOut > 0) {
								var consReg = GameMain.statistics.production.factoryStatPool[factory.index].consumeRegister;
								moved = factory.InsertInto(mc.insertTarget, 0, orm.ProductID, toOut, 0, out _);
								var cons = orm.Prod2Ore(moved);
								mc.productCount -= cons;
								_ = Interlocked.Add(ref consReg[mc.productId], cons);
								_ = Interlocked.Add(ref prodReg[orm.ProductID], moved);
							}
						}
					} else {
						toOut = (byte)((mc.productCount < 4) ? mc.productCount : 4);
						moved = factory.InsertInto(mc.insertTarget, 0, mc.productId, toOut, 0, out _);
						mc.productCount -= moved;
					}
					#endregion
				} else {
					ref var ent = ref factory.entityPool[mc.entityId];
					#region StationID-based st-ore-age
					if(ent.stationId > 0) {
						//Original forces index 0, we'll do the same till we have reason to not.
						ref var storeage = ref factory.transport.stationPool[ent.stationId].storage[0];
						int maxMove = storeage.max - storeage.count;
						if(maxMove <= 0)
							return;//busywait shortcut
						if(OreRemap.HasRemap) {
							var orm = OreRemap.Get(ent.protoId, mc.productId);
							if(orm.IsDefault) {
								moved = mc.productCount > maxMove ? maxMove : mc.productCount;
								mc.productCount -= moved;
								_ = Interlocked.Add(ref storeage.count, moved);
								storeage.itemId = mc.productId;
							} else {
								var consReg = GameMain.statistics.production.factoryStatPool[factory.index].consumeRegister;

								int adjusted = orm.Ore2Prod(mc.productCount);
								moved = adjusted > maxMove ? maxMove : adjusted;
								int cons = orm.Prod2Ore(moved);
								mc.productCount -= cons;
								_ = Interlocked.Add(ref storeage.count, moved);
								_ = Interlocked.Add(ref consReg[mc.productId], cons);
								_ = Interlocked.Add(ref prodReg[orm.ProductID], moved);
								storeage.itemId = orm.ProductID;
							}
						} else {
							moved = mc.productCount > maxMove ? maxMove : mc.productCount;
							mc.productCount -= moved;
							_ = Interlocked.Add(ref storeage.count, moved);
							storeage.itemId = mc.productId;
						}
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
				if(curTime >= newTime)
					return;
			} while(Interlocked.CompareExchange(ref anim.time, newTime, curTime) != curTime);
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void _safeAddFlagsToFactory_inline(ref PlanetFactory factory, in EVeinType addVeinType) {
			int flagToSet = 1 << (int)addVeinType;
			int cur;
			do {
				cur = factory._miningFlag;
				if((cur & flagToSet) == flagToSet)
					break;
			} while(Interlocked.CompareExchange(ref factory._miningFlag, cur | flagToSet, cur) != cur);
			do {
				cur = factory._veinMiningFlag;
				if((cur & flagToSet) == flagToSet)
					break;
			} while(Interlocked.CompareExchange(ref factory._veinMiningFlag, cur | flagToSet, cur) != cur);
		}


    // Removed code needed for old PRNG mining rate; kept here in case we wanna provide a "Legacy Mode" later
		// Was deleted in v0.2.7, in case you wanna see the old usage.
    /** /
    /// <summary>
    /// Returns if consumption should occur
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool _iu_cycleSeed_inline(ref uint curSeed, in float miningRate) {
      //todo: a where-tf-this-lives note
      curSeed = (uint)((ulong)(curSeed % 2147483646U + 1U) * 48271UL % 2147483647UL) - 1U;
      return (curSeed / 2147483646.0 < (double)miningRate);
    }

    /// <summary>
    /// Decrements vein by 1, increments realized & consume, returns if vein is now empty.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool _iu_tryConsume_inline(ref VeinData vtarg, ref int realized, ref int consume) {
      switch(Interlocked.Decrement(ref vtarg.amount)) {
        case < 0:
          return true;//nothing left to eat.
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
		/**/

  }
}