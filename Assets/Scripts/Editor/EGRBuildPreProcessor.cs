using MRK;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class EGRBuildPreProcessor : IPreprocessBuildWithReport {
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report) {
        System.IO.File.WriteAllText($"{Application.dataPath}\\Resources\\BuildInfo\\Build.txt", $"{EGRVersion.Build}");
    }
}