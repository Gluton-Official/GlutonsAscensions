using System.Reflection;
using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ascension;

namespace GlutonsAscensions;

using static GlutonsAscensionsMod;

public static class GlutonsAscensionLevel {
    [CustomEnum]
    public static AscensionLevel TornRug;
    [CustomEnum]
    public static AscensionLevel OutOfBusiness;
    [CustomEnum]
    public static AscensionLevel Barren;
    [CustomEnum]
    public static AscensionLevel VolatileVials;
    [CustomEnum]
    public static AscensionLevel ShortSupply;
    [CustomEnum]
    public static AscensionLevel SlimPickings;
    [CustomEnum]
    public static AscensionLevel Plundered;
    [CustomEnum]
    public static AscensionLevel ColdComfort;
    [CustomEnum]
    public static AscensionLevel Unprepared;
    [CustomEnum]
    public static AscensionLevel LockedIn;

    private static readonly FieldInfo[] Enums = AccessTools
        .GetDeclaredFields(typeof(GlutonsAscensionLevel))
        .Where(f => f.FieldType == typeof(AscensionLevel))
        .ToArray();

    private static bool _initialized = false;

    private static readonly string[] _names = [
        nameof(TornRug),
        nameof(OutOfBusiness),
        nameof(Barren),
        nameof(VolatileVials),
        nameof(ShortSupply),
        nameof(SlimPickings),
        nameof(Plundered), 
        nameof(ColdComfort),
        nameof(Unprepared),
        nameof(LockedIn) 
    ];

    internal static int BaseMaxAscensionAllowed;
    internal static int FirstGlutonAscensionLevel => BaseMaxAscensionAllowed + 1;
    internal static int LastGlutonAscensionLevel => BaseMaxAscensionAllowed + Enums.Length;
    internal static int MaxAscensionAllowed => LastGlutonAscensionLevel;

    private static readonly Dictionary<AscensionLevel, int> _ascensionToLevelMap = new();
    private static readonly Dictionary<int, AscensionLevel> _levelToAscensionMap = new();
    
    private static readonly Dictionary<AscensionLevel, string> _ascensionNames = new();

    private static readonly List<AscensionLevel> Values = [];
    
    public static void Initialize() {
        for (var ascensionLevel = FirstGlutonAscensionLevel; ascensionLevel <= LastGlutonAscensionLevel; ascensionLevel++) {
            var index = ascensionLevel - FirstGlutonAscensionLevel;
            var enumField = Enums[index];
            var ascension = enumField.GetValue(null) as AscensionLevel? ?? throw new Exception($"[GlutonsAscensions] CustomEnum field {enumField.Name} is not an AscensionLevel enum value");
            AddAscension(ascension, ascensionLevel, _names[index]);
        }
        Logger.Info($"Initialized {Values.Count} ascensions: {Values.Join(value => _ascensionNames[value])}");
        _initialized = true;
    }

    private static void AddAscension(AscensionLevel ascension, int level, string name) {
        Values.Add(ascension);
        _ascensionNames[ascension] = name;
        _ascensionToLevelMap[ascension] = level;
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
        return Values.Contains(ascension);
    }

    public static int? AscensionToLevel(AscensionLevel ascension) {
        AssertInitialized(); 
        return IsGlutonsAscension(ascension) && _ascensionToLevelMap.TryGetValue(ascension, out var level) ? level : null;
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