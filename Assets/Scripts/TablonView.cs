using TMPro;
using UnityEngine;

public class TablonView : MonoBehaviour
{
    [Header("Información")]
    public TMP_Text textoNombre;
    public TMP_Text textoEdad;

    public TMP_Text textoVelocidad;
    public TMP_Text textoStamina;
    public TMP_Text textoSuerte;
    public TMP_Text textoInteligencia;

    public void SeleccionarRelampago()
    {
        textoNombre.text = "Relámpago";
        textoEdad.text = "Edad: 5";

        textoVelocidad.text = "Velocidad: ?";
        textoStamina.text = "Stamina: ?";
        textoSuerte.text = "Suerte: ?";
        textoInteligencia.text = "Inteligencia: ?";
    }

    public void SeleccionarTormenta()
    {
        textoNombre.text = "Tormenta";
        textoEdad.text = "Edad: 4";

        textoVelocidad.text = "Velocidad: ?";
        textoStamina.text = "Stamina: ?";
        textoSuerte.text = "Suerte: ?";
        textoInteligencia.text = "Inteligencia: ?";
    }

    public void SeleccionarCentella()
    {
        textoNombre.text = "Centella";
        textoEdad.text = "Edad: 6";

        textoVelocidad.text = "Velocidad: ?";
        textoStamina.text = "Stamina: ?";
        textoSuerte.text = "Suerte: ?";
        textoInteligencia.text = "Inteligencia: ?";
    }

    public void SeleccionarPegaso()
    {
        textoNombre.text = "Pegaso";
        textoEdad.text = "Edad: 3";

        textoVelocidad.text = "Velocidad: ?";
        textoStamina.text = "Stamina: ?";
        textoSuerte.text = "Suerte: ?";
        textoInteligencia.text = "Inteligencia: ?";
    }

    public void SeleccionarTrueno()
    {
        textoNombre.text = "Trueno";
        textoEdad.text = "Edad: 7";

        textoVelocidad.text = "Velocidad: ?";
        textoStamina.text = "Stamina: ?";
        textoSuerte.text = "Suerte: ?";
        textoInteligencia.text = "Inteligencia: ?";
    }
}