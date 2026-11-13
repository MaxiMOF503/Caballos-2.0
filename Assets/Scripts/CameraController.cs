using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class CameraController : MonoBehaviour
{
    public static CameraController instancia;

    [Header("Movimiento relativo fuera del Hub")]
    public float sensibilidad = 100f;

    [Header("Navegacion fija del hub")]
    public bool fixedHubNavigation;
    public float centralYawOffset;
    [Range(-90f, 0f)] public float minYaw = -90f;
    [Range(0f, 90f)] public float maxYaw = 90f;
    [Range(-89f, 0f)] public float minPitch = -25f;
    [Range(0f, 89f)] public float maxPitch = 30f;

    [Header("Zoom fuera del Hub")]
    public float zoom = 60f;
    public float zoomMin = 30f;
    public float zoomMax = 80f;
    public float velocidadZoom = 200f;

    enum Seguimiento { Suspendido, Sincronizando, Activo }
    Seguimiento seguimiento;
    Camera cam;
    Mouse mouseAnterior;
    Vector3 posicionInicial;
    Quaternion rotacionCentral;
    Vector2 angulosHub, cursorSolicitado, cursorAntesDeSincronizar, ultimaPosicion;
    Rect areaAnterior;
    double muestraAntesDeSincronizar;
    int frameSolicitud;
    bool tieneFoco;
    float rotacionX, rotacionY;

    bool InterfazActiva => MenuPausa.BloqueaEntrada ||
        (GameManager.Instancia != null && GameManager.Instancia.estadoActual != GameState.Hub) ||
        (UIManager.instance != null && UIManager.instance.HayInterfazAbierta);

    void Awake()
    {
        instancia = this;
        cam = GetComponent<Camera>();
        zoom = fixedHubNavigation ? 60f : cam.fieldOfView;
        if (fixedHubNavigation) cam.fieldOfView = 60f;
        posicionInicial = transform.position;
        rotacionCentral = Quaternion.AngleAxis(centralYawOffset, Vector3.up) * transform.rotation;
    }

    void OnEnable() => OnApplicationFocus(Application.isFocused);

    void Update()
    {
        if (fixedHubNavigation) return;
        rotacionX = Mathf.Clamp(rotacionX - Input.GetAxis("Mouse Y") * sensibilidad * Time.deltaTime, -60f, 60f);
        rotacionY += Input.GetAxis("Mouse X") * sensibilidad * Time.deltaTime;
        transform.rotation = Quaternion.Euler(rotacionX, rotacionY, 0);
        zoom = Mathf.Clamp(zoom - Input.GetAxis("Mouse ScrollWheel") * velocidadZoom * Time.deltaTime,
            zoomMin, zoomMax);
        cam.fieldOfView = zoom;
    }

    void LateUpdate()
    {
        if (!fixedHubNavigation) return;
        transform.position = posicionInicial;
        var mouse = Mouse.current;
        bool focoJuego = tieneFoco && VentanaDeJuegoActiva();
        bool bloqueada = InterfazActiva;
        ActualizarCursorHub(focoJuego && !bloqueada);
        if (mouse != mouseAnterior) { Suspender(); mouseAnterior = mouse; }
        if (mouse == null) { Suspender(); return; }

        // Única entrada del Hub: posición absoluta del mismo dispositivo del Input System.
        Vector2? sincronizar = ProcesarMuestraHub(mouse.position.ReadValue(), cam.pixelRect,
            bloqueada, focoJuego, mouse.lastUpdateTime, Time.frameCount);
        if (sincronizar.HasValue && !Application.isBatchMode)
            mouse.WarpCursorPosition(sincronizar.Value);
    }

    Vector2? ProcesarMuestraHub(Vector2 posicion, Rect area, bool bloqueada, bool foco,
        double instanteMuestra, int frame)
    {
        if (area != areaAnterior) { areaAnterior = area; Suspender(); }
        if (bloqueada || !foco || !PosicionValida(posicion, area))
        {
            Suspender();
            return null;
        }
        if (seguimiento == Seguimiento.Suspendido)
        {
            // Una única solicitud al recuperar control. Nunca recentrar durante navegación.
            cursorSolicitado = CursorDesdeAngulos(angulosHub, area);
            cursorAntesDeSincronizar = posicion;
            muestraAntesDeSincronizar = instanteMuestra;
            frameSolicitud = frame;
            seguimiento = Seguimiento.Sincronizando;
            return cursorSolicitado;
        }
        if (seguimiento == Seguimiento.Sincronizando)
        {
            // Warp actualiza Pointer.position en el siguiente input update, no inmediatamente.
            if (frame <= frameSolicitud) return null;
            if ((posicion - cursorSolicitado).sqrMagnitude <= 1f)
            {
                seguimiento = Seguimiento.Activo;
                ultimaPosicion = posicion;
                return null; // La confirmación conserva exactamente la orientación anterior.
            }
            // Si ya hubo movimiento posterior al warp, su posición final puede superar el destino.
            // Ignorar la primera actualización evita consumir eventos previos al cambio de estado.
            if (frame <= frameSolicitud + 1 || instanteMuestra <= muestraAntesDeSincronizar ||
                (posicion - cursorAntesDeSincronizar).sqrMagnitude < .0001f) return null;
            seguimiento = Seguimiento.Activo;
        }

        // Conservar también el ángulo exacto si Windows redondeó el warp a un píxel entero.
        if ((posicion - ultimaPosicion).sqrMagnitude < .0001f) return null;
        ultimaPosicion = posicion;
        angulosHub = AngulosDesdeCursor(posicion, area);
        transform.rotation = rotacionCentral * Quaternion.Euler(angulosHub.y, angulosHub.x, 0);
        return null;
    }

    Vector2 AngulosDesdeCursor(Vector2 posicion, Rect area)
    {
        float x = Mathf.Clamp01((posicion.x - area.xMin) / (area.width - 1));
        float y = Mathf.Clamp01((posicion.y - area.yMin) / (area.height - 1));
        float pitch = y <= .5f ? Mathf.Lerp(maxPitch, 0, y * 2) : Mathf.Lerp(0, minPitch, (y - .5f) * 2);
        return new Vector2(Mathf.Lerp(minYaw, maxYaw, x), pitch);
    }

    Vector2 CursorDesdeAngulos(Vector2 angulos, Rect area)
    {
        float x = Mathf.InverseLerp(minYaw, maxYaw, Mathf.Clamp(angulos.x, minYaw, maxYaw));
        float pitch = Mathf.Clamp(angulos.y, minPitch, maxPitch);
        float y = pitch >= 0 ? Mathf.Lerp(.5f, 0, Mathf.InverseLerp(0, maxPitch, pitch))
            : Mathf.Lerp(.5f, 1, Mathf.InverseLerp(0, minPitch, pitch));
        return new Vector2(area.xMin + x * (area.width - 1), area.yMin + y * (area.height - 1));
    }

    static bool PosicionValida(Vector2 posicion, Rect area) =>
        area.width > 1 && area.height > 1 && float.IsFinite(posicion.x) && float.IsFinite(posicion.y) &&
        posicion.x >= area.xMin && posicion.y >= area.yMin &&
        posicion.x < area.xMax && posicion.y < area.yMax;

    void Suspender() => seguimiento = Seguimiento.Suspendido;

    void OnApplicationFocus(bool foco)
    {
        tieneFoco = foco;
        Suspender();
        if (fixedHubNavigation && !foco) ActualizarCursorHub(false);
    }

    void OnDisable()
    {
        Suspender();
        if (fixedHubNavigation) ActualizarCursorHub(false);
    }

    void OnDestroy() { if (instancia == this) instancia = null; }

    static bool VentanaDeJuegoActiva()
    {
#if UNITY_EDITOR
        if (!Application.isBatchMode)
        {
            var ventana = UnityEditor.EditorWindow.focusedWindow;
            return ventana != null && ventana.GetType().Name == "GameView";
        }
#endif
        return true;
    }

    static void ActualizarCursorHub(bool navegar)
    {
        if (Application.isBatchMode) return;
        bool compatible = Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.LinuxPlayer;
        var modo = navegar && compatible ? CursorLockMode.Confined : CursorLockMode.None;
        if (Cursor.lockState != modo) Cursor.lockState = modo;
    }

    void OnValidate()
    {
        minYaw = Mathf.Clamp(minYaw, -90f, 0f);
        maxYaw = Mathf.Clamp(maxYaw, 0f, 90f);
        minPitch = Mathf.Clamp(minPitch, -89f, 0f);
        maxPitch = Mathf.Clamp(maxPitch, 0f, 89f);
    }
}
