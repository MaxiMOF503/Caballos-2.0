using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController instancia;

    [Header("Movimiento")]
    public float sensibilidad = 100f;

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
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        zoom = cam.fieldOfView;
        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
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


    // ENFOCAR OBJETO Y BLOQUEAR

    public void EnfocarObjeto(Transform objetivo)
    {
        puedeMover = false;

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
        puedeMover = true;
    }
}
