using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MainMenuBuilder
{
    public const string ArtworkPath = "Assets/Art/MainMenu/MainMenu.png";
    static readonly Color Green = new Color(.015f,.16f,.14f, .98f);
    static readonly Color Gold = new Color(1f,.83f,.4f);
    static readonly Color Beige = new Color(1f,.92f,.72f);
    static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    [MenuItem("Caballos/Main menu/Build scene")]
    public static void Build()
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(ArtworkPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("MainMenu");
        var controller = root.AddComponent<MainMenuController>();
        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camera.transform.SetParent(root.transform);
        camera.tag = "MainCamera";
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camera.GetComponent<Camera>().backgroundColor = Green;
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        events.transform.SetParent(root.transform);
        var canvasObject = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(root.transform);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080);
        scaler.matchWidthOrHeight = .5f;
        var canvas = (RectTransform)canvasObject.transform;
        var margins = Rect("SafeMargins", canvas); Stretch(margins);
        margins.gameObject.AddComponent<Image>().color = Green;
        var background = Rect("Background", canvas); Stretch(background);
        var image = background.gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtworkPath);
        image.preserveAspect = true; image.raycastTarget = false;
        var fitter = background.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1659f / 948f;
        var buttons = Rect("MenuButtons", background); Stretch(buttons);
        controller.menuButtons = buttons.gameObject.AddComponent<CanvasGroup>();
        controller.newGameButton = Hotspot("Button_NewGame", buttons, 361);
        controller.loadGameButton = Hotspot("Button_LoadGame", buttons, 443);
        controller.settingsButton = Hotspot("Button_Settings", buttons, 517);
        controller.exitButton = Hotspot("Button_Exit", buttons, 591);
        var ordered = new[] {controller.newGameButton,controller.loadGameButton,controller.settingsButton,controller.exitButton};
        for (int i=0;i<4;i++) ordered[i].navigation = new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=ordered[(i+3)%4],selectOnDown=ordered[(i+1)%4]};
        events.GetComponent<EventSystem>().firstSelectedGameObject = controller.newGameButton.gameObject;

        controller.statusText = Label("Status", canvas, "", new Vector2(750,90), 25);
        var statusRect = controller.statusText.rectTransform;
        statusRect.anchorMin = statusRect.anchorMax = new Vector2(0,0);
        statusRect.pivot = new Vector2(0,0); statusRect.anchoredPosition = new Vector2(60,35);
        controller.statusText.alignment = TextAnchor.MiddleLeft;

        var settingsOverlay = Overlay("SettingsPanel", canvas);
        controller.settingsPanel = settingsOverlay.gameObject;
        var settingsBox = Box("SettingsCard", settingsOverlay, new Vector2(860,680));
        Position(Label("Title",settingsBox,"Configuración",new Vector2(740,80),46).rectTransform,0,245);
        Position(Label("MasterLabel",settingsBox,"Volumen general",new Vector2(670,55),30).rectTransform,0,145);
        var sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderObject.name = "MasterVolume"; sliderObject.transform.SetParent(settingsBox,false);
        var sliderRect=(RectTransform)sliderObject.transform; sliderRect.sizeDelta=new Vector2(540,35); Position(sliderRect,-40,85);
        var slider=sliderObject.GetComponent<Slider>(); slider.minValue=0;slider.maxValue=1;
        var percent=Label("VolumePercent",settingsBox,"100%",new Vector2(110,45),28); Position(percent.rectTransform,310,85);
        var toggleObject=DefaultControls.CreateToggle(new DefaultControls.Resources());
        toggleObject.name="Fullscreen";toggleObject.transform.SetParent(settingsBox,false);
        var toggleRect=(RectTransform)toggleObject.transform;toggleRect.sizeDelta=new Vector2(650,55);Position(toggleRect,0,-10);
        var toggle=toggleObject.GetComponent<Toggle>();
        toggle.GetComponentInChildren<Text>().text="Pantalla completa";
        Position(Label("ResolutionLabel",settingsBox,"Resolución",new Vector2(650,55),30).rectTransform,0,-95);
        var dropdownObject=DefaultControls.CreateDropdown(new DefaultControls.Resources());
        dropdownObject.name="Resolution";dropdownObject.transform.SetParent(settingsBox,false);
        var dropdownRect=(RectTransform)dropdownObject.transform;dropdownRect.sizeDelta=new Vector2(540,55);Position(dropdownRect,0,-155);
        var dropdown=dropdownObject.GetComponent<Dropdown>(); dropdown.options.Clear();
        controller.settingsBackButton=Button("Back",settingsBox,"Volver",new Vector2(260,65));Position(controller.settingsBackButton.transform as RectTransform,0,-260);
        var settings=settingsOverlay.gameObject.AddComponent<MenuSettings>();
        settings.masterVolume=slider;settings.volumeLabel=percent;settings.fullscreen=toggle;settings.resolution=dropdown;controller.volumeSlider=slider;
        foreach(var graphic in settingsBox.GetComponentsInChildren<Image>(true))
            if (graphic.gameObject != settingsBox.gameObject) graphic.color = graphic.name.Contains("Background") ? new Color(.12f,.27f,.24f) : Gold;
        foreach(var text in settingsBox.GetComponentsInChildren<Text>(true))
        { text.font=Font; text.color=Beige; if(text.fontSize<24)text.fontSize=26; }
        // Dropdown uses a light background; keep its text legible.
        foreach(var text in dropdown.GetComponentsInChildren<Text>(true)) text.color=Green;
        controller.settingsBackButton.GetComponent<Image>().color=Green;

        var confirmationOverlay=Overlay("ConfirmationPanel",canvas);
        controller.confirmationPanel=confirmationOverlay.gameObject;
        var confirmationBox=Box("ConfirmationCard",confirmationOverlay,new Vector2(900,380));
        controller.confirmationText=Label("Message",confirmationBox,"",new Vector2(790,180),33);
        Position(controller.confirmationText.rectTransform,0,65);
        controller.confirmButton=Button("Confirm",confirmationBox,"Confirmar",new Vector2(300,70));Position(controller.confirmButton.transform as RectTransform,-185,-100);
        controller.cancelButton=Button("Cancel",confirmationBox,"Cancelar",new Vector2(300,70));Position(controller.cancelButton.transform as RectTransform,185,-100);
        settingsOverlay.gameObject.SetActive(false);confirmationOverlay.gameObject.SetActive(false);

        MainMenuHoverSetup.Configure(controller);
        EditorSceneManager.SaveScene(scene,GameLaunch.MenuScenePath);
        var previous=EditorBuildSettings.scenes.Where(s=>s.path!=GameLaunch.MenuScenePath && s.path!=GameLaunch.HubScenePath);
        EditorBuildSettings.scenes=new[] {new EditorBuildSettingsScene(GameLaunch.MenuScenePath,true),new EditorBuildSettingsScene(GameLaunch.HubScenePath,true)}.Concat(previous).ToArray();
        AssetDatabase.SaveAssets();
    }

    static Button Hotspot(string name,RectTransform parent,float top)
    {
        var rect=Rect(name,parent);
        rect.anchorMin=new Vector2(78f/1659f,1f-(top+73)/948f);
        rect.anchorMax=new Vector2(538f/1659f,1f-top/948f);
        rect.offsetMin=rect.offsetMax=Vector2.zero;
        var image=rect.gameObject.AddComponent<Image>();image.color=Color.clear;
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
        button.transition=Selectable.Transition.None;
        return button;
    }
    static RectTransform Rect(string name,Transform parent)
    { var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);return (RectTransform)obj.transform; }
    static void Stretch(RectTransform rect)
    { rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero; }
    static void Position(RectTransform rect,float x,float y)
    {rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(x,y);}
    static RectTransform Overlay(string name,Transform parent)
    {var rect=Rect(name,parent);Stretch(rect);rect.gameObject.AddComponent<Image>().color=new Color(0,.08f,.07f,.86f);return rect;}
    static RectTransform Box(string name,Transform parent,Vector2 size)
    {
        var rect=Rect(name,parent);rect.sizeDelta=size;
        rect.gameObject.AddComponent<Image>().color=Green;
        var outline=rect.gameObject.AddComponent<Outline>();outline.effectColor=Gold;outline.effectDistance=new Vector2(2,-2);return rect;
    }
    static Text Label(string name,Transform parent,string value,Vector2 size,int fontSize)
    {
        var rect=Rect(name,parent);rect.sizeDelta=size;
        var text=rect.gameObject.AddComponent<Text>();text.font=Font;text.text=value;text.fontSize=fontSize;text.color=Beige;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
    }
    static Button Button(string name,Transform parent,string label,Vector2 size)
    {
        var rect=Box(name,parent,size);var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();
        var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.9f,.6f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.65f,.65f,.4f);button.colors=colors;
        var text=Label("Label",rect,label,size,30);Stretch(text.rectTransform);return button;
    }
}
