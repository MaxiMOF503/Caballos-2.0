using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class AnimacionesCatalogoProbe
{
    const string Key="AnimacionesCatalogoProbe.Active";
    const string Output="C:/Caballos/Validation/AnimacionCamara/";
    static readonly List<string> checks=new();
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static IEnumerator tests;
    static AnimacionesCatalogoProbe()=>EditorApplication.update+=Step;
    public static void Run()
    {
        SessionState.SetBool("AnimacionesCatalogoProbe.Partial",false);
        if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH"))||File.Exists(SaveSystem.RutaArchivo))throw new InvalidOperationException("Guardado aislado requerido");
        EditorSceneManager.OpenScene(GameLaunch.HubScenePath);
        EditorApplication.delayCall+=()=>EditorApplication.delayCall+=()=>{SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;};
    }
    public static void RunParcial(){Run();SessionState.SetBool("AnimacionesCatalogoProbe.Partial",true);}
    static void Step(){if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;try{tests??=Tests();if(!tests.MoveNext())Finish(0);}catch(Exception e){Debug.LogException(e);Finish(1);}}
    static IEnumerator Tests()
    {
        while(GameManager.Instancia==null||GameManager.Instancia.caballosJugador.Count==0)yield return null;
        double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready)yield return null;
        var game=GameManager.Instancia;var catalog=HorseVisualCatalog.Default;
        bool partial=SessionState.GetBool("AnimacionesCatalogoProbe.Partial",false);
        var available=CatalogoCaballos.Todos.Where(i=>Resources.Load<HorseVisualProfile>("HorseRaceProfiles/"+i.id)!=null).ToArray();
        Check(available.Length==(partial?89:100),(partial?"89":"100")+" perfiles nuevos de carrera disponibles");
        catalog.catalogAnimationsEnabled=true; // Solo esta ejecución aislada; no se guarda el asset.
        var profiles=new HashSet<HorseVisualProfile>();var textures=new HashSet<Texture2D>();
        foreach(var identity in available)
        {
            var horse=new Caballo{velocidad=6,stamina=5,suerte=4,inteligencia=3,potencial=12};
            CatalogoCaballos.Asignar(horse,identity);var profile=catalog.ResolveForRace(horse);
            Check(profile!=null&&profile.IsValid&&profile.productionReady&&!profile.provisional,"Perfil revisado "+identity.id);
            Check(profile.catalogIdentityId==identity.id&&profile.profileId=="catalogo:"+identity.id&&profile.framesPerSecond==9,"ID propio y 9 FPS "+identity.id);
            var splash=RecursosCatalogo.Cargar(identity.splashRecurso);
            Check(profile.baseSprite==splash&&horse.sprite==splash&&catalog.Splash(horse).texture==splash.texture,"Splash y salida propios "+identity.id);
            Check(profile.runFrames.Length==8&&profile.runFrames.All(s=>s.texture==profile.runFrames[0].texture)&&profile.runFrames.Distinct().Count()==8,"Ocho frames propios "+identity.id);
            // Unity escala rectángulos de origen fraccional; admitir solo 0,01 píxel de redondeo.
            Check(profile.runFrames.All(s=>s.name.StartsWith(identity.id+"_run_")&&Mathf.Abs(s.rect.width-256)<.01f&&Mathf.Abs(s.rect.height-256)<.01f&&(s.pivot-new Vector2(128,128)).sqrMagnitude<.0001f),"Marco y pivote uniformes "+identity.id);
            var texture=profile.runFrames[0].texture;
            Check(texture.width==1024&&texture.height==512&&texture.filterMode==FilterMode.Point&&!texture.isReadable&&texture.mipmapCount==1&&texture.format==TextureFormat.BC7,"Importación pixel art y BC7 "+identity.id);
            profiles.Add(profile);textures.Add(texture);
            var obj=new GameObject("AnimadorValidacion",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            var image=obj.GetComponent<Image>();var animator=obj.AddComponent<HorseVisualAnimator>();animator.Bind(horse,image);
            animator.StartRun();Check(animator.IsRunning&&animator.Profile==profile&&image.sprite==profile.runFrames[0],"Inicia animación propia "+identity.id);
            for(int i=0;i<8;i++)
            {
                typeof(HorseVisualAnimator).GetField("elapsed",Flags).SetValue(animator,i/9f+.01f-Time.deltaTime);
                typeof(HorseVisualAnimator).GetMethod("Update",Flags).Invoke(animator,null);
                Check(image.sprite==profile.runFrames[i],"Fase "+(i+1)+" "+identity.id);
            }
            horse.catalogIdentityId="caballo_100";
            Check(animator.Profile==profile,"Perfil enlazado no cambia durante la competición "+identity.id);
            animator.StopRun();Check(image.sprite==splash&&!animator.IsRunning,"Restaura splash correspondiente "+identity.id);
            horse.catalogIdentityId=identity.id;
            var json=JsonUtility.ToJson(CaballoData.DesdeCaballo(horse,-1));var data=JsonUtility.FromJson<CaballoData>(json);
            var restored=data.CrearCaballo();Check(catalog.RestoreCatalogVisual(restored),"Restaura catálogo "+identity.id);
            Check(restored.id==horse.id&&restored.catalogIdentityId==identity.id&&restored.visualProfileId==identity.visualProfileId&&restored.velocidad==6&&restored.stamina==5&&restored.suerte==4&&restored.inteligencia==3&&catalog.ResolveForRace(restored)==profile,"JSON conserva instancia, estadísticas y animación "+identity.id);
            Object.DestroyImmediate(obj);
        }
        Check(profiles.Count==available.Length&&textures.Count==available.Length,"Cada identidad disponible tiene su perfil y spritesheet propios");
        long memory=textures.Sum(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t));
        File.WriteAllText(Output+"memoria-animaciones.txt",textures.Count+" atlas cargados; "+memory+" bytes reportados por Unity.");
        Check(memory<128L*1024*1024,"Atlas nuevos disponibles por debajo de 128 MiB cargados en el Editor");
        foreach(var old in catalog.profiles)
        {
            var horse=new Caballo{id="legacy-"+old.profileId,nombre="Nombre anterior conservado",visualProfileId=old.profileId,velocidad=7,stamina=8,suerte=9,inteligencia=10,potencial=11};
            Check(catalog.RestoreCatalogVisual(horse),"Migra familia recuperada "+old.profileId);
            string id=horse.catalogIdentityId;
            Check(CatalogoCaballos.Buscar(id).visualProfileId==old.profileId&&horse.nombre=="Nombre anterior conservado"&&horse.velocidad==7&&horse.stamina==8&&horse.id=="legacy-"+old.profileId,"Migración conserva pelaje, nombre, instancia y estadísticas "+old.profileId);
            Check(catalog.RestoreCatalogVisual(horse)&&horse.catalogIdentityId==id,"Migración idempotente "+old.profileId);
        }
        var legacy=new Caballo{id="legado28",nombre="Rayo",visualProfileId="legacy:0",sprite=game.spritesCaballos[0]};
        Check(!catalog.RestoreCatalogVisual(legacy)&&string.IsNullOrEmpty(legacy.catalogIdentityId)&&legacy.sprite==game.spritesCaballos[0],"Sprite legacy no reconocido no se reasigna por nombre");
        game.rivalPersistente=null;
        Check(game.GuardarPartida(),"Guarda sin rival persistente");
        Check(SaveSystem.IntentarCargar(out var rivalFixture),"Lee DTO de rival ausente");
        Debug.Log("RIVAL NULO SERIALIZADO: "+JsonUtility.ToJson(rivalFixture.rivalPersistente));
        Check((bool)Call(game,"CargarPartida")&&game.rivalPersistente==null,"Rival ausente no se convierte en un caballo vacío al cargar");
        var oldRival=new Caballo{nombre="Rival anterior conservado",visualProfileId="legacy:0",velocidad=7,sprite=game.spritesCaballos[0]};
        rivalFixture.rivalPersistente=CaballoData.DesdeCaballo(oldRival,0);
        Check(SaveSystem.Guardar(rivalFixture)&&(bool)Call(game,"CargarPartida"),"Carga rival legacy real");
        Check(game.rivalPersistente?.id==oldRival.id&&game.rivalPersistente.nombre==oldRival.nombre&&game.rivalPersistente.sprite==game.spritesCaballos[0]&&game.rivalPersistente.velocidad==7,"La protección del DTO vacío conserva un rival legacy legítimo");
        game.rivalPersistente=null;
        Check(game.GuardarPartida()&&(bool)Call(game,"CargarPartida")&&game.rivalPersistente==null,"Segundo guardado/carga no reintroduce el rival vacío");
        game.dinero=100000;
        if(partial && !available.Any(i=>i.id==game.SubastaActual.caballo.catalogIdentityId))
        {
            var fixture=new Caballo();CatalogoCaballos.Asignar(fixture,available[0]);
            game.SubastaActual.caballo=CaballoData.DesdeCaballo(fixture,-1);
        }
        var auction=game.SubastaActual;foreach(var bidder in auction.postores)bidder.retirado=true;
        var auctionHorse=game.CrearVistaSubasta();var auctionProfile=catalog.ResolveForRace(auctionHorse);
        Check(auctionProfile!=null&&game.PujarSubasta(auction.id,SistemaSubastas.Minima(auction),out _),"Puja por caballo con animación propia");
        Call(game,"CerrarSubasta");
        var delivered=game.caballosJugador.Single(h=>h.id==auctionHorse.id);
        Check(delivered.catalogIdentityId==auctionHorse.catalogIdentityId&&delivered.visualProfileId==auctionHorse.visualProfileId&&catalog.ResolveForRace(delivered)==auctionProfile,"Entrega conserva identidad y animación ofertadas");
        Check(game.SeleccionarCaballoActivo(delivered.id,out _),"Selecciona caballo adquirido");
        Check(game.GuardarPartida(),"Guarda caballo adquirido");Check((bool)Call(game,"CargarPartida"),"Carga caballo adquirido");
        Check(game.CaballoActivo.id==delivered.id&&catalog.ResolveForRace(game.CaballoActivo)==auctionProfile,"Activo y animación persisten al cargar");
        if(partial)game.rivalPersistente=null;
        Check(game.ElegirCarrera(game.ObtenerOfertaCarreras()[0].id,out _),"Prepara carrera real");
        if(partial)
        {
            for(int attempt=0;attempt<50 && game.ObtenerCarreraProgramada().participantes.Any(p=>!available.Any(i=>i.id==p.caballo.catalogIdentityId));attempt++)
            {
                typeof(GameManager).GetField("carreraProgramada",Flags).SetValue(game,null);
                game.rivalPersistente=null;
                typeof(GameManager).GetField("semillaOfertaCarreras",Flags).SetValue(game,9000+attempt);
                Check(game.ElegirCarrera(game.ObtenerOfertaCarreras()[0].id,out _),"Selecciona parrilla de prueba con recursos disponibles "+attempt);
            }
            Check(game.ObtenerCarreraProgramada().participantes.All(p=>available.Any(i=>i.id==p.caballo.catalogIdentityId)),"Prueba parcial usa únicamente las identidades realmente disponibles");
        }
        var scheduled=game.ObtenerCarreraProgramada();var snapshot=JsonUtility.ToJson(scheduled);
        game.SetState(Interactable.Tipo.TV);yield return null;
        var race=UIManager.instance.viewCarrera.GetComponentInChildren<CarreraUI>();
        var actors=Object.FindObjectsByType<HorseVisualAnimator>(FindObjectsSortMode.None);
        Debug.Log("PARRILLA DIAGNOSTICO: "+string.Join("; ",scheduled.participantes.Select(p=>p.caballo.nombre+" cat="+p.caballo.catalogIdentityId+" perfil="+p.caballo.visualProfileId))+" | actores="+string.Join("; ",actors.Select(a=>a.name+"="+(a.Profile?.profileId??"null"))));
        Check(actors.Length==scheduled.participantes.Count&&actors.All(a=>a.Profile!=null&&a.Profile.productionReady),"Toda la parrilla usa perfiles nuevos");
        Check(actors.All(a=>a.Profile.runFrames.All(s=>AssetDatabase.GetAssetPath(s).StartsWith("Assets/Art/HorseRaceCatalog/"))),"No hay sprites antiguos en carrera del catálogo");
        Time.captureFramerate=9;race.IniciarCarrera();
        var bound=actors.Select(a=>a.Profile).ToArray();var seen=actors.Select(_=>new HashSet<Sprite>()).ToArray();
        for(int frame=0;frame<4000&&EnCurso(race);frame++)
        {
            yield return null;
            for(int i=0;i<actors.Length;i++)
            {
                Check(actors[i].Profile==bound[i],"Identidad fija en carrera actor "+i+" frame "+frame);
                if(actors[i].IsRunning)seen[i].Add(actors[i].GetComponentInChildren<Image>(true).sprite);
            }
        }
        Time.captureFramerate=0;
        Check(!EnCurso(race)&&seen.All(s=>s.Count==8),"Carrera real termina tras reproducir las ocho fases de todos los participantes");
        Check(actors.All(a=>!a.IsRunning&&a.GetComponentInChildren<Image>(true).sprite==a.Profile.runFrames[2]),"Todos restauran su propia pose de carrera al terminar");
        Check(game.GuardarPartida()&&(bool)Call(game,"CargarPartida"),"Guarda y carga después de carrera");
        Check(catalog.ResolveForRace(game.CaballoActivo)==auctionProfile,"Aspecto del caballo activo sigue fijo al terminar");
        Debug.Log((partial?"ANIMACIONES PARCIAL PASS: ":"ANIMACIONES CATALOGO PASS: ")+checks.Count+" comprobaciones");
    }
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Flags).Invoke(target,args);
    static bool EnCurso(CarreraUI race)=>(bool)typeof(CarreraUI).GetField("carreraActiva",Flags).GetValue(race);
    static void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);checks.Add(text);}
    static void Finish(int code){SessionState.SetBool(Key,false);Time.captureFramerate=0;File.WriteAllLines(Output+"animaciones-pruebas.txt",checks);EditorApplication.Exit(code);}
}
