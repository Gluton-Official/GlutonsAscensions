using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ascension;

namespace GlutonsAscensions;

using static GlutonsAscensionsMod;

public static class GlutonsAscensionLevel {
    public static AscensionLevel TornRug { get; private set; } = 0;
    public static AscensionLevel OutOfBusiness { get; private set; } = 0;
    public static AscensionLevel Barren { get; private set; } = 0;
    public static AscensionLevel VolatileVials { get; private set; } = 0;
    public static AscensionLevel ShortSupply { get; private set; } = 0;
    public static AscensionLevel SlimPickings { get; private set; } = 0;
    public static AscensionLevel Plundered { get; private set; } = 0;
    public static AscensionLevel ColdComfort { get; private set; } = 0;
    public static AscensionLevel Unprepared { get; private set; } = 0;
    public static AscensionLevel LockedIn { get; private set; } = 0;

    private static readonly PropertyInfo[] _ascensionLevelProperties = AccessTools
        .GetDeclaredProperties(typeof(GlutonsAscensionLevel))
        .Where(f => f.PropertyType == typeof(AscensionLevel))
        .ToArray();

    private static bool _initialized = false;

    internal static int BaseMaxAscensionAllowed;
    internal static int FirstGlutonAscensionLevel => BaseMaxAscensionAllowed + 1;
    internal static int LastGlutonAscensionLevel => BaseMaxAscensionAllowed + _ascensionLevelProperties.Length;
    internal static int MaxAscensionAllowed => LastGlutonAscensionLevel;

    private static readonly Dictionary<int, AscensionLevel> _levelToAscensionMap = new();
    
    private static readonly Dictionary<AscensionLevel, string> _ascensionNames = new();

    private static readonly List<AscensionLevel> _ascensionLevels = [];
    
    public static void Initialize() {
        for (var ascensionLevel = FirstGlutonAscensionLevel; ascensionLevel <= LastGlutonAscensionLevel; ascensionLevel++) {
            var ascensionLevelProperty = _ascensionLevelProperties[ascensionLevel - FirstGlutonAscensionLevel];
            ascensionLevelProperty.SetValue(null, ascensionLevel);
            var ascension = ascensionLevelProperty.GetValue(null) as AscensionLevel? ?? throw new Exception($"[GlutonsAscensions] Custom enum field {ascensionLevelProperty.Name} is not an AscensionLevel enum value");
            AddAscension(ascension, ascensionLevel, ascensionLevelProperty.Name);
        }
        foreach (var ascensionLevelProperty in _ascensionLevelProperties) {
            if ((int) (ascensionLevelProperty.GetValue(null) ?? 0) == 0) {
                throw new Exception($"[GlutonsAscensions] Custom enum field {ascensionLevelProperty.Name} is not initialized: ({ascensionLevelProperty.GetValue(null)})");
            }
        }
        Logger.Info($"Initialized {_ascensionLevels.Count} ascensions: {_ascensionLevels.Join(value => $"{_ascensionNames[value]} ({(int) value})")}");
        _initialized = true;
    }

    private static void AddAscension(AscensionLevel ascension, int level, string name) {
        _ascensionLevels.Add(ascension);
        _ascensionNames[ascension] = name;
        _levelToAscensionMap[level] = ascension;
    }

    internal static void UpdateMaxAscensionAllowed(ref int maxAscensionAllowed) {
        if (maxAscensionAllowed < BaseMaxAscensionAllowed) {
            throw new Exception($"[GlutonsAscensions] Max ascension allowed was attempted to be set lower than previous value ({BaseMaxAscensionAllowed}): {maxAscensionAllowed}");
        }

        if (maxAscensionAllowed > BaseMaxAscensionAllowed) {
            BaseMaxAscensionAllowed = maxAscensionAllowed;
            Logger.Info($"Updated max ascension allowed to {MaxAscensionAllowed}");
        }

        maxAscensionAllowed = MaxAscensionAllowed;
    }

    private static void AssertInitialized() {
        if (!_initialized) throw new Exception("[GlutonsAscensions] GlutonsAscensionLevel was not initialized");
    }
    
    public static bool IsGlutonsAscensionLevel(int level) {
        AssertInitialized();
        return level >= FirstGlutonAscensionLevel && level <= LastGlutonAscensionLevel;
    }

    public static bool IsGlutonsAscension(AscensionLevel ascension) {
        AssertInitialized();
        return _ascensionLevels.Contains(ascension);
    }

    public static AscensionLevel? LevelToAscension(int level) {
        AssertInitialized();
        return IsGlutonsAscensionLevel(level) && _levelToAscensionMap.TryGetValue(level, out var ascension) ? ascension : null;
    }

    public static string? NameOf(AscensionLevel ascension) {
        AssertInitialized();
        return IsGlutonsAscension(ascension) && _ascensionNames.TryGetValue(ascension, out var name) ? name : null;
    }
}