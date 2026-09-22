using System.ComponentModel;
using System.Diagnostics;

namespace DriverCatalog.Catalog;

/// <summary>
/// CAB extractor implementation that shells out to the cross-platform <c>cabextract</c> utility.
/// </summary>
public sealed partial class CabextractExtractor(ILogger<CabextractExtractor> logger) : ICabExtractor
{
    private const string ExecutableName = "cabextract";

    /// <inheritdoc />
    public async Task<string> ExtractFileAsync(
        string cabPath,
        string fileName,
        string outputDirectory,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);

        var processInfo = new ProcessStartInfo(ExecutableName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // cabextract -F <file> -d <dir> <cab>: extract only the named file into the output directory.
        processInfo.ArgumentList.Add("-F");
        processInfo.ArgumentList.Add(fileName);
        processInfo.ArgumentList.Add("-d");
        processInfo.ArgumentList.Add(outputDirectory);
        processInfo.ArgumentList.Add(cabPath);

        LogRunning(ExecutableName, fileName, outputDirectory, cabPath);

        Process process;
        try
        {
            process = Process.Start(processInfo)
                ?? throw new InvalidOperationException($"Failed to start the {ExecutableName} process.");
        }
        catch (Win32Exception ex)
        {
            throw new InvalidOperationException(
                $"The '{ExecutableName}' executable was not found. Install it (e.g. 'sudo apt-get install cabextract') and try again.", ex);
        }

        using (process)
        {
            // Read both streams concurrently with the wait to avoid deadlocks on full buffers.
            var standardOutput = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardError = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            var output = await standardOutput;
            var error = await standardError;

            if (!string.IsNullOrWhiteSpace(output))
                LogStdout(output);
            if (!string.IsNullOrWhiteSpace(error))
                LogStderr(error);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"CAB extraction failed with exit code {process.ExitCode}: {error}");
            }
        }

        var extractedPath = Path.Join(outputDirectory, fileName);

        if (!File.Exists(extractedPath))
        {
            throw new FileNotFoundException(
                $"Expected extracted file {extractedPath} does not exist after CAB extraction.", extractedPath);
        }

        return extractedPath;
    }
}
