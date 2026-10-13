using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>One target per batch process. The launcher selects the target before script compilation.</summary>
public static class MutinyReleaseBuild
{
    [Serializable]
    private sealed class Result
    {
        public bool success;
        public string platform;
        public string version;
        public int buildNumber;
        public string unityVersion;
        public string outputPath;
        public string error;
    }

    public static void Build()
    {
        var result = new Result { unityVersion = Application.unityVersion };
        string reportPath = null;
        try
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Use the local release launcher in batch mode.");

            string[] args = Environment.GetCommandLineArgs();
            reportPath = Path.GetFullPath(Argument(args, "-mutinyReport"));
            result.platform = Argument(args, "-mutinyPlatform");
            result.version = Argument(args, "-mutinyVersion");
            result.buildNumber = int.Parse(Argument(args, "-mutinyBuildNumber"), CultureInfo.InvariantCulture);
            result.outputPath = Path.GetFullPath(Argument(args, "-mutinyOutput"));

            if (!Regex.IsMatch(result.version, @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$"))
                throw new InvalidOperationException("Release version must be major.minor.patch.");
            if (PlayerSettings.bundleVersion != result.version ||
                result.buildNumber <= 0 || result.buildNumber > 2100000000 ||
                PlayerSettings.Android.bundleVersionCode != result.buildNumber)
                throw new InvalidOperationException("Commit the release version and Android build number before tagging. Release builds never increment them.");

            BuildTarget target;
            switch (result.platform)
            {
                case "Windows": target = BuildTarget.StandaloneWindows64; break;
                case "Android": target = BuildTarget.Android; break;
                default: throw new InvalidOperationException("Only Windows and Android are supported by the local release pipeline.");
            }

            if (EditorUserBuildSettings.activeBuildTarget != target)
                throw new InvalidOperationException("Launch Unity with the matching -buildTarget; do not switch targets inside executeMethod.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
                throw new InvalidOperationException("The requested platform module is not installed.");

            // GitHub releases distribute APKs. This setting only affects the isolated release worktree.
            if (target == BuildTarget.Android)
            {
                EditorUserBuildSettings.buildAppBundle = false;
                EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            }

            if (!MutinyBuild.BuildTargetNow(target, result.version, result.buildNumber, result.outputPath))
                throw new InvalidOperationException("BuildPipeline did not report Succeeded. See the platform log.");
            if (!File.Exists(result.outputPath) || new FileInfo(result.outputPath).Length == 0)
                throw new InvalidOperationException("Build succeeded without the expected output file.");

            result.success = true;
        }
        catch (Exception exception)
        {
            result.error = exception.Message;
            Debug.LogError("[Mutiny Release] " + exception);
        }

        try
        {
            if (reportPath != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                File.WriteAllText(reportPath, JsonUtility.ToJson(result, true), new UTF8Encoding(false));
            }
        }
        catch (Exception exception)
        {
            result.success = false;
            Debug.LogError("[Mutiny Release] Cannot write build report: " + exception);
        }

        Debug.Log($"[Mutiny Release] {result.platform} success={result.success}, version={result.version}+{result.buildNumber}");
        EditorApplication.Exit(result.success ? 0 : 1);
    }

    private static string Argument(string[] args, string name)
    {
        int index = Array.IndexOf(args, name);
        if (index < 0 || index + 1 >= args.Length || args[index + 1].StartsWith("-", StringComparison.Ordinal))
            throw new ArgumentException("Missing command-line argument " + name);
        return args[index + 1];
    }
}
