using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace GlutonsAscensions.Patches;

using static HarmonyExtensions;

[HarmonyPatch]
public static class AscensionPatches {
    private static readonly CodeMatch LoadConstantInt32 = CodeMatch.WithOpcodes([OpCodes.Ldc_I4_S, OpCodes.Ldc_I4]);
    private static readonly CodeMatch AppendFormattedInt = CodeMatch.Calls(AccessTools.Method(
        typeof(DefaultInterpolatedStringHandler),
        nameof(DefaultInterpolatedStringHandler.AppendFormatted),
        [typeof(int)],
        [typeof(int)]
    ));
    
    /// <summary>
    /// Replaces the current ldc.i4 instruction with an ldc.i4(.s) with the additional ascension levels
    /// </summary>
    private static void ModifyMaxAscensionsAllowedConstant(this CodeMatcher codeMatcher) {
        if (codeMatcher.Instruction.operand is null) {
            GlutonsAscensionsMod.Logger.Error($"Trying to modify non-ldc.i4 ascension constant: {codeMatcher.Instruction}");
            return;
        }
        
        var maxAscensionAllowed = Convert.ToInt32(codeMatcher.Instruction.operand);
        if (maxAscensionAllowed < 0) {
            GlutonsAscensionsMod.Logger.Error($"Trying to modify negative ascension constant: {maxAscensionAllowed}");
            return;
        }
        
        GlutonsAscensionLevel.UpdateMaxAscensionAllowed(ref maxAscensionAllowed);
        
        codeMatcher
            .RemoveInstruction()
            .InsertAndAdvance(new CodeInstruction(
                maxAscensionAllowed > 0xff ? OpCodes.Ldc_I4 : OpCodes.Ldc_I4_S,
                maxAscensionAllowed
            ));
    }

    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
    [HarmonyPostfix]
    static void InitPostfix() {
        GlutonsAscensionLevel.Initialize();
    }

    [HarmonyPatch(typeof(AscensionManager), MethodType.Constructor, typeof(AscensionLevel))]
    [HarmonyPostfix]
    static void PatchAscensionManagerConstructor(AscensionLevel level, ref int ____level) {
        if (level.IsGlutonsAscension()) {
            ____level = level.Level();
        }
    }

    [HarmonyPatch(typeof(AscensionManager), nameof(AscensionManager.HasLevel))]
    [HarmonyPostfix]
    static void PatchAscensionManagerHasLevel(AscensionManager __instance, AscensionLevel level, ref bool __result) {
        if (level.IsGlutonsAscension()) {
            __result = __instance._level >= level.Level();
        }
    }
    
    [HarmonyPatch(typeof(AscensionHelper), nameof(AscensionHelper.GetKey))]
    [HarmonyPrefix]
    static bool PatchAscensionHelperGetKey(ref string __result, int level) {
        if (!level.IsGlutonsAscension()) return PrefixRunOriginal;
        
        if (GlutonsAscensionLevel.NameOf(AscensionLevel.FromLevel(level))?.ToUpper() is { } locKey) {
            __result = locKey;
            return PrefixSkipOriginal;
        }
        
        return PrefixRunOriginal;
    }
    
    [HarmonyPatch(typeof(ProgressSaveManager), nameof(ProgressSaveManager.IncrementSingleplayerAscension))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> PatchIncrementSingleplayerAscension(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions);
        
        // if (charStats.MaxAscension >= 10)
        // 
        // callvirt     instance int32 MegaCrit.Sts2.Core.Saves.CharacterStats::get_MaxAscension()
        // ldc.i4.s     10
        codeMatcher
            .MatchEndForward(
                CodeMatch.Calls(AccessTools.PropertyGetter(typeof(CharacterStats), nameof(CharacterStats.MaxAscension))),
                LoadConstantInt32
            )
            .ThrowIfInvalid("Could not find call to CharacterStats.MaxAscension followed by a ldc opcode")
            .ModifyMaxAscensionsAllowedConstant();

        return codeMatcher.Instructions();
    }
    
    [HarmonyPatch(typeof(ProgressSaveManager), nameof(ProgressSaveManager.IncrementMultiplayerAscension))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> PatchIncrementMultiplayerAscension(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions);
        
        // if (this.Progress.MaxMultiplayerAscension >= 10)
        // 
        // callvirt     instance int32 MegaCrit.Sts2.Core.Saves.ProgressState::get_MaxMultiplayerAscension()
        // ldc.i4.s     10
        codeMatcher
            .MatchEndForward(
                CodeMatch.Calls(AccessTools.PropertyGetter(typeof(ProgressState), nameof(ProgressState.MaxMultiplayerAscension))),
                LoadConstantInt32
            )
            .ThrowIfInvalid("Could not find call to ProgressState.MaxMultiplayerAscension followed by a ldc opcode")
            .ModifyMaxAscensionsAllowedConstant();

        return codeMatcher.Instructions();
    }
    
    [HarmonyPatch(typeof(ProgressState), nameof(ProgressState.ClampAscension))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> PatchClampAscension(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions);

        // if (value <= 10)
        // 
        // ldarg.0      value
        // ldc.i4.s     10
        codeMatcher
            .MatchEndForward(
                CodeMatch.IsLdarg(0),
                LoadConstantInt32
            )
            .ThrowIfInvalid("Could not find ldarg.0 followed by a ldc.i4(.s) opcode")
            .ModifyMaxAscensionsAllowedConstant();
        
        // ctx.Warn($"Value ({value}) exceeds allowed ({10}), clamping");
        // 
        // ldc.i4.s     10 
        // call         instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int32>(!!0/*int32*/)
        codeMatcher
            .MatchStartForward(
                LoadConstantInt32,
                AppendFormattedInt
            )
            .ThrowIfInvalid( "Could not find ldc.i4(.s) opcode followed by a call to DefaultInterpolatedStringHandler.AppendFormatted<int32>")
            .ModifyMaxAscensionsAllowedConstant();
        
        // return 10;
        // 
        // ldc.i4.s     10
        // ret
        codeMatcher
            .MatchStartForward(
                LoadConstantInt32,
                CodeMatch.WithOpcodes([OpCodes.Ret])
            )
            .ThrowIfInvalid("Could not find ldc.i4(.s) opcode followed by a return")
            .ModifyMaxAscensionsAllowedConstant();

        return codeMatcher.Instructions();
    }

    [HarmonyPatch(typeof(ProgressState), nameof(ProgressState.ClampCharacterStatsFields))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> PatchClampCharacterStatsAcensionFields(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions);

        // if (stats.MaxAscension > 10)
        // 
        // callvirt     instance int32 MegaCrit.Sts2.Core.Saves.CharacterStats::get_MaxAscension()
        // ldc.i4.s     10
        codeMatcher
            .MatchEndForward(
                CodeMatch.Calls(AccessTools.PropertyGetter(typeof(CharacterStats), nameof(CharacterStats.MaxAscension))),
                LoadConstantInt32
            )
            .ThrowIfInvalid("Could not find call to CharacterStats.get_MaxAscension followed by a ldc.i4(.s) opcode")
            .ModifyMaxAscensionsAllowedConstant();
        
        // ctx.Warn($"MaxAscension ({stats.MaxAscension}) exceeds allowed ({10}), clamping");
        // 
        // ldc.i4.s     10
        // call         instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int32>(!!0/*int32*/)
        codeMatcher
            .MatchStartForward(
                LoadConstantInt32,
                AppendFormattedInt
            )
            .ThrowIfInvalid("Could not find ldc.i4(.s) opcode followed by a call to DefaultInterpolatedStringHandler.AppendFormatted<int32>")
            .ModifyMaxAscensionsAllowedConstant();
        
        // stats.MaxAscension = 10;
        // 
        // ldc.i4.s     10
        // callvirt     instance void MegaCrit.Sts2.Core.Saves.CharacterStats::set_MaxAscension(int32)
        codeMatcher
            .MatchStartForward(
                LoadConstantInt32,
                CodeMatch.Calls(AccessTools.PropertySetter(typeof(CharacterStats), nameof(CharacterStats.MaxAscension)))
            )
            .ThrowIfInvalid("Could not find ldc.i4(.s) opcode followed by a call to CharacterStats.set_MaxAscension")
            .ModifyMaxAscensionsAllowedConstant();
        
        // if (stats.PreferredAscension > 10)
        // 
        // callvirt     instance int32 MegaCrit.Sts2.Core.Saves.CharacterStats::get_PreferredAscension()
        // ldc.i4.s     10
        codeMatcher
            .MatchEndForward(
                CodeMatch.Calls(AccessTools.PropertyGetter(typeof(CharacterStats), nameof(CharacterStats.PreferredAscension))),
                LoadConstantInt32
            )
            .ThrowIfInvalid("Could not find call to CharacterStats.get_PreferredAscension followed by a ldc.i4(.s) opcode")
            .ModifyMaxAscensionsAllowedConstant();
        
        // ctx.Warn($"PreferredAscension ({stats.PreferredAscension}) exceeds allowed ({10}), clamping");
        // 
        // ldc.i4.s     10
        // call         instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int32>(!!0/*int32*/)
        codeMatcher
            .MatchStartForward(
                LoadConstantInt32,
                AppendFormattedInt
            )
            .ThrowIfInvalid("Could not find ldc.i4(.s) opcode followed by a call to DefaultInterpolatedStringHandler.AppendFormatted<int32>")
            .ModifyMaxAscensionsAllowedConstant();
        
        // stats.PreferredAscension = 10;
        // 
        // ldc.i4.s     10
        // callvirt     instance void MegaCrit.Sts2.Core.Saves.CharacterStats::set_PreferredAscension(int32)
        codeMatcher
            .MatchStartForward(
                LoadConstantInt32,
                CodeMatch.Calls(AccessTools.PropertySetter(typeof(CharacterStats), nameof(CharacterStats.PreferredAscension)))
            )
            .ThrowIfInvalid("Could not find ldc.i4(.s) opcode followed by a call to CharacterStats.set_PreferredAscension")
            .ModifyMaxAscensionsAllowedConstant();
        
        return codeMatcher.Instructions();
    }
    
    [HarmonyPatch(typeof(UnlockConsoleCmd), nameof(UnlockConsoleCmd.UnlockAscensions))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> PatchUnlockAscensionsCommand(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions);

        // SaveManager.Instance.Progress.MaxMultiplayerAscension = 10;
        // 
        // ldc.i4.s     10
        // callvirt     instance void MegaCrit.Sts2.Core.Saves.ProgressState::set_MaxMultiplayerAscension(int32)
        codeMatcher
            .MatchStartForward(
                LoadConstantInt32,
                CodeMatch.Calls(AccessTools.PropertySetter(typeof(ProgressState), nameof(ProgressState.MaxMultiplayerAscension)))
            )
            .ThrowIfInvalid("Could not find ldc.i4(.s) opcode followed by a call to ProgressState.set_MaxMultiplayerAscension")
            .ModifyMaxAscensionsAllowedConstant();
        
        // SaveManager.Instance.Progress.GetOrCreateCharacterStats(allCharacter.Id).MaxAscension = 10;
        // 
        // ldc.i4.s     10
        // callvirt     instance void MegaCrit.Sts2.Core.Saves.CharacterStats::set_MaxAscension(int32)
        codeMatcher
            .MatchStartForward(
                LoadConstantInt32,
                CodeMatch.Calls(AccessTools.PropertySetter(typeof(CharacterStats), nameof(CharacterStats.MaxAscension)))
            )
            .ThrowIfInvalid("Could not find ldc.i4(.s) opcode followed by a call to CharacterStats.set_MaxAscension")
            .ModifyMaxAscensionsAllowedConstant();

        return codeMatcher.Instructions();
    }
}