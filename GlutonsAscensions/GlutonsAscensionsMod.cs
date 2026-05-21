using BaseLib.Config;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace GlutonsAscensions;

[ModInitializer(nameof(Initialize))]
public partial class GlutonsAscensionsMod : Node {
    public const string ModId = "GlutonsAscensions";
    public const string ModNamespace = $"dev.gluton.{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    private static readonly string _nodeNamespace = ModNamespace.Replace('.', '_');
    public static string NodeNamespace(string nodeName) => $"{_nodeNamespace}_{nodeName}";
    public static string ModResource(string resourceName) => $"res://{ModId}/{resourceName}";

    public static void Initialize() {
        ModConfigRegistry.Register(ModId, new GlutonsAscensionsConfig());

        Harmony harmony = new(ModId);
        harmony.PatchAll();
    }
}