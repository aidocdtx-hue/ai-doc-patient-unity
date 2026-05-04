using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

public class CopyKeepUnitySymbols : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        var targetPath = "Library/Bee/Android/Prj/IL2CPP/Gradle/unityLibrary/shared";
        var sourceFile = "Assets/Plugins/Android/shared/keepUnitySymbols.gradle";
        var destFile = Path.Combine(targetPath, "keepUnitySymbols.gradle");

        if (!Directory.Exists(targetPath))
        {
            Directory.CreateDirectory(targetPath);
        }

        File.Copy(sourceFile, destFile, true);
    }
}
