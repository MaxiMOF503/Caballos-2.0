using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class AnimacionesRenderProbe
{
    const string Key="AnimacionesRenderProbe.Active";
    const string Output="C:/Caballos/Validation/AnimacionCamara/Render100/";
    static IEnumerator tests;
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static AnimacionesRenderProbe()=>EditorApplication.update+=Step;
    public static void Run()
    {
        if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH"))||File.Exists(SaveSystem.RutaArchivo))throw new InvalidOperationException("Guardado aislado nuevo requerido");
        Directory.CreateDirectory(Output);EditorSceneManager.OpenScene(GameLaunch.HubScenePath);
        EditorApplication.delayCall+=()=>EditorApplication.delayCall+=()=>{SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;};
    }
    static void Step(){if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;try{tests??=Tests();if(!tests.MoveNext())Finish(0);}catch(Exception e){Debug.LogException(e);Finish(1);}}
    static IEnumerator Tests()
    {
        while(GameManager.Instancia==null||GameManager.Instancia.caballosJugador.Count==0)yield return null;
        double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready)yield return null;
        var game=GameManager.Instancia;HorseVisualCatalog.Default.catalogAnimationsEnabled=true;
        CatalogoCaballos.Asignar(game.CaballoActivo,CatalogoCaballos.Buscar("caballo_096"));
        game.rivalPersistente=null;
        typeof(GameManager).GetField("carreraProgramada",Flags).SetValue(game,null);
        typeof(GameManager).GetField("nombres",Flags).SetValue(game,new[]{"Tesoro","Sereno","Solano","Victoria","Vigía"});
        if(!game.ElegirCarrera(game.ObtenerOfertaCarreras()[0].id,out var error))throw new InvalidOperationException(error);
        game.SetState(Interactable.Tipo.TV);yield return null;
        var race=UIManager.instance.viewCarrera.GetComponentInChildren<CarreraUI>();
        var actors=Object.FindObjectsByType<HorseVisualAnimator>(FindObjectsSortMode.None);
        if(actors.Length!=5||actors.Any(a=>a.Profile==null||!a.Profile.productionReady))throw new InvalidOperationException("Parrilla de render incompleta");
        if(actors.Any(a=>int.Parse(a.Profile.catalogIdentityId.Substring(8))<90))throw new InvalidOperationException("El render debe usar los nuevos recursos del lote final");
        var canvas=race.GetComponentInParent<Canvas>();var camera=new GameObject("RenderAnimaciones").AddComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-100);camera.cullingMask=(1<<5)|1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        Time.timeScale=0;int captures=0;
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1366,768),new Vector2Int(1280,720)})
        {
            var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
            for(int phase=-1;phase<(size.x==1920?8:1);phase++)
            {
                foreach(var actor in actors)actor.GetComponentInChildren<Image>(true).sprite=phase<0?actor.Profile.baseSprite:actor.Profile.runFrames[phase];
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                var snapshot=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                snapshot.ReadPixels(new Rect(0,0,size.x,size.y),0,0);snapshot.Apply();
                File.WriteAllBytes(Output+size.x+"x"+size.y+(phase<0?"-reposo":"-fase-"+(phase+1).ToString("00"))+".png",snapshot.EncodeToPNG());
                RenderTexture.active=null;Object.Destroy(snapshot);captures++;
            }
            camera.targetTexture=null;Object.Destroy(target);
        }
        foreach(var actor in actors)actor.StopRun();
        Object.Destroy(camera.gameObject);Time.timeScale=1;
        Debug.Log("ANIMACIONES RENDER PASS: "+captures+" capturas de cinco identidades, cuatro resoluciones; sin entrada física.");
    }
    static void Finish(int code){SessionState.SetBool(Key,false);Time.timeScale=1;EditorApplication.Exit(code);}
}
