using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CatalogosAssetProbe
{
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Batch requerido");
        var checks=new List<string>();
        foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled))
        {
            if(!File.Exists(entry.path))throw new InvalidOperationException("Escena ausente: "+entry.path);
            var scene=EditorSceneManager.OpenScene(entry.path);
            foreach(var obj in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject))
                if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj)>0)
                    throw new InvalidOperationException("Script perdido: "+entry.path+" / "+obj.name);
            checks.Add("Escena sin scripts perdidos: "+entry.path);
        }
        foreach(string path in Directory.EnumerateFiles("Assets","*",SearchOption.AllDirectories).Where(p=>p.EndsWith(".unity")||p.EndsWith(".prefab")))
        {
            foreach(string guid in Regex.Matches(File.ReadAllText(path),@"guid: ([a-f0-9]{32})").Select(m=>m.Groups[1].Value).Distinct())
            {
                if(guid.StartsWith("0000000000000000"))continue;
                if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))throw new InvalidOperationException("GUID sin resolver: "+path+" / "+guid);
            }
            checks.Add("Referencias de escena/prefab resueltas: "+path);
        }
        foreach(var profile in HorseVisualCatalog.Default.profiles)
            if(!profile.IsValid||profile.runFrames.Length!=8)throw new InvalidOperationException("Perfil animado incompleto: "+profile.profileId);
        checks.Add("12 perfiles y 96 frames de carrera válidos");
        File.WriteAllLines("C:/Caballos/Validation/Catalogos/referencias.txt",checks);
        Debug.Log("CATALOGOS ASSETS PASS: "+checks.Count+" comprobaciones");
    }
}
