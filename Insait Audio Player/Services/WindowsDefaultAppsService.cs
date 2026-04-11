using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Insait_Audio_Player.Services;

 [SupportedOSPlatform("windows")]
public sealed class WindowsDefaultAppsService
{
    private const string ApplicationName = "Insait Audio Player";
    private const string ApplicationDescription = "Insait Audio Player for MP3 files";
    private const string RegisteredApplicationsKeyPath = @"Software\RegisteredApplications";
    private const string CapabilitiesKeyPath = @"Software\Insait Audio Player\Capabilities";
    private const string ProgId = "InsaitAudioPlayer.mp3";
    private static readonly Guid ApplicationAssociationRegistrationUiClsid = new("1968106D-F3B5-44CF-890E-116FCB9ECEF1");

    public DefaultMp3AssociationResult RegisterAndPromptForMp3()
    {
        if (!OperatingSystem.IsWindows())
        {
            return new DefaultMp3AssociationResult(DefaultMp3AssociationOutcome.NotSupported);
        }

        if (!TryResolveExecutablePath(out var executablePath, out var launchCommand, out var errorMessage))
        {
            return new DefaultMp3AssociationResult(DefaultMp3AssociationOutcome.Failed, errorMessage);
        }

        try
        {
            RegisterCurrentUserAssociation(executablePath, launchCommand);
            NotifyShellAboutAssociationChange();
        }
        catch (Exception exception)
        {
            return new DefaultMp3AssociationResult(DefaultMp3AssociationOutcome.Failed, exception.Message);
        }

        if (TryLaunchAdvancedAssociationUi())
        {
            return new DefaultMp3AssociationResult(DefaultMp3AssociationOutcome.OpenedAdvancedUi);
        }

        if (TryOpenSystemDefaultsSettings())
        {
            return new DefaultMp3AssociationResult(DefaultMp3AssociationOutcome.OpenedSettings);
        }

        return new DefaultMp3AssociationResult(DefaultMp3AssociationOutcome.RegisteredOnly);
    }

    private static void RegisterCurrentUserAssociation(string executablePath, string launchCommand)
    {
        var executableName = Path.GetFileName(executablePath);
        if (string.IsNullOrWhiteSpace(executableName))
        {
            throw new InvalidOperationException("Не удалось определить имя исполняемого файла приложения.");
        }

        using (var registeredApplications = Registry.CurrentUser.CreateSubKey(RegisteredApplicationsKeyPath))
        {
            registeredApplications.SetValue(ApplicationName, CapabilitiesKeyPath, RegistryValueKind.String);
        }

        using (var capabilities = Registry.CurrentUser.CreateSubKey(CapabilitiesKeyPath))
        {
            capabilities.SetValue("ApplicationName", ApplicationName, RegistryValueKind.String);
            capabilities.SetValue("ApplicationDescription", ApplicationDescription, RegistryValueKind.String);

            using var fileAssociations = capabilities.CreateSubKey("FileAssociations");
            fileAssociations.SetValue(".mp3", ProgId, RegistryValueKind.String);
        }

        using (var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}"))
        {
            progIdKey.SetValue(string.Empty, "Insait Audio Player MP3", RegistryValueKind.String);
            progIdKey.SetValue("FriendlyTypeName", "MP3 Audio", RegistryValueKind.String);
            progIdKey.SetValue("AppUserModelID", "InsaitAudioPlayer", RegistryValueKind.String);

            using var defaultIconKey = progIdKey.CreateSubKey("DefaultIcon");
            defaultIconKey.SetValue(string.Empty, $"\"{executablePath}\",0", RegistryValueKind.String);

            using var openCommandKey = progIdKey.CreateSubKey(@"shell\open\command");
            openCommandKey.SetValue(string.Empty, launchCommand, RegistryValueKind.String);
        }

        using (var openWithProgIds = Registry.CurrentUser.CreateSubKey(@"Software\Classes\.mp3\OpenWithProgids"))
        {
            openWithProgIds.SetValue(ProgId, string.Empty, RegistryValueKind.String);
        }

        using (var applicationsKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\Applications\{executableName}"))
        {
            applicationsKey.SetValue("FriendlyAppName", ApplicationName, RegistryValueKind.String);

            using var supportedTypesKey = applicationsKey.CreateSubKey("SupportedTypes");
            supportedTypesKey.SetValue(".mp3", string.Empty, RegistryValueKind.String);

            using var shellCommandKey = applicationsKey.CreateSubKey(@"shell\open\command");
            shellCommandKey.SetValue(string.Empty, launchCommand, RegistryValueKind.String);
        }
    }

    private static bool TryResolveExecutablePath(out string executablePath, out string launchCommand, out string? errorMessage)
    {
        executablePath = string.Empty;
        launchCommand = string.Empty;
        errorMessage = null;

        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath) &&
            File.Exists(processPath) &&
            !string.Equals(Path.GetFileName(processPath), "dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            executablePath = processPath;
            launchCommand = BuildLaunchCommand(executablePath);
            return true;
        }

        var entryAssemblyPath = Assembly.GetEntryAssembly()?.Location;
        if (!string.IsNullOrWhiteSpace(entryAssemblyPath))
        {
            var appHostPath = Path.ChangeExtension(entryAssemblyPath, ".exe");
            if (File.Exists(appHostPath))
            {
                executablePath = appHostPath;
                launchCommand = BuildLaunchCommand(executablePath);
                return true;
            }
        }

        errorMessage = "Не удалось определить путь к EXE приложения. Запустите опубликованную или собранную EXE-версию и попробуйте ещё раз.";
        return false;
    }

    private static string BuildLaunchCommand(string executablePath)
    {
        return $"\"{executablePath}\" \"%1\"";
    }

    private static bool TryLaunchAdvancedAssociationUi()
    {
        object? instance = null;
        try
        {
            var registrationUiType = Type.GetTypeFromCLSID(ApplicationAssociationRegistrationUiClsid, throwOnError: false);
            if (registrationUiType is null)
            {
                return false;
            }

            instance = Activator.CreateInstance(registrationUiType);
            if (instance is not IApplicationAssociationRegistrationUI registrationUi)
            {
                return false;
            }

            registrationUi.LaunchAdvancedAssociationUI(ApplicationName);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (instance is not null && Marshal.IsComObject(instance))
            {
                Marshal.FinalReleaseComObject(instance);
            }
        }
    }

    private static bool TryOpenSystemDefaultsSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:defaultapps",
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void NotifyShellAboutAssociationChange()
    {
        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint eventId, uint flags, IntPtr item1, IntPtr item2);

    [ComImport]
    [Guid("1F76A169-F994-40AC-8FC8-0959E8874710")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IApplicationAssociationRegistrationUI
    {
        void LaunchAdvancedAssociationUI([MarshalAs(UnmanagedType.LPWStr)] string appRegistryName);
    }
}

public enum DefaultMp3AssociationOutcome
{
    OpenedAdvancedUi,
    OpenedSettings,
    RegisteredOnly,
    NotSupported,
    Failed
}

public sealed class DefaultMp3AssociationResult
{
    public DefaultMp3AssociationResult(DefaultMp3AssociationOutcome outcome, string? errorMessage = null)
    {
        Outcome = outcome;
        ErrorMessage = errorMessage;
    }

    public DefaultMp3AssociationOutcome Outcome { get; }

    public string? ErrorMessage { get; }
}

