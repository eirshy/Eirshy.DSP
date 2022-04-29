using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;

using Eirshy.DSP.Rythmn.Logging;

namespace Eirshy.DSP.Rythmn {
    public sealed class StaticSong : IReadOnlyDictionary<Type, StaticVerse> {
        readonly SortedList<Type, StaticVerse> _internal;
        StaticSong(Type source) {
            var listed = source.Assembly.GetTypes()
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(StaticVerse)))
                .ToDictionary(t => t, t => (StaticVerse)t.GetConstructor(Type.EmptyTypes)?.Invoke(null))
            ;
            listed.Where(kvp => kvp.Value is null).DoForEach(kvp => listed.Remove(kvp.Key));
            _internal = new SortedList<Type, StaticVerse>(listed, UtilAndExt.TypeComparer.Instance);
            RythmnKit.LogProvider.Log($"{_internal.Count} verses registered for {source.Name}!");
        }
        StaticSong(IEnumerable<StaticVerse> verses) {
            var dedup = verses.Where(v => v != null)
                .GroupBy(v => v.GetType())
                .ToDictionary(grp => grp.Key, grp => grp.First())
            ;
            _internal = new SortedList<Type, StaticVerse>(dedup);
        }

        #region static For/From pseudo-constructors

        /// <summary>
        /// Scans the assembly around plugin for anything implementing StaticVerse.
        /// </summary>
        /// <param name="plugin">Generally, you'll want to use: <c>typeof(YourPlugin)</c></param>
        public static StaticSong ForPlugin(Type plugin) => new StaticSong(plugin);
        /// <summary>
        /// Manually creates a bundle of StaticVerse objects, rather than using assembly scanning.
        /// </summary>
        /// <param name="verses">
        /// Generally, you'll want to use something like:
        /// <br /><c>
        /// new[] { new MyFirstVerse(), new MySecondVerse(), /*...*/ }
        /// </c>
        /// </param>
        public static StaticSong FromVerses(IEnumerable<StaticVerse> verses) => new StaticSong(verses);

        #endregion
        #region NonToolkit Actions

        public StaticSong HarmonyPatchAll(Harmony harmony) {
            Values.Where(sv => sv.HasHarmonyPatches)
                .Select(sv => sv.GetType())
                .DoForEach(harmony.PatchAll)
            ;
            return this;
        }
        public StaticSong HamonyPatchAll(Lazy<Harmony> harmony) {
            Values.Where(sv => sv.HasHarmonyPatches)
                .Select(sv => sv.GetType())
                .DoForEach(sv => harmony.Value.PatchAll(sv));
            ;
            return this;
        }

        #endregion
        #region StaticVerse All-do-X actions

        public StaticSong RegisterLogger(ManualLogSource bepinLogger) {
            var provider = new BepInManualLogger(bepinLogger);
            RegisterLogger(provider);
            return this;
        }
        public StaticSong RegisterLogger(ILogProvider provider) => Values.DoForEach(verse => verse.RegisterLogger(provider), this);

        public StaticSong AwakeAll() => Values.DoForEach(verse => verse.VerseAwake(), this);

        public StaticSong LoadConfigAll(ConfigFile config) => Values.DoForEach(verse => verse.LoadConfig(config), this);

        public StaticSong RegisterSetupPhasesAll() => Values.DoForEach(verse => verse.RegisterSetupPhases(), this);
        public StaticSong RegisterLoadEventsAll() => Values.DoForEach(verse => verse.RegisterLoadEvents(), this);
        internal StaticSong RegisterSaveEventsAll() => Values.DoForEach(verse => verse.RegisterSaveEvents(), this);

        #endregion
        #region Compose-All - aka both "Oh sweet, it just werks!" and "I wasn't paying attention, how do I use this again?"

        /// <summary>
        /// Composes all verses in this Song, awakening them and registering all of their events.
        /// <br /> Does NOT include a config file or a logger- are you sure this is what you want?
        /// </summary>
        /// <remarks>
        /// Obsolete mark is solely for the compiler warning. 
        /// This option is not likely to be removed.
        /// --Eirshy
        /// </remarks>
        [Obsolete(StaticVerse.NULLARY_COMPOSE_WARNING)]
        public StaticSong ComposeAll() => Values.DoForEach(verse => verse.Compose(), this);
        /// <summary>
        /// Composes all verses in this Song, awakening them and registering all of their events.
        /// <br /> Does NOT include a logger- are you sure this is what you want?
        /// </summary>
        public StaticSong ComposeAll(ConfigFile config) => Values.DoForEach(verse => verse.Compose(config), this);
        /// <summary>
        /// Composes all verses in this Song, awakening them and registering all of their events.
        /// <br /> Does NOT include a config file- are you sure this is what you want?
        /// </summary>
        public StaticSong ComposeAll(ILogProvider provider) => Values.DoForEach(verse => verse.Compose(provider), this);
        /// <summary>
        /// Composes all verses in this Song, awakening them and registering all of their events.
        /// <br /> Does NOT include a config file- are you sure this is what you want?
        /// </summary>
        public StaticSong ComposeAll(ManualLogSource bepinLogger) {
            var provider = new BepInManualLogger(bepinLogger);
            Values.DoForEach(verse => verse.Compose(provider));
            return this;
        }
        /// <summary>
        /// Composes all verses in this Song, awakening them and registering all of their events.
        /// </summary>
        public StaticSong ComposeAll(ILogProvider provider, ConfigFile config) => Values.DoForEach(verse => verse.Compose(provider, config), this);
        /// <summary>
        /// Composes all verses in this Song, awakening them and registering all of their events.
        /// </summary>
        public StaticSong ComposeAll(ManualLogSource bepinLogger, ConfigFile config) {
            var provider = new BepInManualLogger(bepinLogger);
            Values.DoForEach(verse => verse.Compose(provider, config));
            return this;
        }

        #endregion
        #region IROD impls (through _internal)

        public bool ContainsKey(Type key) => _internal.ContainsKey(key);
        bool IReadOnlyDictionary<Type, StaticVerse>.TryGetValue(Type key, out StaticVerse value) => _internal.TryGetValue(key, out value);

        public StaticVerse this[Type key] => _internal[key];

        public IEnumerable<Type> Keys => _internal.Keys;

        public IEnumerable<StaticVerse> Values => _internal.Values;

        public int Count => _internal.Count;

        IEnumerator<KeyValuePair<Type, StaticVerse>> IEnumerable<KeyValuePair<Type, StaticVerse>>.GetEnumerator() => _internal.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_internal).GetEnumerator();

        #endregion
    }
}
