using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace FinalProject.MVC.Hosting;

/// <summary>
/// Development-only helper that starts the existing FinalProject API when MVC is launched by itself.
/// Production startup never calls this type.
/// </summary>
internal static class DevelopmentApiHost
{
    private const string ApiMutexName = @"Local\FinalProject.DevelopmentApi";
    private const string LauncherMutexName = @"Local\FinalProject.MvcDevelopmentApiLauncher";

    private static readonly HttpClient ProbeClient = new(new SocketsHttpHandler
    {
        ConnectTimeout = TimeSpan.FromSeconds(2)
    })
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private static readonly StringBuilder ProcessError = new();
    private static readonly object ErrorGate = new();
    private static Process? _ownedProcess;
    private static Mutex? _launcherMutex;
    private static int _stopRequested;

    public static async Task EnsureStartedAsync(
        string contentRootPath,
        string apiBaseUrl,
        IHostApplicationLifetime lifetime,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await IsReadyAsync(apiBaseUrl, cancellationToken))
        {
            logger.LogInformation("Development API is already available at {ApiBaseUrl}.", apiBaseUrl);
            return;
        }

        if (!TryBecomeLauncher())
        {
            logger.LogInformation("Another MVC process is starting the development API. Waiting for {ApiBaseUrl}.", apiBaseUrl);
            await WaitUntilReadyAsync(apiBaseUrl, TimeSpan.FromSeconds(120), ownedProcess: null, logger, cancellationToken);
            return;
        }

        if (await WaitForExternalApiAsync(apiBaseUrl, TimeSpan.FromSeconds(4), cancellationToken))
        {
            logger.LogInformation("Development API was started separately. Waiting for {ApiBaseUrl}.", apiBaseUrl);
            await WaitUntilReadyAsync(apiBaseUrl, TimeSpan.FromSeconds(90), ownedProcess: null, logger, cancellationToken);
            return;
        }

        var apiProjectPath = Path.GetFullPath(Path.Combine(contentRootPath, "..", "FinalProject", "FinalProject.csproj"));
        if (!File.Exists(apiProjectPath))
        {
            throw new InvalidOperationException(
                $"Development API project was not found at '{apiProjectPath}'. Start FinalProject, or run MVC from the repository layout.");
        }

        var apiProjectDirectory = Path.GetDirectoryName(apiProjectPath)!;
        var applicationUrls = ReadHttpsApplicationUrls(apiProjectDirectory, apiBaseUrl);
        logger.LogInformation("Starting FinalProject API in the background at {ApplicationUrls}. Swagger will not be opened.", applicationUrls);

        var process = StartApiProcess(apiProjectPath, apiProjectDirectory, applicationUrls);
        _ownedProcess = process;
        WindowsProcessJob.TryAssign(process, logger);
        lifetime.ApplicationStopping.Register(StopOwnedProcess);
        AppDomain.CurrentDomain.ProcessExit += (_, _) => StopOwnedProcess();

        try
        {
            await WaitUntilReadyAsync(apiBaseUrl, TimeSpan.FromSeconds(120), process, logger, cancellationToken);
        }
        catch
        {
            StopOwnedProcess();
            throw;
        }

        logger.LogInformation("FinalProject API is ready at {ApiBaseUrl}.", apiBaseUrl);
    }

    public static void StopOwnedProcess()
    {
        if (Interlocked.Exchange(ref _stopRequested, 1) == 1)
        {
            return;
        }

        var process = _ownedProcess;
        _ownedProcess = null;
        _launcherMutex?.Dispose();
        _launcherMutex = null;
        if (process == null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
        catch (Win32Exception)
        {
        }
    }

    private static bool TryBecomeLauncher()
    {
        var mutex = new Mutex(initiallyOwned: false, LauncherMutexName, out var createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            return false;
        }

        _launcherMutex = mutex;
        return true;
    }

    private static async Task<bool> WaitForExternalApiAsync(string apiBaseUrl, TimeSpan gracePeriod, CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.Add(gracePeriod);
        while (DateTime.UtcNow < deadline)
        {
            if (await IsReadyAsync(apiBaseUrl, cancellationToken) || IsExternalApiAnnounced() || IsApiExecutableRunning())
            {
                return true;
            }

            await Task.Delay(250, cancellationToken);
        }

        return IsExternalApiAnnounced() || IsApiExecutableRunning();
    }

    private static bool IsExternalApiAnnounced()
    {
        try
        {
            using var mutex = Mutex.OpenExisting(ApiMutexName);
            return true;
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private static bool IsApiExecutableRunning()
    {
        try
        {
            return Process.GetProcessesByName("FinalProject").Length > 0;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static Process StartApiProcess(string apiProjectPath, string apiProjectDirectory, string applicationUrls)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveDotnetHost(),
            Arguments = $"run --no-launch-profile --project \"{apiProjectPath}\"",
            WorkingDirectory = apiProjectDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = applicationUrls;
        startInfo.Environment["ASPNETCORE_HTTPS_PORT"] = new Uri(applicationUrls.Split(';', 2)[0]).Port.ToString();
        startInfo.Environment.Remove("DOTNET_LAUNCH_PROFILE");

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, eventArgs) => AppendError(eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => AppendError(eventArgs.Data);
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start the development FinalProject API process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return process;
    }

    private static string ResolveDotnetHost()
    {
        var hostPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        if (!string.IsNullOrWhiteSpace(hostPath) && File.Exists(hostPath))
        {
            return hostPath;
        }

        var candidate = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe");
        return File.Exists(candidate) ? candidate : "dotnet";
    }

    private static string ReadHttpsApplicationUrls(string apiProjectDirectory, string apiBaseUrl)
    {
        var launchSettingsPath = Path.Combine(apiProjectDirectory, "Properties", "launchSettings.json");
        if (!File.Exists(launchSettingsPath))
        {
            throw new InvalidOperationException($"API launch settings were not found at '{launchSettingsPath}'.");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(launchSettingsPath));
        if (!document.RootElement.TryGetProperty("profiles", out var profiles)
            || !profiles.TryGetProperty("https", out var httpsProfile)
            || !httpsProfile.TryGetProperty("applicationUrl", out var applicationUrlElement)
            || string.IsNullOrWhiteSpace(applicationUrlElement.GetString()))
        {
            throw new InvalidOperationException("The FinalProject https launch profile does not define applicationUrl.");
        }

        var applicationUrls = applicationUrlElement.GetString()!;
        var authority = new Uri(apiBaseUrl).GetLeftPart(UriPartial.Authority);
        if (applicationUrls.IndexOf(authority, StringComparison.OrdinalIgnoreCase) < 0)
        {
            throw new InvalidOperationException(
                $"The API https profile listens on '{applicationUrls}', which does not include ApiSettings:BaseUrl '{apiBaseUrl}'.");
        }

        if (httpsProfile.TryGetProperty("launchBrowser", out var launchBrowser) && launchBrowser.ValueKind == JsonValueKind.True)
        {
            throw new InvalidOperationException(
                "The FinalProject https profile is set to launch a browser. Background API startup requires launchBrowser to stay false so Swagger does not open.");
        }

        return applicationUrls;
    }

    private static async Task WaitUntilReadyAsync(
        string apiBaseUrl,
        TimeSpan timeout,
        Process? ownedProcess,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ownedProcess is { HasExited: true })
            {
                if (await IsReadyAsync(apiBaseUrl, cancellationToken))
                {
                    _ownedProcess = null;
                    logger.LogInformation("The launched API process exited because another API instance is already serving {ApiBaseUrl}.", apiBaseUrl);
                    return;
                }

                throw new InvalidOperationException(
                    "The development FinalProject API exited before it accepted requests."
                    + Environment.NewLine
                    + ReadError());
            }

            if (await IsReadyAsync(apiBaseUrl, cancellationToken))
            {
                return;
            }

            await Task.Delay(500, cancellationToken);
        }

        throw new TimeoutException(
            $"FinalProject API did not become ready at {apiBaseUrl} within {timeout.TotalSeconds:0} seconds."
            + Environment.NewLine
            + ReadError());
    }

    private static async Task<bool> IsReadyAsync(string apiBaseUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await ProbeClient.GetAsync(new Uri(new Uri(apiBaseUrl), "api/cars/lookups"), cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            return false;
        }
    }

    private static void AppendError(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        lock (ErrorGate)
        {
            if (ProcessError.Length > 8000)
            {
                ProcessError.Remove(0, 2000);
            }

            ProcessError.AppendLine(line);
        }
    }

    private static string ReadError()
    {
        lock (ErrorGate)
        {
            return ProcessError.ToString();
        }
    }

    private static class WindowsProcessJob
    {
        private const int JobObjectExtendedLimitInformationClass = 9;
        private const uint KillOnJobClose = 0x2000;

        private static IntPtr _jobHandle;

        public static void TryAssign(Process process, ILogger logger)
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            try
            {
                if (_jobHandle == IntPtr.Zero)
                {
                    _jobHandle = CreateJobObject(IntPtr.Zero, null);
                    var info = new JobObjectExtendedLimitInformation
                    {
                        BasicLimitInformation = new JobObjectBasicLimitInformation
                        {
                            LimitFlags = KillOnJobClose
                        }
                    };
                    var length = Marshal.SizeOf<JobObjectExtendedLimitInformation>();
                    var pointer = Marshal.AllocHGlobal(length);
                    try
                    {
                        Marshal.StructureToPtr(info, pointer, false);
                        if (!SetInformationJobObject(_jobHandle, JobObjectExtendedLimitInformationClass, pointer, (uint)length))
                        {
                            throw new Win32Exception(Marshal.GetLastWin32Error());
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(pointer);
                    }
                }

                if (!AssignProcessToJobObject(_jobHandle, process.Handle))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
            {
                logger.LogWarning(exception, "The development API process could not be tied to the MVC process lifetime. It will still be stopped when MVC shuts down.");
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectBasicLimitInformation
        {
            public long PerProcessUserTimeLimit;
            public long PerJobUserTimeLimit;
            public uint LimitFlags;
            public UIntPtr MinimumWorkingSetSize;
            public UIntPtr MaximumWorkingSetSize;
            public uint ActiveProcessLimit;
            public long Affinity;
            public uint PriorityClass;
            public uint SchedulingClass;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IoCounters
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct JobObjectExtendedLimitInformation
        {
            public JobObjectBasicLimitInformation BasicLimitInformation;
            public IoCounters IoInfo;
            public UIntPtr ProcessMemoryLimit;
            public UIntPtr JobMemoryLimit;
            public UIntPtr PeakProcessMemoryUsed;
            public UIntPtr PeakJobMemoryUsed;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetInformationJobObject(IntPtr hJob, int jobObjectInfoClass, IntPtr lpJobObjectInfo, uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
    }
}
