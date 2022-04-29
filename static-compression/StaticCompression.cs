using System;
using System.Threading.Tasks;
using BepInEx;
using HarmonyLib;

using Eirshy.DSP.Rythmn;

//todo: make research recipe compression required CustomBuffers, but as a soft dep

namespace Eirshy.DSP.StaticCompression {
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency(RythmnKit.GUID)]
    [BepInDependency(CUSTOM_BUFFERS_GUID)]
    public class StaticCompression : BaseUnityPlugin {
        public const string MODID = "StaticCompression";
        public const string ROOT = "eirshy.dsp.";
        public const string GUID = ROOT + MODID;
        public const string NAME = "Static Compression";
        public const string VERSION = "1.0.0.0";
        const string CUSTOM_BUFFERS_GUID = ROOT + "ReBuffer";

        internal static Harmony Harmony => _harmony.Value;
        readonly static Lazy<Harmony> _harmony = new Lazy<Harmony>(() => new Harmony(GUID));


        private void Awake() {
            var song = StaticSong.ForPlugin(typeof(StaticCompression));
            //song.HarmonyPatchAll(Harmony);//we don't use any patches
            song.ComposeAll(Config);
        }
    }
}
