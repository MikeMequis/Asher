using Asher.Services.Platform;

namespace Asher.Services.Tests;

public class SteamLibraryVdfTests
{
    private static string ToForwardSlashes(string path) => path.Replace('\\', '/');

    private static string Vdf(params string[] libraryPaths)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("\"libraryfolders\"");
        builder.AppendLine("{");

        for (var i = 0; i < libraryPaths.Length; i++)
        {
            builder.AppendLine($"\t\"{i}\"");
            builder.AppendLine("\t{");
            builder.AppendLine($"\t\t\"path\"\t\t\"{libraryPaths[i]}\"");
            builder.AppendLine("\t}");
        }

        builder.AppendLine("}");
        return builder.ToString();
    }

    [Fact]
    public void Reads_all_library_paths()
    {
        using var temp = new TempDirectory();
        var libraryA = Path.Combine(temp.Root, "libraryA");
        var libraryB = Path.Combine(temp.Root, "libraryB");

        var vdfPath = temp.WriteFile(
            "libraryfolders.vdf",
            Vdf(ToForwardSlashes(libraryA), ToForwardSlashes(libraryB)));

        var paths = SteamLibraryVdf.ReadLibraryPaths(vdfPath).ToList();

        Assert.Contains(ToForwardSlashes(libraryA), paths);
        Assert.Contains(ToForwardSlashes(libraryB), paths);
    }

    [Fact]
    public void Returns_empty_when_file_missing()
    {
        using var temp = new TempDirectory();

        Assert.Empty(SteamLibraryVdf.ReadLibraryPaths(Path.Combine(temp.Root, "missing.vdf")));
    }

    [Fact]
    public void Ignores_entries_without_a_path_key()
    {
        using var temp = new TempDirectory();
        var vdfPath = temp.WriteFile(
            "libraryfolders.vdf",
            "\"libraryfolders\"\n{\n\t\"0\"\n\t{\n\t\t\"totalsize\"\t\t\"123\"\n\t}\n}\n");

        Assert.Empty(SteamLibraryVdf.ReadLibraryPaths(vdfPath));
    }
}
