using UnityEngine;
using TMPro;

public class EstabloView : MonoBehaviour
{
    [Header("Ventanas")]
    public GameObject panelInfoCaballo;

    public GameObject panelUps;
    public GameObject panelProcrear;
    public GameObject panelItems;

    [Header("Stats")]
    public TMP_Text textoNombre;
    public TMP_Text textoEdad;

    public TMP_Text textoVelocidad;
    public TMP_Text textoStamina;
    public TMP_Text textoSuerte;
    public TMP_Text textoInteligencia;

    public TMP_Text textoPotencial;

    public TMP_Text textoRelacion;
    public TMP_Text textoEstres;

    public void AbrirCaballo()
    {
        panelInfoCaballo.SetActive(true);

        MostrarStats();

        Caballo caballo = GameManager.Instancia.caballosJugador[0];

        textoNombre.text = caballo.nombre;

        textoEdad.text = "Edad: " + caballo.edad;

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

        textoRelacion.text =
            "Relación: " + caballo.relacion;

        textoEstres.text =
            "Estrés: " + caballo.estres;
    }

    public void CerrarCaballo()
    {
        panelInfoCaballo.SetActive(false);
        panelUps.SetActive(false);
        panelProcrear.SetActive(false);
        panelItems.SetActive(false);
    }

    public void MostrarStats()
    {
        panelInfoCaballo.SetActive(true);

        panelUps.SetActive(false);
        panelProcrear.SetActive(false);
        panelItems.SetActive(false);
    }

    public void MostrarUps()
    {
        panelInfoCaballo.SetActive(false);

        panelUps.SetActive(true);
        panelProcrear.SetActive(false);
        panelItems.SetActive(false);
    }

    public void MostrarProcrear()
    {
        panelInfoCaballo.SetActive(false);
        panelUps.SetActive(false);

        panelProcrear.SetActive(true);
        panelItems.SetActive(false);
    }

    public void MostrarItems()
    {
        panelInfoCaballo.SetActive(false);
        panelUps.SetActive(false);
        panelProcrear.SetActive(false);

        panelItems.SetActive(true);
    }
}