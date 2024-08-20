using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

namespace Eirshy.DSP.LazyOutposting {
    internal static class Extensions {
        /// <summary>
        /// Convenience; replaces the opcode and the operand on this instruction
        /// </summary>
        public static void Reop(this CodeInstruction ci, OpCode op, object operand = null) {
            ci.opcode = op;
            ci.operand = operand;
        }
    }
}
