using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BetaV02Build
{
    const string ReportRoot = "C:/Caballos/Validation/BetaV02";
    public static void Diagnostic() => Build(true);
    public static void Release() => Build(false);
    static void Build(bool diagnostic)
    {
        if (!Application.isBatchMode || !Application.dataPath.Replace('\\','/').Equals(
            "C:/Caballos/Validation/MainMenu/Assets", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Build only from isolated batch project");
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
            throw new InvalidOperationException("Windows x64 build support missing");
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        if (scenes.Length != 3 || scenes[0] != GameLaunch.MenuScenePath || !scenes.Contains(GameLaunch.HubScenePath))
            throw new InvalidOperationException("Unexpected build scenes");
        foreach (var path in scenes) if (!File.Exists(path)) throw new FileNotFoundException(path);
        var catalog = HorseVisualCatalog.Default;
        if (catalog == null || catalog.profiles.Length != 12 || catalog.profiles.Any(p => p == null || !p.IsValid))
            throw new InvalidOperationException("Incomplete horse catalog");
        foreach (var identity in CatalogoCaballos.Todos)
            if (Resources.Load<HorseVisualProfile>("HorseRaceProfiles/"+identity.id)?.IsValid != true)
                throw new InvalidOperationException("Perfil ausente: "+identity.id);
        PlayerSettings.productName = diagnostic ? "CaballosBetaValidation-20261107-R2" : "Caballos 2.0";
        PlayerSettings.companyName = diagnostic ? "CaballosAcademicValidation" : "DefaultCompany";
        PlayerSettings.bundleVersion = "0.2.0";
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.allowFullscreenSwitch = true;
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.allowDebugging = false;
        EditorUserBuildSettings.connectProfiler = false;
        AssetDatabase.SaveAssets();
        string output = diagnostic ? ReportRoot + "/DiagnosticPlayerR2/CaballosProbe.exe" :
            "C:/Caballos/Builds/Caballos-2.0-Beta-v0.2/Caballos-2.0.exe";
        if (File.Exists(output)) throw new InvalidOperationException("Output already exists; preserve previous build");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = scenes, target = BuildTarget.StandaloneWindows64,
            locationPathName = output, options = BuildOptions.None,
            extraScriptingDefines = diagnostic ? new[] { "CABALLOS_BETA_PROBE" } : Array.Empty<string>()
        });
        string name = diagnostic ? "diagnostic-r2" : "release";
        File.WriteAllText(ReportRoot + "/" + name + "-build-report.txt",
            "Result: " + report.summary.result + "\nUnity: " + Application.unityVersion +
            "\nProduct: " + PlayerSettings.productName + "\nVersion: " + PlayerSettings.bundleVersion +
            "\nTarget: " + report.summary.platform + "\nOptions: " + report.summary.options +
            "\nDevelopment: " + EditorUserBuildSettings.development + "\nDebugging: " + EditorUserBuildSettings.allowDebugging + "\nResolution: 1920x1080 Windowed; resizable\nBytes: " + report.summary.totalSize +
            "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings +
            "\nScenes:\n" + string.Join("\n", scenes) + "\n");
        if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
            throw new InvalidOperationException("Build failed: " + report.summary.result);
        Debug.Log("BETA " + name.ToUpperInvariant() + " BUILD PASS: " + report.summary.totalSize + " bytes");
    }
}
