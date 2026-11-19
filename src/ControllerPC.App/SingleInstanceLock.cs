namespace ControllerPC.App;

/// <summary>
/// A named mutex held by the one running instance of the app (per user session). It can be let go and taken
/// back, so a restart as administrator can hand it to the new instance and reclaim it if the user cancels.
/// </summary>
/// <remarks>Mutex ownership belongs to a thread: acquire and release on the UI thread only.</remarks>
internal sealed class SingleInstanceLock : IDisposable
{
    private readonly Mutex _mutex;
    private bool _owned;

    private SingleInstanceLock(Mutex mutex)
    {
        _mutex = mutex;
    }

    /// <summary>Takes the lock named <paramref name="name"/>; <c>null</c> when another instance holds it.</summary>
    public static SingleInstanceLock? TryAcquire(string name)
    {
        Mutex mutex;
        try
        {
            mutex = new Mutex(initiallyOwned: false, name);
        }
        catch (UnauthorizedAccessException)
        {
            // An elevated instance created the mutex and a normal process may not open it: that instance is running.
            return null;
        }

        var instanceLock = new SingleInstanceLock(mutex);
        if (instanceLock.TryReacquire())
        {
            return instanceLock;
        }

        instanceLock.Dispose();
        return null;
    }

    /// <summary>Takes the lock back after <see cref="Release"/>; false when another instance took it meanwhile.</summary>
    public bool TryReacquire()
    {
        if (_owned)
        {
            return true;
        }

        try
        {
            _owned = _mutex.WaitOne(TimeSpan.Zero);
        }
        catch (AbandonedMutexException)
        {
            // The previous holder ended without releasing it; the wait still made this instance the owner.
            _owned = true;
        }

        return _owned;
    }

    /// <summary>Lets another instance take the lock, while keeping the mutex open to take it back.</summary>
    public void Release()
    {
        if (_owned)
        {
            _mutex.ReleaseMutex();
            _owned = false;
        }
    }

    public void Dispose()
    {
        Release();
        _mutex.Dispose();
    }
}
