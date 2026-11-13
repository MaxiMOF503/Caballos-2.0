using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class CatalogosProbe
{
    const string Active="CatalogosProbe.Active";
    const string Output="C:/Caballos/Validation/Catalogos/";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly List<string> checks=new();
    static IEnumerator tests;
    static GameManager game;
    static double started;
    static CatalogosProbe()=>EditorApplication.update+=Step;

    public static void PrepararYProbar(){Importar();Run();}
    public static void Completo(){SessionState.SetBool("CatalogosProbe.ArtComplete",true);PrepararYProbar();}

    public static void Run()
    {
        string path=Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH");
        if(!Application.isBatchMode || string.IsNullOrEmpty(path) ||
            !Path.GetFullPath(path).StartsWith("C:\\Caballos\\Validation\\ValidationSave\\",StringComparison.OrdinalIgnoreCase)||File.Exists(SaveSystem.RutaArchivo))
            throw new InvalidOperationException("Se necesita un guardado de validación nuevo.");
        EditorSceneManager.OpenScene(GameLaunch.HubScenePath);
        EditorApplication.delayCall+=()=>EditorApplication.delayCall+=()=>{SessionState.SetBool(Active,true);EditorApplication.isPlaying=true;};
    }

    static void Step()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying)return;
        try
        {
            if(tests==null){started=EditorApplication.timeSinceStartup;tests=Tests();}
            if(EditorApplication.timeSinceStartup-started>250)throw new TimeoutException("Catálogos");
            if(!tests.MoveNext())Finish(0);
        }
        catch(Exception error){Debug.LogException(error);Finish(1);}
    }

    static IEnumerator Tests()
    {
        while(GameManager.Instancia==null||GameManager.Instancia.caballosJugador.Count==0)yield return null;
        game=GameManager.Instancia;
        // Dejar terminar el inicio de escena y el primer guardado antes del fixture.
        double ready=EditorApplication.timeSinceStartup+2;
        while(EditorApplication.timeSinceStartup<ready)yield return null;
        var products=game.CatalogoTienda();
        var identities=CatalogoCaballos.Todos;
        Check(products.Count==80 && products.Select(p=>p.idDefinicion).Distinct().Count()==80,"80 productos con ID único");
        Check(identities.Count==100 && identities.Select(h=>h.id).Distinct().Count()==100 &&
            identities.Select(h=>h.nombre).Distinct().Count()==100,"100 identidades y nombres únicos");
        Check(products.Count(p=>p.tipoProducto==TipoProductoTienda.Consumible)==15,"15 consumibles");
        foreach(var entry in new[]{(TipoSlotItem.Cabeza,20),(TipoSlotItem.Espalda,15),(TipoSlotItem.Patas,30)})
            Check(products.Count(p=>p.tieneSlotEquipable&&p.slotPermitido==entry.Item1)==entry.Item2,"Categoría "+entry.Item1);
        Check(products.All(p=>p.precio>0 && p.precio<=1600 && !string.IsNullOrEmpty(p.EfectoVisible)),"Precios y utilidad de los 80 artículos");
        var legacy=new Dictionary<string,int>{{"herradura_plata",200},{"manzana_dorada",350},{"herradura_oro",500},{"zapatillas",150},{"sombrero_copa",300},{"sombrero_noble",450}};
        Check(legacy.All(p=>products.Single(d=>d.idDefinicion==p.Key).precio==p.Value),"IDs y precios anteriores conservados");
        Check(game.caballosJugador.Count==1,"El catálogo no incorpora 100 caballos al establo");
        bool complete=SessionState.GetBool("CatalogosProbe.ArtComplete",false);
        if(complete)
        {
            Check(products.All(p=>p.visualDefinitivo&&p.sprite!=null),"Los 80 productos tienen iconos revisados");
            Check(products.Where(p=>p.indiceLegacy<0).All(p=>p.sprite==RecursosCatalogo.Cargar(p.iconoRecurso)),"74 iconos nuevos propios, sin marcador provisional");
            Check(identities.All(i=>i.visualDefinitivo&&RecursosCatalogo.Cargar(i.splashRecurso)!=null),"100 splashes revisados disponibles");
            Check(identities.Select(i=>RecursosCatalogo.Cargar(i.splashRecurso)).Distinct().Count()==100,"100 recursos de presentación distintos");
        }
        foreach(var identity in identities)
        {
            var horse=new Caballo();CatalogoCaballos.Asignar(horse,identity);
            var profile=HorseVisualCatalog.Default.Resolve(horse.visualProfileId);
            Check(profile!=null&&profile.IsValid&&horse.catalogIdentityId==identity.id,"Perfil animado válido "+identity.id);
            Check(HorseVisualCatalog.Default.Splash(horse)!=null,"Resuelve presentación o respaldo explícito "+identity.id);
            if(complete)Check(HorseVisualCatalog.Default.Splash(horse).texture==RecursosCatalogo.Cargar(identity.splashRecurso).texture,"Splash propio corresponde al ID y perfil "+identity.id);
        }
        var own=game.CaballoActivo;
        own.nivel=3;game.dinero=100000;
        foreach(var product in products)
        {
            game.estadoTienda.idsProductosStock=new List<string>{product.idDefinicion};
            Check(game.IntentarComprarItem(product.idDefinicion),"Compra real "+product.idDefinicion);
            yield return null;
        }
        Check(game.itemsJugador.Count==80&&game.itemsJugador.Select(i=>i.id).Distinct().Count()==80,"Unidades de inventario tienen IDs independientes");
        foreach(var product in products.Where(p=>p.tieneSlotEquipable))
        {
            var item=game.itemsJugador.Single(i=>i.idDefinicion==product.idDefinicion);
            var slot=SistemaEquipamiento.RanuraInicial(product.slotPermitido);
            if(product.nivelMinimo>1)
            {
                own.nivel=1;
                Check(!game.EquiparItem(own,item,slot,true),"Nivel mínimo protege "+product.idDefinicion);
                own.nivel=3;
            }
            Check(game.EquiparItem(own,item,slot,true),"Equipa "+product.idDefinicion);
            foreach(TipoEstadisticaCaballo stat in Enum.GetValues(typeof(TipoEstadisticaCaballo)))
                Check(game.EstadisticaEfectiva(own,stat)==Caballo.NormalizarEstadistica(ProgresionCaballo.ValorBase(own,stat)+product.BonoPara(stat)),
                    "Bonificación efectiva "+product.idDefinicion+" "+stat);
            Check(game.DesequiparItem(own,item),"Desequipa "+product.idDefinicion);
            yield return null;
        }
        var rare=products.First(p=>p.tieneSlotEquipable&&p.nivelMinimo==3);
        own.nivel=1;
        Check(!game.EquiparItem(own,game.itemsJugador.Single(i=>i.idDefinicion==rare.idDefinicion)),"Equipo caro bloqueado en nivel inicial");
        own.nivel=3;
        bool ranRace=false;
        foreach(var product in products.Where(p=>p.tipoProducto==TipoProductoTienda.Consumible))
        {
            own=game.CaballoActivo;
            var item=game.itemsJugador.Single(i=>i.idDefinicion==product.idDefinicion);
            int xpBefore=own.experiencia;
            Check(!game.EquiparItem(own,item),"Consumible no ocupa ranura "+product.idDefinicion);
            var copy=CaballoData.DesdeCaballo(own,-1).CrearCaballo();
            Check(game.UsarConsumible(copy,item,out _),"Usa consumible por ID canónico "+product.idDefinicion);
            Check(game.itemsJugador.All(i=>i.id!=item.id),"Retira una sola unidad "+product.idDefinicion);
            Check(own.experiencia==xpBefore+product.experiencia,"Experiencia real "+product.idDefinicion);
            foreach(TipoEstadisticaCaballo stat in Enum.GetValues(typeof(TipoEstadisticaCaballo)))
                Check(game.EstadisticaEfectiva(own,stat)==Caballo.NormalizarEstadistica(ProgresionCaballo.ValorBase(own,stat)+product.BonoPara(stat)),
                    "Preparación efectiva "+product.idDefinicion+" "+stat);
            var other=game.itemsJugador.FirstOrDefault(i=>SistemaTienda.Buscar(products,i.idDefinicion)?.tipoProducto==TipoProductoTienda.Consumible);
            if(other!=null)Check(!game.UsarConsumible(own,other,out _),"Un solo uso por caballo y día");
            int usedDay=game.dia;
            Save();Load();own=game.CaballoActivo;
            Check(own.ultimoDiaConsumible==usedDay&&own.experiencia==xpBefore+product.experiencia,"Carga conserva efecto y límite diario");
            if(!ranRace)
            {
                Check(game.ElegirCarrera(game.ObtenerOfertaCarreras()[0].id,out _),"Selecciona carrera preparada");
                var race=game.ObtenerCarreraProgramada();
                Check(race.participantes[0].caballo.stamina==game.EstadisticaEfectiva(own,TipoEstadisticaCaballo.Stamina),"Parrilla incorpora preparación temporal");
                string frozen=JsonUtility.ToJson(race);
                Check(other==null||!game.UsarConsumible(own,other,out _),"Parrilla fijada impide consumos inconsistentes");
                Check(JsonUtility.ToJson(race)==frozen,"Parrilla mantiene valores fijados");
                game.SetState(Interactable.Tipo.TV);
                var raceView=UIManager.instance.viewCarrera.GetComponentInChildren<CarreraUI>();
                raceView.IniciarCarrera();Time.timeScale=15;
                while(game.dia==usedDay)yield return null;
                Time.timeScale=1;game.VolverAlHub();ranRace=true;
            }
            else game.SiguienteDia();
            Check(game.CaballoActivo.preparacionVelocidad+game.CaballoActivo.preparacionStamina+
                game.CaballoActivo.preparacionSuerte+game.CaballoActivo.preparacionInteligencia==0,"El cambio de día vence la preparación");
            yield return null;
        }
        Check(game.itemsJugador.Count==65,"Los 15 consumibles se usan y quedan 65 equipables");
        var ids=game.itemsJugador.Select(i=>i.id).ToArray();
        Save();Load();
        Check(ids.SequenceEqual(game.itemsJugador.Select(i=>i.id)),"Guardar y cargar conserva IDs de unidades nuevas");
        foreach(var identity in identities)
        {
            var generated=SistemaSubastas.Crear(17,new[]{identity.nombre},HorseVisualCatalog.Default,null,111);
            Check(generated.caballo.catalogIdentityId==identity.id&&generated.caballo.visualProfileId==identity.visualProfileId&&
                generated.caballo.raza==identity.raza,"Subasta usa identidad de catálogo "+identity.id);
            var roundtrip=JsonUtility.FromJson<SubastaData>(JsonUtility.ToJson(generated));
            Check(JsonUtility.ToJson(roundtrip.caballo)==JsonUtility.ToJson(generated.caballo),"Instancia y estadísticas persisten "+identity.id);
        }
        var snap=game.SubastaActual;
        string auctionBefore=JsonUtility.ToJson(snap);
        Save();Load();
        Check(JsonUtility.ToJson(game.SubastaActual)==auctionBefore,"Subasta ampliada conserva oferta, revelaciones y aspecto");
        var newProfile=game.CaballoActivo.visualProfileId;
        var stableId=game.CaballoActivo.id;
        Check(SaveSystem.IntentarCargar(out var old),"Carga fixture anterior");
        foreach(var horse in old.caballosJugador){horse.catalogIdentityId=null;horse.preparacionVelocidad=0;horse.ultimoDiaConsumible=0;}
        Check(SaveSystem.Guardar(old),"Escribe caballo sin ID de catálogo");Load();
        Check(game.CaballoActivo.id==stableId&&game.CaballoActivo.visualProfileId==newProfile&&string.IsNullOrEmpty(game.CaballoActivo.catalogIdentityId),
            "Caballo anterior conserva ID y apariencia sin reasignación por nombre");
        TestRollback();
        CaptureMemory(products);
        Debug.Log("CATALOGOS PASS: "+checks.Count+" comprobaciones");
    }

    static void TestRollback()
    {
        var product=game.CatalogoTienda().Single(p=>p.idDefinicion=="manual_ejercicios");
        game.estadoTienda.idsProductosStock=new List<string>{product.idDefinicion};
        Check(game.IntentarComprarItem(product.idDefinicion),"Compra consumible para rollback");
        var item=game.itemsJugador.Last();var horse=game.CaballoActivo;
        int level=horse.nivel,points=horse.puntosMejoraPendientes;
        horse.experiencia=ProgresionCaballo.ExperienciaNecesariaParaSiguienteNivel(level)-5;
        Save();
        string before=JsonUtility.ToJson(CaballoData.DesdeCaballo(horse,-1));
        var randomBefore=UnityEngine.Random.state;
        string path=SaveSystem.RutaArchivo+".tmp";Directory.CreateDirectory(path);
        try
        {
            Check(!game.UsarConsumible(horse,item,out _)&&game.itemsJugador.Contains(item)&&
                JsonUtility.ToJson(CaballoData.DesdeCaballo(horse,-1))==before&&UnityEngine.Random.state.Equals(randomBefore),"Fallo de guardado revierte nivel, puntos, XP, preparación, límite, azar y unidad consumida");
        }
        finally{Directory.Delete(path);}
        Check(game.UsarConsumible(horse,item,out _)&&horse.nivel==level+1&&horse.experiencia==30&&
            horse.puntosMejoraPendientes>points,"Consumible que cruza nivel concede puntos una sola vez");
        Save();Load();
        Check(game.CaballoActivo.nivel==level+1&&game.CaballoActivo.experiencia==30,"Nivel y XP de consumible persisten después de cargar");
    }

    static void CaptureMemory(List<DefinicionProductoTienda> products)
    {
        var textures=products.Select(p=>p.sprite?.texture).Where(t=>t!=null).Distinct().ToList();
        foreach(var identity in CatalogoCaballos.Todos)
        {
            var sprite=RecursosCatalogo.Cargar(identity.splashRecurso);
            if(sprite!=null&&!textures.Contains(sprite.texture))textures.Add(sprite.texture);
        }
        long bytes=textures.Sum(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t));
        File.WriteAllText(Output+"memoria.txt",textures.Count+" texturas únicas cargadas. Memoria reportada "+bytes+" bytes.");
        File.WriteAllLines(Output+"texturas.csv",new[]{"ruta,ancho,alto,formato,bytes"}.Concat(textures.Select(t=>AssetDatabase.GetAssetPath(t)+","+t.width+","+t.height+","+t.format+","+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t))));
        Check(bytes<96L*1024*1024,"Texturas de catálogo por debajo de 96 MiB con todos sus recursos cargados");
        Check(textures.Where(t=>AssetDatabase.GetAssetPath(t).Contains("/Catalogos/")).All(t=>t.width<=384&&!t.isReadable&&t.format==TextureFormat.BC7),
            "Texturas nuevas limitadas a 384, BC7 y sin copia CPU legible");
    }
    static object Call(object obj,string method)=>obj.GetType().GetMethod(method,Flags).Invoke(obj,null);
    static void Save()=>Check(game.GuardarPartida(),"Guardado completo");
    static void Load()=>Check((bool)Call(game,"CargarPartida"),"Carga completa");
    static void Check(bool ok,string label){if(!ok)throw new InvalidOperationException(label);checks.Add(label);}
    static void Finish(int code){SessionState.SetBool(Active,false);Time.timeScale=1;File.WriteAllLines(Output+"pruebas.txt",checks);EditorApplication.Exit(code);}
    public static void Importar()
    {
        foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Catalogos"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;
            importer.isReadable=false;
            importer.npotScale=TextureImporterNPOTScale.None;
            // 1536x1024 -> 384x256: conserva la proporción y admite bloques BC7.
            // A 512x341 Unity vuelve a RGBA32 por las dimensiones no múltiplo de cuatro.
            importer.maxTextureSize=384;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name="Standalone", overridden=true, maxTextureSize=384,
                format=TextureImporterFormat.BC7, compressionQuality=100
            });
            importer.filterMode=path.Contains("/Caballos/") ? FilterMode.Point : FilterMode.Bilinear;
            importer.wrapMode=TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        Debug.Log("CATALOGOS COMPILE AND IMPORT PASS");
    }
}
