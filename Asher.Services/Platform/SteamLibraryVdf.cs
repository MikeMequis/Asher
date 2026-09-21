namespace Asher.Services.Platform
{
    /// <summary>
    /// Reads quoted path values from Steam's steamapps/libraryfolders.vdf.
    /// Shared by the Windows and Linux discovery implementations; callers filter for the
    /// path shape of their own platform (drive paths vs absolute POSIX paths).
    /// </summary>
    public static class SteamLibraryVdf
    {
        public static IEnumerable<string> ReadLibraryPaths(string vdfPath)
        {
            string[] lines;
            try
            {
                if (!File.Exists(vdfPath))
                    return Array.Empty<string>();

                lines = File.ReadAllLines(vdfPath);
            }
            catch
            {
                return Array.Empty<string>();
            }

            var paths = new List<string>();
            foreach (var line in lines)
            {
                if (!line.Contains("path", StringComparison.Ordinal))
                    continue;

                foreach (var part in line.Split('"'))
                {
                    if (part.Length > 0)
                        paths.Add(part);
                }
            }

            return paths;
        }
    }
}
