using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eirshy.DSP.Rythmn.Enums {

    /// <summary>
    /// Event chain:
    /// <list type="number">
    /// <item>WhatsLDBTool</item>
    /// <item>ProtosAllCreated</item>
    /// <item>ProtosAllUpdated</item>
    /// <item>ProtosAllFixed</item>
    /// <item>TheChadLastChance</item>
    /// </list>
    /// </summary>
    public enum ESetupPhase : int {
        /// <summary>
        /// For actions that create new protos <strong>WITHOUT</strong> using LDBTool
        /// </summary>
        WhatsLDBTool = -3000,

        /// <summary>
        /// For actions that expect nobody to create protos after this point, 
        /// but WILL NOT modify protos.
        /// </summary>
        ProtosCreatedReadOnly = ProtosCreated - 1,
        /// <summary>
        /// For actions that expect nobody to create protos after this point.
        /// <br /> Intended for making the bulk of your proto changes.
        /// </summary>
        ProtosCreated = 0,

        /// <summary>
        /// For actions that need to sniff protos after most people have done their updates, 
        /// but WILL NOT modify protos.
        /// </summary>
        ProtosUpdatedReadOnly = ProtosUpdated - 1,
        /// <summary>
        /// For actions that need to sniff protos after most people have done their updates.
        /// <br /> Intended for reacting to any proto changes that were made.
        /// </summary>
        ProtosUpdated = 3000,

        /// <summary>
        /// For actions that need to assume everyone's done changing their protos.
        /// <br />Should be used for mod compatibility changes primarily.
        /// </summary>
        ProtosFinalFixes = 6000,


        /// <summary>
        /// For when you took one look at the event chain and usage explanation and said "I'm not reading that."
        /// <br />Or for when someone else did but used it wrong and you're trying for compatibility.
        /// <br />No other setup action phase will be added after this point.
        /// <br />No guarantee *yours* will go off after everyone else's picks for here.
        /// </summary>
        /// <remarks>
        /// The Obsolete tag is because that's the only attribute that can cause a compiler warning.
        /// <br />And newbie coders are traditionally terrified of squiggly lines.
        /// <br />I have zero intention of removing this option
        /// <br />--Eirshy
        /// </remarks>
        [Obsolete("Are you ABSOLUTELY CERTAIN that you can't use an earlier slot in the event chain?")]
        TheChadTrueLast = 9001,
    }
}
