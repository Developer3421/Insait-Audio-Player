using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Insait_Audio_Player.Services;

[SupportedOSPlatform("windows")]
public static class AudioDeviceService
{
    // WAVE_MAPPER — "best fit" waveOut device selector
    public static readonly IntPtr WaveMapper = new(-1);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WAVEOUTCAPS
    {
        public ushort wMid;
        public ushort wPid;
        public uint   vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public uint   dwFormats;
        public ushort wChannels;
        public ushort wReserved1;
        public uint   dwSupport;
    }

    [DllImport("winmm.dll")]
    private static extern int waveOutGetNumDevs();

    [DllImport("winmm.dll", CharSet = CharSet.Unicode, EntryPoint = "waveOutGetDevCapsW")]
    private static extern int waveOutGetDevCapsW(IntPtr uDeviceID, ref WAVEOUTCAPS pwoc, int cbwoc);

    [DllImport("winmm.dll")]
    private static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

    /// <summary>Returns the number of waveOut audio output devices in the system.</summary>
    public static int GetOutputDeviceCount()
    {
        try
        {
            var n = waveOutGetNumDevs();
            return n < 0 ? 0 : n;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>Returns the friendly name of every detected waveOut device.</summary>
    public static IReadOnlyList<string> GetOutputDeviceNames()
    {
        var names = new List<string>();
        try
        {
            var count = waveOutGetNumDevs();
            for (var i = 0; i < count; i++)
            {
                var caps = default(WAVEOUTCAPS);
                if (waveOutGetDevCapsW((IntPtr)i, ref caps, Marshal.SizeOf<WAVEOUTCAPS>()) == 0 &&
                    !string.IsNullOrWhiteSpace(caps.szPname))
                {
                    names.Add(caps.szPname);
                }
            }
        }
        catch
        {
            // ignored — return whatever we collected
        }

        return names;
    }

    /// <summary>
    /// Sets the master volume on the default waveOut device (WAVE_MAPPER).
    /// This is a reliable fallback when MCI setaudio volume fails on some devices.
    /// volumePercent: 0–100.
    /// </summary>
    public static void TrySetWaveMapperVolume(double volumePercent)
    {
        try
        {
            var level  = (ushort)Math.Round(Math.Clamp(volumePercent, 0, 100) / 100.0 * 0xFFFF);
            var dword  = (uint)((level << 16) | level); // left | right channel
            waveOutSetVolume(WaveMapper, dword);
        }
        catch
        {
            // ignored
        }
    }
}


