using UnityEngine;
using TMPro;

public class CarreraUI : MonoBehaviour
{
    public RectTransform track;

    public RectTransform caballo1;
    public RectTransform caballo2;
    public RectTransform caballo3;

    public TextMeshProUGUI textoEstado;

    public GameObject botonIniciar;
    public GameObject botonVolver;

    float progreso1;
    float progreso2;
    float progreso3;

    float vel1;
    float vel2;
    float vel3;

    bool carreraActiva;

    float anchoTrack;

    const float POSICION_INICIAL_X = 40f;

    void OnEnable()
    {
        anchoTrack = track.rect.width;
        PrepararCarrera();
    }

    void Update()
    {
        if (!carreraActiva) return;

        progreso1 += vel1 * Time.deltaTime;
        progreso2 += vel2 * Time.deltaTime;
        progreso3 += vel3 * Time.deltaTime;

        Mover(caballo1, progreso1);
        Mover(caballo2, progreso2);
        Mover(caballo3, progreso3);

        if (progreso1 >= 100)
        {
            Terminar(1);
            return;
        }

        if (progreso2 >= 100)
        {
            Terminar(2);
            return;
        }

        if (progreso3 >= 100)
        {
            Terminar(3);
            return;
        }
    }

    void PrepararCarrera()
    {
        progreso1 = 0;
        progreso2 = 0;
        progreso3 = 0;

        ReiniciarPosiciones();

        carreraActiva = false;

        textoEstado.text = "Listo para correr";

        botonIniciar.SetActive(true);
        botonVolver.SetActive(true);
    }

    public void IniciarCarrera()
    {
        vel1 = Random.Range(8f, 12f);
        vel2 = Random.Range(8f, 12f);
        vel3 = Random.Range(8f, 12f);

        carreraActiva = true;

        botonIniciar.SetActive(false);
        botonVolver.SetActive(false);

        textoEstado.text = "¡Carrera iniciada!";
    }

    void Mover(RectTransform caballo, float progreso)
    {
        float x = POSICION_INICIAL_X + ((progreso / 100f) * (anchoTrack - POSICION_INICIAL_X));
        caballo.anchoredPosition = new Vector2(x, caballo.anchoredPosition.y);
    }

    void ReiniciarPosiciones()
    {
        caballo1.anchoredPosition =
            new Vector2(POSICION_INICIAL_X, caballo1.anchoredPosition.y);

        caballo2.anchoredPosition =
            new Vector2(POSICION_INICIAL_X, caballo2.anchoredPosition.y);

        caballo3.anchoredPosition =
            new Vector2(POSICION_INICIAL_X, caballo3.anchoredPosition.y);
    }

    void Terminar(int ganador)
    {
        carreraActiva = false;

        textoEstado.text = "Ganó el caballo " + ganador;

        if (ganador == 1)
            GameManager.Instancia.AgregarDinero(500);
        else
            GameManager.Instancia.RestarDinero(200);

        ReiniciarPosiciones();

        botonIniciar.SetActive(true);
        botonVolver.SetActive(true);
    }

    public void Volver()
    {
        if (carreraActiva) return;

        GameManager.Instancia.VolverAlHub();
    }
}