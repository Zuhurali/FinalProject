using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace FinalProject.MVC.Hosting;

/// <summary>
/// Development-only helper that starts the existing FinalProject API when MVC is launched by itself.
/// Builds into the normal bin/Debug output, copies to artifacts/run/api (shadow), then runs the DLL
/// from the shadow folder so subsequent builds are not blocked by MSB3027/MSB3021 file locks.
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
        string? apiSigningKey = null,
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

        var repoRoot = FindRepoRoot(contentRootPath);
        var apiProjectPath = Path.Combine(repoRoot, "FinalProject", "FinalProject.csproj");
        if (!File.Exists(apiProjectPath))
        {
            throw new InvalidOperationException(
                $"Development API project was not found at '{apiProjectPath}'. Start FinalProject, or run MVC from the repository layout.");
        }

        var apiProjectDirectory = Path.GetDirectoryName(apiProjectPath)!;
        var applicationUrls = ReadHttpsApplicationUrls(apiProjectDirectory, apiBaseUrl);
        var shadowDirectory = Path.Combine(repoRoot, "artifacts", "run", "api");
        var buildOutputDirectory = Path.Combine(apiProjectDirectory, "bin", "Debug", "net8.0");

        logger.LogInformation(
            "Preparing FinalProject API shadow-run at {ShadowDirectory} (build output stays at {BuildOutput}).",
            shadowDirectory,
            buildOutputDirectory);

        BuildApiProject(apiProjectPath, apiProjectDirectory, logger);
        SyncDirectory(buildOutputDirectory, shadowDirectory, logger);

        logger.LogInformation("Starting FinalProject API from shadow {ShadowDirectory} at {ApplicationUrls}. Swagger will not be opened.", shadowDirectory, applicationUrls);

        var process = StartApiProcess(shadowDirectory, applicationUrls, apiSigningKey);
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

        logger.LogInformation("FinalProject API is ready at {ApiBaseUrl} (shadow-run).", apiBaseUrl);
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
            foreach (var process in Process.GetProcessesByName("FinalProject"))
            {
                try
                {
                    if (process.HasExited)
                    {
                        continue;
                    }

                    // Leftover processes that cannot be inspected still match the
                    // process name. They are not a starting API and must not make
                    // MVC wait until the readiness timeout.
                    _ = process.StartTime;
                    return true;
                }
                catch (Exception exception) when (exception is InvalidOperationException or Win32Exception or UnauthorizedAccessException)
                {
                }
                finally
                {
                    process.Dispose();
                }
            }

            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static string FindRepoRoot(string startPath)
    {
        var fromEnv = Environment.GetEnvironmentVariable("FINALPROJECT_REPO_ROOT");
        if (!string.IsNullOrWhiteSpace(fromEnv) && Directory.Exists(fromEnv))
        {
            var envApi = Path.Combine(fromEnv, "FinalProject", "FinalProject.csproj");
            if (File.Exists(envApi))
            {
                return Path.GetFullPath(fromEnv);
            }
        }

        var dir = new DirectoryInfo(Path.GetFullPath(startPath));
        while (dir != null)
        {
            var apiProject = Path.Combine(dir.FullName, "FinalProject", "FinalProject.csproj");
            var solution = Path.Combine(dir.FullName, "FinalProject.slnx");
            if (File.Exists(apiProject) || File.Exists(solution))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate the repository root from '{startPath}'. Set FINALPROJECT_REPO_ROOT or run from the repo layout.");
    }

    private static void BuildApiProject(string apiProjectPath, string apiProjectDirectory, ILogger logger)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveDotnetHost(),
            Arguments = $"build \"{apiProjectPath}\" -c Debug --verbosity minimal",
            WorkingDirectory = apiProjectDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, eventArgs) => AppendError(eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => AppendError(eventArgs.Data);
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start 'dotnet build' for the development API.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                "Development API build failed before shadow-run copy."
                + Environment.NewLine
                + ReadError());
        }

        logger.LogInformation("Development API build succeeded; syncing to shadow-run folder.");
    }

    private static void SyncDirectory(string sourceDirectory, string destinationDirectory, ILogger logger)
    {
        if (!Directory.Exists(sourceDirectory))
        {
            throw new InvalidOperationException($"API build output was not found at '{sourceDirectory}'.");
        }

        Directory.CreateDirectory(destinationDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = "robocopy",
            Arguments = $"\"{sourceDirectory}\" \"{destinationDirectory}\" /MIR /R:2 /W:1 /NFL /NDL /NJH /NJS /NP /XD refs",
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        process.WaitForExit();
        // robocopy: 0-7 = success / no fatal error
        if (process.ExitCode >= 8)
        {
            throw new InvalidOperationException(
                $"Failed to sync API build output to shadow '{destinationDirectory}' (robocopy exit {process.ExitCode}). "
                + "Stop any process still locking artifacts/run/api (scripts/dev-stop.ps1), then retry.");
        }

        var dllPath = Path.Combine(destinationDirectory, "FinalProject.dll");
        if (!File.Exists(dllPath))
        {
            throw new InvalidOperationException($"Shadow API DLL was not found at '{dllPath}' after sync.");
        }

        logger.LogInformation("API shadow sync complete -> {ShadowDirectory}", destinationDirectory);
    }

    private static Process StartApiProcess(
        string shadowDirectory,
        string applicationUrls,
        string? apiSigningKey)
    {
        var dllPath = Path.Combine(shadowDirectory, "FinalProject.dll");
        var startInfo = new ProcessStartInfo
        {
            FileName = ResolveDotnetHost(),
            Arguments = $"\"{dllPath}\"",
            WorkingDirectory = shadowDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        startInfo.Environment["ASPNETCORE_URLS"] = applicationUrls;
        startInfo.Environment["ASPNETCORE_HTTPS_PORT"] = new Uri(applicationUrls.Split(';', 2)[0]).Port.ToString();
        startInfo.Environment["FINALPROJECT_SHADOW_RUN"] = "1";
        startInfo.Environment.Remove("DOTNET_LAUNCH_PROFILE");
        // Pass the signing key via env so the child API can validate JWTs without reading
        // committed appsettings. Never log this value.
        if (!string.IsNullOrWhiteSpace(apiSigningKey))
        {
            startInfo.Environment["ApiAuthentication__SigningKey"] = apiSigningKey;
        }

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, eventArgs) => AppendError(eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => AppendError(eventArgs.Data);
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start the development FinalProject API process from the shadow folder.");
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
            using var response = await ProbeClient.GetAsync(new Uri(new Uri(apiBaseUrl), "health/ready"), cancellationToken);
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
