using System.Reflection;
using BaseLib.Extensions;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace GlutonsAscensions.Patches;

using static Helpers.HarmonyExtensions;

[HarmonyPatch]
public static class AscensionPatches {
    private static readonly MethodInfo AppendFormattedGenericDefinition = typeof(DefaultInterpolatedStringHandler)
        .GetMethods().Single(methodInfo =>
            methodInfo is { Name: nameof(DefaultInterpolatedStringHandler.AppendFormatted), IsGenericMethodDefinition: true }
            && methodInfo.GetGenericArguments().Length == 1
            && methodInfo.GetParameters().Length == 1
        );
    private static readonly MethodInfo AppendFormattedIntMethod = AppendFormattedGenericDefinition.MakeGenericMethod(typeof(int));
    private static readonly CodeMatch AppendFormattedInt = CodeMatch.Calls(AppendFormattedIntMethod);
    
    /// <summary>
    /// Replaces the current ldc.i4 instruction with a ldc.i4(.s) with the additional ascension levels
    /// </summary>
    private static void ModifyMaxAscensionsAllowedConstant(this CodeMatcher codeMatcher, bool isBlitz = false) {
        if (!codeMatcher.Instruction.TryGetIntValue(out var maxAscensionAllowed)) {
            GlutonsAscensionsMod.Logger.Error($"Trying to modify non-ldc.i4 ascension constant: {codeMatcher.Instruction}");
            return;
        }
        
        if (maxAscensionAllowed < 0) {
            GlutonsAscensionsMod.Logger.Error($"Trying to modify negative ascension constant: {maxAscensionAllowed}");
            return;
        }

        if (isBlitz) {
            GlutonsAscensionLevel.UpdateMaxBlitzAscensionAllowed(ref maxAscensionAllowed);
        } else {
            GlutonsAscensionLevel.UpdateMaxAscensionAllowed(ref maxAscensionAllowed);
        } 
        
        codeMatcher
            .RemoveInstruction()
            .InsertAndAdvance(new CodeInstruction(
                maxAscensionAllowed > 0xff ? OpCodes.Ldc_I4 : OpCodes.Ldc_I4_S,
                maxAscensionAllowed
            ));
    }
    
    [HarmonyPatch(typeof(ModelDb), nameof(ModelDb.Init))]
    [HarmonyPostfix]
    static void InitializeAfterModelDbInit() => GlutonsAscensionLevel.Initialize();

    [HarmonyPatch]
    public class AscensionManagerDebugBetaCompatPatch {
        [HarmonyPrepare]
        static bool IsBeta() => BetaCompatHelpers.IsAtLeastVersion(BetaCompatHelpers.VERSION_113);

        [HarmonyTargetMethod]
        static MethodBase Target() => AccessTools.Constructor(typeof(AscensionManager), [BetaCompatHelpers.GameModeType.Type, typeof(int)]);

        [HarmonyPatch]
        [HarmonyPostfix]
        static void AscensionManagerConstructorDebugPostfix(AscensionManager __instance, int level) {
            GlutonsAscensionsMod.Logger.Info($"AscensionManager level: {level}, ascensions: {__instance._ascensions.Join(converter: al => $"{GlutonsAscensionLevel.NameOf(al)} ({al.Level()})")}");
        }
    }

    [HarmonyPatch]
    public class RunStateFromSerializableClampBetaCompatPatch {
        private static readonly MethodInfo _clampToModeMethod = AccessTools.Method(typeof(AscensionManager), "ClampToMode", [BetaCompatHelpers.GameModeType.Type, typeof(int)]);

        private static int ClampToMode(GameMode gameMode, int level) => Math.Clamp(level, 0, AscensionManager.GetMaxAllowed(gameMode));  

        [HarmonyPrepare]
        static bool IsBeta() => BetaCompatHelpers.IsAtLeastVersion(BetaCompatHelpers.VERSION_113);
        
        [HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> PatchRunStateFromSerializableClampToModeTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codeMatcher = new CodeMatcher(instructions);

            // call         int32 MegaCrit.Sts2.Core.Entities.Ascension.AscensionManager::ClampToMode(valuetype MegaCrit.Sts2.Core.Runs.GameMode, int32)
            codeMatcher
                .MatchStartForward(
                    CodeMatch.Calls(_clampToModeMethod)
                )
                .ThrowIfInvalid("Could not find call to AscensionManager.ClampToMode")
                .Replace(CodeInstruction.Call(() => ClampToMode(0, 0)));

            return codeMatcher.Instructions();
        }
    }

    [HarmonyPatch]
    public class AscensionManagerMaxAllowedAscensionBetaCompatPatch {
        [HarmonyPrepare]
        static bool IsBeta() => BetaCompatHelpers.IsAtLeastVersion(BetaCompatHelpers.VERSION_113);

        [HarmonyTargetMethod]
        static MethodBase Target() => AccessTools.Method(typeof(AscensionManager), "GetMaxAllowed");

        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> PatchMaxAllowedAscension(IEnumerable<CodeInstruction> instructions) {
            var codeMatcher = new CodeMatcher(instructions);

            //-gameMode != GameMode.Blitz ? 10 : 5;
            //+gameMode != GameMode.Blitz ? <GlutonsAscensionLevel.MaxAscensionAllowed> : <GlutonsAscensionLevel.MaxBlitzAscensionAllowed>;
            // 
            // ldarg.0      // gameMode
            // ldc.i4.4
            // beq.s
            // 
            //-ldc.i4.s     10
            //+ldc.i4.s     <GlutonsAscensionLevel.MaxAscensionAllowed>
            // ret
            //-ldc.i4.5
            //+ldc.i4.s     <GlutonsAscensionLevel.MaxBlitzAscensionAllowed>
            // ret
            codeMatcher
                .MatchStartForward(
                    new CodeMatch(OpCodes.Beq_S),
                    CodeMatch.LoadsConstant(),
                    new CodeMatch(OpCodes.Ret),
                    CodeMatch.LoadsConstant(),
                    new CodeMatch(OpCodes.Ret)
                )
                .ThrowIfInvalid("Could not find beq.s opcode followed by a ldc.i4(.s) opcode followed by a ret opcode followed by a ldc.i4(.s) opcode followed by a ret opcode")
                .Advance()
                .ReplaceAndAdvance(CodeInstruction.LoadConstant(GlutonsAscensionLevel.MaxAscensionAllowed))
                .Advance()
                .ReplaceAndAdvance(CodeInstruction.LoadConstant(GlutonsAscensionLevel.MaxBlitzAscensionAllowed));
            
            return codeMatcher.Instructions();
        }
    }

    [HarmonyPatch]
    public static class AscensionConstructorOldCompatPatch {
        [HarmonyPrepare]
        static bool HasOldConstructorInfo() => BetaCompatHelpers.IsBeforeVersion(BetaCompatHelpers.VERSION_113);

        [HarmonyPatch(typeof(AscensionManager), MethodType.Constructor, typeof(AscensionLevel))]
        [HarmonyPostfix]
        static void PatchAscensionManagerConstructor(AscensionLevel level, ref int ____level) {
            if (level.IsGlutonsAscension()) {
                ____level = level.Level();
            }
        }
    }

    [HarmonyPatch]
    public class AscensionManagerHasLevelOldCompatPatch {
        [HarmonyPrepare]
        static bool HasOldConstructorInfo() => BetaCompatHelpers.IsBeforeVersion(BetaCompatHelpers.VERSION_113);
        
        [HarmonyPatch(typeof(AscensionManager), nameof(AscensionManager.HasLevel))]
        [HarmonyPostfix]
        static void PatchAscensionManagerHasLevel(AscensionManager __instance, AscensionLevel level, ref bool __result) {
            if (level.IsGlutonsAscension()) {
                var _level = (int) (AccessTools.DeclaredField(typeof(AscensionManager), "_level")?.GetValue(__instance)
                    ?? throw new Exception("[GlutonsAscensions] Could not get AscensionManager._level field"));
                
                __result = _level >= level.Level();
            }
        }
    }

    [HarmonyPatch]
    public class AscensionHelperGetKeyOldCompatPatch {
        [HarmonyPrepare]
        static bool IsNotBeta() => BetaCompatHelpers.IsBeforeVersion(BetaCompatHelpers.VERSION_113);
        
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
    }
    
    [HarmonyPatch]
    public class AscensionHelperGetKeyBetaCompatPatch {
        [HarmonyPrepare]
        static bool IsBeta() => BetaCompatHelpers.IsAtLeastVersion(BetaCompatHelpers.VERSION_113);

        [HarmonyPatch(typeof(AscensionHelper), nameof(AscensionHelper.GetKey))]
        [HarmonyPrefix]
        static bool PatchAscensionHelperGetKey(ref string __result, int level, int gameMode) {
            // TODO: handle Blitz ascensions
            if (gameMode == BetaCompatHelpers.GameModeBlitz || !level.IsGlutonsAscension()) return PrefixRunOriginal;
        
            if (GlutonsAscensionLevel.NameOf(AscensionLevel.FromLevel(level))?.ToUpper() is { } locKey) {
                __result = locKey;
                return PrefixSkipOriginal;
            }
        
            return PrefixRunOriginal;
        }
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

    [HarmonyPatch]
    public static class BlitzBetaCompatPatch {
        [HarmonyPrepare]
        static bool IsBeta() => BetaCompatHelpers.IsAtLeastVersion(BetaCompatHelpers.VERSION_113);
    
        [HarmonyPatch(typeof(ProgressSaveManager), "IncrementBlitzAscension")]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> PatchIncrementBlitzAscension(IEnumerable<CodeInstruction> instructions) {
            var codeMatcher = new CodeMatcher(instructions);
            
            // if (this.Progress.MaxBlitzAscension >= 5)
            // 
            // callvirt     instance int32 MegaCrit.Sts2.Core.Saves.ProgressState::get_MaxBlitzAscension()
            // ldc.i4.5
            codeMatcher
                .MatchEndForward(
                    CodeMatch.Calls(AccessTools.PropertyGetter(typeof(ProgressState), "MaxBlitzAscension")),
                    LoadConstantInt32
                )
                .ThrowIfInvalid("Could not find call to ProgressState.MaxBlitzAscension followed by a ldc opcode")
                .ModifyMaxAscensionsAllowedConstant(isBlitz: true);

            return codeMatcher.Instructions();
        }
    }

    [HarmonyPatch]
    public static class DeserializeProgressStateBetaCompatPatch {
        [HarmonyPrepare]
        static bool IsBeta() => BetaCompatHelpers.IsAtLeastVersion(BetaCompatHelpers.VERSION_113);

        [HarmonyPatch(typeof(ProgressState), nameof(ProgressState.FromSerializable))]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> FromSerializableTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codeMatcher = new CodeMatcher(instructions);

            // PreferredMultiplayerAscension = ProgressState.ClampAscension(save.PreferredMultiplayerAscension, 10, "PreferredMultiplayerAscension", ctx)
            // 
            // callvirt     instance int32 MegaCrit.Sts2.Core.Saves.SerializableProgress::get_PreferredMultiplayerAscension()
            // ldc.i4.s     10
            codeMatcher
                .MatchEndForward(
                    CodeMatch.Calls(AccessTools.PropertyGetter(typeof(SerializableProgress), nameof(SerializableProgress.PreferredMultiplayerAscension))),
                    LoadConstantInt32
                )
                .ThrowIfInvalid("Could not find call to SerializableProgress.PreferredMultiplayerAscension followed by a ldc opcode")
                .ModifyMaxAscensionsAllowedConstant();
            
            // MaxMultiplayerAscension = ProgressState.ClampAscension(save.MaxMultiplayerAscension, 10, "MaxMultiplayerAscension", ctx)
            // 
            // callvirt     instance int32 MegaCrit.Sts2.Core.Saves.SerializableProgress::get_MaxMultiplayerAscension()
            // ldc.i4.s     10
            codeMatcher
                .MatchEndForward(
                    CodeMatch.Calls(AccessTools.PropertyGetter(typeof(SerializableProgress), nameof(SerializableProgress.MaxMultiplayerAscension))),
                    LoadConstantInt32
                )
                .ThrowIfInvalid("Could not find call to SerializableProgress.MaxMultiplayerAscension followed by a ldc opcode")
                .ModifyMaxAscensionsAllowedConstant();

            // PreferredBlitzAscension = ProgressState.ClampAscension(save.PreferredBlitzAscension, 5, "PreferredBlitzAscension", ctx)
            // 
            // callvirt     instance int32 MegaCrit.Sts2.Core.Saves.SerializableProgress::get_PreferredBlitzAscension()
            // ldc.i4.5
            codeMatcher
                .MatchEndForward(
                    CodeMatch.Calls(AccessTools.PropertyGetter(typeof(SerializableProgress), "PreferredBlitzAscension")),
                    LoadConstantInt32
                )
                .ThrowIfInvalid("Could not find call to SerializableProgress.PreferredBlitzAscension followed by a ldc opcode")
                .ModifyMaxAscensionsAllowedConstant(isBlitz: true);
            
            // MaxBlitzAscension = ProgressState.ClampAscension(save.MaxBlitzAscension, 5, "MaxBlitzAscension", ctx)
            // 
            // callvirt     instance int32 MegaCrit.Sts2.Core.Saves.SerializableProgress::get_MaxBlitzAscension()
            // ldc.i4.5
            codeMatcher
                .MatchEndForward(
                    CodeMatch.Calls(AccessTools.PropertyGetter(typeof(SerializableProgress), "MaxBlitzAscension")),
                    LoadConstantInt32
                )
                .ThrowIfInvalid("Could not find call to SerializableProgress.MaxBlitzAscension followed by a ldc opcode")
                .ModifyMaxAscensionsAllowedConstant(isBlitz: true);

            return codeMatcher.Instructions();
        }
    }

    [HarmonyPatch]
    public static class ClampAscensionOldCompatPatch {
        [HarmonyPrepare]
        static bool IsNotBeta() => BetaCompatHelpers.IsBeforeVersion(BetaCompatHelpers.VERSION_113);
        
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
    }

    [HarmonyPatch(typeof(ProgressState), nameof(ProgressState.ClampCharacterStatsFields))]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> PatchClampCharacterStatsAscensionFields(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions);

        // if (stats.MaxAscension > 10)
        // 
        // callvirt     instance int32 MegaCrit.Sts2.Core.Saves.CharacterStats::get_MaxAscension()
        // ldc.i4.s     10
        // ble.s
        codeMatcher
            .MatchEndForward(
                CodeMatch.Calls(AccessTools.PropertyGetter(typeof(CharacterStats), nameof(CharacterStats.MaxAscension))),
                LoadConstantInt32,
                new CodeMatch(OpCodes.Ble_S)
            )
            .ThrowIfInvalid("Could not find call to CharacterStats.get_MaxAscension followed by a ldc.i4(.s) opcode")
            .Advance(-1)
            .ModifyMaxAscensionsAllowedConstant();

//         var instruction = codeMatcher.InstructionAt(18);
//         var methodInfo = (MethodInfo) instruction.operand;
//         var genericMethodDefinition = methodInfo.GetGenericMethodDefinition();
//         GlutonsAscensionsMod.Logger.Info($"""
//         {instruction}
//         opcode: {instruction.opcode}
//         operand: {instruction.operand}
//         methodInfo: {methodInfo}
//         declaringType: {methodInfo.DeclaringType}
//         methodName: {methodInfo.Name}
//         isGenericMethod: {methodInfo.IsGenericMethod}
//         genericMethodName: {genericMethodDefinition.Name}
//         genericArguments: {genericMethodDefinition.GetGenericArguments().Join(converter: t => t.)}
//         methodParameters: {methodInfo.GetParameters().Join(converter: p => $"{p.ParameterType.Name} {p.Name}")}
//         """);
        
        // ctx.Warn($"MaxAscension ({stats.MaxAscension}) exceeds allowed ({10}), clamping");
        // 
        // ldc.i4.s     10
        // call         instance void [System.Runtime]System.Runtime.CompilerServices.DefaultInterpolatedStringHandler::AppendFormatted<int32>(!!0/*int32*/)
        codeMatcher
            .MatchStartForward(
                LoadConstantInt32,
                AppendFormattedInt
            )
            .ThrowIfInvalid("Could not find ldc.i4(.s) opcode followed by a call to DefaultInterpolatedStringHandler.AppendFormatted<int32>");
        
        // GlutonsAscensionsMod.Logger.Info(codeMatcher.InstructionsWithOffsets(0, 1).Join(delimiter: "\n"));
        // GlutonsAscensionsMod.Logger.Info($"{codeMatcher.InstructionAt(1).operand}");
            
        codeMatcher
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
        // ble.s
        codeMatcher
            .MatchEndForward(
                CodeMatch.Calls(AccessTools.PropertyGetter(typeof(CharacterStats), nameof(CharacterStats.PreferredAscension))),
                LoadConstantInt32,
                new CodeMatch(OpCodes.Ble_S)
            )
            .ThrowIfInvalid("Could not find call to CharacterStats.get_PreferredAscension followed by a ldc.i4(.s) opcode")
            .Advance(-1)
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

        if (BetaCompatHelpers.IsAtLeastVersion(BetaCompatHelpers.VERSION_113)) {
            // SaveManager.Instance.Progress.MaxBlitzAscension = 5;
            // 
            // ldc.i4.5
            // callvirt     instance void MegaCrit.Sts2.Core.Saves.ProgressState::set_MaxBlitzAscension(int32)
            codeMatcher
                .MatchStartForward(
                    LoadConstantInt32,
                    CodeMatch.Calls(AccessTools.PropertySetter(typeof(ProgressState), "MaxBlitzAscension"))
                )
                .ThrowIfInvalid("Could not find ldc.i4(.s) opcode followed by a call to ProgressState.set_MaxBlitzAscension")
                .ModifyMaxAscensionsAllowedConstant(isBlitz: true);
        }
        
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