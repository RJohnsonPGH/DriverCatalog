namespace DriverCatalog.Catalog;

/// <summary>
/// Extracts files from CAB cabinet archives.
/// </summary>
public interface ICabExtractor
{
    /// <summary>
    /// Extracts a single named file from a cabinet archive into the output directory.
    /// </summary>
    /// <param name="cabPath">Path to the CAB archive.</param>
    /// <param name="fileName">Name of the file to extract from the archive.</param>
    /// <param name="outputDirectory">Directory the file is extracted into (created if missing).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The path to the extracted file.</returns>
    Task<string> ExtractFileAsync(
        string cabPath,
        string fileName,
        string outputDirectory,
        CancellationToken cancellationToken = default);
}
