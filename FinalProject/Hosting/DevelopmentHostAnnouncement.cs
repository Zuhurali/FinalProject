namespace FinalProject.Hosting;

/// <summary>
/// Keeps a named mutex alive for the development API process so MVC can see that
/// this host is already starting and does not launch a second copy.
/// </summary>
internal static class DevelopmentHostAnnouncement
{
    internal const string MutexName = @"Local\FinalProject.DevelopmentApi";

    private static Mutex? _mutex;

    internal static void Announce()
    {
        if (_mutex != null
            || !string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _mutex = new Mutex(initiallyOwned: false, MutexName);
        GC.KeepAlive(_mutex);
    }
}
