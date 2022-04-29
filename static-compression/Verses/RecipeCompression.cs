using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Configuration;
using HarmonyLib;

using Eirshy.DSP.Rythmn;
using Eirshy.DSP.Rythmn.Protos;
using Eirshy.DSP.Rythmn.Enums;

namespace Eirshy.DSP.StaticCompression.Verses {
    class RecipeCompression : StaticVerse {
        public override EOnLoadPrefabSync WillNeedSyncsFor => EOnLoadPrefabSync.PowerConsumer | EOnLoadPrefabSync.Other_Recipies;
        public static int MaxAwareFractRatio => DataLookups.Belt_PhysicalMaxStackSize;

        //hoisted for easy toggle-off if we decide we wanna
        static ERecipeType[] StdAppliesTo => new[] {
            ERecipeType.Assemble,
            ERecipeType.Smelt,
            ERecipeType.Refine,
            ERecipeType.Chemical,
            ERecipeType.Particle,
            ERecipeType.Research,
        };



        enum EWSIDCats {
            _NONE,
            CommonOre, UncommonOre, RareCrystal, UniMags, Ocean, Hydrogen, FireIce,
        }
        string _wscats_desc(EWSIDCats @for) {
            switch(@for) {
                case EWSIDCats.CommonOre: return "the common ores- Iron, Copper, Stone, Silicon, and Titanium-";
                case EWSIDCats.FireIce: return "Fire Ice";
                case EWSIDCats.Hydrogen: return "Hydrogen";
                case EWSIDCats.Ocean: return "the ocean-available fluids- Water and Sulfuric Acid-";
                case EWSIDCats.RareCrystal: return "the rare crystals- Spiniform, Organic, and Optical Gating-";
                case EWSIDCats.UncommonOre: return "the uncommon ores- Kimberlite and Fractal Silica-";
                case EWSIDCats.UniMags: return "Unipolar Magnets";
                case EWSIDCats._NONE: throw new InvalidOperationException();
#if DEBUG
                default: throw new NotImplementedException();
#else
                default: return "";
#endif
            }
        }
        IEnumerable<int> _wscats_items(EWSIDCats @for) {
            switch(@for) {
                case EWSIDCats.CommonOre: return new int[] { Item.RawCopper, Item.RawIron, Item.RawSilic, Item.RawStone, Item.RawTitan, };
                case EWSIDCats.FireIce: return new int[] { Item.RawFireIce, };
                case EWSIDCats.Hydrogen: return new int[] { Item.RawHyd, };
                case EWSIDCats.Ocean: return new int[] { Item.RawWater, Item.RawSulfAcid, };
                case EWSIDCats.RareCrystal: return new int[] { Item.RawSpinCry, Item.RawOrgCry, Item.RawOpGCry, };
                case EWSIDCats.UncommonOre: return new int[] { Item.RawFracSili, Item.RawKimber, };
                case EWSIDCats.UniMags: return new int[] { Item.RawUniMag, };
                case EWSIDCats._NONE: return Enumerable.Empty<int>();
#if DEBUG
                default: throw new NotImplementedException();
#else
                default: return Enumerable.Empty<int>();
#endif
            }
        }

        bool BeltAware { get; set; }
        bool StdHasHalfSecMinimum { get; set; }

        double XL { get; set; }
        double XL_PROD { get; set; }
        double MAX_SPEED_BOOST { get; set; }

        string WontSprayInpManual { get; set; }
        IList<EWSIDCats> WontSprayInpCats { get; set; }
        string WontSprayResManual { get; set; }
        IList<EWSIDCats> WontSprayResCats { get; set; }

        IDictionary<ERecipeType, int> StdCompression { get; set; }
        ILookup<ERecipeType, ItemProto> _facs { get; set; }
        IDictionary<ERecipeType, double> _maxBase { get; set; }
        IDictionary<ERecipeType, int> _powerMult { get; set; }

        bool NeverSpray { get; set; }
        bool AlwaysSpraySpeed { get; set; }
        bool SpraySpeedIfNotProd { get; set; }
        ISet<int> _wsInp { get; set; }
        ISet<int> _wsRes { get; set; }

        int FractCompressionPerc { get; set; }

        static int MatrixBufferSize { get; set; } = 3;

        protected override void _stanza_config(ConfigFile config) {
            const string HDR = nameof(RecipeCompression);
            const string HDR_STDMULT = HDR + ".StdMult";
            const string HDR_FRACT = HDR + ".Fract";
            const string HDR_SPRAY = HDR + ".Spraying";
            const string HDR_WONTSPRAY = HDR + ".WontSpray";
            const string HDR_WONTSPRAY_INP = HDR_WONTSPRAY + ".Input";
            const string HDR_WONTSPRAY_RES = HDR_WONTSPRAY + ".Result";
            const string HDR_WONTSPRAY_REGISTRY = "zz." + HDR_WONTSPRAY + ".Manual";

            BeltAware = config.Bind<bool>(HDR, nameof(BeltAware), true, new ConfigDescription(
                "If true, we will limit our multipliers based on the throughput of the top-tier belt and top-tier crafter in a category." +
                "\nPreventing a crafter from consuming/outputting more than a single unstacked belt prevents a lot of potential issues with" +
                " inserter timings, as well as keeps your builds from becoming too wonky. However, if you don't want this feature, it can be" +
                " disabled."
            )).Value;
            StdHasHalfSecMinimum = config.Bind<bool>(HDR, nameof(StdHasHalfSecMinimum), true, new ConfigDescription(
                "If true, we will ignore any recipes for standard compression that have a base time under (but not including) 0.5s." +
                "\nBy vanilla, there are no recipes that do this. However, some mods, like More Megastructures, may add utility-like" +
                " recipes that should be completely ignored by our compression method. This being enabled guards against needing some obvious" +
                " explicit mod compatibility support. Feel free to disable if you either don't care, or want sub-half-second recipes compressed."
            )).Value;


            StdCompression = StdAppliesTo.ToDictionary(k => k, k => config.Bind<int>(HDR_STDMULT, $"{nameof(StdCompression)}_{k}", 1, new ConfigDescription(
                  $"The Standard Maximum Compression for {k}-type recipe crafters." +
                  $"\nNote that if {nameof(BeltAware)} is true, the power cost increase will be limited to the highest multiplier actually used," +
                  $" rather than the value here itself."
                  , new AcceptableValueRange<int>(1, 256)
              )).Value
            );
            FractCompressionPerc = config.Bind<int>(HDR_FRACT, nameof(FractCompressionPerc), 1_00, new ConfigDescription(
                "The percentile compression factor for Fractionators." +
                "\nAs an example, a value of 1250 would translate to multiplying the rate of fractionating success by x12.5." +
                " For vanilla's Hydrogen-to-Deuterium recipe, this would set it to 1 in 8." +
                $"\nNote that if {nameof(BeltAware)} is true, the success rate will be limited to at most 1 in 4, and the highest multiplier" +
                $" actually used will be used as the power cost multiplier, rather than the value here itself."
                , new AcceptableValueRange<int>(1_00, 100_00)
            )).Value;



            NeverSpray = config.Bind<bool>(HDR_SPRAY, nameof(NeverSpray), false, new ConfigDescription(
                "If true, we will never factor spray effects into our belt awareness multiplier limiting."
            )).Value;
            AlwaysSpraySpeed = config.Bind<bool>(HDR_SPRAY, nameof(AlwaysSpraySpeed), false, new ConfigDescription(
                "If true, we will always assume Speed spraying rather than Production spraying, even if Production is available."
            )).Value;
            SpraySpeedIfNotProd = config.Bind<bool>(HDR_SPRAY, nameof(SpraySpeedIfNotProd), true, new ConfigDescription(
                "If true, we assume that you are still spraying any non-Production-allowed recipe for Speed."
            )).Value;


            //WONT_SPRAY

            var allWSCats = ((EWSIDCats[])Enum.GetValues(typeof(EWSIDCats)))
                .Where(e => e != EWSIDCats._NONE)
                .OrderBy(e => (int)e)
                .ToList()
            ;
            const string WONT_SPRAY_INP_EXPLN = "" +
                "\nSo long as ALL items in the ingredients list for a given recipe have been" +
                " marked as WONT-SPRAY-INP, the belt limits for a given recipe will assume you will" +
                " NOT be spraying."
            ;
            const string WONT_SPRAY_RES_EXPLN = "" +
                "\nSo long as ANY items in the results list for a given recipe have been" +
                " marked as WONT-SPRAY-RES, the belt limits for a given recipe will assume you will" +
                " NOT be spraying."
            ;

            WontSprayInpCats = allWSCats.Select(cat =>
                config.Bind<bool>(HDR_WONTSPRAY_INP, $"{nameof(WontSprayInpCats)}_{cat}", false, new ConfigDescription(
                    $"If set, we'll mark all of {_wscats_desc(cat)} as WONT-SPRAY-INP." +
                    $"{WONT_SPRAY_INP_EXPLN}"
                )).Value ? cat : EWSIDCats._NONE
            ).ToList();
            WontSprayResCats = allWSCats.Select(cat =>
                config.Bind<bool>(HDR_WONTSPRAY_RES, $"{nameof(WontSprayResCats)}_{cat}", false, new ConfigDescription(
                    $"If set, we'll mark all of {_wscats_desc(cat)} as WONT-SPRAY-INP." +
                    $"{WONT_SPRAY_RES_EXPLN}"
                )).Value ? cat : EWSIDCats._NONE
            ).ToList();

            WontSprayInpManual = config.Bind<string>(HDR_WONTSPRAY_REGISTRY, nameof(WontSprayInpManual), "", new ConfigDescription(
                "A comma-separated list of Item ProtoIDs to mark as WONT-SPRAY-INP." +
                $"{WONT_SPRAY_INP_EXPLN}" +
                "\nExample, to disable spraying on recipes using Iron and Copper Plates," +
                $" you would set this to (without the quotes)" +
                $"\n\"{Item.RefIron.Id}, {Item.RefCopper.Id}\""
            )).Value;
            WontSprayResManual = config.Bind<string>(HDR_WONTSPRAY_REGISTRY, nameof(WontSprayResManual), "", new ConfigDescription(
                "A comma-separated list of Item ProtoIDs to mark as WONT-SPRAY-RES." +
                $"{WONT_SPRAY_RES_EXPLN}" +
                "\nExample, to disable spraying on recipes that produce Graphene and Carbon Nanotubes," +
                " you would set this to (without the quotes)" +
                $"\n\"{Item.RefGraph.Id}, {Item.RefCarbTube.Id}\""
            )).Value;


        }

        protected override void _stanza_setup_ProtosCreatedReadOnly() => createdRO_CalcSettings();
        protected override void _stanza_setup_ProtosCreated() {
            created_DoStandard();
            created_DoFract();
        }

        void createdRO_CalcSettings() {
            double ASSEMBLER_SPEED_OF_ONE = DataLookups.PrefabUnit_Per_One__AssemblerSpeed;
            double RESEARCH_SPEED_OF_ONE = DataLookups.PrefabUnit_Per_One__ResearchSpeed;
            XL = 60 * DataLookups.Belt_MaxSpeed_ItemsPerSecond;
            XL_PROD = XL / (1.0 + DataLookups.Proli_MaxProd);
            MAX_SPEED_BOOST = DataLookups.Proli_MaxSpeed;

            //load same-key dicts
            _facs = LDB.items.dataArray
                .Where(ite => false
                    || (ite.prefabDesc.isAssembler && StdCompression.ContainsKey(ite.prefabDesc.assemblerRecipeType))
                    || (ite.prefabDesc.isLab && StdCompression.ContainsKey(ERecipeType.Research))
                )
                .ToLookup(ite =>
                    ite.prefabDesc.isAssembler ? ite.prefabDesc.assemblerRecipeType
                    : ite.prefabDesc.isLab ? ERecipeType.Research
                    : throw new NotImplementedException($"Unknown facility recipe on Proto ID ${ite.ID};")
                )
            ;
            _maxBase = _facs
                .GroupBy(grp => grp.Key, grp =>
                    grp.Select(ite =>
                        ite.prefabDesc.isAssembler ? ite.prefabDesc.assemblerSpeed / ASSEMBLER_SPEED_OF_ONE
                        : ite.prefabDesc.isLab ? ite.prefabDesc.labResearchSpeed / RESEARCH_SPEED_OF_ONE
                        : throw new NotImplementedException($"Unknown facility speed field for RecType category {grp.Key}")
                    ).Max()
                )
                .ToDictionary(grp => grp.Key, grp => grp.Max())//can probably be .First().
            ;
            _powerMult = _maxBase.Keys.ToDictionary(k => k, k => 1);

            //Wont-Spray bullshit
            _wsInp = WontSprayInpCats.SelectMany(_wscats_items)
                .Concat(
                    WontSprayInpManual.Split(',').Select(s => s.Trim())
                    .Select(s => int.TryParse(s, out var i) ? (int?)i : null)
                    .Where(ni => ni.HasValue)
                    .Select(ni => ni.Value)
                )
                .ToSizedHashSet()
            ;

            _wsRes = WontSprayResCats.SelectMany(_wscats_items)
                .Concat(
                    WontSprayResManual.Split(',').Select(s => s.Trim())
                    .Select(s => int.TryParse(s, out var i) ? (int?)i : null)
                    .Where(ni => ni.HasValue)
                    .Select(ni => ni.Value)
                )
                .ToSizedHashSet()
            ;
        }

        void created_DoStandard() {

            //recipe walk
            var all = LDB.recipes.dataArray;
            int limOut;
            int limIn;
            int mult;
            int pickMax = 0;
            int pickLO = 0;
            int pickLI = 0;
            foreach(var rp in all) {
                if(StdCompression.TryGetValue(rp.Type, out var maxMult)) {
                    if(!BeltAware) {
                        for(int i = 0; i < rp.ItemCounts.Length; i++) rp.ItemCounts[i] *= maxMult;
                        for(int i = 0; i < rp.ResultCounts.Length; i++) rp.ResultCounts[i] *= maxMult;
                        pickMax++;
                        continue;
                    }
                    if(StdHasHalfSecMinimum && rp.TimeSpend < 30) continue;

                    bool wontspray = NeverSpray || rp.Items.All(_wsInp.Contains) || rp.Results.Any(_wsRes.Contains);
                    bool useProd = !wontspray && rp.productive && !AlwaysSpraySpeed;
                    bool useSpeed = !wontspray && ((!rp.productive && SpraySpeedIfNotProd) || AlwaysSpraySpeed);

                    double base_rpm = 1
                        * 60.0 //min -> sec
                        * 60.0 //sec -> tss (1/60)
                        * _maxBase[rp.Type]//facility-specific
                        * (useSpeed ? MAX_SPEED_BOOST : 1)
                        / rp.TimeSpend//in tss
                    ;
                    double XL_out = useProd ? XL_PROD : XL;

                    limIn = (int)Math.Floor(XL / (base_rpm * rp.ItemCounts.Max()));
                    limOut = (int)Math.Floor(XL_out / (base_rpm * rp.ResultCounts.Max()));
                    mult = maxMult < limIn
                        ? maxMult < limOut
                            ? (0 * pickMax++) + maxMult
                            : (0 * pickLO) + limOut
                        : limIn < limOut ? (0 * pickLI++) + limIn
                        : (0 * pickLO++) + limOut
                    ;

                    if(mult <= 1) continue;
                    for(int i = 0; i < rp.ItemCounts.Length; i++) rp.ItemCounts[i] *= mult;
                    for(int i = 0; i < rp.ResultCounts.Length; i++) rp.ResultCounts[i] *= mult;
                    if(_powerMult[rp.Type] < mult) _powerMult[rp.Type] = mult;
                }
            }
            Log($"MaxMult: {pickMax} -- In-Limit: {pickLI} -- Out-Limit: {pickLO} -- NoChange: {LDB.recipes.Length - pickMax - pickLI - pickLO}");

            //power mults as highest actually-used value
            foreach(var grp in _facs) {
                var powermult = _powerMult[grp.Key];
                grp.DoForEach(f => f.prefabDesc.workEnergyPerTick *= powermult);
                Log($"{grp.Key} power increased by x{powermult}");
            }
        }
        void created_DoFract() {
            if(FractCompressionPerc < 0) return;

            var fracts = LDB.items.dataArray.Where(ip => ip.prefabDesc.isFractionator).ToList();
            var recs = LDB.recipes.dataArray.Where(rp => rp.Type == ERecipeType.Fractionate).ToList();
            var powerPerc = BeltAware ? 1 : FractCompressionPerc;

            foreach(var rp in recs) {
                var baseIn = rp.ItemCounts.First();
                var baseOut = rp.ResultCounts.First();

                var maxIn = baseIn * 100;
                var maxOut = baseOut * FractCompressionPerc;
                //technically could simplify this, but why bother?

                if(BeltAware) {
                    if(maxIn / maxOut > MaxAwareFractRatio) {
                        rp.ItemCounts[0] = MaxAwareFractRatio;
                        rp.ResultCounts[0] = 1;
                        powerPerc = baseIn * 100 / baseOut / MaxAwareFractRatio;
                    } else {
                        rp.ItemCounts[0] = maxIn;
                        rp.ResultCounts[0] = maxOut;
                        if(powerPerc < FractCompressionPerc) powerPerc = FractCompressionPerc;
                    }
                } else {
                    rp.ItemCounts[0] = maxIn;
                    rp.ResultCounts[0] = maxOut;
                }
            }
            Log($"{recs.Count} Fract recipes incread by up to {powerPerc}%");

            fracts.DoForEach(f => f.prefabDesc.workEnergyPerTick = f.prefabDesc.workEnergyPerTick * powerPerc / 100);
            Log($"{fracts.Count} Fractionators power increased by {powerPerc}%");
        }


    }
}
