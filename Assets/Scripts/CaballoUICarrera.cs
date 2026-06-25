using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CaballoUICarrera : MonoBehaviour
{
    public Image imagen;
    public TextMeshProUGUI textoNombre;

    private Caballo data;

    public void Setup(Caballo c)
    {
        data = c;

        if (imagen != null)
            imagen.sprite = c.sprite;

        if (textoNombre != null)
            textoNombre.text = c.nombre;
    }
}