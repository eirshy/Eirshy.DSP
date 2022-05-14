using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx.Configuration;

using Eirshy.DSP.Rythmn;
using Eirshy.DSP.Rythmn.Enums;

//That's not how acc timing works...
//  Might have to actually do overrides on ExchangerComponent to make it possible to change time-per

namespace Eirshy.DSP.StaticCompression.Verses {
    class ByTheKilowatt : StaticVerse {
        public override EOnLoadPrefabSync WillNeedSyncsFor => 
            EOnLoadPrefabSync.PowerAccumulator
            | EOnLoadPrefabSync.PowerExchanger
            | EOnLoadPrefabSync.PowerGenerator
        ;

        ConfigFile _config;

        long ExchangerTarget { get; set; }
        int AccumulatorTargetSeconds { get; set; }
        int TargetAccTick => AccumulatorTargetSeconds * 60;

        enum EPassiveGen { Solar, Wind, Geothermal, RayReciever, Unknown }
        IDictionary<EPassiveGen, long> PassiveTargets;
        IDictionary<int, long> BurnerTargets;

        const string CFG_HDR = nameof(ByTheKilowatt);
        const string CFG_HDR_EXACC = CFG_HDR + ".ExAcc";
        const string CFG_HDR_PASSIVE = CFG_HDR + ".PassiveGens";
        const string CFG_HDR_DYNABURN = CFG_HDR + ".DynaBurnerGens";
        const string CFG_EXPLN = "\nPositive values are interpreted as the exact KW rate you want max-tier power source to operate at." +
            "\nNegative values are interpreted as a (positive) multiplier instead (ie, -2 would multiply the generation rate by 2)" +
            "\nA value of 0 indicates no change."
        ;
        const string CFG_RAYR_NOTE = "\nNote that a KW-based setting will be applied as the *base* rate. Full recieving is x2.5," +
            " a grav lens is x2, blue spray on the lens is another x2, and product mode is x8."
        ;
        string AddRayRNote(EPassiveGen epg) => epg == EPassiveGen.RayReciever ? CFG_RAYR_NOTE : "";

        long BindDynamicBurner(int type, string name) {
            return _config.Bind<long>(CFG_HDR_DYNABURN, $"BurnerType_{type}_Target", 0, new ConfigDescription(
                $"Target for burner generators which consume fuel of type {type}- such as {name}.{CFG_EXPLN}"
            )).Value;
        }

        protected override void _stanza_config(ConfigFile config) {
            _config = config;

            ExchangerTarget = config.Bind<long>(CFG_HDR_EXACC, nameof(ExchangerTarget), 0, new ConfigDescription(
                "Target for Exchangers." + CFG_EXPLN
            )).Value;

            AccumulatorTargetSeconds = config.Bind<int>(CFG_HDR_EXACC, nameof(AccumulatorTargetSeconds), 6, new ConfigDescription(
                "Number of seconds that the max-level Exchanger will take to fully drain/fill a max-level Accumulator."                
                , new AcceptableValueRange<int>(1, 60)
            )).Value;


            //This enum's static so just do each manually
            PassiveTargets = ((EPassiveGen[])Enum.GetValues(typeof(EPassiveGen)))
                .Where(k=>k!= EPassiveGen.Unknown)
                .OrderBy(k=>k.ToString())
                .ToDictionary(k => k, k => config.Bind<long>(CFG_HDR_PASSIVE, $"{k}Target", 0, new ConfigDescription(
                    $"Target for {k}-based generators.{CFG_EXPLN}" +
                    $"{AddRayRNote(k)}"
                )).Value)
            ;

            //Burner targets are loaded in createdRO_CalcSettings();
        }

        IList<ItemProto> _exs;
        IList<ItemProto> _accs;
        ILookup<EPassiveGen, ItemProto> _passiveGens;
        ILookup<int, ItemProto> _burnerGens;

        protected override void _stanza_setup_ProtosCreatedReadOnly() {
            createdRO_CalcSettings();
        }
        protected override void _stanza_setup_ProtosCreated() {
            created_DoExAcc();
            created_DoGens();
        }

        void createdRO_CalcSettings() {
            //gather protos
            var scanall = LDB.items.dataArray.Where(ip =>
                ip.prefabDesc.isPowerGen
                || ip.prefabDesc.isPowerExchanger
                || ip.prefabDesc.isAccumulator
            ).ToList();
            //accs with heat = 0 are empties, not fulls
            _accs = scanall.Where(ip => ip.prefabDesc.isAccumulator && ip.HeatValue != 0).ToList();
            _exs = scanall.Where(ip => ip.prefabDesc.isPowerExchanger).ToList();

            //Categorize the rest and bind our configs
            var otherGennies = scanall.Where(ip => ip.prefabDesc.isPowerGen).ToList();
            _burnerGens = otherGennies
                .Where(ip => ip.prefabDesc.fuelMask != 0)
                .ToLookup(brn => brn.prefabDesc.fuelMask & -brn.prefabDesc.fuelMask)
            ;
            var fuels = LDB.items.dataArray.Where(ip => ip.FuelType != 0).ToLookup(f=>f.FuelType);
            BurnerTargets = new Dictionary<int, long>(_burnerGens.Count);
            foreach(var grp in _burnerGens) {
                var hottest = fuels[grp.Key]
                    .Aggregate((cur, nw) =>
                        cur == null ? nw
                        : cur.HeatValue < nw.HeatValue ? nw
                        : cur
                    )
                ;
                BurnerTargets.Add(grp.Key, BindDynamicBurner(grp.Key, hottest.name));
            }

            _passiveGens = otherGennies
                .Where(ip => ip.prefabDesc.fuelMask == 0)
                .ToLookup(ip => {
                    if(ip.prefabDesc.photovoltaic) return EPassiveGen.Solar;
                    if(ip.prefabDesc.windForcedPower) return EPassiveGen.Wind;
                    if(ip.prefabDesc.gammaRayReceiver) return EPassiveGen.RayReciever;
                    if(ip.prefabDesc.geothermal) return EPassiveGen.Geothermal;
                    return EPassiveGen.Unknown;
                })
            ;
        }

        void created_DoExAcc() {
            //one setting is declarative, we need to always enforce now

            double maxBase_extick = _exs.Select(ex => ex.prefabDesc.exchangeEnergyPerTick).Max();
            double exmult = ExchangerTarget > 0 ? ExchangerTarget * 1_000.0 / 60.0 / maxBase_extick : -ExchangerTarget;
            if(ExchangerTarget != 0) {
                foreach(var ex in _exs) {
                    ex.prefabDesc.exchangeEnergyPerTick = (long)Math.Ceiling(ex.prefabDesc.exchangeEnergyPerTick * exmult);
                    ex.prefabDesc.maxAcuEnergy = (long)Math.Ceiling(ex.prefabDesc.maxAcuEnergy * exmult);
                    ex.prefabDesc.maxExcEnergy = (long)Math.Ceiling(ex.prefabDesc.maxExcEnergy * exmult);
                }
            } else exmult = 1.0; //fix mult;
            double multed_extick = maxBase_extick * exmult;

            //now to find out how much we're shifting time by
            double maxBase_accv = _accs.Select(acc => acc.HeatValue).Max();
            double maxBase_acctick = maxBase_accv / maxBase_extick;
            double perTick = multed_extick * (TargetAccTick / maxBase_acctick);

            foreach(var acc in _accs) {
                double myBaseTick = acc.HeatValue / maxBase_extick;
                acc.HeatValue = (long)Math.Ceiling(myBaseTick * perTick);
                acc.prefabDesc.maxAcuEnergy = acc.HeatValue;
                //This part lets you use them as limited exchangers, so sync with exmult directly
                acc.prefabDesc.inputEnergyPerTick = (long)Math.Ceiling(acc.prefabDesc.inputEnergyPerTick * exmult);
                acc.prefabDesc.outputEnergyPerTick = (long)Math.Ceiling(acc.prefabDesc.outputEnergyPerTick * exmult);
            }
            Log($"EnEx ({_exs.Count}) inc by x{exmult} and Acc ({_accs.Count}) scaled to a max of {AccumulatorTargetSeconds}s");
        }
        void created_DoGens() {
            if(_passiveGens[EPassiveGen.Unknown]?.Any() ?? false) {
                Log($"{_passiveGens[EPassiveGen.Unknown].Count()} unknown generator types found! --" +
                    $"\n... ... {String.Join("\n... ...", _passiveGens[EPassiveGen.Unknown].Select(gen => gen.name))}"
                );
            }
            foreach(var kvp in PassiveTargets) {
                var gens = _passiveGens[kvp.Key];
                if(gens?.Any() ?? false) created_DoGens_inner(gens, kvp.Value, $"{kvp.Key}", false);
            }
            foreach(var kvp in BurnerTargets) {
                var burners = _burnerGens[kvp.Key];
                if(burners?.Any() ?? false) created_DoGens_inner(burners, kvp.Value, $"Mask #{kvp.Key}", true);
            }
        }
        void created_DoGens_inner(IEnumerable<ItemProto> ieg, long target, string lbl, bool hasFuel = false) {
            if(target == 0) return;
            var gens = ieg as List<ItemProto> ?? ieg.ToList();

            double maxGenTick = gens.Select(gen => gen.prefabDesc.genEnergyPerTick).Max();
            double mult = target > 0 ? target * 1_000.0 / 60.0 / maxGenTick : -target;
            foreach(var gen in gens) {
                gen.prefabDesc.genEnergyPerTick = (long)Math.Ceiling(gen.prefabDesc.genEnergyPerTick * mult);
                if(hasFuel) gen.prefabDesc.useFuelPerTick = (long)Math.Ceiling(gen.prefabDesc.useFuelPerTick * mult);
            }

            Log($"{lbl} Gens ({gens.Count}) inc by x{mult}");
        }
    }
}
