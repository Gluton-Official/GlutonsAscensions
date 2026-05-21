using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Helpers;

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
}