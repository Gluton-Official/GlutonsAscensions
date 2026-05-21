namespace GlutonsAscensions.Helpers;

public static class HarmonyExtensions {
    /// <returns>False if the original method execution should be skipped</returns>
    public static bool AsPrefixReturnValue(this bool shouldSkipOriginal) => !shouldSkipOriginal;

    public static readonly bool PrefixSkipOriginal = false;
    public static readonly bool PrefixRunOriginal = true;
}