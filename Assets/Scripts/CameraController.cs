using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController instancia;

    [Header("Movimiento relativo fuera del Hub")]
    public float sensibilidad = 100f;

    [Header("Navegacion fija del hub")]
    [Tooltip("Usar la posicion del cursor dentro de la pantalla para orientar la camara fija.")]
    public bool fixedHubNavigation;
    [Tooltip("Desplazamiento horizontal respecto a la orientacion inicial de la camara.")]
    public float centralYawOffset;
    [Range(-90f, 0f)] public float minYaw = -90f;
    [Range(0f, 90f)] public float maxYaw = 90f;
    [Range(-89f, 0f)] public float minPitch = -25f;
    [Range(0f, 89f)] public float maxPitch = 30f;
    [Tooltip("0 aplica la posicion del cursor inmediatamente. Valores mayores suavizan el seguimiento.")]
    [Min(0f)] public float rotationSmoothTime;
    [Tooltip("Transicion breve al cerrar menus, recuperar el foco o volver a entrar en la ventana.")]
    [Min(0f)] public float resumeBlendTime = 0.12f;

    Vector3 posicionInicial;
    Quaternion rotacionCentral;
    float yaw;
    float pitch;
    float smoothYaw;
    float smoothPitch;
    float yawVelocity;
    float pitchVelocity;
    bool tieneFoco;
    bool navegacionActiva;
    bool cursorFuera;
    bool reanudando;
    Vector2 angulosAlRetomar;
    float tiempoRetorno;

    float rotacionX;
    float rotacionY;
    bool puedeMover = true;

    [Header("Zoom")]
    public float zoom = 60f;
    public float zoomMin = 30f;
    public float zoomMax = 80f;
    public float velocidadZoom = 200f;

    Camera cam;

    void Awake()
    {
        instancia = this;
        posicionInicial = transform.position;
        rotacionCentral = Quaternion.AngleAxis(centralYawOffset, Vector3.up) * transform.rotation;
        tieneFoco = Application.isFocused;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        zoom = cam.fieldOfView;
        if (fixedHubNavigation)
            ActualizarCursorHub();
        else if (!Application.isBatchMode)
            Cursor.lockState = CursorLockMode.None;
    }

    void OnEnable()
    {
        tieneFoco = Application.isFocused;
        navegacionActiva = false;
    }

    bool PuedeNavegarHub => puedeMover && tieneFoco &&
        (GameManager.Instancia == null || GameManager.Instancia.estadoActual == GameState.Hub);

    void Update()
    {
        if (fixedHubNavigation)
        {
            bool navegar = PuedeNavegarHub;
            if (navegar != navegacionActiva)
            {
                if (navegar) PrepararRetorno();
                else DetenerSuavizado();
                navegacionActiva = navegar;
                ActualizarCursorHub();
            }
            if (!navegar || Screen.width <= 1 || Screen.height <= 1) return;

            Vector2 posicion = Input.mousePosition;
            if (posicion.x < 0 || posicion.x > Screen.width || posicion.y < 0 || posicion.y > Screen.height)
            {
                cursorFuera = true;
                DetenerSuavizado();
                return;
            }
            if (cursorFuera)
            {
                cursorFuera = false;
                PrepararRetorno();
            }

            // Keep the view used for the click raycast stable for this frame.
            if (Input.GetMouseButtonDown(0)) return;
            AplicarMiradaHub(posicion, new Vector2(Screen.width, Screen.height), Time.unscaledDeltaTime);
            return;
        }

        if (puedeMover)
        {
            float mouseX = Input.GetAxis("Mouse X") * sensibilidad * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * sensibilidad * Time.deltaTime;
            rotacionX = Mathf.Clamp(rotacionX - mouseY, -60f, 60f);
            rotacionY += mouseX;
            transform.rotation = Quaternion.Euler(rotacionX, rotacionY, 0f);

            zoom = Mathf.Clamp(zoom - Input.GetAxis("Mouse ScrollWheel") * velocidadZoom * Time.deltaTime,
                zoomMin, zoomMax);
            cam.fieldOfView = zoom;
        }
    }

    void LateUpdate()
    {
        if (!fixedHubNavigation) return;
        transform.position = posicionInicial;
        transform.rotation = rotacionCentral * Quaternion.Euler(smoothPitch, smoothYaw, 0f);
    }

    void AplicarMiradaHub(Vector2 posicion, Vector2 pantalla, float deltaTime)
    {
        if (pantalla.x <= 1 || pantalla.y <= 1) return;
        float horizontal = NormalizarCursor(posicion.x, pantalla.x) * 2 - 1;
        float vertical = NormalizarCursor(posicion.y, pantalla.y) * 2 - 1;
        yaw = horizontal < 0 ? -horizontal * minYaw : horizontal * maxYaw;
        pitch = vertical < 0 ? -vertical * maxPitch : vertical * minPitch;

        if (reanudando && resumeBlendTime > 0)
        {
            // Ignore the first delta after focus returns, which can include time spent in another app.
            tiempoRetorno = tiempoRetorno < 0 ? 0 : tiempoRetorno + Mathf.Clamp(deltaTime, 0, .05f);
            float progreso = Mathf.Clamp01(tiempoRetorno / resumeBlendTime);
            float mezcla = Mathf.SmoothStep(0, 1, progreso);
            smoothYaw = Mathf.Lerp(angulosAlRetomar.x, yaw, mezcla);
            smoothPitch = Mathf.Lerp(angulosAlRetomar.y, pitch, mezcla);
            reanudando = progreso < 1;
        }
        else
        {
            reanudando = false;
            if (rotationSmoothTime <= 0)
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
        }

        smoothYaw = Mathf.Clamp(smoothYaw, minYaw, maxYaw);
        smoothPitch = Mathf.Clamp(smoothPitch, minPitch, maxPitch);
    }

    static float NormalizarCursor(float posicion, float longitud)
    {
        // A confined cursor stops at the last physical pixel. Snap that pixel to the exact angle limit.
        return posicion >= longitud - 1 ? 1 : Mathf.Clamp01(posicion / longitud);
    }

    void PrepararRetorno()
    {
        angulosAlRetomar = new Vector2(smoothYaw, smoothPitch);
        tiempoRetorno = -1;
        reanudando = resumeBlendTime > 0;
        yawVelocity = pitchVelocity = 0;
    }

    void DetenerSuavizado()
    {
        yaw = smoothYaw;
        pitch = smoothPitch;
        yawVelocity = pitchVelocity = 0;
        reanudando = false;
    }

    void OnApplicationFocus(bool foco)
    {
        tieneFoco = foco;
        if (!fixedHubNavigation) return;
        navegacionActiva = false;
        DetenerSuavizado();
        ActualizarCursorHub();
    }

    void OnDisable()
    {
        if (!fixedHubNavigation) return;
        navegacionActiva = false;
        DetenerSuavizado();
        if (Application.isBatchMode) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    static CursorLockMode ModoCursorHub(bool navegando, RuntimePlatform plataforma)
    {
        bool compatible = plataforma == RuntimePlatform.WindowsPlayer || plataforma == RuntimePlatform.LinuxPlayer;
        return navegando && compatible ? CursorLockMode.Confined : CursorLockMode.None;
    }

    void ActualizarCursorHub()
    {
        // Batch validation must never change the desktop pointer.
        if (Application.isBatchMode) return;
        Cursor.visible = true;
        Cursor.lockState = ModoCursorHub(PuedeNavegarHub, Application.platform);
    }

    void OnValidate()
    {
        minYaw = Mathf.Clamp(minYaw, -90f, 0f);
        maxYaw = Mathf.Clamp(maxYaw, 0f, 90f);
        minPitch = Mathf.Clamp(minPitch, -89f, 0f);
        maxPitch = Mathf.Clamp(maxPitch, 0f, 89f);
        rotationSmoothTime = Mathf.Max(0, rotationSmoothTime);
        resumeBlendTime = Mathf.Max(0, resumeBlendTime);
    }

    public void EnfocarObjeto(Transform objetivo)
    {
        SetCameraLocked(true);
        if (fixedHubNavigation)
        {
            if (objetivo == null) return;
            Vector3 direccionLocal = Quaternion.Inverse(rotacionCentral) * (objetivo.position - posicionInicial);
            if (direccionLocal.sqrMagnitude < .0001f) return;
            Vector3 angulos = Quaternion.LookRotation(direccionLocal).eulerAngles;
            smoothYaw = Mathf.Clamp(Mathf.DeltaAngle(0, angulos.y), minYaw, maxYaw);
            smoothPitch = Mathf.Clamp(Mathf.DeltaAngle(0, angulos.x), minPitch, maxPitch);
            DetenerSuavizado();
            transform.rotation = rotacionCentral * Quaternion.Euler(smoothPitch, smoothYaw, 0);
            return;
        }

        transform.rotation = Quaternion.LookRotation(objetivo.position - transform.position);
        rotacionX = transform.eulerAngles.x;
        rotacionY = transform.eulerAngles.y;
    }

    public void ActivarControl() => SetCameraLocked(false);

    public void SetCameraLocked(bool locked)
    {
        puedeMover = !locked;
        if (!fixedHubNavigation) return;
        navegacionActiva = false;
        DetenerSuavizado();
        ActualizarCursorHub();
    }
}
