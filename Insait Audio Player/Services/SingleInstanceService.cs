using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace Insait_Audio_Player.Services;

/// <summary>
/// Ensures only one instance of the application runs at a time.
/// When a second instance is launched it forwards its file arguments
/// to the primary instance via a named pipe and then exits.
/// </summary>
public static class SingleInstanceService
{
    private const string MutexName = "InsaitAudioPlayerSingleInstance";
    private const string PipeName  = "InsaitAudioPlayerIPC";

    /// <summary>
    /// Raised on the UI thread in the primary instance when a secondary instance
    /// passes one or more MP3 file paths to it.
    /// </summary>
    public static event Action<IReadOnlyList<string>>? FilesReceived;

    private static Mutex? _mutex;
    private static CancellationTokenSource? _cts;

    /// <summary>
    /// Tries to acquire the single-instance mutex.
    /// Returns <c>true</c> if this process is the primary instance,
    /// <c>false</c> if another instance is already running.
    /// </summary>
    public static bool TryBecomeMainInstance()
    {
        _mutex = new Mutex(initiallyOwned: false, MutexName, out _);
        return _mutex.WaitOne(millisecondsTimeout: 0);
    }

    /// <summary>
    /// Sends <paramref name="filePaths"/> to the already-running primary instance.
    /// Call this when <see cref="TryBecomeMainInstance"/> returns <c>false</c>.
    /// </summary>
    public static void SendFilesToMainInstance(IReadOnlyList<string> filePaths)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            pipe.Connect(timeout: 2000);
            using var writer = new StreamWriter(pipe);
            foreach (var path in filePaths)
            {
                writer.WriteLine(path);
            }
        }
        catch
        {
            // Ignored — the main instance may have just closed or the pipe is not ready yet.
        }
    }

    /// <summary>
    /// Starts the background named-pipe listener that receives file paths
    /// forwarded by secondary instances.
    /// </summary>
    public static void StartPipeListener()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        Task.Run(() => PipeListenerLoop(token), token);
    }

    /// <summary>Cancels the listener and releases the mutex.</summary>
    public static void StopAndRelease()
    {
        _cts?.Cancel();
        try { _mutex?.ReleaseMutex(); } catch { /* already released or abandoned */ }
        _mutex?.Dispose();
        _mutex = null;
    }

    // ── Private implementation ────────────────────────────────────────────────

    private static void PipeListenerLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.In,
                    maxNumberOfServerInstances: 1,
                    transmissionMode: PipeTransmissionMode.Byte,
                    options: PipeOptions.Asynchronous);

                // Wait until a secondary instance connects (or we are cancelled).
                pipe.WaitForConnectionAsync(cancellationToken).Wait(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var paths = new List<string>();
                using var reader = new StreamReader(pipe);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        paths.Add(trimmed);
                    }
                }

                if (paths.Count > 0)
                {
                    IReadOnlyList<string> snapshot = paths.AsReadOnly();
                    Dispatcher.UIThread.Post(() => FilesReceived?.Invoke(snapshot));
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // Pipe error — brief pause before retrying.
                if (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(250);
                }
            }
        }
    }
}

