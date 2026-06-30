using System.Text.Json;
using System.Text.Json.Serialization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Migrations;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace GlutonsAscensions.Saves;

public class AscensionProgress : ISaveSchema {
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; }
    [JsonPropertyName("multiplayer")]
    [JsonSerializeCondition(SerializationCondition.SaveIfNotTypeDefault)]
    public UnlockedAscensions Multiplayer { get; set; }
    [JsonPropertyName("characters")]
    [JsonSerializeCondition(SerializationCondition.SaveIfNotCollectionEmptyOrNull)]
    public Dictionary<string, UnlockedAscensions> CharacterAscensions { get; set; } = [];
    
    private static readonly string SaveFilePath = GlutonsAscensionsMod.ModSaveFile("ascension_progress.save");

    private static readonly JsonSerializerOptions JsonOptions = new() {
        IncludeFields = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        WriteIndented = true,
    };

    public static AscensionProgress? Load(MigrationManager migrationManager, IProfileIdProvider profileIdProvider) {
        var readSaveResult = migrationManager.LoadSave<AscensionProgress>(GetSavePathForProfile(profileIdProvider.CurrentProfileId));
        GlutonsAscensionsMod.Logger.Info($"Loaded ascension progress with status: {readSaveResult.Status}");
        return readSaveResult.SaveData;
    }

    public void Save(ISaveStore saveStore, MigrationManager migrationManager, IProfileIdProvider profileIdProvider) {
        try {
            SchemaVersion = migrationManager.GetLatestVersion<AscensionProgress>();
            var json = JsonSerializationUtility.ToJson(this);
            saveStore.WriteFile(GetSavePathForProfile(profileIdProvider.CurrentProfileId), json);
            GlutonsAscensionsMod.Logger.Info($"Saved ascension progress to {GetSavePathForProfile(profileIdProvider.CurrentProfileId)}");
        } catch (Exception ex) {
            GlutonsAscensionsMod.Logger.Error($"Failed to save ascension progress: {ex}");
        }
    }

    private static string GetSavePathForProfile(int profileId) => 
        Path.Combine(UserDataPathProvider.GetProfileDir(profileId), UserDataPathProvider.SavesDir, SaveFilePath);

    public static AscensionProgress FromProgressState(ProgressState progressState) {
        var characterAscensions = new Dictionary<string, UnlockedAscensions>();
        foreach (var characterStats in progressState.CharacterStats) {
            if (UnlockedAscensions.CreateIfNeeded(characterStats.Value.MaxAscension, characterStats.Value.PreferredAscension) is { } unlockedAscensions) {
                characterAscensions[GetSaveKeyOfModelId(characterStats.Key)] = unlockedAscensions;     
            }
        }
        return new AscensionProgress {
            Multiplayer = UnlockedAscensions.CreateIfNeeded(progressState.MaxMultiplayerAscension, progressState.PreferredMultiplayerAscension) ?? default,
            CharacterAscensions = characterAscensions,
        };
    }

    public void ApplyToProgressState(ProgressState progressState) {
        progressState.MaxMultiplayerAscension = Math.Max(progressState.MaxMultiplayerAscension, Multiplayer.Ascension);
        progressState.PreferredMultiplayerAscension = Math.Max(progressState.PreferredMultiplayerAscension, Multiplayer.PreferredAscension);
        foreach (var characterStats in progressState.CharacterStats) {
            if (CharacterAscensions.TryGetValue(GetSaveKeyOfModelId(characterStats.Key), out var characterAscensions)) {
                characterStats.Value.MaxAscension = Math.Max(characterStats.Value.MaxAscension, characterAscensions.Ascension);
                characterStats.Value.PreferredAscension = Math.Max(characterStats.Value.PreferredAscension, characterAscensions.PreferredAscension);
            }
        }
    }

    private static string GetSaveKeyOfModelId(ModelId modelId) => modelId.Entry.ToLowerInvariant();

    public struct UnlockedAscensions : IEquatable<UnlockedAscensions> {
        [JsonPropertyName("ascension")]
        [JsonSerializeCondition(SerializationCondition.SaveIfNotTypeDefault)]
        public int Ascension { get; set; }
        [JsonPropertyName("preferred_ascension")]
        [JsonSerializeCondition(SerializationCondition.SaveIfNotTypeDefault)]
        public int PreferredAscension { get; set; }

        public override bool Equals(object? obj) => obj is UnlockedAscensions other && Equals(other);
        public bool Equals(UnlockedAscensions other) => Ascension == other.Ascension && PreferredAscension == other.PreferredAscension;
        public override int GetHashCode() => HashCode.Combine(Ascension, PreferredAscension);
        
        public static bool operator ==(UnlockedAscensions left, UnlockedAscensions right) => left.Equals(right);
        public static bool operator !=(UnlockedAscensions left, UnlockedAscensions right) => !(left == right);
        
        public static UnlockedAscensions? CreateIfNeeded(int currentAscension, int currentPreferredAscension) {
            var unlockedAscensions = new UnlockedAscensions {
                Ascension = currentAscension > GlutonsAscensionLevel.BaseMaxAscensionAllowed ? currentAscension : 0,
                PreferredAscension = currentPreferredAscension > GlutonsAscensionLevel.BaseMaxAscensionAllowed ? currentPreferredAscension : 0,
            };
            return unlockedAscensions == default ? null : unlockedAscensions;
        }
    }
}
