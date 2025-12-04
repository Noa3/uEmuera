/// <summary>
/// Provides localized game messages for the Emuera engine.
/// This class centralizes all user-facing messages that need to be translated.
/// </summary>
public static class GameMessages
{
    /// <summary>
    /// Gets a localized message by key.
    /// Falls back to the key itself if no translation is found.
    /// </summary>
    public static string Get(string key)
    {
        return MultiLanguage.GetText(key);
    }

    // System messages - Loading/Initialization
    public static string LoadingMacroTxt => Get("[LoadingMacroTxt]");
    public static string LoadingReplaceCsv => Get("[LoadingReplaceCsv]");
    public static string LoadingRenameCsv => Get("[LoadingRenameCsv]");
    public static string ConfigFileError => Get("[ConfigFileError]");
    public static string ConfigFileErrorTitle => Get("[ConfigFileErrorTitle]");
    public static string ConfigExitSelected => Get("[ConfigExitSelected]");
    public static string ResourcesLoadError => Get("[ResourcesLoadError]");
    public static string ReplaceCsvError => Get("[ReplaceCsvError]");
    public static string ReplaceCsvErrorTitle => Get("[ReplaceCsvErrorTitle]");
    public static string ReplaceCsvExitSelected => Get("[ReplaceCsvExitSelected]");
    public static string RenameCsvNotFound => Get("[RenameCsvNotFound]");
    public static string GamebaseLoadError => Get("[GamebaseLoadError]");
    public static string ErhLoadError => Get("[ErhLoadError]");
    public static string FatalInitError => Get("[FatalInitError]");

    // System messages - Analysis mode
    public static string FileAnalysisComplete => Get("[FileAnalysisComplete]");
    public static string PressEnterToExit => Get("[PressEnterToExit]");
    public static string ErbCodeError => Get("[ErbCodeError]");
    public static string CompatibilityOptionHint => Get("[CompatibilityOptionHint]");
    public static string OutputLogToFile => Get("[OutputLogToFile]");

    // Error messages
    public static string ErrorAtFunctionEnd => Get("[ErrorAtFunctionEnd]");
    public static string EmueraErrorAtFunctionEnd => Get("[EmueraErrorAtFunctionEnd]");
    public static string UnexpectedErrorAtFunctionEnd => Get("[UnexpectedErrorAtFunctionEnd]");
    public static string ThrowOccurred => Get("[ThrowOccurred]");
    public static string ThrowContent => Get("[ThrowContent]");
    public static string ErrorOccurred => Get("[ErrorOccurred]");
    public static string ErrorContent => Get("[ErrorContent]");
    public static string CurrentFunction => Get("[CurrentFunction]");
    public static string FunctionCallStack => Get("[FunctionCallStack]");
    public static string EmueraErrorOccurred => Get("[EmueraErrorOccurred]");
    public static string UnexpectedErrorOccurred => Get("[UnexpectedErrorOccurred]");

    // Game flow messages
    public static string InvalidValue => Get("[InvalidValue]");
    public static string CommandExecutionFailed => Get("[CommandExecutionFailed]");
    public static string AutoSaveError => Get("[AutoSaveError]");
    public static string AutoSaveSkipped => Get("[AutoSaveSkipped]");
    public static string NotEnoughMoney => Get("[NotEnoughMoney]");
    public static string NotForSale => Get("[NotForSale]");
    public static string SaveError => Get("[SaveError]");
    public static string NoData => Get("[NoData]");

    // Save/Load messages
    public static string WhichNumberToSave => Get("[WhichNumberToSave]");
    public static string WhichNumberToLoad => Get("[WhichNumberToLoad]");
    public static string Back => Get("[Back]");
    public static string ShowSaveData => Get("[ShowSaveData]");
    public static string DataExistsOverwrite => Get("[DataExistsOverwrite]");
    public static string Yes => Get("[Yes]");
    public static string No => Get("[No]");

    // Infinite loop detection
    public static string InfiniteLoopDetected => Get("[InfiniteLoopDetected]");
    public static string InfiniteLoopMessage => Get("[InfiniteLoopMessage]");

    // Format helper for continuous command execution
    public static string ContinuousCommandFormat => Get("[ContinuousCommandFormat]");
}
