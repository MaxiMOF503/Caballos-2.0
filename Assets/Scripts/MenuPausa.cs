using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(UIManager))]
[DefaultExecutionOrder(-100)]
public sealed class MenuPausa : MonoBehaviour
{
    [SerializeField] Sprite madera;
    [SerializeField] Sprite marco;
    [SerializeField] TMP_FontAsset fuente;

    enum Destino { Ninguno, MenuPrincipal, Salir }

    static MenuPausa actual;
    internal static bool Activa => actual != null && actual.abierta;
    internal static bool BloqueaEntrada => Activa ||
        (actual != null && actual.ultimoFrameEntrada == Time.frameCount);

    UIManager ui;
    MenuPausaVista vista;
    CanvasGroup bloqueoJuego;
    GameObject focoAnterior;
    float escalaAnterior;
    bool abierta;
    bool saliendo;
    int ultimoFrameEntrada = -1;
    Destino destino;

    bool CarreraEnCurso => ui.viewCarrera.activeInHierarchy &&
        ui.viewCarrera.GetComponentInChildren<CarreraUI>()?.EnCurso == true;

    void Awake()
    {
        actual = this;
        ui = GetComponent<UIManager>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ProcesarEscape();
    }

    internal void ProcesarEscape()
    {
        if (saliendo || GameManager.Instancia?.Derrotado == true) return;
        if (abierta)
        {
            if (destino != Destino.Ninguno) CancelarSalida();
            else Cerrar();
            return;
        }

        var juego = GameManager.Instancia;
        if (juego == null || juego.caballosJugador.Count == 0 || ui.ConsumirEscape()) return;

        if (vista == null)
        {
            vista = new MenuPausaVista(transform, madera, marco, fuente, () => Cerrar(),
                Guardar, () => PreguntarSalida(Destino.MenuPrincipal), () => PreguntarSalida(Destino.Salir),
                CancelarSalida, ConfirmarSalida);
            bloqueoJuego = ui.viewCarrera.GetComponentInParent<Canvas>(true).gameObject.AddComponent<CanvasGroup>();
        }

        focoAnterior = EventSystem.current?.currentSelectedGameObject;
        EventSystem.current?.SetSelectedGameObject(null);
        escalaAnterior = Time.timeScale;
        abierta = true;
        Time.timeScale = 0;
        bloqueoJuego.interactable = false;
        bloqueoJuego.blocksRaycasts = false;
        vista.Mostrar(CarreraEnCurso);
    }

    void Cerrar(bool restaurarFoco = true)
    {
        if (!abierta) return;

        abierta = false;
        ultimoFrameEntrada = Time.frameCount;
        destino = Destino.Ninguno;
        if (vista.Raiz != null) vista.Raiz.SetActive(false);
        if (bloqueoJuego != null)
        {
            bloqueoJuego.interactable = true;
            bloqueoJuego.blocksRaycasts = true;
        }
        Time.timeScale = escalaAnterior;
        if (restaurarFoco && focoAnterior != null && focoAnterior.activeInHierarchy)
            EventSystem.current?.SetSelectedGameObject(focoAnterior);
        focoAnterior = null;
    }

    internal void CerrarParaReinicio() => Cerrar(false);

    void Guardar()
    {
        if (CarreraEnCurso) return;
        vista.Estado = GameManager.Instancia.GuardarPartida()
            ? "Partida guardada."
            : "No se pudo guardar. Intentá de nuevo.";
    }

    void PreguntarSalida(Destino elegido)
    {
        destino = elegido;
        bool salir = elegido == Destino.Salir;
        vista.Preguntar(
            salir ? "¿Seguro que querés salir del juego?" : "¿Volver al menú principal?",
            CarreraEnCurso
                ? "La carrera quedará pendiente. Al cargar, se correrá desde el inicio con la misma parrilla y apuesta."
                : "Se guardará tu partida al confirmar.",
            salir ? "Salir" : "Confirmar");
    }

    void CancelarSalida()
    {
        destino = Destino.Ninguno;
        vista.Mostrar(CarreraEnCurso);
    }

    void ConfirmarSalida()
    {
        if (!abierta || saliendo || destino == Destino.Ninguno) return;
        if (destino == Destino.MenuPrincipal && !Application.CanStreamedLevelBeLoaded(GameLaunch.MenuScenePath))
        {
            vista.Detalle = "No se encontró la escena del menú principal.";
            return;
        }

        // El inicio de la carrera ya guarda la parrilla, las semillas y la apuesta.
        // Los avances de la simulación no se guardan ni se liquidan aquí.
        if (CarreraEnCurso ? !SaveSystem.PuedeCargar(out _) : !GameManager.Instancia.GuardarPartida())
        {
            vista.Detalle = "No se pudo conservar la partida. Cancelá para continuar e intentar de nuevo.";
            return;
        }

        var elegido = destino;
        saliendo = true;
        ui.CerrarTodo();
        Cerrar(false);
        Time.timeScale = 1;
        if (elegido == Destino.MenuPrincipal)
        {
            SceneManager.LoadScene(GameLaunch.MenuScenePath);
            return;
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDisable() => Cerrar(false);

    void OnDestroy()
    {
        if (actual == this) actual = null;
    }
}

internal sealed class MenuPausaVista
{
    static readonly Vector2 Medida = new(580, 510);
    static readonly Color Crema = new Color32(255, 234, 201, 255);
    static readonly Color Oro = new Color32(171, 117, 57, 255);

    internal GameObject Raiz { get; }
    internal string Estado { set => estado.text = value; }
    internal string Detalle { set => detalle.text = value; }

    readonly GameObject acciones, confirmacion;
    readonly Button guardar;
    readonly TMP_Text estado, pregunta, detalle, etiquetaConfirmar;

    internal MenuPausaVista(Transform padre, Sprite madera, Sprite marco, TMP_FontAsset fuente,
        UnityEngine.Events.UnityAction continuar, UnityEngine.Events.UnityAction alGuardar,
        UnityEngine.Events.UnityAction alMenu, UnityEngine.Events.UnityAction alSalir,
        UnityEngine.Events.UnityAction cancelar, UnityEngine.Events.UnityAction confirmar)
    {
        Raiz = new GameObject("MenuPausa", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Raiz.transform.SetParent(padre, false);
        var canvas = Raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        var escala = Raiz.GetComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(1600, 900);
        escala.matchWidthOrHeight = .5f;

        var velo = TablonLayout.Surface("Velo", (RectTransform)Raiz.transform,
            new Rect(0, 0, 1, 1), Vector2.one, new Color32(12, 8, 6, 146));
        velo.GetComponent<Image>().raycastTarget = true;
        var panel = new GameObject("Panel", typeof(RectTransform)).GetComponent<RectTransform>();
        panel.SetParent(velo, false);
        panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = Medida;
        TablonLayout.Wood("Fondo", panel, new Rect(0, 0, 580, 510), Medida, madera)
            .GetComponent<RawImage>().color = new Color32(140, 128, 119, 255);
        var borde = TablonLayout.Rect("Marco", panel, new Rect(0, 0, 580, 510), Medida)
            .gameObject.AddComponent<Image>();
        borde.sprite = marco;
        borde.type = Image.Type.Sliced;
        borde.fillCenter = false;
        borde.raycastTarget = false;

        var titulo = Texto("Título", panel, new Rect(60, 36, 460, 70), fuente, "PAUSA", 54);
        titulo.fontStyle = FontStyles.Bold;
        TablonLayout.Surface("Separador", panel, new Rect(122, 113, 336, 2), Medida, Oro);

        var menu = TablonLayout.Rect("Acciones", panel, new Rect(0, 0, 580, 510), Medida);
        acciones = menu.gameObject;
        Boton("Continuar", menu, new Rect(85, 145, 410, 53), madera, fuente, "Continuar", continuar);
        guardar = Boton("Guardar", menu, new Rect(85, 212, 410, 53), madera, fuente, "Guardar partida", alGuardar);
        Boton("MenuPrincipal", menu, new Rect(85, 279, 410, 53), madera, fuente, "Menú principal", alMenu);
        Boton("Salir", menu, new Rect(85, 346, 410, 53), madera, fuente, "Salir del juego", alSalir);
        estado = Texto("Estado", menu, new Rect(58, 412, 464, 58), fuente, "", 22);

        var dialogo = TablonLayout.Rect("Confirmacion", panel, new Rect(0, 0, 580, 510), Medida);
        confirmacion = dialogo.gameObject;
        pregunta = Texto("Pregunta", dialogo, new Rect(54, 142, 472, 70), fuente, "", 33);
        detalle = Texto("Detalle", dialogo, new Rect(68, 215, 444, 109), fuente, "", 23);
        Boton("Cancelar", dialogo, new Rect(63, 355, 215, 53), madera, fuente, "Cancelar", cancelar);
        var aceptar = Boton("Confirmar", dialogo, new Rect(302, 355, 215, 53), madera, fuente, "Confirmar", confirmar);
        etiquetaConfirmar = aceptar.GetComponentInChildren<TMP_Text>();
        Raiz.SetActive(false);
    }

    internal void Mostrar(bool carreraEnCurso)
    {
        confirmacion.SetActive(false);
        acciones.SetActive(true);
        guardar.interactable = !carreraEnCurso;
        Estado = carreraEnCurso ? "Podés guardar cuando termine la carrera." : "";
        Raiz.SetActive(true);
    }

    internal void Preguntar(string titulo, string explicacion, string aceptar)
    {
        pregunta.text = titulo;
        Detalle = explicacion;
        etiquetaConfirmar.text = aceptar;
        acciones.SetActive(false);
        confirmacion.SetActive(true);
    }

    static TMP_Text Texto(string nombre, RectTransform padre, Rect area, TMP_FontAsset fuente,
        string texto, float tamaño) =>
        TablonLayout.Label(nombre, padre, area, Medida, fuente, texto, tamaño, TextAlignmentOptions.Center, Crema);

    static Button Boton(string nombre, RectTransform padre, Rect area, Sprite madera, TMP_FontAsset fuente,
        string texto, UnityEngine.Events.UnityAction accion)
    {
        var borde = TablonLayout.Surface(nombre, padre, area, Medida, Oro);
        var cara = TablonLayout.Wood("Madera", borde,
            new Rect(2, 2, area.width - 4, area.height - 4), area.size, madera).GetComponent<RawImage>();
        var hitbox = borde.GetComponent<Image>();
        hitbox.raycastTarget = true;
        var boton = borde.gameObject.AddComponent<Button>();
        boton.targetGraphic = cara;
        boton.navigation = new Navigation { mode = Navigation.Mode.None };
        var colores = ColorBlock.defaultColorBlock;
        colores.normalColor = new Color32(218, 199, 178, 255);
        colores.selectedColor = colores.normalColor;
        colores.highlightedColor = Color.white;
        colores.pressedColor = new Color32(185, 158, 132, 255);
        colores.disabledColor = new Color32(122, 113, 103, 255);
        boton.colors = colores;
        boton.onClick.AddListener(accion);
        TablonLayout.Label("Etiqueta", borde, new Rect(8, 0, area.width - 16, area.height),
            area.size, fuente, texto, 27, TextAlignmentOptions.Center, Crema);
        return boton;
    }
}
