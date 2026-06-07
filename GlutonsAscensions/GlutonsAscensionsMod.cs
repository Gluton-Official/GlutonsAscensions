using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;

namespace GlutonsAscensions;

[ModInitializer(nameof(Initialize))]
public partial class GlutonsAscensionsMod : Node {
    public const string ModId = "GlutonsAscensions";
    
    private static readonly string LocKey = ModId.ToUpperInvariant();

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);
    
    // public static readonly SavedSpireField<CharacterModel, int> UnlockedAscensionLevel = new (() => 0, $"{ModId}_UnlockedAscensionLevel");
    // [SavedProperty]
    // public static int UnlockedMultiplayerAscensionLevel { get; set; }

    public static string NodeNamespace(string nodeName) => $"{ModId}_{nodeName}";
    public static string ModResource(string resourceName) => $"res://{ModId}/{resourceName}";
    public static LocString ModLocString(string locTable, string locEntryKey) => new(locTable, $"{LocKey}-{locEntryKey}");

    public static void Initialize() {
        Harmony harmony = new(ModId);
        harmony.PatchAll();
    }
}