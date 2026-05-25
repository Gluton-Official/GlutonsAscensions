using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace GlutonsAscensions;

[ModInitializer(nameof(Initialize))]
public partial class GlutonsAscensionsMod : Node {
    public const string ModId = "GlutonsAscensions";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static string NodeNamespace(string nodeName) => $"{ModId}_{nodeName}";
    public static string ModResource(string resourceName) => $"res://{ModId}/{resourceName}";

    public static void Initialize() {
        Harmony harmony = new(ModId);
        harmony.PatchAll();
        
        // Register config after patching since we patch some BaseLib config methods
        ModConfigRegistry.Register(ModId, new GlutonsAscensionsConfig());
    }
}