using System;
using System.IO;
using System.Linq;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

[InitializeOnLoad]
public static class MutinyBuild
{
    private const string MenuRoot = "Mutiny X/Build/";
    private const string QueueKey = "MutinyX.Build.Queue";
    private const string QueueActiveKey = "MutinyX.Build.QueueActive";
    private const string InstallerScriptPath = "BuildTools/WindowsInstaller/MutinyX.iss";
    private const string InnoCompilerPrefKey = "MutinyX.InnoSetup.CompilerPath";
    private const string LocalizationSourcePath = "Assets/Mutiny/Localization/Mutiny.tsv";
    private const string LocalizationRuntimePath = "Assets/Mutiny/Resources/Localization/MutinyRuntime.txt";

    [Serializable]
    private class BuildQueueState
    {
        public int[] targets;
        public int index;
        public int buildNumber;
        public string version;
        public bool buildWindowsInstaller;
    }

    static MutinyBuild()
    {
        if (SessionState.GetBool(QueueActiveKey, false))
        {
            EditorApplication.delayCall += ContinueQueue;
        }
    }

    [MenuItem(MenuRoot + "Build Windows", priority = 10)]
    public static void BuildWindows()
    {
        StartQueue(BuildTarget.StandaloneWindows64);
    }

    [MenuItem(MenuRoot + "Build Windows Installer", priority = 11)]
    public static void BuildWindowsInstaller()
    {
        StartQueue(true, BuildTarget.StandaloneWindows64);
    }

    [MenuItem(MenuRoot + "Build Android", priority = 12)]
    public static void BuildAndroid()
    {
        StartQueue(BuildTarget.Android);
    }

    [MenuItem(MenuRoot + "Build iOS", priority = 13)]
    public static void BuildIOS()
    {
        StartQueue(BuildTarget.iOS);
    }

    [MenuItem(MenuRoot + "Build All Platforms", priority = 20)]
    public static void BuildAll()
    {
        StartQueue(
            true,
            BuildTarget.StandaloneWindows64,
            BuildTarget.Android,
            BuildTarget.iOS
        );
    }

    [MenuItem(MenuRoot + "Configure Inno Setup Compiler...", priority = 30)]
    public static void ConfigureInnoSetupCompiler()
    {
        string current = EditorPrefs.GetString(InnoCompilerPrefKey, string.Empty);
        string directory = string.IsNullOrEmpty(current) ? string.Empty : Path.GetDirectoryName(current);
        string selected = EditorUtility.OpenFilePanel(
            "Select Inno Setup command-line compiler",
            directory ?? string.Empty,
            "exe"
        );

        if (string.IsNullOrEmpty(selected))
            return;

        if (!string.Equals(Path.GetFileName(selected), "ISCC.exe", StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog(
                "Invalid Inno Setup compiler",
                "Please select ISCC.exe from an Inno Setup installation.",
                "OK"
            );
            return;
        }

        EditorPrefs.SetString(InnoCompilerPrefKey, selected);
        Debug.Log($"[Mutiny Build] Inno Setup compiler configured: {selected}");
    }

    [MenuItem(MenuRoot + "Open Build Folder", priority = 40)]
    public static void OpenBuildFolder()
    {
        string path = Path.GetFullPath("Builds");
        Directory.CreateDirectory(path);
        EditorUtility.RevealInFinder(path);
    }

    private static void StartQueue(params BuildTarget[] targets)
    {
        StartQueue(false, targets);
    }

    private static void StartQueue(bool buildWindowsInstaller, params BuildTarget[] targets)
    {
        if (BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling)
        {
            Debug.LogWarning("[Mutiny Build] Unity is currently compiling or building. Try again when it finishes.");
            return;
        }

        if (SessionState.GetBool(QueueActiveKey, false))
        {
            Debug.LogWarning("[Mutiny Build] A multi-platform build is already running.");
            return;
        }

        int buildNumber = GetNextBuildNumber();
        string version = string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion)
            ? "1.0.0"
            : PlayerSettings.bundleVersion.Trim();

        ApplySharedVersion(version, buildNumber);
        AssetDatabase.SaveAssets();

        var state = new BuildQueueState
        {
            targets = targets.Select(t => (int)t).ToArray(),
            index = 0,
            buildNumber = buildNumber,
            version = version,
            buildWindowsInstaller = buildWindowsInstaller
        };

        SessionState.SetString(QueueKey, JsonUtility.ToJson(state));
        SessionState.SetBool(QueueActiveKey, true);

        Debug.Log($"[Mutiny Build] Starting build {version}+{buildNumber}: {string.Join(", ", targets.Select(t => t.ToString()))}");
        ContinueQueue();
    }

    private static int GetNextBuildNumber()
    {
        int android = Mathf.Max(0, PlayerSettings.Android.bundleVersionCode);
        int ios = 0;
        int.TryParse(PlayerSettings.iOS.buildNumber, out ios);
        return Mathf.Max(android, ios) + 1;
    }

    private static void ApplySharedVersion(string version, int buildNumber)
    {
        PlayerSettings.bundleVersion = version;
        PlayerSettings.Android.bundleVersionCode = buildNumber;
        PlayerSettings.iOS.buildNumber = buildNumber.ToString();

        Debug.Log($"[Mutiny Build] Version synchronized: version={version}, build={buildNumber}");
    }

    private static void ContinueQueue()
    {
        if (!SessionState.GetBool(QueueActiveKey, false))
            return;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating || BuildPipeline.isBuildingPlayer)
        {
            EditorApplication.delayCall += ContinueQueue;
            return;
        }

        string json = SessionState.GetString(QueueKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            FinishQueue();
            return;
        }

        var state = JsonUtility.FromJson<BuildQueueState>(json);
        if (state == null || state.targets == null || state.index >= state.targets.Length)
        {
            FinishQueue();
            return;
        }

        BuildTarget target = (BuildTarget)state.targets[state.index];
        BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);

        if (!BuildPipeline.IsBuildTargetSupported(group, target))
        {
            Debug.LogError($"[Mutiny Build] {target} build support is not installed. Install the platform module in Unity Hub.");
            AdvanceQueue(state);
            return;
        }

        if (EditorUserBuildSettings.activeBuildTarget != target)
        {
            Debug.Log($"[Mutiny Build] Switching active build target to {target}...");
            SessionState.SetString(QueueKey, JsonUtility.ToJson(state));

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
            {
                Debug.LogError($"[Mutiny Build] Failed to switch active build target to {target}.");
                AdvanceQueue(state);
                return;
            }

            // Switching platforms can trigger compilation/domain reload.
            // Queue state is kept in SessionState and resumes automatically.
            EditorApplication.delayCall += ContinueQueue;
            return;
        }

        bool success = BuildTargetNow(target, state.version, state.buildNumber);
        if (!success)
        {
            Debug.LogError($"[Mutiny Build] {target} build failed. Continuing with the remaining queued platforms.");
        }
        else if (target == BuildTarget.StandaloneWindows64 && state.buildWindowsInstaller)
        {
            if (!CreateWindowsInstaller(state.version, state.buildNumber))
                Debug.LogError("[Mutiny Build] Windows player succeeded, but installer creation failed.");
        }

        AdvanceQueue(state);
    }

    private static void AdvanceQueue(BuildQueueState state)
    {
        state.index++;
        SessionState.SetString(QueueKey, JsonUtility.ToJson(state));

        if (state.index >= state.targets.Length)
        {
            FinishQueue();
        }
        else
        {
            EditorApplication.delayCall += ContinueQueue;
        }
    }

    private static void FinishQueue()
    {
        string json = SessionState.GetString(QueueKey, string.Empty);
        var state = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<BuildQueueState>(json);

        SessionState.EraseString(QueueKey);
        SessionState.SetBool(QueueActiveKey, false);

        if (state != null)
            Debug.Log($"[Mutiny Build] Build queue finished: {state.version}+{state.buildNumber}. Output: {Path.GetFullPath("Builds")}");
        else
            Debug.Log("[Mutiny Build] Build queue finished.");
    }

    internal static bool BuildTargetNow(BuildTarget target, string version, int buildNumber, string outputPath = null)
    {
        if (!File.Exists(LocalizationSourcePath) || !File.Exists(LocalizationRuntimePath) ||
            !File.ReadAllBytes(LocalizationSourcePath).SequenceEqual(File.ReadAllBytes(LocalizationRuntimePath)) ||
            AssetDatabase.LoadAssetAtPath<TextAsset>(LocalizationRuntimePath) == null)
        {
            Debug.LogError("[Mutiny Build] Runtime localization text is missing or stale. Run Mutiny/Localization/Rebuild String Tables.");
            return false;
        }

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[Mutiny Build] No enabled scenes found in Build Settings.");
            return false;
        }

        string location = outputPath ?? GetOutputPath(target, version, buildNumber);
        string directory = target == BuildTarget.iOS ? location : Path.GetDirectoryName(location);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = location,
            target = target,
            options = BuildOptions.None
        };

        Debug.Log($"[Mutiny Build] Building {target} -> {location}");
        BuildReport report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[Mutiny Build] {target} succeeded. Size: {report.summary.totalSize} bytes, Time: {report.summary.totalTime}");
            return true;
        }

        Debug.LogError($"[Mutiny Build] {target} result: {report.summary.result}");
        return false;
    }

    private static bool CreateWindowsInstaller(string version, int buildNumber)
    {
        string compiler = FindInnoSetupCompiler();
        if (string.IsNullOrEmpty(compiler))
        {
            Debug.LogError(
                "[Mutiny Build] Inno Setup compiler (ISCC.exe) was not found. " +
                "Install Inno Setup 7, or use 'Mutiny X > Build > Configure Inno Setup Compiler...'. " +
                "Winget: winget install --id JRSoftware.InnoSetup.7 -e -s winget -i"
            );
            return false;
        }

        string script = Path.GetFullPath(InstallerScriptPath);
        if (!File.Exists(script))
        {
            Debug.LogError($"[Mutiny Build] Installer script not found: {script}");
            return false;
        }

        string playerExe = Path.GetFullPath(GetOutputPath(BuildTarget.StandaloneWindows64, version, buildNumber));
        string sourceDirectory = Path.GetDirectoryName(playerExe);
        if (string.IsNullOrEmpty(sourceDirectory) || !Directory.Exists(sourceDirectory))
        {
            Debug.LogError($"[Mutiny Build] Windows build directory not found: {sourceDirectory}");
            return false;
        }

        string outputDirectory = Path.GetFullPath(Path.Combine("Builds", "Installer"));
        Directory.CreateDirectory(outputDirectory);

        var startInfo = new ProcessStartInfo
        {
            FileName = compiler,
            Arguments = QuoteProcessArgument(script),
            WorkingDirectory = Path.GetDirectoryName(script) ?? Directory.GetCurrentDirectory(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.EnvironmentVariables["MUTINY_APP_VERSION"] = version;
        startInfo.EnvironmentVariables["MUTINY_BUILD_NUMBER"] = buildNumber.ToString();
        startInfo.EnvironmentVariables["MUTINY_SOURCE_DIR"] = sourceDirectory;
        startInfo.EnvironmentVariables["MUTINY_OUTPUT_DIR"] = outputDirectory;

        Debug.Log($"[Mutiny Build] Creating Windows installer with {compiler}");

        try
        {
            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    Debug.LogError("[Mutiny Build] Failed to start Inno Setup compiler.");
                    return false;
                }

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(stdout))
                    Debug.Log("[Mutiny Build][Inno Setup]\n" + stdout.Trim());

                if (process.ExitCode != 0)
                {
                    Debug.LogError(
                        $"[Mutiny Build] Inno Setup failed with exit code {process.ExitCode}.\n{stderr.Trim()}"
                    );
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(stderr))
                    Debug.LogWarning("[Mutiny Build][Inno Setup]\n" + stderr.Trim());
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Mutiny Build] Failed to create Windows installer: {exception}");
            return false;
        }

        string expectedInstaller = Path.Combine(
            outputDirectory,
            $"MutinyX-Setup-{SanitizeVersionForFileName(version)}+{buildNumber}.exe"
        );

        if (!File.Exists(expectedInstaller))
        {
            Debug.LogError($"[Mutiny Build] Inno Setup completed but installer was not found: {expectedInstaller}");
            return false;
        }

        Debug.Log($"[Mutiny Build] Windows installer created: {expectedInstaller}");
        return true;
    }

    private static string FindInnoSetupCompiler()
    {
        string configured = EditorPrefs.GetString(InnoCompilerPrefKey, string.Empty);
        if (File.Exists(configured))
            return configured;

        string environmentPath = Environment.GetEnvironmentVariable("INNO_SETUP_COMPILER");
        if (File.Exists(environmentPath))
            return environmentPath;

        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        string[] candidates =
        {
            Path.Combine(programFiles, "Inno Setup 7", "ISCC.exe"),
            Path.Combine(programFilesX86, "Inno Setup 7", "ISCC.exe"),
            Path.Combine(programFiles, "Inno Setup 6", "ISCC.exe"),
            Path.Combine(programFilesX86, "Inno Setup 6", "ISCC.exe"),
            Path.Combine(localAppData, "Programs", "Inno Setup 7", "ISCC.exe"),
            Path.Combine(localAppData, "Programs", "Inno Setup 6", "ISCC.exe")
        };

        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        foreach (string entry in path.Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            string candidate;
            try
            {
                candidate = Path.Combine(entry.Trim().Trim('"'), "ISCC.exe");
            }
            catch
            {
                continue;
            }

            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    private static string QuoteProcessArgument(string value)
    {
        return "\"" + value.Replace("\"", "\\\"") + "\"";
    }

    private static string SanitizeVersionForFileName(string version)
    {
        return version.Replace('/', '-').Replace('\\', '-');
    }

    private static string GetOutputPath(BuildTarget target, string version, int buildNumber)
    {
        string safeVersion = SanitizeVersionForFileName(version);
        string label = $"MutinyX-{safeVersion}+{buildNumber}";

        switch (target)
        {
            case BuildTarget.StandaloneWindows64:
                return Path.Combine("Builds", "Windows", label, "Mutiny X.exe");

            case BuildTarget.Android:
                string extension = EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk";
                return Path.Combine("Builds", "Android", label + extension);

            case BuildTarget.iOS:
                return Path.Combine("Builds", "iOS", label);

            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, null);
        }
    }
}
