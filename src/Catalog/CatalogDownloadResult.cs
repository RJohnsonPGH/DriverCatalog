namespace DriverCatalog.Catalog;

/// <summary>
/// Represents a downloaded (and possibly extracted) catalog file with automatic cleanup on disposal.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CatalogDownloadResult"/> class.
/// </remarks>
/// <param name="filePath">The path to the downloaded catalog file.</param>
/// <param name="workDirectory">Optional temporary work directory that is removed recursively on disposal.</param>
public sealed class CatalogDownloadResult(string filePath, string? workDirectory = null) : IDisposable
{
	private bool _disposed;

	/// <summary>
	/// Gets the path to the downloaded catalog file.
	/// </summary>
	public string FilePath { get; } = filePath ?? throw new ArgumentNullException(nameof(filePath));

	/// <summary>
	/// Disposes the download result and removes the temporary work directory, if any.
	/// Cleanup failures are ignored; the OS temp directory maintenance will remove leftovers.
	/// </summary>
	public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (workDirectory is not null && Directory.Exists(workDirectory))
        {
            try
            {
                Directory.Delete(workDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures - the file will be cleaned up by temp directory maintenance.
            }
        }
    }
}
