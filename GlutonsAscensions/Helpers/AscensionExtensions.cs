using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Saves;

namespace GlutonsAscensions.Helpers;

public static class AscensionExtensions {
    extension(AscensionLevel ascension) {
        public int Level() => GlutonsAscensionLevel.AscensionToLevel(ascension) ?? (int) ascension;
        public string RawName() => GlutonsAscensionLevel.NameOf(ascension) ?? ascension.ToString();
        public string FormattedName() => AscensionHelper.GetTitle(ascension.Level()).GetFormattedText();

        public static AscensionLevel FromLevel(int level) => GlutonsAscensionLevel.LevelToAscension(level) ?? (AscensionLevel) level;
        
        public bool IsGlutonsAscension() => GlutonsAscensionLevel.IsGlutonsAscension(ascension);
        
        public bool HasAscension() => AscensionHelper.HasAscension(ascension);
    }

    extension(int level) {
        public bool IsGlutonsAscension() => GlutonsAscensionLevel.IsGlutonsAscensionLevel(level);
    }

    extension(NAscensionPanel ascensionPanel) {
        public void UnlockAscension(int level) {
            switch (ascensionPanel._mode) {
                case MultiplayerUiMode.Singleplayer when ascensionPanel.SelectedCharacter()?.Stats is { } characterStats:
                    if (characterStats.MaxAscension < level) {
                        characterStats.MaxAscension = level;
                        SaveManager.Instance.SaveProgressFile();
                    }
                    break;
                case MultiplayerUiMode.Host:
                    var progress = SaveManager.Instance.Progress;
                    if (progress.MaxMultiplayerAscension < level) {
                        progress.MaxMultiplayerAscension = level;
                        SaveManager.Instance.SaveProgressFile();
                    }
                    break;
            }
        }
    }
}