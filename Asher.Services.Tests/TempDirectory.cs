namespace Asher.Services.Tests;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Root = Path.Combine(Path.GetTempPath(), "asher-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string Root { get; }

    public string CreateGameFolder(params string[] relativeGameFolderSegments)
    {
        var directory = Path.Combine(
            new[] { Root }.Concat(relativeGameFolderSegments).ToArray());

        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "DustAET"), string.Empty);
        return directory;
    }

    public string CreateDirectory(params string[] relativeSegments)
    {
        var directory = Path.Combine(
            new[] { Root }.Concat(relativeSegments).ToArray());

        Directory.CreateDirectory(directory);
        return directory;
    }

    public string WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(Root, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Root))
                Directory.Delete(Root, recursive: true);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
