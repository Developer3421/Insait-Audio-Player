using System;
using System.IO;

namespace Insait_Audio_Player.Services;

public static class AppDataPaths
{
    public static string DataDirectory { get; } = EnsureDirectory();

    private static string EnsureDirectory()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Insait Audio Player");

        Directory.CreateDirectory(path);
        return path;
    }
}

