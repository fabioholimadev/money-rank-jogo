using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MoneyRank.EditorTools
{
    public static class BuildAndroidApk
    {
        public static void Build()
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogError("Could not switch active build target to Android.");
                EditorApplication.Exit(1);
                return;
            }

            var outputPath = GetOutputPath();
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            EditorUserBuildSettings.buildAppBundle = false;
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/_MoneyRank/Scenes/DevSandbox.unity" },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"Android build result: {report.summary.result}; output: {options.locationPathName}");
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        private static string GetOutputPath()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-buildOutput")
                    return Path.GetFullPath(args[i + 1]);
            }

            return Path.GetFullPath("Builds/MoneyRank.apk");
        }
    }
}
