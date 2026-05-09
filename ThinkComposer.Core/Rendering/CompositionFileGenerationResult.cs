namespace Instrumind.ThinkComposer.Core.Rendering;

public sealed record CompositionFileGenerationResult(IReadOnlyList<CompositionGeneratedFile>? Files = null)
{
    public IReadOnlyList<CompositionGeneratedFile> Files { get; init; } =
        Files ?? Array.Empty<CompositionGeneratedFile>();

    public void WriteToDirectory(string targetDirectory)
    {
        if (string.IsNullOrWhiteSpace(targetDirectory))
        {
            throw new ArgumentException("Target directory is required.", nameof(targetDirectory));
        }

        Directory.CreateDirectory(targetDirectory);
        var fullRootPath = EnsureTrailingSeparator(Path.GetFullPath(targetDirectory));
        foreach (var file in Files)
        {
            var targetPath = Path.Combine(targetDirectory, file.RelativePath);
            var fullTargetPath = Path.GetFullPath(targetPath);
            if (!fullTargetPath.StartsWith(fullRootPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Generated path escapes target directory: {file.RelativePath}");
            }

            var directory = Path.GetDirectoryName(fullTargetPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullTargetPath, file.Content);
        }
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) ||
            path.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
    }
}
