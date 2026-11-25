using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace GlutonsAscensions.Helpers;

public static class HarmonyExtensions {
    public const bool PrefixSkipOriginal = false;
    public const bool PrefixRunOriginal = true;
    
    public static readonly CodeMatch LoadConstantInt32 = CodeMatch.WithOpcodes([
        OpCodes.Ldc_I4_S, OpCodes.Ldc_I4,
        OpCodes.Ldc_I4_M1, OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_1, OpCodes.Ldc_I4_2, OpCodes.Ldc_I4_3,
        OpCodes.Ldc_I4_4, OpCodes.Ldc_I4_5, OpCodes.Ldc_I4_6, OpCodes.Ldc_I4_7, OpCodes.Ldc_I4_8,
    ]);

    extension(CodeMatcher codeMatcher) {
        public CodeMatcher Replace(CodeInstruction instruction) {
            var labels = codeMatcher.Labels ?? [];
            return codeMatcher
                .RemoveInstruction()
                .Insert(instruction)
                .AddLabels(labels);
        }
        public CodeMatcher ReplaceAndAdvance(CodeInstruction instruction) => codeMatcher.Replace(instruction).Advance();
    }

    extension(CodeMatch) {
        public static CodeMatch Calls() => new(OpCodes.Call);
        public static CodeMatch CallsVirt() => new(OpCodes.Callvirt);
    }

    extension(CodeInstruction) {
        public static CodeInstruction LoadConstant(int value) {
            var opcode = value switch {
                -1 => OpCodes.Ldc_I4_M1,
                0 => OpCodes.Ldc_I4_0,
                1 => OpCodes.Ldc_I4_1,
                2 => OpCodes.Ldc_I4_2,
                3 => OpCodes.Ldc_I4_3,
                4 => OpCodes.Ldc_I4_4,
                5 => OpCodes.Ldc_I4_5,
                6 => OpCodes.Ldc_I4_6,
                7 => OpCodes.Ldc_I4_7,
                8 => OpCodes.Ldc_I4_8,
                >= sbyte.MinValue and <= sbyte.MaxValue => OpCodes.Ldc_I4_S,
                _ => OpCodes.Ldc_I4,
            };
            return new CodeInstruction(opcode, opcode == OpCodes.Ldc_I4_S || opcode == OpCodes.Ldc_I4 ? value : null);
        }
        
        public static CodeInstruction Call(MethodInfo methodInfo) => new(OpCodes.Call, methodInfo);
    }
}