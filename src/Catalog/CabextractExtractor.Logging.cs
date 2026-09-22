namespace DriverCatalog.Catalog;

public sealed partial class CabextractExtractor
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = "Running {Executable} -F {FileName} -d {OutputDirectory} {CabPath}")]
    private partial void LogRunning(string executable, string fileName, string outputDirectory, string cabPath);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug, Message = "cabextract stdout: {Output}")]
    private partial void LogStdout(string output);

    [LoggerMessage(EventId = 3, Level = LogLevel.Debug, Message = "cabextract stderr: {Error}")]
    private partial void LogStderr(string error);
}
