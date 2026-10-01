using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController instancia;

    [Header("Movimiento")]
    public float sensibilidad = 100f;

    [Header("Navegacion fija del hub")]
    [Tooltip("Activar solo en la camara de esta habitacion. Conserva el control anterior en otros usos.")]
    public bool fixedHubNavigation;
    [Tooltip("Desplazamiento horizontal respecto a la orientacion inicial de la camara.")]
    public float centralYawOffset;
    [Min(0f)] public float horizontalSensitivity = 3f;
    [Min(0f)] public float verticalSensitivity = 2f;
    [Range(-90f, 0f)] public float minYaw = -90f;
    [Range(0f, 90f)] public float maxYaw = 90f;
    [Range(-89f, 0f)] public float minPitch = -25f;
    [Range(0f, 89f)] public float maxPitch = 30f;
    [Min(0f)] public float rotationSmoothTime = 0.045f;

    Vector3 posicionInicial;
    Quaternion rotacionCentral;
    float yaw;
    float pitch;
    float smoothYaw;
    float smoothPitch;
    float yawVelocity;
    float pitchVelocity;

    float rotacionX = 0f;
    float rotacionY = 0f;

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
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        zoom = cam.fieldOfView;
        Cursor.lockState = CursorLockMode.None;
        if (fixedHubNavigation)
            Cursor.visible = true;
    }

    void Update()
    {
        if (fixedHubNavigation)
        {
            bool menuAbierto = GameManager.Instancia != null &&
                GameManager.Instancia.estadoActual != GameState.Hub;
            // Detener tambien el suavizado pendiente al abrir UI o hacer clic.
            if (!puedeMover || menuAbierto || Input.GetMouseButton(0))
            {
                DetenerSuavizado();
                return;
            }

            // Mouse X/Y ya son deltas: no multiplicarlos por deltaTime.
            AplicarMiradaHub(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"),
                Time.unscaledDeltaTime);
            return;
        }

        if (puedeMover)
        {
            // ROTACIÓN
            float mouseX = Input.GetAxis("Mouse X") * sensibilidad * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * sensibilidad * Time.deltaTime;

            rotacionX -= mouseY;
            rotacionY += mouseX;

            rotacionX = Mathf.Clamp(rotacionX, -60f, 60f);

            transform.rotation = Quaternion.Euler(rotacionX, rotacionY, 0f);

            // ZOOM
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            zoom -= scroll * velocidadZoom * Time.deltaTime;
            zoom = Mathf.Clamp(zoom, zoomMin, zoomMax);

            cam.fieldOfView = zoom;
        }
    }

    void LateUpdate()
    {
        if (!fixedHubNavigation)
            return;

        transform.position = posicionInicial;
        transform.rotation = rotacionCentral * Quaternion.Euler(smoothPitch, smoothYaw, 0f);
    }

    void AplicarMiradaHub(float inputX, float inputY, float deltaTime)
    {
        yaw = Mathf.Clamp(yaw + inputX * horizontalSensitivity, minYaw, maxYaw);
        pitch = Mathf.Clamp(pitch - inputY * verticalSensitivity, minPitch, maxPitch);

        if (rotationSmoothTime <= 0f)
        {
            smoothYaw = yaw;
            smoothPitch = pitch;
        }
        else if (deltaTime > 0f)
        {
            smoothYaw = Mathf.SmoothDamp(smoothYaw, yaw, ref yawVelocity,
                rotationSmoothTime, Mathf.Infinity, deltaTime);
            smoothPitch = Mathf.SmoothDamp(smoothPitch, pitch, ref pitchVelocity,
                rotationSmoothTime, Mathf.Infinity, deltaTime);
        }

        smoothYaw = Mathf.Clamp(smoothYaw, minYaw, maxYaw);
        smoothPitch = Mathf.Clamp(smoothPitch, minPitch, maxPitch);
    }

    void DetenerSuavizado()
    {
        yaw = smoothYaw;
        pitch = smoothPitch;
        yawVelocity = 0f;
        pitchVelocity = 0f;
    }

    void OnValidate()
    {
        minYaw = Mathf.Clamp(minYaw, -90f, 0f);
        maxYaw = Mathf.Clamp(maxYaw, 0f, 90f);
        minPitch = Mathf.Clamp(minPitch, -89f, 0f);
        maxPitch = Mathf.Clamp(maxPitch, 0f, 89f);
    }


    // ENFOCAR OBJETO Y BLOQUEAR

    public void EnfocarObjeto(Transform objetivo)
    {
        SetCameraLocked(true);

        if (fixedHubNavigation)
        {
            if (objetivo == null)
                return;
            Vector3 direccionLocal = Quaternion.Inverse(rotacionCentral) *
                (objetivo.position - posicionInicial);
            if (direccionLocal.sqrMagnitude < 0.0001f)
                return;

            Vector3 angulos = Quaternion.LookRotation(direccionLocal).eulerAngles;
            smoothYaw = Mathf.Clamp(Mathf.DeltaAngle(0f, angulos.y), minYaw, maxYaw);
            smoothPitch = Mathf.Clamp(Mathf.DeltaAngle(0f, angulos.x), minPitch, maxPitch);
            DetenerSuavizado();
            transform.rotation = rotacionCentral * Quaternion.Euler(smoothPitch, smoothYaw, 0f);
            return;
        }

        Vector3 direccion = objetivo.position - transform.position;
        Quaternion rotacion = Quaternion.LookRotation(direccion);

        transform.rotation = rotacion;

        // sincronizar rotación interna para evitar salto al volver
        rotacionX = transform.eulerAngles.x;
        rotacionY = transform.eulerAngles.y;
    }

   
    // VOLVER AL CONTROL
 
    public void ActivarControl()
    {
        SetCameraLocked(false);
    }

    public void SetCameraLocked(bool locked)
    {
        puedeMover = !locked;
        if (fixedHubNavigation)
            DetenerSuavizado();
    }
}
