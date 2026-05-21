namespace GlutonsAscensions.Helpers;

public static class HarmonyExtensions {
    /// <returns>False if the original method execution should be skipped</returns>
    public static bool AsPrefixReturnValue(this bool shouldSkipOriginal) => !shouldSkipOriginal;

    public const bool PrefixSkipOriginal = false;
    public const bool PrefixRunOriginal = true;
}