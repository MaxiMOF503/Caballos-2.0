using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class TablonView : MonoBehaviour
{
    [Header("UI Info")]
    public TMP_Text textoNombre;
    public TMP_Text textoEdad;
    public TMP_Text textoNivel;

    public TMP_Text textoVelocidad;
    public TMP_Text textoStamina;
    public TMP_Text textoSuerte;
    public TMP_Text textoInteligencia;
    public TMP_Text textoPotencial;

    [Header("Scroll")]
    public Transform content;
    public GameObject caballoButtonPrefab;

    private List<Caballo> listaCaballos;
    private Caballo caballoActual;

    void OnEnable()
    {
        if (GameManager.Instancia == null) return;

        listaCaballos = GameManager.Instancia.caballosDelDia;

        GenerarLista();
    }

    void GenerarLista()
    {
        if (listaCaballos == null) return;

        // limpiar botones viejos
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        // crear botones nuevos
        for (int i = 0; i < listaCaballos.Count; i++)
        {
            GameObject go = Instantiate(caballoButtonPrefab, content);

            CaballoButtonUI btn = go.GetComponent<CaballoButtonUI>();

            btn.Setup(listaCaballos[i], this);
        }
    }

    public void SeleccionarCaballo(Caballo caballo)
    {
        caballoActual = caballo;

        MostrarCaballo(caballo);
    }

    void MostrarCaballo(Caballo caballo)
    {
        if (caballo == null)
        {
            textoNombre.text = "-";
            textoEdad.text = "Edad: -";
            textoNivel.text = "Nivel: -";
            textoVelocidad.text = "Velocidad: -";
            textoStamina.text = "Stamina: -";
            textoSuerte.text = "Suerte: -";
            textoInteligencia.text = "Inteligencia: -";
            textoPotencial.text = "Potencial: -";
            return;
        }

        textoNombre.text = caballo.nombre;
        textoEdad.text = "Edad: " + caballo.edad;
        textoNivel.text = "Nivel: " + caballo.nivel;

        textoVelocidad.text =
            "Velocidad: " + RangoCaballo.ObtenerRango(caballo.velocidad);

        textoStamina.text =
            "Stamina: " + RangoCaballo.ObtenerRango(caballo.stamina);

        textoSuerte.text =
            "Suerte: " + RangoCaballo.ObtenerRango(caballo.suerte);

        textoInteligencia.text =
            "Inteligencia: " + RangoCaballo.ObtenerRango(caballo.inteligencia);

        textoPotencial.text =
            "Potencial: " + RangoCaballo.ObtenerRango(caballo.potencial);
    }
}
