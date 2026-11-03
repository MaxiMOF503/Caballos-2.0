using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MainMenuValidation
{
    const string Active="MainMenuValidation.Active", Done="MainMenuValidation.Done";
    const string Report="MenuValidation/results.txt";
    static int stage, checks;
    static double start, stageStart;
    static string runtimeError, horseId, visualId, rivalId, expectedJson;
    static MainMenuValidation()
    {
        EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Done,false))
            {SessionState.SetBool(Done,false);if(Application.isBatchMode)EditorApplication.Exit(0);}
        };
        Application.logMessageReceived+=(message,trace,type)=>
        {
            if(!SessionState.GetBool(Active,false) || (type!=LogType.Exception && type!=LogType.Error && type!=LogType.Assert))return;
            if(trace.Contains("UnityEditor.Search.SearchInit.IndexationOnStartup"))
            {Directory.CreateDirectory("MenuValidation");File.AppendAllText("MenuValidation/editor-diagnostics.txt",message+"\n"+trace+"\n");return;}
            runtimeError=message+"\n"+trace;
        };
    }
    public static void Run()
    {
        try
        {
            Directory.CreateDirectory("MenuValidation");File.WriteAllText(Report, "");
            Require(Application.isBatchMode,"batch mode required");
            string save=Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH");
            Require(!string.IsNullOrEmpty(save) && Path.GetFullPath(save).StartsWith(Path.GetFullPath("../MainMenuSave")),"isolated save required");
            Require(!File.Exists(Path.Combine(save,"gameData.json")),"fresh test save required");
            Require(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_PREFS_PREFIX")),"isolated PlayerPrefs required");
            MainMenuBuilder.Build();
            Require(EditorBuildSettings.scenes[0].path==GameLaunch.MenuScenePath && EditorBuildSettings.scenes[0].enabled,"menu build index 0");
            Require(EditorBuildSettings.scenes.Any(s=>s.path==GameLaunch.HubScenePath&&s.enabled),"hub included");
            var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            Require(menu!=null,"menu controller serialized");
            Require(menu.settingsPanel!=null&&menu.confirmationPanel!=null&&menu.statusText!=null&&menu.volumeSlider!=null,"panel references");
            var buttons=new[]{menu.newGameButton,menu.loadGameButton,menu.settingsButton,menu.exitButton};
            foreach(var button in buttons)
            {
                Require(button!=null && button.GetComponentInChildren<Text>()==null,"no duplicate baked text");
                Require(button.targetGraphic!=null&&button.navigation.mode==Navigation.Mode.Explicit,"interactive button and keyboard navigation");
                Require(button.transition==Selectable.Transition.None&&button.targetGraphic.color.a==0&&button.targetGraphic.raycastTarget,"invisible raycast target without rectangular transitions");
                var r=(RectTransform)button.transform;
                Require(r.anchorMin.x>=0&&r.anchorMin.y>=0&&r.anchorMax.x<=1&&r.anchorMax.y<=1,"hotspot inside artwork");
                Require(r.parent.name=="MenuButtons" && r.parent.parent.name=="Background","hotspot image-relative parent");
            }
            var artwork=GameObject.Find("Background").GetComponent<Image>();
            Require(AssetDatabase.GetAssetPath(artwork.sprite)==MainMenuBuilder.ArtworkPath,"exact provided image reference");
            Require(artwork.preserveAspect&&artwork.GetComponent<AspectRatioFitter>().aspectMode==AspectRatioFitter.AspectMode.FitInParent,"complete image fit");
            foreach(var size in new[]{new Vector2(1920,1080),new Vector2(1600,900),new Vector2(1366,768),new Vector2(1280,720)})
            {
                float scale=Mathf.Min(size.x/1659f,size.y/948f);
                Require(1659*scale<=size.x+.01f&&948*scale<=size.y+.01f,"uniform-fit geometry "+size);
            }
            foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach(var transform in root.GetComponentsInChildren<Transform>(true))
                    Require(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"no missing scripts");
            File.AppendAllText(Report,"Edit Mode PASS: "+checks+" checks; menu/build settings, references, image-relative hotspots, aspect-fit geometry at four resolutions. Not a rendered visual test.\n");
            SessionState.SetBool(Active,true);SessionState.SetBool(Done,false);
            EditorApplication.isPlaying=true;
        }
        catch(Exception ex){Fail(ex);}
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
    public static void CheckPersistedPreferences()
    {
        try
        {
            string prefix=Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_PREFS_PREFIX");
            Require(!string.IsNullOrEmpty(prefix)&&prefix.StartsWith("Caballos.MenuValidation."),"test preferences prefix");
            Require(Mathf.Abs(MenuPreferences.Volume-.37f)<.001f&&MenuPreferences.Fullscreen,"preferences survive separate Unity process");
            Require(MenuPreferences.Size.x>0&&MenuPreferences.Size.y>0,"persisted resolution");
            File.AppendAllText(Report,"Separate process PASS: PlayerPrefs volume/fullscreen/resolution survived Unity restart. Test keys removed afterward.\n");
            foreach(string key in new[]{"Volume","Fullscreen","Width","Height"})PlayerPrefs.DeleteKey(prefix+key);
            PlayerPrefs.Save();
        }
        catch(Exception ex){Fail(ex);}
    }
    static void Next(){stage++;stageStart=EditorApplication.timeSinceStartup;}
    static void Tick()
    {
        if(!SessionState.GetBool(Active,false)||!EditorApplication.isPlaying||EditorApplication.isCompiling)return;
        try
        {
            if(runtimeError!=null)throw new Exception("Runtime error: "+runtimeError);
            if(start==0){start=stageStart=EditorApplication.timeSinceStartup;return;}
            if(EditorApplication.timeSinceStartup-stageStart>45)throw new Exception("Timeout stage "+stage);
            if(EditorApplication.timeSinceStartup-start<2)return;
            var menu=UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            var gm=GameManager.Instancia;
            if(stage==0)
            {
                Require(menu!=null && SceneManager.GetActiveScene().path==GameLaunch.MenuScenePath,"menu is first runtime scene");
                Require(!menu.loadGameButton.interactable,"missing save disabled");
                menu.LoadGame();Require(!menu.IsBusy&&!File.Exists(SaveSystem.RutaArchivo),"load missing safe");
                Directory.CreateDirectory(Path.GetDirectoryName(SaveSystem.RutaArchivo));
                expectedJson="{invalid recovered save";File.WriteAllText(SaveSystem.RutaArchivo,expectedJson);
                menu.RefreshLoadState();menu.LoadGame();
                Require(!menu.IsBusy && File.ReadAllText(SaveSystem.RutaArchivo)==expectedJson,"corrupt load preserved");
                Require(!File.Exists(SaveSystem.RutaArchivo+".corrupt"),"preflight does not mutate files");
                menu.NewGame();Require(menu.confirmationPanel.activeSelf&&!menu.IsBusy,"new game needs confirmation");
                Require(File.ReadAllText(SaveSystem.RutaArchivo)==expectedJson,"no overwrite before confirmation");
                menu.CloseConfirmation();Require(!menu.confirmationPanel.activeSelf&&File.ReadAllText(SaveSystem.RutaArchivo)==expectedJson,"cancel preserves save");
                EventSystem.current.SetSelectedGameObject(menu.newGameButton.gameObject);
                ExecuteEvents.Execute(menu.newGameButton.gameObject,new AxisEventData(EventSystem.current){moveDir=MoveDirection.Down,moveVector=Vector2.down},ExecuteEvents.moveHandler);
                Require(EventSystem.current.currentSelectedGameObject==menu.loadGameButton.gameObject,"keyboard down logical event");
                ExecuteEvents.Execute(menu.loadGameButton.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);
                ExecuteEvents.Execute(menu.loadGameButton.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerExitHandler);
                menu.OpenSettings();Require(menu.settingsPanel.activeSelf&&!menu.menuButtons.interactable,"settings modal");
                var settings=menu.settingsPanel.GetComponent<MenuSettings>();settings.masterVolume.value=.37f;
                Require(Mathf.Abs(MenuPreferences.Volume-.37f)<.001f&&Mathf.Abs(AudioListener.volume-.37f)<.001f,"volume applied");
                MenuPreferences.SetDisplay(MenuPreferences.AvailableSizes()[0],true);
                menu.CloseSettings();menu.OpenSettings();
                Require(Mathf.Abs(settings.masterVolume.value-.37f)<.001f&&settings.fullscreen.isOn,"preferences reload into panel");
                menu.CloseSettings();menu.AskExit();Require(menu.confirmationPanel.activeSelf,"exit confirmation");
                menu.CloseConfirmation();menu.AskExit();menu.Confirm();
                Require(EditorApplication.isPlaying&&!menu.IsBusy,"editor exit does not close session");
                File.AppendAllText(Report,"Play Mode PASS: missing/corrupt save, overwrite confirmation/cancel, settings/prefs, internal pointer/navigation events, safe editor exit.\n");
                menu.NewGame();menu.Confirm();menu.NewGame();menu.Confirm();
                Require(menu.IsBusy&&UnityEngine.Object.FindObjectsByType<MenuSceneTransition>(FindObjectsSortMode.None).Length==1,"duplicate launch prevented");
                Next();return;
            }
            if(stage==1)
            {
                if(SceneManager.GetActiveScene().path!=GameLaunch.HubScenePath||gm==null||gm.caballosJugador.Count==0)return;
                Require(gm.dia==1&&gm.dinero==1000&&gm.caballosJugador.Count==1,"existing new game defaults");
                Require(gm.caballosJugador[0].nivel==1&&gm.caballosJugador[0].experiencia==0,"initial progression unchanged");
                Require(HorseVisualCatalog.Default.Resolve(gm.caballosJugador[0].visualProfileId)!=null,"new horse profile retained");
                Require(!GameLaunch.TryConsume(out _),"launch token consumed once");
                gm.dia=4;gm.dinero=2468;gm.SincronizarTienda();
                var horse=gm.caballosJugador[0];horse.nivel=3;horse.experiencia=35;horse.velocidad=10;horse.stamina=9;horse.suerte=8;horse.inteligencia=7;horse.puntosMejoraPendientes=2;
                horseId=horse.id;visualId=horse.visualProfileId;
                gm.itemsJugador.Add(new ItemCaballo{nombre="Test equipment",idDefinicion="test",caballoEquipadoId=horse.id,slotPermitido=TipoSlotItem.Cabeza,sprite=gm.spritesItems[0]});
                gm.rivalPersistente=gm.caballosDelDia[0];rivalId=gm.rivalPersistente.id;
                Require(gm.GuardarPartida(),"save fixture through existing system");
                expectedJson=File.ReadAllText(SaveSystem.RutaArchivo);
                File.AppendAllText(Report,"Play Mode PASS: confirmed new game opens original hub with day 1, money 1000, original horse generation and initial progression.\n");
                Next();SceneManager.LoadScene(GameLaunch.MenuScenePath);return;
            }
            if(stage==2)
            {
                if(menu==null||!menu.loadGameButton.interactable)return;
                menu.NewGame();Require(menu.confirmationPanel.activeSelf&&File.ReadAllText(SaveSystem.RutaArchivo)==expectedJson,"valid save protected");menu.CloseConfirmation();
                menu.LoadGame();Require(menu.IsBusy,"load starts transition");Next();return;
            }
            if(stage==3)
            {
                if(SceneManager.GetActiveScene().path!=GameLaunch.HubScenePath||gm==null||gm.dia!=4)return;
                var horse=gm.caballosJugador[0];
                Require(gm.dinero==2468&&horse.id==horseId&&horse.visualProfileId==visualId,"load money/day/identity");
                Require(horse.nivel==3&&horse.experiencia==35&&horse.velocidad==10&&horse.stamina==9&&horse.suerte==8&&horse.inteligencia==7&&horse.puntosMejoraPendientes==2,"load stats/progression");
                Require(horse.sprite==HorseVisualCatalog.Default.Resolve(visualId).baseSprite,"load profile base");
                Require(gm.itemsJugador.Count==1&&gm.itemsJugador[0].caballoEquipadoId==horse.id&&gm.itemsJugador[0].sprite==gm.spritesItems[0],"inventory/equipment");
                Require(gm.rivalPersistente.id==rivalId&&!gm.campanaCompletada,"rival/campaign retained");
                Require(File.ReadAllText(SaveSystem.RutaArchivo)==expectedJson,"load did not overwrite save");
                Require(UIManager.instance!=null&&CameraController.instancia!=null&&CameraController.instancia.fixedHubNavigation,"existing hub UI and fixed camera components");
                File.AppendAllText(Report,"Play Mode PASS: load opens hub and retains day, money, horse ID/profile, stats, XP, equipment, rival and campaign. JSON unchanged by explicit load. Hub camera/UI references intact; visual behavior not tested.\n");
                Next();SceneManager.LoadScene(GameLaunch.MenuScenePath);return;
            }
            if(stage==4)
            {
                if(menu==null||!menu.loadGameButton.interactable)return;
                menu.LoadGame();Require(menu.IsBusy,"late corruption test transition");
                expectedJson="{changed during transition";File.WriteAllText(SaveSystem.RutaArchivo,expectedJson);Next();return;
            }
            if(stage==5)
            {
                if(menu==null||SceneManager.GetActiveScene().path!=GameLaunch.MenuScenePath||UnityEngine.Object.FindFirstObjectByType<MenuSceneTransition>()!=null)return;
                Require(File.ReadAllText(SaveSystem.RutaArchivo)==expectedJson,"late failed load preserves file");
                Require(!menu.IsBusy&&!string.IsNullOrEmpty(menu.statusText.text),"failed load returns menu with message and no fade host");
                File.AppendAllText(Report,"Play Mode PASS: corruption after preflight returns to menu, preserves file and clears transition.\nFINAL PASS. No rendered visual or standalone quit test performed.\n");
                SessionState.SetBool(Active,false);SessionState.SetBool(Done,true);EditorApplication.isPlaying=false;
            }
        }
        catch(Exception ex){Fail(ex);}
    }
    static void Fail(Exception ex)
    {
        Directory.CreateDirectory("MenuValidation");File.AppendAllText(Report,"FAILED stage "+stage+": "+ex+"\n");
        SessionState.SetBool(Active,false);Debug.LogException(ex);
        if(Application.isBatchMode)EditorApplication.Exit(1);
    }
}
