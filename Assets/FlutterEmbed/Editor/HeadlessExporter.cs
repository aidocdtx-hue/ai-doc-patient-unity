using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// CLI에서 호출:
//   Unity.exe -batchmode -nographics -quit
//             -projectPath <unity_project>
//             -executeMethod HeadlessExporter.ExportAndroid
//             -logFile <log path>
// FLUTTER_UNITY_EXPORT_PATH 환경변수로 export 위치 override 가능. 기본값은 <unity_project>/../../android/unityLibrary
public static class HeadlessExporter
{
    public static void ExportAndroid()
    {
        try
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Debug.Log("Switching active build target to Android...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            }

            EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

            string exportPath = Environment.GetEnvironmentVariable("FLUTTER_UNITY_EXPORT_PATH");
            if (string.IsNullOrEmpty(exportPath))
            {
                string projectRoot = Directory.GetCurrentDirectory();
                exportPath = Path.GetFullPath(Path.Combine(projectRoot, "..", "..", "android", "unityLibrary"));
            }

            if (Directory.Exists(exportPath))
            {
                Directory.Delete(exportPath, true);
            }
            Directory.CreateDirectory(exportPath);

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray(),
                locationPathName = exportPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            };

            Debug.Log($"HeadlessExporter: scenes={buildPlayerOptions.scenes.Length}, target={exportPath}");

            new ProjectExporterAndroid().Export(buildPlayerOptions, new List<string>());

            Debug.Log("HeadlessExporter: export completed");
        }
        catch (Exception e)
        {
            Debug.LogError($"HeadlessExporter failed: {e}");
            EditorApplication.Exit(1);
        }
    }
}
