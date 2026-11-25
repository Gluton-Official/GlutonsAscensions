using System.Reflection;
using System.Reflection.Emit;
using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes;

namespace GlutonsAscensions.Patches;

[HarmonyPatch]
public static class DreadPatches {
    [HarmonyPatch]
    public static class DreadInitializeMultiplayerAsHostAscensionBetaCompatPatch {
        [HarmonyPrepare]
        static bool IsBeta() => BetaCompatHelpers.IsAtLeastVersion(BetaCompatHelpers.VERSION_113);

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() => AccessTools.Method(BetaCompatHelpers.NDreadScreenType.Type, "InitializeMultiplayerAsHost");

        [HarmonyPatch]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> InitializeMultiplayerAsHostTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codeMatcher = new CodeMatcher(instructions);

            //-this._lobby.AddLocalHostPlayer(new UnlockState(SaveManager.Instance.Progress), 10, SaveManager.Instance.Progress.MaxBlitzAscension);
            //+this._lobby.AddLocalHostPlayer(new UnlockState(SaveManager.Instance.Progress), <GlutonsAscensionLevel.MaxAscensionAllowed>, SaveManager.Instance.Progress.MaxBlitzAscension);
            // 
            // ldfld        class MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.StartRunLobby MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen::_lobby
            // call         class MegaCrit.Sts2.Core.Saves.SaveManager MegaCrit.Sts2.Core.Saves.SaveManager::get_Instance()
            // callvirt     instance class MegaCrit.Sts2.Core.Saves.ProgressState MegaCrit.Sts2.Core.Saves.SaveManager::get_Progress()
            // newobj       instance void MegaCrit.Sts2.Core.Unlocks.UnlockState::.ctor(class MegaCrit.Sts2.Core.Saves.ProgressState)
            //-ldc.i4.s     10
            //+ldc.i4       <GlutonsAscensionLevel.MaxAscensionAllowed>
            // call         class MegaCrit.Sts2.Core.Saves.SaveManager MegaCrit.Sts2.Core.Saves.SaveManager::get_Instance()
            // callvirt     instance class MegaCrit.Sts2.Core.Saves.ProgressState MegaCrit.Sts2.Core.Saves.SaveManager::get_Progress()
            // callvirt     instance int32 MegaCrit.Sts2.Core.Saves.ProgressState::get_MaxBlitzAscension()
            // callvirt     instance valuetype [System.Runtime]System.Nullable`1<valuetype MegaCrit.Sts2.Core.Entities.Multiplayer.StartRunLobbyPlayer> MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.StartRunLobby::AddLocalHostPlayer(class MegaCrit.Sts2.Core.Unlocks.UnlockState, int32, int32)
            codeMatcher
                .MatchStartForward(
                    HarmonyExtensions.LoadConstantInt32,
                    CodeMatch.WithOpcodes([OpCodes.Call]),
                    CodeMatch.WithOpcodes([OpCodes.Callvirt]),
                    CodeMatch.WithOpcodes([OpCodes.Callvirt]),
                    CodeMatch.Calls(AccessTools.Method(typeof(StartRunLobby), nameof(StartRunLobby.AddLocalHostPlayer)))
                )
                .ThrowIfInvalid("Could not find ldc.i4(.s) opcode followed by a call opcode followed by 2 callvirt opcodes followed by a call to StartRunLobby.AddLocalHostPlayer")
                .RemoveInstruction()
                .InsertAndAdvance(CodeInstruction.LoadConstant(GlutonsAscensionLevel.MaxAscensionAllowed));
                
            return codeMatcher.Instructions();
        }
    }

    [HarmonyPatch]
    public class DreadStartNewSingleplayerRunAscensionBetaCompatPatch {
        [HarmonyPrepare]
        static bool IsBeta() => BetaCompatHelpers.IsAtLeastVersion(BetaCompatHelpers.VERSION_113);

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() => AccessTools.AsyncMoveNext(AccessTools.Method(BetaCompatHelpers.NDreadScreenType.Type, "StartNewSingleplayerRun"));

        [HarmonyPatch]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> StartNewSingleplayerDreadRunTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codeMatcher = new CodeMatcher(instructions);

            //-RunState runState = await NGame.Instance.StartNewSingleplayerRun(player, true, (IReadOnlyList<ActModel>) acts, (IReadOnlyList<ModifierModel>) Array.Empty<ModifierModel>(), seed, GameMode.Dread, false, 10);
            //+RunState runState = await NGame.Instance.StartNewSingleplayerRun(player, true, (IReadOnlyList<ActModel>) acts, (IReadOnlyList<ModifierModel>) Array.Empty<ModifierModel>(), seed, GameMode.Dread, false, <GlutonsAscensionLevel.MaxAllowedAscension>);
            //
            // call         class MegaCrit.Sts2.Core.Nodes.NGame MegaCrit.Sts2.Core.Nodes.NGame::get_Instance()
            // ldarg.0      // this
            // ldfld        class MegaCrit.Sts2.Core.Entities.Players.Player MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen/'<StartNewSingleplayerRun>d__54'::'<player>5__2'
            // ldc.i4.1
            // ldarg.0      // this
            // ldfld        class [System.Collections]System.Collections.Generic.List`1<class MegaCrit.Sts2.Core.Models.ActModel> MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen/'<StartNewSingleplayerRun>d__54'::acts
            // call         !!0/*class MegaCrit.Sts2.Core.Models.ModifierModel*/[] [System.Runtime]System.Array::Empty<class MegaCrit.Sts2.Core.Models.ModifierModel>()
            // ldarg.0      // this
            // ldfld        string MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen/'<StartNewSingleplayerRun>d__54'::seed
            // ldc.i4.3
            // ldc.i4.0
            //-ldc.i4.s     10
            //+ldc.i4.s     <GlutonsAscensionLevel.MaxAllowedAscension>
            // ldloca.s     V_5
            // initobj      valuetype [System.Runtime]System.Nullable`1<valuetype [System.Runtime]System.DateTimeOffset>
            // ldloc.s      V_5
            // callvirt     instance class [System.Runtime]System.Threading.Tasks.Task`1<class MegaCrit.Sts2.Core.Runs.RunState> MegaCrit.Sts2.Core.Nodes.NGame::StartNewSingleplayerRun(class MegaCrit.Sts2.Core.Entities.Players.Player, bool, class [System.Runtime]System.Collections.Generic.IReadOnlyList`1<class MegaCrit.Sts2.Core.Models.ActModel>, class [System.Runtime]System.Collections.Generic.IReadOnlyList`1<class MegaCrit.Sts2.Core.Models.ModifierModel>, string, valuetype MegaCrit.Sts2.Core.Runs.GameMode, bool, int32, valuetype [System.Runtime]System.Nullable`1<valuetype [System.Runtime]System.DateTimeOffset>)
            // callvirt     instance valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<!0/*class MegaCrit.Sts2.Core.Runs.RunState*/> class [System.Runtime]System.Threading.Tasks.Task`1<class MegaCrit.Sts2.Core.Runs.RunState>::GetAwaiter()
            // stloc.s      V_4
            codeMatcher
                .MatchStartForward(
                    HarmonyExtensions.LoadConstantInt32,
                    CodeMatch.LoadsLocal(useAddress: true),
                    CodeMatch.WithOpcodes([OpCodes.Initobj]),
                    CodeMatch.LoadsLocal(),
                    CodeMatch.Calls(AccessTools.Method(typeof(NGame), nameof(NGame.StartNewSingleplayerRun)))
                )
                .ThrowIfInvalid("Could not find ldc.i4(.s) opcode followed by ldloca opcode followed by initobj opcode followed by ldloc opcode followed by call to NGame.StartNewSingleplayerRun")
                .RemoveInstruction()
                .InsertAndAdvance(CodeInstruction.LoadConstant(GlutonsAscensionLevel.MaxAscensionAllowed));

            return codeMatcher.Instructions();
        }
    }
    
    [HarmonyPatch]
    public class DreadStartNewMultiplayerRunAscensionBetaCompatPatch {
        [HarmonyPrepare]
        static bool IsBeta() => BetaCompatHelpers.IsAtLeastVersion(BetaCompatHelpers.VERSION_113);

        [HarmonyTargetMethod]
        static MethodBase TargetMethod() => AccessTools.AsyncMoveNext(AccessTools.Method(BetaCompatHelpers.NDreadScreenType.Type, "StartNewMultiplayerRun"));

        [HarmonyPatch]
        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> StartNewMultiplayerDreadRunTranspiler(IEnumerable<CodeInstruction> instructions) {
            var codeMatcher = new CodeMatcher(instructions);

            //-RunState runState = await NGame.Instance.StartNewMultiplayerRun(players, ndreadScreen._lobby, true, (IReadOnlyList<ActModel>) acts, (IReadOnlyList<ModifierModel>) Array.Empty<ModifierModel>(), seed, 10);
            //+RunState runState = await NGame.Instance.StartNewMultiplayerRun(players, ndreadScreen._lobby, true, (IReadOnlyList<ActModel>) acts, (IReadOnlyList<ModifierModel>) Array.Empty<ModifierModel>(), seed, <GlutonsAscensionLevel.MaxAscensionAllowed>);
            //
            // call         class MegaCrit.Sts2.Core.Nodes.NGame MegaCrit.Sts2.Core.Nodes.NGame::get_Instance()
            // ldarg.0      // this
            // ldfld        class [System.Collections]System.Collections.Generic.List`1<class MegaCrit.Sts2.Core.Entities.Players.Player> MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen/'<StartNewMultiplayerRun>d__55'::'<players>5__2'
            // ldloc.1      // ndreadScreen
            // ldfld        class MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.StartRunLobby MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen::_lobby
            // ldc.i4.1
            // ldarg.0      // this
            // ldfld        class [System.Collections]System.Collections.Generic.List`1<class MegaCrit.Sts2.Core.Models.ActModel> MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen/'<StartNewMultiplayerRun>d__55'::acts
            // call         !!0/*class MegaCrit.Sts2.Core.Models.ModifierModel*/[] [System.Runtime]System.Array::Empty<class MegaCrit.Sts2.Core.Models.ModifierModel>()
            // ldarg.0      // this
            // ldfld        string MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen/'<StartNewMultiplayerRun>d__55'::seed
            //-ldc.i4.s     10
            //+ldc.i4.s     <GlutonsAscensionLevel.MaxAscensionAllowed>
            // ldloca.s     V_13
            // initobj      valuetype [System.Runtime]System.Nullable`1<valuetype [System.Runtime]System.DateTimeOffset>
            // ldloc.s      V_13
            // callvirt     instance class [System.Runtime]System.Threading.Tasks.Task`1<class MegaCrit.Sts2.Core.Runs.RunState> MegaCrit.Sts2.Core.Nodes.NGame::StartNewMultiplayerRun(class [System.Collections]System.Collections.Generic.List`1<class MegaCrit.Sts2.Core.Entities.Players.Player>, class MegaCrit.Sts2.Core.Multiplayer.Game.Lobby.StartRunLobby, bool, class [System.Runtime]System.Collections.Generic.IReadOnlyList`1<class MegaCrit.Sts2.Core.Models.ActModel>, class [System.Runtime]System.Collections.Generic.IReadOnlyList`1<class MegaCrit.Sts2.Core.Models.ModifierModel>, string, int32, valuetype [System.Runtime]System.Nullable`1<valuetype [System.Runtime]System.DateTimeOffset>)
            // callvirt     instance valuetype [System.Runtime]System.Runtime.CompilerServices.TaskAwaiter`1<!0/*class MegaCrit.Sts2.Core.Runs.RunState*/> class [System.Runtime]System.Threading.Tasks.Task`1<class MegaCrit.Sts2.Core.Runs.RunState>::GetAwaiter()
            // stloc.s      V_12
            codeMatcher
                .MatchStartForward(
                    HarmonyExtensions.LoadConstantInt32,
                    CodeMatch.LoadsLocal(useAddress: true),
                    CodeMatch.WithOpcodes([OpCodes.Initobj]),
                    CodeMatch.LoadsLocal(),
                    CodeMatch.Calls(AccessTools.Method(typeof(NGame), nameof(NGame.StartNewMultiplayerRun)))
                )
                .ThrowIfInvalid("Could not find ldc.i4(.s) opcode followed by ldloca opcode followed by initobj opcode followed by ldloc opcode followed by call to NGame.StartNewMultiplayerRun")
                .RemoveInstruction()
                .InsertAndAdvance(CodeInstruction.LoadConstant(GlutonsAscensionLevel.MaxAscensionAllowed));

            return codeMatcher.Instructions();
        }
    }
}
