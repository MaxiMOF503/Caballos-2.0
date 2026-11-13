using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
    public CanvasGroup menuButtons;
    public Button newGameButton, loadGameButton, settingsButton, exitButton;
    public GameObject settingsPanel, confirmationPanel;
    public Text confirmationText, statusText;
    public Button confirmButton, cancelButton, settingsBackButton;
    public Slider volumeSlider;
    public bool IsBusy { get; private set; }
    Action confirmation;

    void Awake()
    {
        newGameButton.onClick.AddListener(NewGame);
        loadGameButton.onClick.AddListener(LoadGame);
        settingsButton.onClick.AddListener(OpenSettings);
        exitButton.onClick.AddListener(AskExit);
        confirmButton.onClick.AddListener(Confirm);
        cancelButton.onClick.AddListener(CloseConfirmation);
        settingsBackButton.onClick.AddListener(CloseSettings);
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        settingsPanel.SetActive(false);
        confirmationPanel.SetActive(false);
        RefreshLoadState();
        string error = GameLaunch.TakeError();
        if (!string.IsNullOrEmpty(error)) statusText.text = error;
        Select(newGameButton);
    }
    void Update()
    {
        if (IsBusy || !Input.GetKeyDown(KeyCode.Escape)) return;
        if (confirmationPanel.activeSelf) CloseConfirmation();
        else if (settingsPanel.activeSelf) CloseSettings();
    }
    public void RefreshLoadState()
    {
        RefreshLoadAvailability();
        statusText.text = loadGameButton.interactable ? "" : "No se encontró ninguna partida guardada.";
    }
    public void NewGame()
    {
        if (IsBusy || settingsPanel.activeSelf || confirmationPanel.activeSelf) return;
        if (File.Exists(SaveSystem.RutaArchivo))
            Ask("¿Querés comenzar una nueva partida? El progreso guardado podría reemplazarse.", () => Launch(GameLaunchMode.NewGame), "Confirmar");
        else Launch(GameLaunchMode.NewGame);
    }
    public void LoadGame()
    {
        if (IsBusy || settingsPanel.activeSelf || confirmationPanel.activeSelf) return;
        if (!SaveSystem.PuedeCargar(out string error)) { statusText.text = error; RefreshLoadAvailability(); return; }
        Launch(GameLaunchMode.LoadGame);
    }
    void RefreshLoadAvailability()
    {
        loadGameButton.interactable = File.Exists(SaveSystem.RutaArchivo);
        var controls = loadGameButton.interactable
            ? new[] {newGameButton,loadGameButton,settingsButton,exitButton}
            : new[] {newGameButton,settingsButton,exitButton};
        for (int i=0;i<controls.Length;i++)
            controls[i].navigation = new Navigation {mode=Navigation.Mode.Explicit,selectOnUp=controls[(i+controls.Length-1)%controls.Length],selectOnDown=controls[(i+1)%controls.Length]};
    }
    void Launch(GameLaunchMode mode)
    {
        if (IsBusy) return;
        if (!Application.CanStreamedLevelBeLoaded(GameLaunch.HubScenePath))
        { statusText.text = "No se pudo encontrar la escena del hub. La partida no se modificó."; return; }
        IsBusy = true;
        menuButtons.interactable = false;
        menuButtons.blocksRaycasts = false;
        MenuSceneTransition.Begin(mode, message =>
        {
            if (this == null) return;
            IsBusy = false; menuButtons.interactable = true; menuButtons.blocksRaycasts = true;
            statusText.text = message; Select(newGameButton);
        });
    }
    public void OpenSettings()
    {
        if (IsBusy || confirmationPanel.activeSelf) return;
        menuButtons.interactable = false;
        settingsPanel.SetActive(true);
        Select(volumeSlider);
    }
    public void CloseSettings()
    {
        settingsPanel.SetActive(false); menuButtons.interactable = true; Select(settingsButton);
    }
    public void AskExit()
    {
        if (IsBusy || settingsPanel.activeSelf || confirmationPanel.activeSelf) return;
        Ask("¿Seguro que querés salir del juego?", () =>
        {
            // Editor: never stop somebody's editor session or close the editor.
            if (Application.isEditor) statusText.text = "Salir cierra el juego en una build.";
            else Application.Quit();
        }, "Salir");
    }
    void Ask(string message, Action action, string confirmLabel)
    {
        confirmation = action; confirmationText.text = message;
        confirmButton.GetComponentInChildren<Text>().text = confirmLabel;
        menuButtons.interactable = false; confirmationPanel.SetActive(true); Select(cancelButton);
    }
    public void Confirm()
    {
        if (IsBusy || !confirmationPanel.activeSelf) return;
        var action = confirmation;
        CloseConfirmation();
        action?.Invoke();
    }
    public void CloseConfirmation()
    {
        confirmation = null; confirmationPanel.SetActive(false);
        menuButtons.interactable = true; Select(newGameButton);
    }
    static void Select(Selectable control)
    {
        if (EventSystem.current != null && control != null)
            EventSystem.current.SetSelectedGameObject(control.gameObject);
    }
}

// Created only during a requested scene transition. No persistent game state.
public sealed class MenuSceneTransition : MonoBehaviour
{
    public static void Begin(GameLaunchMode mode, Action<string> failed)
    {
        var host = new GameObject("MenuTransition");
        DontDestroyOnLoad(host);
        host.AddComponent<MenuSceneTransition>().StartCoroutine(Load(mode, failed, host));
    }
    static IEnumerator Load(GameLaunchMode mode, Action<string> failed, GameObject host)
    {
        var canvas = host.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
        host.AddComponent<GraphicRaycaster>();
        var imageObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(host.transform, false);
        var rect = (RectTransform)imageObject.transform;
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var image = imageObject.GetComponent<Image>(); image.color = new Color(0,0,0,0);
        yield return Fade(image, 0, 1);
        GameLaunch.Request(mode);
        AsyncOperation operation = null;
        string error = null;
        try { operation = SceneManager.LoadSceneAsync(GameLaunch.HubScenePath); }
        catch (Exception ex) { error = "No se pudo abrir el hub: " + ex.Message; }
        if (operation == null)
        {
            GameLaunch.Fail(error ?? "No se pudo abrir el hub.");
            yield return Fade(image, 1, 0);
            failed?.Invoke(GameLaunch.TakeError());
            Destroy(host); yield break;
        }
        while (!operation.isDone) yield return null;
        yield return null; // Let GameManager.Start consume the one-use launch intent.
        yield return Fade(image, 1, 0);
        Destroy(host);
    }
    static IEnumerator Fade(Image image, float from, float to)
    {
        for (float t = 0; t < .25f; t += Time.unscaledDeltaTime)
        { image.color = new Color(0,0,0,Mathf.Lerp(from,to,t/.25f)); yield return null; }
        image.color = new Color(0,0,0,to);
    }
}
