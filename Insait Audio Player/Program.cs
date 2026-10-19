using Avalonia;
using System;
using System.IO;
using System.Linq;
using Insait_Audio_Player.Services;

namespace Insait_Audio_Player;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Collect valid MP3 paths from command-line arguments.
        var mp3Files = args
            .Where(static a => !string.IsNullOrWhiteSpace(a) &&
                               File.Exists(a) &&
                               string.Equals(Path.GetExtension(a), ".mp3", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        // Single-instance guard: if another copy is already running, forward the
        // file paths to it and exit — no second audio device conflict.
        if (!SingleInstanceService.TryBecomeMainInstance())
        {
            if (mp3Files.Length > 0)
            {
                SingleInstanceService.SendFilesToMainInstance(mp3Files);
            }
            return;
        }

        // Primary instance — start the IPC pipe listener before showing the window.
        SingleInstanceService.StartPipeListener();
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        finally
        {
            SingleInstanceService.StopAndRelease();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}