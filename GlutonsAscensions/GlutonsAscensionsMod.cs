using Godot;
using BaseLib.Audio;
using GlutonsAscensions.Helpers;
using GlutonsAscensions.Saves;
using HarmonyLib;
using JetBrains.Annotations;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaLogger = MegaCrit.Sts2.Core.Logging.Logger;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Saves;

namespace GlutonsAscensions;

[ModInitializer(nameof(Initialize))]
public partial class GlutonsAscensionsMod : Node {
    public const string ModId = "GlutonsAscensions";
    
    private static readonly string _keyPrefix = ModId.ToUpperInvariant();

    public static MegaLogger Logger { get; } = new(ModId, LogType.Generic);

    public static string ModNamespace(string key) => $"{ModId}_{key}";
    public static LocString ModLocString(string locTable, string locEntryKey) => new(locTable, $"{_keyPrefix}-{locEntryKey}");
    public static string ModResource([PathReference($"~/{ModId}Resources")] string resourceName) => Path.Combine("res://", ModId, resourceName);
    public static string ModSaveFile(string fileNameWithExtension) => Path.Combine("mods", ModId, fileNameWithExtension);

    public static void Initialize() {
        Harmony harmony = new(ModId);
        harmony.PatchAll();

        var migrationManager = SaveManager.Instance._migrationManager;
        migrationManager.SetMinimumSupportedVersion<AscensionProgress>(1);
        migrationManager.EnsureVersionSet<AscensionProgress>();
        Logger.Info($"AscensionProgress save versions - latest: v{migrationManager._latestVersions[typeof(AscensionProgress)]}, minimum: v{migrationManager._minimumSupportedVersions[typeof(AscensionProgress)]}");
        
        AscensionProgress.RegisterAsSaveType();
    }
}