using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class CorreccionCamaraProbe
{
    const string Key="CorreccionCamaraProbe.Active";
    const string Out="C:/Caballos/Validation/AnimacionCamara/";
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<string> checks=new();
    static IEnumerator tests;
    static CameraController camera;
    static CorreccionCamaraProbe()=>EditorApplication.update+=Step;
    public static void Run()
    {
        var path=Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH");
        if(!Application.isBatchMode||string.IsNullOrEmpty(path)||!path.StartsWith("C:\\Caballos\\Validation\\ValidationSave\\")||File.Exists(SaveSystem.RutaArchivo))throw new InvalidOperationException("Guardado aislado nuevo requerido");
        EditorSceneManager.OpenScene(GameLaunch.HubScenePath);
        EditorApplication.delayCall+=()=>EditorApplication.delayCall+=()=>{SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;};
    }
    static void Step()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        try{tests??=Tests();if(!tests.MoveNext())Finish(0);}catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static IEnumerator Tests()
    {
        while(GameManager.Instancia==null||GameManager.Instancia.caballosJugador.Count==0)yield return null;
        var game=GameManager.Instancia;
        double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready)yield return null;
        camera=CameraController.instancia;camera.enabled=false;
        Check(Object.FindObjectsByType<CameraController>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1,"Un solo controlador de cámara en Hub");
        Check(camera.fixedHubNavigation&&camera.minYaw==-90&&camera.maxYaw==90&&camera.minPitch==-25&&camera.maxPitch==30,"Límites solicitados conservados");
        var fixedPosition=camera.transform.position;float fov=camera.GetComponent<Camera>().fieldOfView;
        var cursor=Cursor.lockState;bool visible=Cursor.visible;
        var size=new Vector2(1920,1080);
        var obj=new GameObject("CamaraAnteriorValidacion",typeof(Camera));
        var old=obj.AddComponent<CameraControllerAnterior>();old.enabled=false;
        var center=new Vector2(1919*.98f,1079*.5f);
        var method=typeof(CameraControllerAnterior).GetMethod("ProcesarPosicionHub",Flags);
        method.Invoke(old,new object[]{center,size,.016f,1,0f});
        for(int i=0;i<200;i++)method.Invoke(old,new object[]{new Vector2(1919,1079*.5f),size,.016f,2+i,1f});
        Check(Mathf.Abs((float)typeof(CameraControllerAnterior).GetField("smoothYaw",Flags).GetValue(old))<.001f,"Reproduce defecto anterior: referencia al 98% impide llegar a la derecha");
        Object.DestroyImmediate(obj);
        foreach(var resolution in new[]{new Vector2(1920,1080),new Vector2(1600,900),new Vector2(1366,768),new Vector2(1280,720),new Vector2(1916,874)})
        {
            Reset(0,0);Tick(.5f,.5f,resolution);Hold(1,.5f,resolution);Near(90,0,"Extremo derecho "+resolution);
            Hold(0,.5f,resolution);Near(-90,0,"Extremo izquierdo "+resolution);
            Hold(.5f,0,resolution);Near(0,30,"Límite vertical inferior "+resolution);
            Hold(.5f,1,resolution);Near(0,-25,"Límite vertical superior "+resolution);
            Hold(.25f,.75f,resolution);Near(-45,-12.5f,"Posición intermedia "+resolution);
            Reset(0,0);Tick(.5f,.5f,resolution);
            for(int i=0;i<=1000;i++)Tick(i/1000f,.5f,resolution,1f/144f);
            Near(90,0,"Barrido lento alcanza derecha "+resolution);
            for(int i=1000;i>=0;i--)Tick(i/1000f,.5f,resolution,1f/144f);
            Near(-90,0,"Barrido lento alcanza izquierda "+resolution);
        }
        foreach(float reference in new[]{0f,.0001f,.01f,.04f,.5f,.96f,.98f,.9999f,1f})
        foreach(float angle in new[]{-89f,-40f,0f,40f,89f})
        {
            Reset(angle,7);Tick(reference,.5f,size);Near(angle,7,"Recalibra sin salto "+reference+" desde "+angle);
            Tick(.5f,.5f,size);Hold(1,.5f,size);Near(90,7,"Derecha alcanzable tras referencia "+reference);
            Hold(0,.5f,size);Near(-90,7,"Izquierda alcanzable tras referencia "+reference);
        }
        foreach(float dt in new[]{1f/30,1f/60,1f/144,.3f})
        {
            Reset(0,0);Tick(.5f,.5f,size);
            for(int i=0;i<500;i++)
            {
                float previous=(float)Get("smoothYaw");
                Tick(i%2==0?0:1,.5f,size,dt);
                Check(Mathf.Abs((float)Get("smoothYaw")-previous)<=camera.velocidadMaximaGiro*Mathf.Min(dt,1f/30f)+.001f,"Barrido rápido con paso angular acotado dt="+dt);
            }
            Hold(1,.5f,size,dt);Near(90,0,"Mantener el extremo termina el giro dt="+dt);
        }
        Reset(37,-9);Tick(.7f,.3f,size);
        var orientation=camera.transform.rotation;
        Call(camera,"ProcesarPosicionHub",new Vector2(-3,100),size,.016f);
        Tick(.95f,.2f,size);Near(37,-9,"Salir y volver a ventana recalibra sin salto");
        Tick(.95f,.2f,new Vector2(1280,720));Near(37,-9,"Cambio de resolución conserva orientación");
        Call(camera,"OnApplicationFocus",false);Call(camera,"LateUpdate");
        Check(Quaternion.Angle(orientation,camera.transform.rotation)<.001f,"Pérdida de foco congela cámara");
        Call(camera,"OnApplicationFocus",true);Tick(.99f,.99f,size);Near(37,-9,"Recuperación de foco conserva orientación");
        Check(game.ElegirCarrera(game.ObtenerOfertaCarreras()[0].id,out _),"Prepara carrera para probar TV y tablón");
        foreach(var type in new[]{Interactable.Tipo.Tienda,Interactable.Tipo.Establo,Interactable.Tipo.Tablon,Interactable.Tipo.TV})
        {
            Reset(42,-8);Tick(.6f,.4f,size);
            game.SetState(type);Call(camera,"LateUpdate");Near(42,-8,"Interfaz congela "+type);
            var pause=UIManager.instance.GetComponent<MenuPausa>();
            Call(pause,"ProcesarEscape");Call(camera,"LateUpdate");Near(42,-8,"Escape sobre interfaz conserva ángulo "+type);
            Call(pause,"Cerrar",true);yield return null;
            Call(camera,"LateUpdate");Near(42,-8,"Cerrar Escape mantiene congelada interfaz inferior "+type);
            game.VolverAlHub();Tick(.98f,.2f,size);Near(42,-8,"Volver al Hub recalibra sin salto "+type);
            Hold(1,.2f,size);Check(Mathf.Abs((float)Get("smoothYaw")-90)<.001f,"Derecha tras cerrar "+type);
        }
        camera.transform.position+=Vector3.one;Call(camera,"LateUpdate");
        Check((camera.transform.position-fixedPosition).sqrMagnitude<.000001f,"Posición fija conservada");
        Check(camera.GetComponent<Camera>().fieldOfView==fov,"FOV conservado");
        Check(Cursor.lockState==cursor&&Cursor.visible==visible,"Batch no modifica cursor de Windows");
        Debug.Log("CORRECCION CAMARA PASS: "+checks.Count+" comprobaciones");
    }
    static void Reset(float yaw,float pitch){Set("smoothYaw",yaw);Set("smoothPitch",pitch);Call(camera,"InvalidarSeguimiento");camera.transform.rotation=(Quaternion)Get("rotacionCentral")*Quaternion.Euler(pitch,yaw,0);Call(camera,"OnApplicationFocus",true);}
    static void Tick(float x,float y,Vector2 size,float dt=1f/60)=>Call(camera,"ProcesarPosicionHub",new Vector2(x*(size.x-1),y*(size.y-1)),size,dt);
    static void Hold(float x,float y,Vector2 size,float dt=1f/60){for(int i=0;i<200;i++)Tick(x,y,size,dt);}
    static void Near(float yaw,float pitch,string text)=>Check(Mathf.Abs((float)Get("smoothYaw")-yaw)<.002f&&Mathf.Abs((float)Get("smoothPitch")-pitch)<.002f,text);
    static object Get(string name)=>typeof(CameraController).GetField(name,Flags).GetValue(camera);
    static void Set(string name,object value)=>typeof(CameraController).GetField(name,Flags).SetValue(camera,value);
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Flags).Invoke(target,args);
    static void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);checks.Add(text);}
    static void Finish(int code){SessionState.SetBool(Key,false);Time.timeScale=1;File.WriteAllLines(Out+"camara-pruebas.txt",checks);EditorApplication.Exit(code);}
}
