using System.Diagnostics;
using ControllerPC.Core.Controller;

namespace ControllerPC.Windows.XInput;

/// <summary>
/// Plays rumble patterns through XInputSetState without blocking the caller.
/// Disposing stops all motors, so vibration never outlives the app.
/// </summary>
/// <remarks>
/// Each slot has at most one current playback, identified by its <see cref="CancellationTokenSource"/>. Only the
/// current playback may write to the slot's motors, and every write happens under a lock, so a replaced playback
/// can never switch motors off (or on) underneath its successor. Whoever removes a playback from
/// <see cref="_currentPlayback"/> owns its token source and disposes it.
/// </remarks>
public sealed class XInputRumble : IRumble, IDisposable
{
    private const ushort PulseMotorSpeed = 0xC000;
    private const ushort MotorsOff = 0;
    private static readonly TimeSpan ShortPulseDuration = TimeSpan.FromMilliseconds(120);
    private static readonly TimeSpan LongPulseDuration = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan GapBetweenPulses = TimeSpan.FromMilliseconds(100);

    private readonly object _gate = new();
    private readonly CancellationTokenSource?[] _currentPlayback =
        new CancellationTokenSource?[XInputNative.MaxControllerCount];

    private bool _disposed;

    /// <exception cref="ArgumentOutOfRangeException"><paramref name="slot"/> is not a valid XInput slot.</exception>
    /// <exception cref="ObjectDisposedException">The instance has been disposed.</exception>
    public void Play(int slot, RumblePattern pattern)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(slot);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(slot, XInputNative.MaxControllerCount);
        (int pulseCount, TimeSpan pulseDuration) = DescribePulses(pattern);

        var playback = new CancellationTokenSource();

        // Read before the playback is published: from then on another thread may replace and dispose it.
        CancellationToken cancellation = playback.Token;
        CancellationTokenSource? replaced;
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            replaced = _currentPlayback[slot];
            _currentPlayback[slot] = playback;
        }

        // Cancelled outside the lock: cancellation may run the replaced playback's continuation inline.
        Stop(replaced);
        _ = PlayPulsesAsync(slot, pulseCount, pulseDuration, playback, cancellation);
    }

    public void Dispose()
    {
        CancellationTokenSource?[] stopped;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            stopped = [.. _currentPlayback];
            Array.Clear(_currentPlayback);
        }

        foreach (CancellationTokenSource? playback in stopped)
        {
            Stop(playback);
        }

        for (int slot = 0; slot < XInputNative.MaxControllerCount; slot++)
        {
            SetMotors(slot, MotorsOff);
        }
    }

    internal static (int PulseCount, TimeSpan PulseDuration) DescribePulses(RumblePattern pattern) =>
        pattern switch
        {
            RumblePattern.EnteredDesktopMode => (1, ShortPulseDuration),
            RumblePattern.ExitedDesktopMode => (2, ShortPulseDuration),
            RumblePattern.ProfileSwitched => (1, LongPulseDuration),
            _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, "Unknown rumble pattern."),
        };

    private static void Stop(CancellationTokenSource? playback)
    {
        playback?.Cancel();
        playback?.Dispose();
    }

    private static void SetMotors(int slot, ushort speed)
    {
        var vibration = new XInputVibration { LeftMotorSpeed = speed, RightMotorSpeed = speed };
        uint result = XInputNative.XInputSetState((uint)slot, vibration);
        if (result is not XInputNative.ErrorSuccess and not XInputNative.ErrorDeviceNotConnected)
        {
            Trace.TraceWarning($"XInputSetState({slot}) failed with error {result}.");
        }
    }

    private async Task PlayPulsesAsync(
        int slot,
        int pulseCount,
        TimeSpan pulseDuration,
        CancellationTokenSource playback,
        CancellationToken cancellation)
    {
        try
        {
            for (int pulse = 0; pulse < pulseCount; pulse++)
            {
                if (pulse > 0)
                {
                    await Task.Delay(GapBetweenPulses, cancellation).ConfigureAwait(false);
                }

                SetMotorsIfCurrent(slot, PulseMotorSpeed, playback);
                await Task.Delay(pulseDuration, cancellation).ConfigureAwait(false);
                SetMotorsIfCurrent(slot, MotorsOff, playback);
            }
        }
        catch (OperationCanceledException)
        {
            // Replaced by a newer pattern (which now owns the motors) or stopped by Dispose (which zeroes them).
        }
        finally
        {
            bool finishedAsCurrent;
            lock (_gate)
            {
                finishedAsCurrent = _currentPlayback[slot] == playback;
                if (finishedAsCurrent)
                {
                    _currentPlayback[slot] = null;
                }
            }

            if (finishedAsCurrent)
            {
                playback.Dispose();
            }
        }
    }

    private void SetMotorsIfCurrent(int slot, ushort speed, CancellationTokenSource playback)
    {
        lock (_gate)
        {
            if (_currentPlayback[slot] == playback)
            {
                SetMotors(slot, speed);
            }
        }
    }
}
