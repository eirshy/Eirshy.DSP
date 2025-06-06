﻿using System;
using System.Threading;

using BepInEx;
using BepInEx.Logging;

using HarmonyLib;


namespace Eirshy.DSP.VeinityProject {

  [BepInPlugin(GUID, NAME, VERSION)]
  [BepInDependency(GUID_SmelterMiner, BepInDependency.DependencyFlags.SoftDependency)]
  public class VeinityProject : BaseUnityPlugin {
    public const string MODID = "VeinityProject";
    public const string ROOT = "eirshy.dsp.";
    public const string GUID = ROOT + MODID;
    public const string VERSION = "0.2.8";
    public const string NAME = "VeinityProject";

    internal const string GUID_SmelterMiner = "Gnimaerd.DSP.plugin.SmelterMiner";

    internal static Harmony Harmony => _harmony.Value;
    readonly static Lazy<Harmony> _harmony = new(() => new Harmony(GUID));

    static internal ManualLogSource Logs { get; private set; }

    private void Awake() {
      Logs = Logger;
      Logger.LogMessage("VeinityProject powdering up!");
      DSP.VeinityProject.Config.Load(Config);
      SmelterMinerCompat.SetUpAwake();
      VeinityPatcher.SetUp();

      Harmony.PatchAll(typeof(VeinityProject));
    }


    static bool hasLoaded = false;
    [HarmonyPostfix]
    [HarmonyPatch(typeof(VFPreload), nameof(VFPreload.InvokeOnLoadWorkEnded))]
    public static void SetupLate() {
      if(hasLoaded)
        return;
      SmelterMinerCompat.SetUpLate();
      //---
      Helpers.OreRemap.Bake();
      hasLoaded = true;
    }
  }
}
