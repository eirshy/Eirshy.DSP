using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using BepInEx.Configuration;

using Eirshy.DSP.Rythmn.Enums;
using Eirshy.DSP.Rythmn.Logging;

namespace Eirshy.DSP.Rythmn {

    public abstract class StaticVerse {
        #region Exceptions ... ... ...

        public class MissingConfigException : InvalidOperationException {
            public MissingConfigException(string msg) : base(msg) { }
        }

        #endregion

        #region Override-Virtual Settings ...
        /// <summary>
        /// If true (default), we will include a log message prior to every non-visitor starting its work.
        /// <br />If false, we won't.
        /// </summary>
        public virtual bool EnableStanzaLogging => true;

        /// <summary>
        /// If true (default), we will automatically attempt to catch exceptions and log them (via our Logger's LogFatal).
        /// <br />If false, exceptions will fall where they lie.
        /// </summary>
        public virtual bool EnableExceptionCatching => true;
        /// <summary>
        /// If true (default), and EnableExceptionCatching is true, we will rethrow caught exceptions after logging.
        /// <br />If false, and EnableExceptionCatching is true, we will NOT rethrow exceptions.
        /// </summary>
        public virtual bool RethrowExceptionsAfterLogging => true;

        /// <summary>
        /// Any prefabs you want StaticBeat to run the auto-sync for.
        /// <br />We'll automatically request them on <c>VerseAwake</c>, regardless of if you set a <c>_stanza_awake</c>
        /// </summary>
        public virtual EOnLoadPrefabSync WillNeedSyncsFor => EOnLoadPrefabSync._NONE;

        /// <summary>
        /// If true, we will not throw an error if a <c>_stanza_config</c> is provided
        /// and we were given <c>null</c> as our config file.
        /// <br />If false (default), we will throw an error.
        /// </summary>
        public virtual bool AllowNullConfigFile => false;

        /// <summary>
        /// If true, and Harmony is passed to a StaticSong containing us, we'll get a patch-all applied.
        /// </summary>
        public virtual bool HasHarmonyPatches => false;

        #endregion
        #region Override Tracking -- bool HasStanza(string) and the OverTrack gang

        const string OVERTRACK_PREFIX = "_stanza";
        const BindingFlags OVERTRACK_BINDINGS = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        #region static readonly HashSet<string> ___overtracked = typeof(StaticVerse)....Where(mi.Name.StartsWith(OVERTRACK_PREFIX))
        static readonly HashSet<string> __overtracked = typeof(StaticVerse)
            .GetMethods(OVERTRACK_BINDINGS)
            .Select(mi => mi.Name)
            .Where(n => n.StartsWith(OVERTRACK_PREFIX))
            .ToSizedHashSet()
        ;

        #endregion
        HashSet<string> ___overtrack = null;
        HashSet<string> __overtrack {
            get {
                if(___overtrack is null) {
                    ___overtrack = GetType()
                        .GetMethods(OVERTRACK_BINDINGS)
                        .Select(mi => mi.Name)
                        .Where(__overtracked.Contains)
                        .ToSizedHashSet()
                    ;
                }
                return ___overtrack;
            }
        }
        bool HasStanza(string stanza) => __overtrack.Contains(stanza);

        #endregion

        #region Composing - aka both "Oh sweet, it just werks!" and "I wasn't paying attention, how do I use this again?"
        
        /// <summary>
        /// If we've used any of the Compose methods.
        /// </summary>
        public bool IsComposed { get; private set; }
        internal const string NULLARY_COMPOSE_WARNING = "Are you sure you have no config file or logger?";

        /// <summary>
        /// Composes this verse, awakening it and registering all of its events.
        /// <br /> Does NOT include a config file or a logger- are you sure this is what you want?
        /// </summary>
        /// <remarks>
        /// Obsolete mark is solely for the squiggle and compiler warning. 
        /// This option is not likely to be removed.
        /// --Eirshy
        /// </remarks>
        [Obsolete(NULLARY_COMPOSE_WARNING)]
        public void Compose() => Compose((ILogProvider)null, null);
        /// <summary>
        /// Composes this verse, awakening it and registering all of its events.
        /// <br /> Does NOT include a logger- are you sure this is what you want?
        /// </summary>
        public void Compose(ConfigFile config) => Compose((ILogProvider)null, config);
        /// <summary>
        /// Composes this verse, awakening it and registering all of its events.
        /// <br /> Does NOT include a config file- are you sure this is what you want?
        /// </summary>
        public void Compose(ManualLogSource bepinLogger) => Compose((BepInManualLogger)bepinLogger, null);
        /// <summary>
        /// Composes this verse, awakening it and registering all of its events.
        /// <br /> Does NOT include a config file- are you sure this is what you want?
        /// </summary>
        public void Compose(ILogProvider logger) => Compose(logger, null);
        /// <summary>
        /// Composes this verse, awakening it and registering all of its events.
        /// </summary>
        public void Compose(ManualLogSource bepinLogger, ConfigFile config) => Compose((BepInManualLogger)bepinLogger, config);
        /// <summary>
        /// Composes this verse, awakening it and registering all of its events.
        /// </summary>
        public void Compose(ILogProvider logger, ConfigFile config) {
            if(IsComposed) return;
            IsComposed = true;

            if(logger != null) RegisterLogger(logger);
            VerseAwake();
            LoadConfig(config);
            RegisterSetupPhases();
            RegisterLoadEvents();
            RegisterSaveEvents();
        }

        #endregion

        #region RegisterLogger ... races, hotswaps, never null.
        /// <summary>
        /// The LogProvider we are currently using, in the event you need direct access to it.
        /// </summary>
        protected ILogProvider LogProvider { get; private set; } = RythmnKit.LogProvider;

        /// <summary>
        /// Registers the given provider as our logging source.
        /// </summary>
        public void RegisterLogger(ILogProvider provider) => LogProvider = provider;
        /// <summary>
        /// Registers the given provider as our logging source.
        /// </summary>
        public void RegisterLogger(ManualLogSource bepinLogger) {
            LogProvider = bepinLogger is null ? LogProvider : (BepInManualLogger)bepinLogger;
        }


        #region protected unwrap of LogProvider, include auto-flush on LogFatal

        /// <summary>
        /// Direct access to the BepIn logger, if one was provided.
        /// Should be null if one was not.
        /// </summary>
        protected ManualLogSource Logger => LogProvider?.BepIn;
        /// <summary>
        /// Logs the given messsage.
        /// </summary>
        protected void Log(string msg) => LogProvider?.Log(msg);
        /// <summary>
        /// Logs that this verse is calling the given stanza.
        /// </summary>
        void LogStanza(string name) => LogProvider?.LogStanza(GetType(), name);
        /// <summary>
        /// Logs that we've encountered a fatal error.
        /// </summary>
        protected void LogFatal(Exception ex, bool noFlush = false) {
            LogProvider?.LogFatal(ex);
            if(!noFlush) LogProvider?.FlushLogBuffer();
        }
        /// <summary>
        /// If the current logging method uses a log buffer, flushes it to whatever output source it uses.
        /// </summary>
        internal protected void FlushLogBuffer() => LogProvider?.FlushLogBuffer();

        #endregion

        #endregion

        //To be OverTracked: protected void _stanza*

        #region Awake ... (run-once enforced)

        /// <summary>
        /// Whether we are Awake yet.
        /// </summary>
        public bool IsAwake { get; private set; }
        /// <summary>
        /// Marks this Verse as Awake, and does any immediate setup it requires.
        /// <br />Does nothing if we are already awake.
        /// <br />Automatically called before any non-visitor stanza if we are not yet awake.
        /// </summary>
        public void VerseAwake() {
            if(IsAwake) return;
            IsAwake = true;
            _internalAwake();
            //check if we are always awake, so we don't log it if we are.
            if(HasStanza(nameof(_stanza_awake))) {
                if(EnableStanzaLogging) LogStanza(nameof(_stanza_awake));
                if(EnableExceptionCatching) {
                    try {
                        _stanza_awake();
                    } catch(Exception ex) {
                        LogFatal(ex, !RethrowExceptionsAfterLogging);
                        if(RethrowExceptionsAfterLogging) throw ex;
                    }
                } else _stanza_awake();
            }
        }

        void _internalAwake() =>  StaticBeat.RequestPrefabSync(WillNeedSyncsFor);

        /// <summary>
        /// If overridden, will be called whenever this Verse wakes up.
        /// </summary>
        /// <remarks>
        /// This is for the same kind of stuff you'd put in your BepIn Plugin's Awake() method, 
        /// but not called until we say instead of when Unity normally would.
        /// </remarks>
        protected virtual void _stanza_awake() { }

        #endregion
        #region Load Config ... (run-once enforced)

        /// <summary>
        /// Whether our Config has been loaded, or we've noticed we don't need one.
        /// </summary>
        public bool IsConfigLoaded { get; private set; }
        /// <summary>
        /// Loads the passed config file after making sure we're awake.
        /// <br />Does nothing if we have already loaded our config
        /// </summary>
        /// <param name="config">If you don't need one, enable <c>AllowNullConfigFile</c> and pass null!</param>
        public void LoadConfig(ConfigFile config) {
            if(IsConfigLoaded) return;
            IsConfigLoaded = true;
            VerseAwake();
            //make sure we care to continue;
            if(!HasStanza(nameof(_stanza_config))) return;

            if(EnableStanzaLogging) LogStanza(nameof(_stanza_config));
            if(!AllowNullConfigFile && config is null) {
                var ex = GetMissingConfigException();
                LogFatal(ex);
                throw ex;
            }
            if(EnableExceptionCatching) {
                try {
                    _stanza_config(config);
                } catch(Exception ex) {
                    LogFatal(ex, !RethrowExceptionsAfterLogging);
                    if(RethrowExceptionsAfterLogging) throw ex;
                }
            } else _stanza_config(config);
        }

        MissingConfigException GetMissingConfigException() {
            var msg = $"{GetType()} declares a {nameof(_stanza_config)}, but we have not been given a config file!";
            return new MissingConfigException(msg);
        }


        /// <summary>
        /// If overridden, this Verse will require a config file be loaded, and will
        /// use this method to load it.
        /// </summary>
        /// <remarks>
        /// Note that if you need to do some config stuff based on values found during your 
        /// setup phases, you can.
        /// <br />We don't do anything special with/to the file - that's your job. 
        /// We just provide a standardized hook and a "did-load-already" check.
        /// <br />--Eirshy
        /// </remarks>
        protected virtual void _stanza_config(ConfigFile config) { }

        #endregion

        void ForceReady() { VerseAwake(); LoadConfig(null); }

        #region ESetupPhase-based ... (run-once enforced)

        //VerseUsesPhase and DoPhase are both manually filled

        readonly HashSet<ESetupPhase> __completedPhases = new HashSet<ESetupPhase>(Enum.GetValues(typeof(ESetupPhase)).Length);
        readonly HashSet<ESetupPhase> __registeredPhases = new HashSet<ESetupPhase>(Enum.GetValues(typeof(ESetupPhase)).Length);

        public bool IsPhaseComplete(ESetupPhase phase) => __completedPhases.Contains(phase);
        public bool VerseUsesPhase(ESetupPhase phase) {
            switch(phase) {
                case ESetupPhase.WhatsLDBTool: return HasStanza(nameof(_stanza_setup_AddProtosWithoutLDBTool));
                case ESetupPhase.ProtosCreatedReadOnly: return HasStanza(nameof(_stanza_setup_ProtosCreatedReadOnly));
                case ESetupPhase.ProtosCreated: return HasStanza(nameof(_stanza_setup_ProtosCreated));
                case ESetupPhase.ProtosUpdatedReadOnly: return HasStanza(nameof(_stanza_setup_ProtosUpdatedReadOnly));
                case ESetupPhase.ProtosUpdated: return HasStanza(nameof(_stanza_setup_ProtosUpdated));
                case ESetupPhase.ProtosFinalFixes: return HasStanza(nameof(_stanza_setup_ProtosFinalFixes));
                default: return false;
                //case EActionPhase.TheChadLastChance: break; // this tool isn't allowed to use this one.
            }
        }

        public void RegisterSetupPhases() {
            ((ESetupPhase[])Enum.GetValues(typeof(ESetupPhase))).DoForEach(RegisterSetupPhase);
        }
        public void RegisterSetupPhase(ESetupPhase phase) {
            if(!__registeredPhases.Add(phase)) return;//this is not completely concurrent-safe
            if(!VerseUsesPhase(phase)) return;//if we don't use this phase, don't register it, it's a noop
            StaticBeat.AddSetup(() => DoPhase(phase), phase);
        }

        void DoSetupStanza(Action stanza) {
            ForceReady();
            if(EnableStanzaLogging) LogStanza(stanza.Method.Name);
            if(EnableExceptionCatching) {
                try {
                    stanza();
                } catch(Exception ex) {
                    LogFatal(ex, !RethrowExceptionsAfterLogging);
                    if(RethrowExceptionsAfterLogging) throw ex;
                }
            } else stanza();
        }
        void DoPhase(ESetupPhase phase) {
            if(!__completedPhases.Add(phase)) return;//this is not completely concurrent-safe
            switch(phase) {
                case ESetupPhase.WhatsLDBTool: DoSetupStanza(_stanza_setup_AddProtosWithoutLDBTool); break;
                case ESetupPhase.ProtosCreatedReadOnly: DoSetupStanza(_stanza_setup_ProtosCreatedReadOnly); break;
                case ESetupPhase.ProtosCreated: DoSetupStanza(_stanza_setup_ProtosCreated); break;
                case ESetupPhase.ProtosUpdatedReadOnly: DoSetupStanza(_stanza_setup_ProtosUpdatedReadOnly); break;
                case ESetupPhase.ProtosUpdated: DoSetupStanza(_stanza_setup_ProtosUpdated); break;
                case ESetupPhase.ProtosFinalFixes: DoSetupStanza(_stanza_setup_ProtosFinalFixes); break;
                //case ESetupPhase.TheChadTrueLast: break;// this tool isn't allowed to use this one automatically
            }
        }

        protected virtual void _stanza_setup_AddProtosWithoutLDBTool() { }
        protected virtual void _stanza_setup_ProtosCreatedReadOnly() { }
        protected virtual void _stanza_setup_ProtosCreated() { }
        protected virtual void _stanza_setup_ProtosUpdatedReadOnly() { }
        protected virtual void _stanza_setup_ProtosUpdated() { }
        protected virtual void _stanza_setup_ProtosFinalFixes() { }

        #endregion

        #region Load* ... (per-game-load)

        public bool LoadEventsRegistered { get; private set; }
        public void RegisterLoadEvents() {
            if(LoadEventsRegistered) return;
            LoadEventsRegistered = true;

            if(HasStanza(nameof(_stanza_load_PreVisit))) StaticBeat.AddLoad_PreVisitor(LoadPreVisit);
            if(HasStanza(nameof(_stanza_load_Visitor))) StaticBeat.AddLoad_Visitor(_stanza_load_Visitor);// no wrapper
            if(HasStanza(nameof(_stanza_load_PostVisit))) StaticBeat.AddLoad_PostVisitor(LoadPostVisit);
        }

        void DoLoadStanza(Action<GameData> stanza, GameData arg) {
            ForceReady();
            if(EnableStanzaLogging) LogStanza(stanza.Method.Name);
            if(EnableExceptionCatching) {
                try {
                    stanza(arg);
                } catch(Exception ex) {
                    LogFatal(ex, !RethrowExceptionsAfterLogging);
                    if(RethrowExceptionsAfterLogging) throw ex;
                }
            } else stanza(arg);
        }

        void LoadPreVisit(GameData gdat) => DoLoadStanza(_stanza_load_PreVisit, gdat);
        void LoadPostVisit(GameData gdat) => DoLoadStanza(_stanza_load_PostVisit, gdat);

        protected virtual void _stanza_load_PreVisit(GameData gdat) { }
        protected virtual void _stanza_load_Visitor(EntityRef entr) { }
        protected virtual void _stanza_load_PostVisit(GameData gdat) { }

        #endregion
        #region Save* ... (per-game-save) -- todo
        
        internal void RegisterSaveEvents() { }
        //This should be a thing in the exact same way Load* is handled

        #endregion

    }
}
