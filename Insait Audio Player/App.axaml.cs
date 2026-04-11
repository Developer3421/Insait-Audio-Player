using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;
using System.IO;
using System.Linq;
using Insait_Audio_Player.Services;

namespace Insait_Audio_Player;

public partial class App : Application
{
    private const string AgreementVersion = "1.0";

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var launchFiles = desktop.Args?
                .Where(static arg => !string.IsNullOrWhiteSpace(arg) &&
                                     string.Equals(Path.GetExtension(arg), ".mp3", StringComparison.OrdinalIgnoreCase) &&
                                     File.Exists(arg))
                .Select(Path.GetFullPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [];

            var agreementRepository = new UserAgreementRepository();
            var agreementState = agreementRepository.Load(AgreementVersion);

            if (agreementState.Accepted)
            {
                desktop.MainWindow = new MainWindow(launchFiles);
            }
            else
            {
                var agreementWindow = new AgreementWindow();
                agreementWindow.Accepted += (_, _) =>
                {
                    agreementRepository.SaveAccepted(AgreementVersion);
                    var mainWindow = new MainWindow(launchFiles);
                    desktop.MainWindow = mainWindow;
                    mainWindow.Show();
                    agreementWindow.Close();
                };

                agreementWindow.Declined += (_, _) =>
                {
                    desktop.Shutdown();
                };

                desktop.MainWindow = agreementWindow;
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}