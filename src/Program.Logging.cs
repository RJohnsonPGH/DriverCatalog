partial class Program
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Missing or invalid 'version' parameter. Expected format YYYY.MM.DD-XX (e.g. 2026.09.20-01). Got: {Version}")]
    internal static partial void LogInvalidVersion(ILogger logger, string version);

    [LoggerMessage(EventId = 2, Level = LogLevel.Critical, Message = "Driver catalog build failed.")]
    internal static partial void LogBuildFailed(ILogger logger, Exception ex);
}
