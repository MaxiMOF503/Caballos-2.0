using UnityEngine;
using UnityEngine.SceneManagement;

// Se actualiza después de la cámara y de las interfaces, incluso con el juego pausado.
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class CursorCaballos : MonoBehaviour
{
    internal enum Estado { Normal, Interactivo, Oculto }

    internal static readonly Vector2 Hotspot = new(2, 2);
    static CursorCaballos instancia;

    Texture2D normal;
    Texture2D interactivo;
    Texture2D texturaAplicada;
    Interactor interactor;
    UIManager ui;
    CarreraUI carrera;
    bool cursorAplicado;
    bool tieneFoco;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Crear()
    {
        if (instancia == null)
            new GameObject("CursorCaballos").AddComponent<CursorCaballos>();
    }

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
        normal = Resources.Load<Texture2D>("Cursor/FlechaBronce");
        interactivo = Resources.Load<Texture2D>("Cursor/FlechaBronceInteractiva");
        if (normal == null || interactivo == null)
            Debug.LogError("Faltan las texturas del cursor Caballos en Resources/Cursor.");

        tieneFoco = Application.isFocused;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        interactor = FindFirstObjectByType<Interactor>();
        ui = UIManager.instance;
        carrera = ui != null && ui.viewCarrera != null
            ? ui.viewCarrera.GetComponentInChildren<CarreraUI>(true)
            : null;
        cursorAplicado = false;
    }

    void LateUpdate()
    {
        if (instancia != this || !tieneFoco) return;
        Aplicar(ResolverEstado(Input.mousePosition, new Vector2(Screen.width, Screen.height)));
    }

    internal Estado ResolverEstado(Vector2 posicion, Vector2 pantalla)
    {
        // Fuera de la ventana, el puntero vuelve a su aspecto visible habitual.
        if (!tieneFoco || !(posicion.x >= 0 && posicion.y >= 0 &&
            posicion.x < pantalla.x && posicion.y < pantalla.y))
            return Estado.Normal;

        if (MenuPausa.Activa) return Estado.Normal;

        if (carrera != null && carrera.isActiveAndEnabled && carrera.EnCurso &&
            !ControlVisible(carrera.botonIniciar) && !ControlVisible(carrera.botonVolver))
            return Estado.Oculto;

        var juego = GameManager.Instancia;
        if (MenuPausa.BloqueaEntrada || juego == null || juego.estadoActual != GameState.Hub ||
            (ui != null && ui.HayInterfazAbierta) || interactor == null || !interactor.isActiveAndEnabled)
            return Estado.Normal;

        return interactor.BuscarInteractuable(posicion) != null ? Estado.Interactivo : Estado.Normal;
    }

    static bool ControlVisible(GameObject control) => control != null && control.activeInHierarchy;

    void Aplicar(Estado estado)
    {
        // Las validaciones batch nunca modifican el puntero de la sesión de Windows.
        if (Application.isBatchMode) return;

        var textura = estado == Estado.Interactivo && interactivo != null ? interactivo : normal;
        if (!cursorAplicado || texturaAplicada != textura)
        {
            Cursor.SetCursor(textura, textura != null ? Hotspot : Vector2.zero, CursorMode.Auto);
            texturaAplicada = textura;
            cursorAplicado = true;
        }

        bool visible = estado != Estado.Oculto;
        if (Cursor.visible != visible) Cursor.visible = visible;
    }

    void OnApplicationFocus(bool foco)
    {
        tieneFoco = foco;
        if (!foco) RestaurarCursor();
        cursorAplicado = false;
    }

    void OnDisable()
    {
        if (instancia != this) return;
        RestaurarCursor();
        cursorAplicado = false;
    }

    void OnDestroy()
    {
        if (instancia != this) return;
        SceneManager.sceneLoaded -= AlCargarEscena;
        instancia = null;
    }

    void RestaurarCursor()
    {
        if (instancia != this || Application.isBatchMode) return;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.visible = true;
    }
}
