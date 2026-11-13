using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Player;
using UnityEngine;

public static class CatalogosWindowsProbe
{
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Validación batch requerida");
        const string output = "C:/Caballos/Validation/Catalogos/WindowsScripts";
        Directory.CreateDirectory(output);
        var result = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings
        {
            target = BuildTarget.StandaloneWindows64,
            group = BuildTargetGroup.Standalone,
            options = ScriptCompilationOptions.None
        }, output);
        if (result.assemblies == null || !result.assemblies.Any(a => Path.GetFileName(a) == "Assembly-CSharp.dll"))
            throw new InvalidOperationException("Falta Assembly-CSharp de Player Windows");
        File.WriteAllLines("C:/Caballos/Validation/Catalogos/windows-compilacion.txt", result.assemblies);
        Debug.Log("CATALOGOS WINDOWS SCRIPTS PASS: " + result.assemblies.Count + " ensamblados; no se genera ejecutable.");
    }
}
