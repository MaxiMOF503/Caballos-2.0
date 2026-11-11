using UnityEngine;
using UnityEngine.InputSystem;

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
    [Tooltip("0 aplica inmediatamente el ángulo que corresponde a la posición del mouse.")]
    [Min(0f)] public float rotationSmoothTime;

    [Header("Zoom")]
    public float zoom = 60f;
    public float zoomMin = 30f;
    public float zoomMax = 80f;
    public float velocidadZoom = 200f;

    enum Seguimiento { Recalibrar, EsperarCentro, Activo }

    Camera cam;
    Vector3 posicionInicial;
    Quaternion rotacionCentral;
    float rotacionX, rotacionY;
    float smoothYaw, smoothPitch;
    float yawVelocity, pitchVelocity;
    Vector2 pantallaAnterior, posicionReferencia, angulosReferencia;
    bool tieneFoco, centrarAlRetomar;
    Seguimiento seguimiento;
    int primerFrameDeLectura, frameDeCentrado;
    float limiteDeCentrado;
    const float EsperaDeCentrado = .25f;

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
        bool bloqueada = InterfazActiva;
        ActualizarCursorHub(tieneFoco && !bloqueada);
        if (bloqueada || !tieneFoco)
        {
            centrarAlRetomar |= bloqueada;
            InvalidarSeguimiento();
            return;
        }

        if (Time.frameCount < primerFrameDeLectura || MenuPausa.BloqueaEntrada || Input.GetMouseButtonDown(0))
            return;

        var pantalla = new Vector2(Screen.width, Screen.height);
        Vector2 posicion = Input.mousePosition;
        if (centrarAlRetomar && PosicionValida(posicion, pantalla))
        {
            centrarAlRetomar = false;
            var mouse = Mouse.current;
            if (!Application.isBatchMode && Application.platform == RuntimePlatform.WindowsPlayer &&
                mouse != null && mouse.native)
            {
                PrepararCentrado(pantalla, Time.frameCount, Time.unscaledTime);
                mouse.WarpCursorPosition(pantalla * .5f);
                return;
            }
        }

        ProcesarPosicionHub(posicion, pantalla, Time.unscaledDeltaTime, Time.frameCount, Time.unscaledTime);
    }

    void PrepararCentrado(Vector2 pantalla, int frame, float tiempo)
    {
        pantallaAnterior = pantalla;
        seguimiento = Seguimiento.EsperarCentro;
        frameDeCentrado = frame;
        limiteDeCentrado = tiempo + EsperaDeCentrado;
    }

    void ProcesarPosicionHub(Vector2 posicion, Vector2 pantalla, float deltaTime, int frame, float tiempo)
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

        if (seguimiento == Seguimiento.EsperarCentro)
        {
            // El comando del mouse se refleja en una actualización de entrada posterior.
            bool centrado = (posicion - pantalla * .5f).sqrMagnitude <= 4f;
            if (frame <= frameDeCentrado || (!centrado && tiempo < limiteDeCentrado)) return;
        }

        var normalizada = new Vector2(Mathf.Min(1, posicion.x / (pantalla.x - 1)),
            Mathf.Min(1, posicion.y / (pantalla.y - 1)));
        if (seguimiento != Seguimiento.Activo)
        {
            posicionReferencia = normalizada;
            angulosReferencia = new Vector2(smoothYaw, smoothPitch);
            yawVelocity = pitchVelocity = 0;
            seguimiento = Seguimiento.Activo;
            return;
        }

        float yaw = MapearEje(normalizada.x, ref posicionReferencia.x, ref angulosReferencia.x, minYaw, maxYaw);
        float pitch = MapearEje(normalizada.y, ref posicionReferencia.y, ref angulosReferencia.y, maxPitch, minPitch);
        if (rotationSmoothTime == 0)
        {
            smoothYaw = yaw;
            smoothPitch = pitch;
        }
        else if (deltaTime > 0)
        {
            smoothYaw = Mathf.SmoothDamp(smoothYaw, yaw, ref yawVelocity, rotationSmoothTime,
                Mathf.Infinity, deltaTime);
            smoothPitch = Mathf.SmoothDamp(smoothPitch, pitch, ref pitchVelocity, rotationSmoothTime,
                Mathf.Infinity, deltaTime);
        }
        smoothYaw = Mathf.Clamp(smoothYaw, minYaw, maxYaw);
        smoothPitch = Mathf.Clamp(smoothPitch, minPitch, maxPitch);
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

        // Si la recalibración cayó justo en el borde, el primer movimiento
        // hacia dentro recupera un tramo utilizable a ambos lados.
        if ((referencia == 0 || referencia == 1) && posicion > 0 && posicion < 1)
        {
            referencia = posicion;
            angulo = objetivo;
        }
        return objetivo;
    }

    void InvalidarSeguimiento()
    {
        seguimiento = Seguimiento.Recalibrar;
        yawVelocity = pitchVelocity = 0;
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
        if (!Cursor.visible) Cursor.visible = true;
    }

    void OnValidate()
    {
        minYaw = Mathf.Clamp(minYaw, -90f, 0f);
        maxYaw = Mathf.Clamp(maxYaw, 0f, 90f);
        minPitch = Mathf.Clamp(minPitch, -89f, 0f);
        maxPitch = Mathf.Clamp(maxPitch, 0f, 89f);
        rotationSmoothTime = Mathf.Max(0, rotationSmoothTime);
    }
}
