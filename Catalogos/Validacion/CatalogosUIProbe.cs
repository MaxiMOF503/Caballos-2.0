using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class CatalogosUIProbe
{
    const string Active="CatalogosUIProbe.Active";
    const string Output="C:/Caballos/Validation/Catalogos/";
    static readonly List<string> checks=new();
    static IEnumerator tests;
    static GameManager game;
    static double start;
    static CatalogosUIProbe()=>EditorApplication.update+=Step;
    public static void Run()
    {
        CatalogosProbe.Importar();
        string path=Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH");
        if(!Application.isBatchMode||string.IsNullOrEmpty(path)||!Path.GetFullPath(path).StartsWith("C:\\Caballos\\Validation\\ValidationSave\\")||File.Exists(SaveSystem.RutaArchivo))
            throw new InvalidOperationException("Guardado aislado requerido");
        EditorSceneManager.OpenScene(GameLaunch.HubScenePath);
        EditorApplication.delayCall+=()=>EditorApplication.delayCall+=()=>{SessionState.SetBool(Active,true);EditorApplication.isPlaying=true;};
    }
    static void Step()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying)return;
        try
        {
            if(tests==null){start=EditorApplication.timeSinceStartup;tests=Tests();}
            if(EditorApplication.timeSinceStartup-start>240)throw new TimeoutException("UI catálogos");
            if(!tests.MoveNext())Finish(0);
        }
        catch(Exception e){Debug.LogException(e);Finish(1);}
    }
    static IEnumerator Tests()
    {
        while(GameManager.Instancia==null||GameManager.Instancia.caballosJugador.Count==0)yield return null;
        game=GameManager.Instancia;game.dinero=100000;
        double ready=EditorApplication.timeSinceStartup+2;
        while(EditorApplication.timeSinceStartup<ready)yield return null;
        var products=game.CatalogoTienda();
        game.estadoTienda.idsProductosStock=new List<string>{"brida_cuero","manual_ejercicios","avena_integral"};
        game.SetState(Interactable.Tipo.Tienda);
        var shop=UIManager.instance.viewTienda.GetComponentInChildren<TiendaView>();
        var root=UIManager.instance.viewTienda.transform.Find("TiendaRustica");
        Check(root!=null,"Tienda conserva su raíz y seis espacios");
        Check(Enumerable.Range(0,6).All(i=>root.Find("ContenidoTienda/Oferta"+i)!=null),"Seis espacios originales");
        shop.SeleccionarManzanaDorada();
        Check(shop.textoNombre.text=="Manual de ejercicios"&&shop.botonComprar.GetComponent<Button>().interactable,"Consumible real se puede comprar desde UI: "+shop.textoNombre.text+"; comprar="+shop.botonComprar.GetComponent<Button>().interactable+"; stock="+string.Join(",",game.estadoTienda.idsProductosStock));
        int balance=game.dinero;int count=game.itemsJugador.Count;
        shop.MostrarConfirmacion();shop.CancelarCompra();
        Check(game.dinero==balance&&game.itemsJugador.Count==count,"Cancelar conserva saldo e inventario");
        shop.MostrarConfirmacion();shop.ConfirmarCompra();shop.ConfirmarCompra();
        Check(game.dinero==balance-200&&game.itemsJugador.Count==count+1,"Confirmación doble compra una sola unidad");
        foreach(var id in new[]{"brida_cuero","avena_integral"})Check(game.IntentarComprarItem(id),"Compra UI-fixture "+id);
        var canvas=root.GetComponentInParent<Canvas>();
        var camera=new GameObject("CatalogosRenderValidation").AddComponent<Camera>();
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.magenta;
        camera.transform.position=new Vector3(0,0,-100);camera.cullingMask=(1<<5)|1;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1366,768),new Vector2Int(1280,720)})
        {
            var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
            foreach(var product in products)
            {
                game.estadoTienda.idsProductosStock=new[]{product.idDefinicion}.Concat(products.Where(p=>p.idDefinicion!=product.idDefinicion).Take(2).Select(p=>p.idDefinicion)).ToList();
                shop.RefrescarPorCambioDeStock();shop.SeleccionarHerraduraPlata();
                Canvas.ForceUpdateCanvases();
                foreach(var t in new[]{shop.textoNombre,shop.textoDescripcion,shop.textoPrecio})
                {t.ForceMeshUpdate();Check(!t.isTextOverflowing,"Detalle legible "+product.idDefinicion+" "+size+" "+t.name);}
                Check(root.Find("ContenidoTienda/Oferta0/IconoProducto").GetComponent<Image>().sprite==product.sprite,"Oferta resuelve icono por ID "+product.idDefinicion);
            }
            Capture(camera,target,"tienda-"+size.x+"x"+size.y);
            camera.targetTexture=null;Object.DestroyImmediate(target);
        }
        game.VolverAlHub();UIManager.instance.AbrirEstablo();
        root=UIManager.instance.viewEstablo.transform.Find("EstabloRustico");
        var stable=root.GetComponentInParent<EstabloView>();
        if(stable==null)stable=UIManager.instance.viewEstablo.GetComponentInChildren<EstabloView>();
        stable.MostrarItems();yield return null;
        string rows="Contenido/Items/Inventario/Viewport/Filas/Item_";
        Click(root,rows+"manual_ejercicios");
        Check(!root.Find(rows+"manual_ejercicios").GetComponents<Component>().Any(c=>c.GetType().Name=="EstabloArrastre"),"Consumible sin arrastre a ranuras");
        Check(root.Find("Contenido/Items/DescripcionItem").GetComponentInChildren<TMP_Text>().text=="Usar consumible","Botón de acción explica el consumo");
        Click(root,"Contenido/Items/DescripcionItem");
        Check(root.Find("DetalleItemModal/ConfirmarDetalle").GetComponentInChildren<TMP_Text>().text=="Usar","Modal existente confirma consumible");
        int xp=game.CaballoActivo.experiencia;
        Click(root,"DetalleItemModal/ConfirmarDetalle");
        Check(game.CaballoActivo.experiencia==xp+35&&game.itemsJugador.All(i=>i.idDefinicion!="manual_ejercicios"),"Consumir desde UI aplica XP y retira unidad");
        yield return null;
        Click(root,rows+"avena_integral");Click(root,"Contenido/Items/DescripcionItem");
        Check(!root.Find("DetalleItemModal/ConfirmarDetalle").GetComponent<Button>().interactable,"UI impide segundo uso en el mismo día");
        Click(root,"DetalleItemModal/CerrarDetalle");
        Click(root,rows+"brida_cuero");Click(root,"Contenido/Items/RanuraCabeza/Estado");yield return null;
        Check(game.ObtenerItemEquipado(game.CaballoActivo,TipoSlotItem.Cabeza)?.idDefinicion=="brida_cuero","Equipo nuevo usa ranura existente");
        Click(root,"Contenido/Items/RanuraCabeza/Estado");
        Check(root.Find("DetalleItemModal/ConfirmarDetalle").GetComponentInChildren<TMP_Text>().text=="Desequipar","Modal recupera acción de equipo");
        Click(root,"DetalleItemModal/CerrarDetalle");
        var identity=CatalogoCaballos.Buscar("caballo_003");
        CatalogoCaballos.Asignar(game.CaballoActivo,identity);stable.MostrarStats();
        Check(root.Find("Contenido/Stats/RetratoGris").GetComponent<Image>().sprite.texture==RecursosCatalogo.Cargar(identity.splashRecurso).texture,"Establo utiliza splash propio y perfil fijo");
        CaptureScreens(camera,"establo");
        game.VolverAlHub();game.SetState(Interactable.Tipo.Tienda);
        Click(UIManager.instance.viewTienda.transform.Find("TiendaRustica"),"ContenidoTienda/Subasta");yield return null;
        var auction=Object.FindFirstObjectByType<SubastaView>();
        var horse=game.CrearVistaSubasta();
        Check(auction.transform.Find("Contenido/CaballoDinamico").GetComponent<Image>().sprite==HorseVisualCatalog.Default.Splash(horse),"Subasta muestra splash de instancia entregable");
        CaptureScreens(camera,"subasta");
        Object.DestroyImmediate(camera.gameObject);
        Debug.Log("CATALOGOS UI PASS: "+checks.Count+" comprobaciones");
    }
    static void Click(Transform root,string path)=>root.Find(path).GetComponent<Button>().onClick.Invoke();
    static void CaptureScreens(Camera camera,string name)
    {
        foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1366,768),new Vector2Int(1280,720)})
        {
            var target=new RenderTexture(size.x,size.y,24);camera.targetTexture=target;
            Capture(camera,target,name+"-"+size.x+"x"+size.y);
            camera.targetTexture=null;Object.DestroyImmediate(target);
        }
    }
    static void Capture(Camera camera,RenderTexture target,string name)
    {
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
        var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
        File.WriteAllBytes(Output+name+".png",image.EncodeToPNG());RenderTexture.active=null;Object.DestroyImmediate(image);
    }
    static void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);checks.Add(label);}
    static void Finish(int code){SessionState.SetBool(Active,false);File.WriteAllLines(Output+"pruebas-ui.txt",checks);EditorApplication.Exit(code);}
}
