using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace GlutonsAscensions.Patches;

[HarmonyPatch(typeof(UnlockConsoleCmd))]
public static class CommandPatches {
    [HarmonyPatch(nameof(UnlockConsoleCmd.UnlockAscensions))]
    [HarmonyPrefix]
    private static bool HandleAscensionLevelAndTargetParameters(List<string>? ascensions) {
        if (ascensions is null || ascensions.Count == 0) return HarmonyExtensions.PrefixRunOriginal;

        if (!int.TryParse(ascensions[0], out var ascensionLevel)) {
            throw new ArgumentException($"[GlutonsAscensions] Invalid ascension level ({ascensions[0]}), expected integer");
        }
        if (ascensionLevel > GlutonsAscensionLevel.MaxAscensionAllowed) {
            throw new ArgumentException($"[GlutonsAscensions] Ascension level ({ascensionLevel}) is greater than the maximum allowed ({GlutonsAscensionLevel.MaxAscensionAllowed})");
        }

        var progress = SaveManager.Instance.Progress;
        
        if (ascensions.Count > 1) {
            var target = ascensions[1];
            if (target == "MULTIPLAYER") {
                progress.MaxMultiplayerAscension = Math.Max(progress.MaxMultiplayerAscension, ascensionLevel);
            } else if (ModelDb.AllCharacters.FirstOrDefault(character => character.Id.Entry == target) is { } targetCharacter) {
                var characterStats = progress.GetOrCreateCharacterStats(targetCharacter.Id);
                characterStats.MaxAscension = Math.Max(characterStats.MaxAscension, ascensionLevel);
            } else {
                throw new ArgumentException($"[GlutonsAscensions] Invalid target character ({target}), expected MULTIPLAYER or character ID");
            }
        } else {
            progress.MaxMultiplayerAscension = Math.Max(progress.MaxMultiplayerAscension, ascensionLevel);
            
            foreach (var character in ModelDb.AllCharacters) {
                var characterStats = progress.GetOrCreateCharacterStats(character.Id);
                characterStats.MaxAscension = Math.Max(characterStats.MaxAscension, ascensionLevel);
            }
        }

        return HarmonyExtensions.PrefixSkipOriginal;
    }

    [HarmonyPatch(nameof(UnlockConsoleCmd.GetArgumentCompletions))]
    [HarmonyPrefix]
    private static bool GetAscensionLevelAndTargetCompletions(UnlockConsoleCmd __instance, string[] args, ref CompletionResult __result) {
        if (!__instance.CmdName.Equals("unlock", StringComparison.OrdinalIgnoreCase) || 
            args.Length < 2 ||
            !args[0].Equals("ascensions", StringComparison.OrdinalIgnoreCase))
        { 
            return HarmonyExtensions.PrefixRunOriginal;
        }

        switch (args.Length) {
            case 2: {
                var ascensionLevel = args[1];
                __result = new CompletionResult {
                    Candidates = Enumerable.Range(1, GlutonsAscensionLevel.MaxAscensionAllowed)
                        .Select(i => i.ToString())
                        .Where(s => s.StartsWith(ascensionLevel))
                        .ToList(),
                    Type = CompletionType.Argument,
                    ArgumentContext = __instance.CmdName,
                };
                return HarmonyExtensions.PrefixSkipOriginal;
            }
            case 3: {
                var target = args[2];
                __result = new CompletionResult {
                    Candidates = ModelDb.AllCharacters
                        .Select(c => c.Id.Entry)
                        .AddItem("MULTIPLAYER")
                        .Where(s => s.StartsWith(target))
                        .ToList(),
                    Type = CompletionType.Argument,
                    ArgumentContext = __instance.CmdName,
                };
                return HarmonyExtensions.PrefixSkipOriginal;
            }
            default:
                return HarmonyExtensions.PrefixRunOriginal;
        }
    }
}