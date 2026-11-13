#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class CamaraAntesItems : MonoBehaviour
{
    public static CamaraAntesItems instancia;

    [Header("Movimiento relativo fuera del Hub")]
    public float sensibilidad = 100f;

    [Header("Navegacion fija del hub")]
    public bool fixedHubNavigation;
    public float centralYawOffset;
    [Range(-90f, 0f)] public float minYaw = -90f;
    [Range(0f, 90f)] public float maxYaw = 90f;
    [Range(-89f, 0f)] public float minPitch = -25f;
    [Range(0f, 89f)] public float maxPitch = 30f;
    [Tooltip("Único límite de velocidad angular. El objetivo alcanza ambos extremos de la ventana.")]
    [Min(1f)] public float velocidadMaximaGiro = 360f;

    [Header("Zoom")]
    public float zoom = 60f;
    public float zoomMin = 30f;
    public float zoomMax = 80f;
    public float velocidadZoom = 200f;

    Camera cam;
    Vector3 posicionInicial;
    Quaternion rotacionCentral;
    float rotacionX, rotacionY;
    float smoothYaw, smoothPitch;
    Vector2 pantallaAnterior, posicionReferencia, angulosReferencia;
    bool tieneFoco, tieneReferencia;
    int primerFrameDeLectura;

    bool InterfazActiva => MenuPausa.Activa ||
        (GameManager.Instancia != null && GameManager.Instancia.estadoActual != GameState.Hub) ||
        (UIManager.instance != null && UIManager.instance.HayInterfazAbierta);

    bool PuedeNavegarHub => tieneFoco && !InterfazActiva;

    void Awake()
    {
        instancia = this;
        cam = GetComponent<Camera>();
        zoom = cam.fieldOfView;
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

        // Las acciones de UI y el raycast del clic ya se resolvieron en Update.
        bool bloqueada = InterfazActiva || MenuPausa.BloqueaEntrada;
        ActualizarCursorHub(tieneFoco && !bloqueada);
        if (bloqueada || !tieneFoco)
        {
            InvalidarSeguimiento();
            return;
        }

        if (Time.frameCount < primerFrameDeLectura || Input.GetMouseButtonDown(0))
            return;

        var pantalla = new Vector2(Screen.width, Screen.height);
        Vector2 posicion = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector2)Input.mousePosition;
        ProcesarPosicionHub(posicion, pantalla, Time.unscaledDeltaTime);
    }

    void ProcesarPosicionHub(Vector2 posicion, Vector2 pantalla, float deltaTime)
    {
        if (pantalla != pantallaAnterior)
        {
            pantallaAnterior = pantalla;
            InvalidarSeguimiento();
        }
        if (!PosicionValida(posicion, pantalla))
        {
            InvalidarSeguimiento();
            return;
        }

        var normalizada = new Vector2(Mathf.Min(1, posicion.x / (pantalla.x - 1)),
            Mathf.Min(1, posicion.y / (pantalla.y - 1)));
        if (!tieneReferencia)
        {
            posicionReferencia = normalizada;
            angulosReferencia = new Vector2(smoothYaw, smoothPitch);
            tieneReferencia = true;
            return;
        }

        float yaw = MapearEje(normalizada.x, ref posicionReferencia.x, ref angulosReferencia.x, minYaw, maxYaw);
        float pitch = MapearEje(normalizada.y, ref posicionReferencia.y, ref angulosReferencia.y, maxPitch, minPitch);
        // Un solo seguimiento con velocidad limitada; sin SmoothDamp ni recentrado del puntero.
        float pasoMaximo = velocidadMaximaGiro * Mathf.Clamp(deltaTime, 0, 1f / 30f);
        smoothYaw = Mathf.Clamp(Mathf.MoveTowards(smoothYaw, yaw, pasoMaximo), minYaw, maxYaw);
        smoothPitch = Mathf.Clamp(Mathf.MoveTowards(smoothPitch, pitch, pasoMaximo), minPitch, maxPitch);
        transform.rotation = rotacionCentral * Quaternion.Euler(smoothPitch, smoothYaw, 0);
    }

    static bool PosicionValida(Vector2 posicion, Vector2 pantalla) =>
        pantalla.x > 1 && pantalla.y > 1 && posicion.x >= 0 && posicion.y >= 0 &&
        posicion.x < pantalla.x && posicion.y < pantalla.y;

    static float MapearEje(float posicion, ref float referencia, ref float angulo,
        float limiteInferior, float limiteSuperior)
    {
        if (posicion == referencia) return angulo;

        float objetivo = posicion < referencia
            ? Mathf.Lerp(angulo, limiteInferior, (referencia - posicion) / referencia)
            : Mathf.Lerp(angulo, limiteSuperior, (posicion - referencia) / (1 - referencia));

        // Solo una referencia exactamente en el borde necesita trasladarse al primer
        // punto distinto. No hay zona muerta: el recorrido hacia fuera sigue siendo válido.
        if (referencia <= 0 || referencia >= 1)
        {
            referencia = posicion;
            angulo = objetivo;
        }
        return objetivo;
    }

    void InvalidarSeguimiento()
    {
        tieneReferencia = false;
    }

    void OnApplicationFocus(bool foco)
    {
        tieneFoco = foco;
        if (!fixedHubNavigation) return;
        primerFrameDeLectura = Time.frameCount + 1;
        InvalidarSeguimiento();
        ActualizarCursorHub(PuedeNavegarHub);
    }

    void OnDisable()
    {
        InvalidarSeguimiento();
        if (fixedHubNavigation) ActualizarCursorHub(false);
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
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
        velocidadMaximaGiro = Mathf.Max(1, velocidadMaximaGiro);
    }
}

#endif
