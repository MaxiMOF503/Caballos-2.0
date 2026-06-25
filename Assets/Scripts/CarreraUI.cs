using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class CarreraUI : MonoBehaviour
{
    [Header("Track")]
    public RectTransform track;

    [Header("Prefab UI")]
    public GameObject caballoPrefab;
    public Transform contenedorCaballos;

    [Header("UI")]
    public TextMeshProUGUI textoEstado;
    public GameObject botonIniciar;
    public GameObject botonVolver;

    [Header("Datos")]
    List<Caballo> caballosData = new List<Caballo>();

    List<RectTransform> caballosUI = new List<RectTransform>();
    List<Image> caballosImg = new List<Image>();

    List<float> progreso = new List<float>();
    List<float> velocidad = new List<float>();

    bool carreraActiva;

    const float POS_INICIO = -1000f;
    const float META_X = 1000f;

    Vector2[] posicionesIniciales =
    {
        new Vector2(-1000, 60),
        new Vector2(-1000, 210),
        new Vector2(-1000, 350),
        new Vector2(-1000, -80),
        new Vector2(-1000, -230),
        new Vector2(-1000, -380)
    };

    float aceleracion = 0.08f;
    float maxVel = 18f;
    float minVel = 3f;

    void OnEnable()
    {
        PrepararCarrera();
    }

    void PrepararCarrera()
    {
        carreraActiva = false;

        textoEstado.text = "Listo para correr";

        botonIniciar.SetActive(true);
        botonVolver.SetActive(true);

        caballosData.Clear();

        if (GameManager.Instancia.caballosJugador.Count > 0)
            caballosData.Add(GameManager.Instancia.caballosJugador[0]);

        caballosData.AddRange(GameManager.Instancia.caballosDelDia);

        CrearCaballosUI();
    }

    void CrearCaballosUI()
    {
        foreach (Transform child in contenedorCaballos)
            Destroy(child.gameObject);

        caballosUI.Clear();
        caballosImg.Clear();
        progreso.Clear();
        velocidad.Clear();

        for (int i = 0; i < caballosData.Count; i++)
        {
            GameObject obj = Instantiate(caballoPrefab, contenedorCaballos);

            RectTransform ui = obj.GetComponent<RectTransform>();

            //  FIX IMPORTANTE: buscar Image en hijos
            Image img = obj.GetComponentInChildren<Image>(true);

            caballosUI.Add(ui);
            caballosImg.Add(img);

            progreso.Add(0f);
            velocidad.Add(0f);

            ui.anchoredPosition = posicionesIniciales[Mathf.Min(i, posicionesIniciales.Length - 1)];

            //  ASIGNAR SPRITE SEGURO
            if (img != null)
            {
                if (caballosData[i].sprite != null)
                {
                    img.sprite = caballosData[i].sprite;
                    img.preserveAspect = true;
                    img.enabled = true;
                }
                else
                {
                    Debug.LogWarning("Caballo sin sprite: " + caballosData[i].nombre);
                }
            }
            else
            {
                Debug.LogError("Prefab no tiene Image en hijos!");
            }
        }
    }

    public void IniciarCarrera()
    {
        carreraActiva = true;

        for (int i = 0; i < caballosData.Count; i++)
            velocidad[i] = CalcularVelocidadBase(caballosData[i]);

        botonIniciar.SetActive(false);
        botonVolver.SetActive(false);

        textoEstado.text = "¡Carrera iniciada!";
    }

    void Update()
    {
        if (!carreraActiva) return;

        float dt = Time.deltaTime;

        for (int i = 0; i < caballosData.Count; i++)
        {
            velocidad[i] += aceleracion * dt;
            velocidad[i] += Random.Range(-0.04f, 0.06f);
            velocidad[i] = Mathf.Clamp(velocidad[i], minVel, maxVel);

            progreso[i] += velocidad[i] * dt;

            float t = Mathf.Clamp01(progreso[i] / 100f);
            float x = Mathf.Lerp(POS_INICIO, META_X, t);

            Vector2 pos = caballosUI[i].anchoredPosition;
            pos.x = x;
            caballosUI[i].anchoredPosition = pos;

            if (x >= META_X)
            {
                Terminar(i);
                return;
            }
        }
    }

    float CalcularVelocidadBase(Caballo c)
    {
        float baseSpeed = 4.5f;

        float v = c.velocidad * 1.3f;
        float s = c.stamina * 0.6f;
        float i = c.inteligencia * 0.4f;
        float su = c.suerte * 0.2f;
        float p = c.potencial * 0.5f;

        return Mathf.Clamp(baseSpeed + v + s + i + su + p, minVel, maxVel);
    }

    void Terminar(int ganador)
    {
        carreraActiva = false;

        textoEstado.text = "Ganó: " + caballosData[ganador].nombre;

        if (ganador == 0)
            GameManager.Instancia.AgregarDinero(500);
        else
            GameManager.Instancia.RestarDinero(200);

        botonIniciar.SetActive(true);
        botonVolver.SetActive(true);
    }

    public void Volver()
    {
        if (carreraActiva) return;
        GameManager.Instancia.VolverAlHub();
    }
}