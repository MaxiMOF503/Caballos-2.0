using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public sealed class HorsePixelImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Art/HorsesRecovered/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = 100;
        importer.spritePivot = new Vector2(.5f, .5f);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.npotScale = TextureImporterNPOTScale.None;
    }
}

[InitializeOnLoad]
public static class HorseVisualRecovery
{
    const string Active = "HorseVisualRecovery.Active";
    static int checks, group;
    static double started, raceStarted;
    static bool running;
    static string[] ids;
    const string Completed = "HorseVisualRecovery.Completed";
    static string runtimeError;
    static readonly System.Collections.Generic.HashSet<Sprite> seenFrames = new System.Collections.Generic.HashSet<Sprite>();
    static HorseVisualRecovery()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;
            SessionState.EraseString("HorseVisualRecovery.SavePath");
            if (!SessionState.GetBool(Completed, false)) return;
            SessionState.SetBool(Completed, false);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        };
        Application.logMessageReceived += (message, trace, type) =>
        {
            if (SessionState.GetBool(Active, false) &&
                (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
            {
                // Unity 6000.3.13f1 can throw in the Editor Search startup index in a
                // fresh headless checkout. Preserve that diagnostic separately;
                // it is not a game exception and must not hide runtime failures.
                if (trace.Contains("UnityEditor.Search.SearchInit.IndexationOnStartup"))
                {
                    Directory.CreateDirectory("VisualValidation");
                    File.AppendAllText("VisualValidation/editor-diagnostics.txt", message + "\n" + trace + "\n");
                    return;
                }
                runtimeError = message + "\n" + trace;
            }
        };
    }

    [MenuItem("Caballos/Recovered visuals/Build catalog")]
    public static void BuildCatalog()
    {
        Directory.CreateDirectory("Assets/Resources/HorseProfiles");
        AssetDatabase.Refresh();
        var catalog = AssetDatabase.LoadAssetAtPath<HorseVisualCatalog>("Assets/Resources/HorseVisualCatalog.asset");
        if (catalog == null) { catalog = ScriptableObject.CreateInstance<HorseVisualCatalog>(); AssetDatabase.CreateAsset(catalog, "Assets/Resources/HorseVisualCatalog.asset"); }
        catalog.profiles = new HorseVisualProfile[12];
        for (int i = 1; i <= 12; i++)
        {
            string id = "horse_" + i.ToString("00");
            string path = "Assets/Resources/HorseProfiles/" + id + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<HorseVisualProfile>(path);
            if (profile == null) { profile = ScriptableObject.CreateInstance<HorseVisualProfile>(); AssetDatabase.CreateAsset(profile, path); }
            profile.profileId = id;
            profile.baseSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/HorsesRecovered/base/" + id + "_base.png");
            profile.runFrames = new Sprite[8];
            for (int frame = 1; frame <= 8; frame++)
                profile.runFrames[frame - 1] = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/HorsesRecovered/" +
                    (i == 2 ? "master/" + id + "_master_run_" : "video/" + id + "_run_") + frame.ToString("00") + ".png");
            if (profile.framesPerSecond <= 0) profile.framesPerSecond = 9;
            profile.provisional = true;
            profile.source = i == 2 ? "Recovered master GIF; keyed transparency; provisional" : "WhatsApp video 500x670; approximate phase selection; provisional";
            EditorUtility.SetDirty(profile);
            catalog.profiles[i - 1] = profile;
        }
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Caballos/Recovered visuals/Validate in Play Mode")]
    public static void Validate()
    {
        try
        {
            BuildCatalog();
            var catalog = HorseVisualCatalog.Default;
            Require(catalog.profiles.Length == 12, "12 profiles");
            Require(catalog.profiles.Select(p => p.profileId).Distinct().Count() == 12, "unique IDs");
            foreach (var p in catalog.profiles)
            {
                Require(p.IsValid, p.profileId + " complete");
                Require(AssetDatabase.GetAssetPath(p.baseSprite) == "Assets/Art/HorsesRecovered/base/" + p.profileId + "_base.png", "base identity " + p.profileId);
                Require(p.runFrames.Distinct().Count() == 8, "8 distinct references " + p.profileId);
                for (int f = 0; f < 8; f++)
                {
                    string expected = "Assets/Art/HorsesRecovered/" + (p.profileId == "horse_02" ? "master/" + p.profileId + "_master_run_" : "video/" + p.profileId + "_run_") + (f + 1).ToString("00") + ".png";
                    Require(AssetDatabase.GetAssetPath(p.runFrames[f]) == expected, "ordered frame identity " + p.profileId + ":" + f);
                }
                foreach (var sprite in p.runFrames.Concat(new[] { p.baseSprite }))
                {
                    Require(sprite.rect.size == new Vector2(512,512), "512 cell");
                    var importer = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite));
                    Require(importer.filterMode == FilterMode.Point && !importer.mipmapEnabled && importer.textureCompression == TextureImporterCompression.Uncompressed, "pixel import");
                    Require(sprite.pivot == new Vector2(256,256) && sprite.pixelsPerUnit == 100 && importer.alphaIsTransparency, "consistent pivot, scale and transparency");
                }
                var horse = new Caballo { visualProfileId = p.profileId, sprite = p.baseSprite };
                var json = JsonUtility.ToJson(CaballoData.DesdeCaballo(horse, -1));
                var loaded = JsonUtility.FromJson<CaballoData>(json).CrearCaballo();
                catalog.Restore(loaded, -1, null);
                Require(loaded.id == horse.id && loaded.visualProfileId == horse.visualProfileId && loaded.sprite == horse.sprite, "JSON identity");
            }
            for (int i = 0; i < 28; i++)
            {
                var horse = JsonUtility.FromJson<CaballoData>("{\"spriteIndex\":" + i + "}").CrearCaballo();
                var sprite = catalog.profiles[0].baseSprite;
                catalog.Restore(horse, i, sprite);
                catalog.Restore(horse, i, sprite);
                Require(horse.visualProfileId == "legacy:" + i && horse.sprite == sprite, "stable legacy " + i);
            }
            Directory.CreateDirectory("VisualValidation");
            File.WriteAllText("VisualValidation/results.txt", "Edit Mode checks passed: " + checks + "\n");
            EditorSceneManager.OpenScene("Assets/Scenes/hub.unity");
            SessionState.SetString("HorseVisualRecovery.SavePath", Path.GetFullPath("../ValidationSave/Run-" + Guid.NewGuid().ToString("N")));
            SessionState.SetBool(Active, true);
            SessionState.SetBool(Completed, false);
            EditorApplication.isPlaying = true;
        }
        catch (Exception ex) { Fail(ex); }
    }

    static void Require(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        checks++;
    }

    static object Field(object obj, string name) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
    static void Tick()
    {
        if (!SessionState.GetBool(Active, false) || !EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if (runtimeError != null) throw new Exception("Runtime error: " + runtimeError);
            if (started == 0) { started = EditorApplication.timeSinceStartup; return; }
            if (EditorApplication.timeSinceStartup - started < 2) return;
            var gm = GameManager.Instancia;
            var race = UnityEngine.Object.FindFirstObjectByType<CarreraUI>(FindObjectsInactive.Include);
            Require(gm != null && race != null, "hub runtime ready");
            if (!running)
            {
                if (group == 0) ValidatePersistence(gm);
                gm.caballosJugador.Clear(); gm.caballosDelDia.Clear();
                for (int i = 0; i < 6; i++)
                {
                    var p = HorseVisualCatalog.Default.profiles[group * 6 + i];
                    var horse = new Caballo { nombre = p.profileId, visualProfileId = p.profileId, sprite = p.baseSprite, esDelJugador = i == 0, velocidad = 8, stamina = 8 };
                    if (i == 0) gm.caballosJugador.Add(horse); else gm.caballosDelDia.Add(horse);
                }
                gm.estadoActual = GameState.TV;
                UIManager.instance.AbrirCarrera();
                if (group > 0)
                    typeof(CarreraUI).GetMethod("PrepararCarrera", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(race, null);
                race.IniciarCarrera();
                ids = gm.caballosJugador.Concat(gm.caballosDelDia).Select(h=>h.visualProfileId).ToArray();
                raceStarted = EditorApplication.timeSinceStartup; running = true; seenFrames.Clear();
                return;
            }
            var animators = ((System.Collections.Generic.List<HorseVisualAnimator>)Field(race, "visualAnimators")).ToArray();
            Require(animators.Length == 6, "six race visuals");
            if ((bool)Field(race, "carreraActiva"))
            {
                foreach (var animator in animators)
                {
                    Require(animator.IsRunning && animator.Profile.IsValid, "running bound profile");
                    var img = animator.GetComponentInChildren<Image>();
                    Require(animator.Profile.runFrames.Contains(img.sprite), "matching frame");
                    Require(img.preserveAspect, "aspect preserved");
                    seenFrames.Add(img.sprite);
                }
                if (EditorApplication.timeSinceStartup - raceStarted > 60) throw new Exception("Race timeout");
                return;
            }
            for (int i = 0; i < 6; i++)
            {
                Require(animators[i].Profile.profileId == ids[i], "identity after race");
                Require(!animators[i].IsRunning && animators[i].GetComponentInChildren<Image>().sprite == animators[i].Profile.baseSprite, "base restored");
                Require(animators[i].Profile.runFrames.All(f => seenFrames.Contains(f)), "all 8 frames observed: " + ids[i]);
            }
            File.AppendAllText("VisualValidation/results.txt", "Real race group " + group + ": 6 profiles, all 48 frames observed, base sprites restored.\n");
            group++; running = false;
            if (group < 2) return;
            File.AppendAllText("VisualValidation/results.txt", "Play Mode PASS: 2 real CarreraUI races, all 12 profiles, correct frames and base restoration. Runtime assertions sampled repeatedly; not counted as independent tests.\n");
            SessionState.SetBool(Active, false);
            SessionState.SetBool(Completed, true);
            EditorApplication.isPlaying = false;
        }
        catch (Exception ex) { Fail(ex); }
    }

    static void ValidatePersistence(GameManager gm)
    {
        var catalog = HorseVisualCatalog.Default;
        Require(Path.GetFullPath(SaveSystem.RutaArchivo).StartsWith(Path.GetFullPath("../ValidationSave")), "isolated save path");
        Require(gm.spritesCaballos.Length == 28 && gm.spritesCaballos.All(s => s != null), "28 legacy references intact");
        // Exercise real generation, not just manually constructed DTOs.
        for (int day = 0; day < 20; day++)
        {
            gm.GenerarCaballosDelDia();
            foreach (var horse in gm.caballosDelDia)
                Require(catalog.Resolve(horse.visualProfileId)?.baseSprite == horse.sprite, "generated identity");
        }
        gm.caballosJugador.Clear();
        foreach (var p in catalog.profiles)
            gm.caballosJugador.Add(new Caballo { nombre = p.profileId, visualProfileId = p.profileId, sprite = p.baseSprite, esDelJugador = true, velocidad = 7, stamina = 6, inteligencia = 5, suerte = 4, potencial = 12 });
        gm.rivalPersistente = new Caballo { nombre = "Persisted rival", visualProfileId = "horse_02", sprite = catalog.Resolve("horse_02").baseSprite };
        var originalIds = gm.caballosJugador.Select(h => h.id).ToArray();
        int originalMoney = gm.dinero;
        Require(gm.GuardarPartida(), "real GameManager save");
        Require(SaveSystem.IntentarCargar(out var saved) && saved.caballosJugador.Count == 12, "read disk JSON");
        Require((bool)typeof(GameManager).GetMethod("CargarPartida", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(gm, null), "real GameManager load");
        for (int i = 0; i < 12; i++)
        {
            var horse = gm.caballosJugador[i];
            Require(horse.id == originalIds[i] && horse.visualProfileId == catalog.profiles[i].profileId && horse.sprite == catalog.profiles[i].baseSprite, "disk identity " + i);
            Require(horse.velocidad == 7 && horse.stamina == 6 && horse.inteligencia == 5 && horse.suerte == 4 && horse.potencial == 12, "stats preserved");
        }
        Require(gm.dinero == originalMoney && gm.rivalPersistente.visualProfileId == "horse_02" && gm.rivalPersistente.sprite == catalog.Resolve("horse_02").baseSprite, "money and rival retained");
        // Load fixtures without visualProfileId, using the actual 28 scene sprites.
        saved.caballosJugador.Clear();
        saved.rivalPersistente = CaballoData.DesdeCaballo(new Caballo { nombre = "Legacy rival" }, 5);
        for (int i = 0; i < 28; i++)
        {
            var legacy = new Caballo { nombre = "Legacy " + i, esDelJugador = true };
            saved.caballosJugador.Add(CaballoData.DesdeCaballo(legacy, i));
        }
        string oldJson = JsonUtility.ToJson(saved).Replace("\"visualProfileId\":\"\",", "").Replace("\"visualProfileId\":null,", "");
        File.WriteAllText(SaveSystem.RutaArchivo, oldJson);
        Require((bool)typeof(GameManager).GetMethod("CargarPartida", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(gm, null), "legacy save loads");
        for (int i = 0; i < 28; i++)
            Require(gm.caballosJugador[i].visualProfileId == "legacy:" + i && gm.caballosJugador[i].sprite == gm.spritesCaballos[i], "exact legacy appearance " + i);
        Require(gm.rivalPersistente.visualProfileId == "legacy:5" && gm.rivalPersistente.sprite == gm.spritesCaballos[5], "legacy rival preserved");
        Require(gm.GuardarPartida(), "persist migration");
        Require((bool)typeof(GameManager).GetMethod("CargarPartida", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(gm, null), "migration reload");
        for (int i = 0; i < 28; i++) Require(gm.caballosJugador[i].visualProfileId == "legacy:" + i && gm.caballosJugador[i].sprite == gm.spritesCaballos[i], "migration stable " + i);
        Require(gm.rivalPersistente.visualProfileId == "legacy:5" && gm.rivalPersistente.sprite == gm.spritesCaballos[5], "legacy rival migration stable");
        File.AppendAllText("VisualValidation/results.txt", "Play Mode persistence: generated rivals, 12 identities saved/loaded through GameManager + SaveSystem, stats and rival preserved; 28 legacy sprites migrated and reloaded unchanged.\n");
    }
    static void Fail(Exception ex)
    {
        Directory.CreateDirectory("VisualValidation");
        File.AppendAllText("VisualValidation/results.txt", "FAILED: " + ex + "\n");
        Debug.LogException(ex);
        SessionState.SetBool(Active, false);
        EditorApplication.isPlaying = false;
        SessionState.EraseString("HorseVisualRecovery.SavePath");
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }
}
