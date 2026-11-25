using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Saves;

namespace GlutonsAscensions.Helpers;

public static class AscensionExtensions {
    private static readonly MethodBase _ascensionGetTitleMethod = AccessTools.Method(typeof(AscensionHelper), nameof(AscensionHelper.GetTitle));

    extension(AscensionLevel ascension) {
        public int Level() => (int) ascension;
        public string RawName() => GlutonsAscensionLevel.NameOf(ascension) ?? ascension.ToString();
        public string FormattedName() => (
            _ascensionGetTitleMethod.Invoke(null,
                BetaCompatHelpers.IsAtLeastVersion(BetaCompatHelpers.VERSION_113)
                    ? [ascension.Level(), BetaCompatHelpers.GameModeNone ?? 0]
                    : [ascension.Level()]
            ) as LocString
        )!.GetFormattedText();

        public static AscensionLevel FromLevel(int level) => GlutonsAscensionLevel.LevelToAscension(level) ?? (AscensionLevel) level;
        
        public bool IsGlutonsAscension() => GlutonsAscensionLevel.IsGlutonsAscension(ascension);
        
        public bool HasAscension() => AscensionHelper.HasAscension(ascension);
    }

    extension(int level) {
        public bool IsGlutonsAscension() => GlutonsAscensionLevel.IsGlutonsAscensionLevel(level);
    }

    extension(NAscensionPanel ascensionPanel) {
        public void UnlockAscension(int level) {
            if (BetaCompatHelpers.IsAtLeastVersion(BetaCompatHelpers.VERSION_113)) {
                if (ascensionPanel.BetaCompatGameMode == BetaCompatHelpers.GameModeBlitz) {
                    var progress = SaveManager.Instance.Progress;
                    if (progress.BetaCompatMaxBlitzAscension < level) {
                        progress.BetaCompatMaxBlitzAscension = level;
                        progress.BetaCompatPreferredBlitzAscension = level;
                        SaveManager.Instance.SaveProgressFile();
                    }
                    return;
                }
            }
            switch (ascensionPanel.BetaCompatUiMode) {
                case MultiplayerUiMode.Singleplayer when ascensionPanel.SelectedCharacter()?.Stats is { } characterStats:
                    if (characterStats.MaxAscension < level) {
                        characterStats.MaxAscension = level;
                        characterStats.PreferredAscension = level;
                        SaveManager.Instance.SaveProgressFile();
                    }
                    break;
                case MultiplayerUiMode.Host:
                    var progress = SaveManager.Instance.Progress;
                    if (progress.MaxMultiplayerAscension < level) {
                        progress.MaxMultiplayerAscension = level;
                        progress.PreferredMultiplayerAscension = level;
                        SaveManager.Instance.SaveProgressFile();
                    }
                    break;
            }
        }
    }
}