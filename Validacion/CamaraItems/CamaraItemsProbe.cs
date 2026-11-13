using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class CamaraItemsProbe
{
    const string Key="CamaraItemsProbe.Active", Output="C:/Caballos/Validation/CamaraItems/";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
    static readonly List<string> checks=new();
    static IEnumerator tests;
    static CameraController camera;
    static GameManager game;
    static UIManager ui;
    static int frame;
    static double sample;
    static CamaraItemsProbe()=>EditorApplication.update+=Step;
    public static void Run()
    {
        var path=Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH");
        if(!Application.isBatchMode||string.IsNullOrEmpty(path)||File.Exists(SaveSystem.RutaArchivo))throw new InvalidOperationException("Guardado batch aislado nuevo requerido");
        EditorSceneManager.OpenScene(GameLaunch.HubScenePath);
        EditorApplication.delayCall+=()=>EditorApplication.delayCall+=()=>{SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;};
    }
    static void Step(){if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;try{tests??=Tests();if(!tests.MoveNext())Finish(0);}catch(Exception e){Debug.LogException(e);Finish(1);}}
    static IEnumerator Tests()
    {
        while(GameManager.Instancia==null||GameManager.Instancia.caballosJugador.Count==0)yield return null;
        double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready)yield return null;
        game=GameManager.Instancia;ui=UIManager.instance;camera=CameraController.instancia;camera.enabled=false;
        var position=camera.transform.position;var cursor=Cursor.lockState;bool visible=Cursor.visible;
        Check(Object.FindObjectsByType<CameraController>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length==1,"Un solo controlador del Hub");
        Check(camera.fixedHubNavigation&&camera.minYaw==-90&&camera.maxYaw==90&&camera.minPitch==-25&&camera.maxPitch==30&&camera.GetComponent<Camera>().fieldOfView==60,"Posición fija, FOV60 y límites solicitados");
        NumericChecks();
        foreach(var kind in new[]{Interactable.Tipo.Tienda,Interactable.Tipo.Establo,Interactable.Tipo.Tablon})
        {
            game.VolverAlHub();var area=new Rect(0,0,1918,870);Follow(new Vector2(37,-8),area);
            var before=camera.transform.rotation;game.SetState(kind);
            Check(Blocked(),"UI bloquea cámara: "+kind);
            for(int i=0;i<12;i++)Check(!Tick(new Vector2(i*100,400),area,Blocked()).HasValue&&Quaternion.Angle(before,camera.transform.rotation)<.01f,"UI no acumula movimiento "+kind+" "+i);
            EventSystem.current?.SetSelectedGameObject(null);
            var pause=ui.GetComponent<MenuPausa>();Call(pause,"ProcesarEscape");
            Check(Blocked()&&BoolStatic(typeof(MenuPausa),"Activa"),"Escape encima de "+kind+" sigue congelando");
            Call(pause,"ProcesarEscape");yield return null;
            Check(Blocked(),"Cerrar Escape conserva bloqueo de la UI inferior: "+kind);
            game.VolverAlHub();yield return null;
            Check(!Blocked(),"Cerrar última UI permite sincronizar: "+kind);
            var request=Tick(new Vector2(1880,100),area,false);
            Check(request.HasValue&&Quaternion.Angle(before,camera.transform.rotation)<.01f,"Volver solicita un único cursor equivalente sin rotar: "+kind);
            Tick(request.Value,area,false);Check(Quaternion.Angle(before,camera.transform.rotation)<.01f,"Confirmar sincronía no salta: "+kind);
        }
        game.VolverAlHub();ui.AbrirSeleccionCarrera();
        Check(Blocked(),"Selector de carreras bloquea aunque GameState siga en Hub");game.VolverAlHub();
        Follow(new Vector2(-38,12),new Rect(0,0,1920,1080));var saved=camera.transform.rotation;
        Call(camera,"OnApplicationFocus",false);
        Check(Get(camera,"seguimiento").ToString()=="Suspendido"&&Quaternion.Angle(saved,camera.transform.rotation)<.01f,"Perder foco invalida seguimiento y conserva ángulo");
        Call(camera,"OnApplicationFocus",true);
        Check(Get(camera,"seguimiento").ToString()=="Suspendido"&&Quaternion.Angle(saved,camera.transform.rotation)<.01f,"Recuperar foco exige sincronía nueva");
        Check(camera.transform.position==position&&camera.GetComponent<Camera>().fieldOfView==60,"Pruebas de control conservan posición y FOV");
        Check(Cursor.lockState==cursor&&Cursor.visible==visible,"Batch no modificó el cursor de Windows");
        var items=ItemsChecks();while(items.MoveNext())yield return items.Current;
        Debug.Log("CAMARA ITEMS PASS: "+checks.Count+" comprobaciones; mouse físico pendiente.");
    }
    static void NumericChecks()
    {
        var obj=new GameObject("CamaraAnteriorSoloPrueba",typeof(Camera));
        var old=obj.AddComponent<CamaraAntesItems>();old.enabled=false;
        var size=new Vector2(1920,1080);var center=new Vector2(1919*.5f,1079*.5f);
        Call(old,"ProcesarPosicionHub",center,size,1f/60);
        Call(old,"ProcesarPosicionHub",new Vector2(1919,center.y),size,1f/60);
        float yaw=(float)Get(old,"smoothYaw");
        Call(old,"ProcesarPosicionHub",new Vector2(1919,center.y),size,1f/60);
        Check((float)Get(old,"smoothYaw")>yaw,"Reproduce error anterior: sigue girando con mouse detenido");
        var args=new object[]{.98f+1f/1919f,.98f,0f,-90f,90f};
        float jump=(float)typeof(CamaraAntesItems).GetMethod("MapearEje",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,args);
        Check(jump>2,"Reproduce ganancia anterior: un píxel tras referencia98% supera2 grados");
        Object.DestroyImmediate(obj);
        foreach(var area in new[]{new Rect(0,0,1920,1080),new Rect(0,0,1600,900),new Rect(0,0,1366,768),new Rect(0,0,1280,720),new Rect(0,0,1918,870),new Rect(100,70,1280,720)})
        {
            for(int x=0;x<=20;x++)for(int y=0;y<=20;y++)
            {
                var angle=new Vector2(-90+x*9,-25+y*2.75f);var pixel=Inverse(angle,area);var actual=Map(pixel,area);
                Check(Vector2.Distance(angle,actual)<.001f,"Conversión reversible "+area+" "+angle);
            }
            Follow(Vector2.zero,area);
            foreach(var point in new[]{Point(0,.5f,area),Point(1,.5f,area),Point(.5f,0,area),Point(.5f,1,area),Point(.5f,.5f,area)})
            {
                Tick(point,area);Near(Map(point,area),"Objetivo inmediato, sin arrastre ni retraso "+point);
                var rotation=camera.transform.rotation;
                for(int hold=0;hold<20;hold++){Tick(point,area);Check(Quaternion.Angle(rotation,camera.transform.rotation)<.01f,"Mouse detenido no arrastra la cámara "+hold);}
            }
            for(int i=0;i<=500;i++){var point=Point(i/500f,.5f,area);Tick(point,area);Near(new Vector2(-90+180*i/500f,0),"Barrido lento sin zona muerta "+i);}
            foreach(float yawAngle in new[]{-90f,-40f,0f,45f,90f})foreach(float menuX in new[]{0f,.02f,.5f,.98f,1f})
            {
                Reset(new Vector2(yawAngle,7));var before=camera.transform.rotation;var menu=Point(menuX,.1f,area);
                var request=Tick(menu,area);
                Check(request.HasValue&&Vector2.Distance(request.Value,Inverse(new Vector2(yawAngle,7),area))<.001f,"Sincroniza al ángulo, no al centro "+yawAngle+" "+menuX);
                Check(Quaternion.Angle(before,camera.transform.rotation)<.01f,"Solicitud no gira cámara");
                for(int stale=0;stale<3;stale++)Check(!Tick(menu,area,false,true,false).HasValue&&Quaternion.Angle(before,camera.transform.rotation)<.01f,"Muestra anterior al warp descartada "+stale);
                Tick(request.Value,area);Check(Quaternion.Angle(before,camera.transform.rotation)<.01f,"Ack conserva orientación");
                float direction=yawAngle==90?-1:1;Tick(request.Value+new Vector2(direction,0),area);
                Near(new Vector2(yawAngle+direction*180/(area.width-1),7),"Ganancia de un píxel constante después de menú");
                Tick(Point(1,.5f,area),area);Near(new Vector2(90,0),"Derecha completa tras menú");
                Tick(Point(0,.5f,area),area);Near(new Vector2(-90,0),"Izquierda completa tras menú");
            }
            Follow(new Vector2(25,-10),area);var frozen=camera.transform.rotation;
            foreach(var invalid in new[]{new Vector2(area.xMin-1,area.center.y),new Vector2(area.xMax,area.center.y),new Vector2(float.NaN,0),new Vector2(0,float.PositiveInfinity)})
                Check(!Tick(invalid,area).HasValue&&Quaternion.Angle(frozen,camera.transform.rotation)<.01f,"Fuera de viewport o dato inválido no gira");
            Check(Tick(area.center,area).HasValue,"Reentrada pide sincronizar, no interpreta salto");
        }
        var viewport=new Rect(0,0,1920,1080);Follow(new Vector2(30,8),viewport);var previous=camera.transform.rotation;
        Check(Tick(new Vector2(500,300),new Rect(0,0,1366,768)).HasValue&&Quaternion.Angle(previous,camera.transform.rotation)<.01f,"Cambio ventana/fullscreen/resolución sincroniza sin salto");
        Reset(new Vector2(20,0));var sync=Tick(Point(.1f,.5f,viewport),viewport);
        Tick(Point(.12f,.5f,viewport),viewport,false,true,true);
        var post=sync.Value+new Vector2(8,0);Tick(post,viewport,false,true,true);
        Near(Map(post,viewport),"Movimiento posterior al warp no exige acertar el píxel exacto");
        var fractional=new Vector2(43.123f,7.432f);Reset(fractional);var requested=Tick(Point(.98f,.1f,viewport),viewport);
        var rounded=new Vector2(Mathf.Round(requested.Value.x),Mathf.Round(requested.Value.y));Tick(rounded,viewport);
        for(int i=0;i<30;i++){Tick(rounded,viewport);Near(fractional,"Redondeo físico del cursor no deriva con el mouse detenido "+i);}
    }
    static IEnumerator ItemsChecks()
    {
        game.VolverAlHub();var first=game.caballosJugador[0];CatalogoCaballos.Asignar(first,CatalogoCaballos.Buscar("caballo_001"));
        var second=new Caballo{nivel=3,velocidad=5,stamina=5,suerte=5,inteligencia=5,esDelJugador=true};
        CatalogoCaballos.Asignar(second,CatalogoCaballos.Buscar("caballo_095"));game.caballosJugador.Add(second);
        game.itemsJugador.Clear();game.dinero=10000;game.SetState(Interactable.Tipo.Establo);yield return null;
        var view=ui.viewEstablo.GetComponentInChildren<EstabloView>();
        var gray=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/caballos establo/4d15d7d2-7f84-4c0e-adab-5d5e12ceec8b.png");
        Check(gray!=null&&(Sprite)Get(view,"spriteManiquiItems")==gray,"Recurso gris original y referencia de escena correctos");
        view.MostrarItems();var image=(Image)Get(view,"maniquiItems");Check(image.sprite==gray,"Items muestra gris original en primer caballo");
        var root=ui.viewEstablo.transform.Find("EstabloRustico");var panel=root.Find("Contenido/Items");
        var slotRects=SistemaEquipamiento.Ranuras.ToDictionary(s=>s,s=>RectSignature((RectTransform)panel.Find("Ranura"+s)));
        Call(view,"SeleccionarCaballo",game.caballosJugador.IndexOf(second));
        Check(image.sprite==gray&&((TMPro.TMP_Text)Get(view,"tituloItems")).text.Contains(second.nombre),"Cambiar caballo conserva maniquí y actualiza caballo real");
        foreach(var slot in SistemaEquipamiento.Ranuras)
        {
            var category=slot==TipoSlotItem.PatasDelanteras||slot==TipoSlotItem.PatasTraseras?TipoSlotItem.Patas:slot;
            var product=game.CatalogoTienda().First(p=>p.tieneSlotEquipable&&p.slotPermitido==category&&p.nivelMinimo<=3);
            game.estadoTienda.idsProductosStock=new List<string>{product.idDefinicion};
            Check(game.IntentarComprarItem(product.idDefinicion),"Compra fixture real "+slot);
            view.MostrarItems();
            var row=root.Find("Contenido/Items/Inventario/Viewport/Filas/Item_"+product.idDefinicion).gameObject;
            var drag=row.GetComponents<MonoBehaviour>().OfType<IBeginDragHandler>().Single();
            var data=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pointerDrag=row,position=new Vector2(500,400)};
            int money=game.dinero;drag.OnBeginDrag(data);
            var destination=panel.Find("Ranura"+slot).GetComponents<MonoBehaviour>().OfType<IDropHandler>().Single();
            destination.OnDrop(data);((IEndDragHandler)drag).OnEndDrag(data);
            Check(game.ObtenerItemEquipado(second,slot)?.idDefinicion==product.idDefinicion&&game.ObtenerItemEquipado(first,slot)==null,"Drop equipa al caballo real seleccionado "+slot);
            Check(game.dinero==money-SistemaEquipamiento.CostoEquiparItem,"Costo de equipar conservado "+slot);
            Check(image.sprite==gray&&RectSignature((RectTransform)panel.Find("Ranura"+slot))==slotRects[slot],"Maniquí, posición y estructura de ranura conservados "+slot);
            yield return null;
        }
        foreach(TipoEstadisticaCaballo stat in Enum.GetValues(typeof(TipoEstadisticaCaballo)))
        {
            int bonus=SistemaEquipamiento.Ranuras.Sum(s=>SistemaTienda.Buscar(game.CatalogoTienda(),game.ObtenerItemEquipado(second,s).idDefinicion).BonoPara(stat));
            Check(game.EstadisticaEfectiva(second,stat)==Caballo.NormalizarEstadistica(5+bonus),"Bonificaciones reales conservadas "+stat);
        }
        foreach(var horse in new[]{first,second})
        {
            Call(view,"SeleccionarCaballo",game.caballosJugador.IndexOf(horse));view.MostrarStats();
            Check(((Image)Get(view,"retratoEnStats")).sprite.texture==HorseVisualCatalog.Default.Splash(horse).texture,"Stats conserva splash propio "+horse.catalogIdentityId);
            view.MostrarUps();Check(((Image)Get(view,"retratoEnUp")).sprite.texture==HorseVisualCatalog.Default.Splash(horse).texture,"Up conserva splash propio "+horse.catalogIdentityId);
            view.MostrarProcrear();Check(((Image)Get(view,"imagenPadre")).sprite==horse.sprite,"Procrear conserva representación dinámica "+horse.catalogIdentityId);
            view.MostrarItems();Check(image.sprite==gray,"Items conserva siempre la misma silueta "+horse.catalogIdentityId);
        }
        string identity=second.catalogIdentityId,id=second.id;
        Check(game.GuardarPartida()&&(bool)Call(game,"CargarPartida"),"Guardar y cargar equipo con maniquí fijo");
        var restored=game.caballosJugador.Single(h=>h.id==id);
        Check(restored.catalogIdentityId==identity&&SistemaEquipamiento.Ranuras.All(s=>game.ObtenerItemEquipado(restored,s)!=null),"Carga conserva identidad y cuatro ranuras del caballo real");
        Call(view,"SeleccionarCaballo",game.caballosJugador.IndexOf(restored));view.MostrarItems();
        RenderItems(view);
        Check(image.sprite==gray,"Render y carga no sustituyen maniquí");
    }
    static void RenderItems(EstabloView view)
    {
        var canvas=view.GetComponentInParent<Canvas>();var render=new GameObject("RenderItemsPrueba").AddComponent<Camera>();
        render.transform.position=new Vector3(0,0,-100);render.clearFlags=CameraClearFlags.SolidColor;render.backgroundColor=Color.black;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=render;canvas.planeDistance=1;
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1366,768),new Vector2Int(1280,720)})
        {
            var rt=new RenderTexture(size.x,size.y,24);render.targetTexture=rt;Canvas.ForceUpdateCanvases();render.Render();RenderTexture.active=rt;
            var screenshot=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);screenshot.ReadPixels(new Rect(0,0,size.x,size.y),0,0);screenshot.Apply();
            File.WriteAllBytes(Output+"items-"+size.x+"x"+size.y+".png",screenshot.EncodeToPNG());RenderTexture.active=null;Object.Destroy(screenshot);render.targetTexture=null;Object.Destroy(rt);
            Check(true,"Render Items "+size);
        }
        Object.Destroy(render.gameObject);
    }
    static string RectSignature(RectTransform r)=>r.anchorMin+"|"+r.anchorMax+"|"+r.anchoredPosition+"|"+r.sizeDelta;
    static Vector2 Point(float x,float y,Rect a)=>new Vector2(a.xMin+x*(a.width-1),a.yMin+y*(a.height-1));
    static Vector2 Map(Vector2 p,Rect a)=>(Vector2)Call(camera,"AngulosDesdeCursor",p,a);
    static Vector2 Inverse(Vector2 p,Rect a)=>(Vector2)Call(camera,"CursorDesdeAngulos",p,a);
    static void Reset(Vector2 a){Set(camera,"angulosHub",a);Call(camera,"Suspender");camera.transform.rotation=(Quaternion)Get(camera,"rotacionCentral")*Quaternion.Euler(a.y,a.x,0);}
    static void Follow(Vector2 a,Rect area){Reset(a);var request=Tick(area.center,area);Check(request.HasValue,"Inicio solicita sincronía");Tick(request.Value,area);}
    static Vector2? Tick(Vector2 p,Rect a,bool blocked=false,bool focus=true,bool fresh=true){frame++;if(fresh)sample+=.1;return (Vector2?)Call(camera,"ProcesarMuestraHub",p,a,blocked,focus,sample,frame);}
    static void Near(Vector2 expected,string text)=>Check(Vector2.Distance((Vector2)Get(camera,"angulosHub"),expected)<.002f,text);
    static bool Blocked()=>(bool)typeof(CameraController).GetProperty("InterfazActiva",Flags).GetValue(camera);
    static bool BoolStatic(Type t,string name)=>(bool)t.GetProperty(name,BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);
    static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static void Set(object o,string n,object v)=>o.GetType().GetField(n,Flags).SetValue(o,v);
    static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n,Flags).Invoke(o,args);
    static void Check(bool condition,string text){if(!condition)throw new InvalidOperationException(text);checks.Add(text);}
    static void Finish(int code){SessionState.SetBool(Key,false);Time.timeScale=1;File.WriteAllLines(Output+"pruebas.txt",checks);EditorApplication.Exit(code);}
}
