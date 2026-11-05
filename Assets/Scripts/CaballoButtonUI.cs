using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CaballoButtonUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text textoNombre;

    private Caballo caballo;
    private TablonView tablon;

    public void Setup(Caballo c, TablonView t)
    {
        caballo = c;
        tablon = t;

        textoNombre.text = c.nombre;

        //  Esto reemplaza el OnClick del inspector
        Button btn = GetComponent<Button>();

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);
        }
    }

    public void RefrescarEtiqueta(CarreraProgramada carrera, string seleccionado)
    {
        var participante = carrera.Buscar(caballo.id);
        bool seleccionadoAhora = caballo.id == seleccionado;
        string marca = seleccionadoAhora ? "> " : "";
        string propio = caballo.esDelJugador ? " (tuyo)" : "";
        textoNombre.text = $"{marca}{caballo.nombre}{propio} · {TablonView.CuotaTexto(participante.cuotaDecimas)}";
        GetComponent<Image>().color = seleccionadoAhora
            ? new Color32(224, 193, 151, 255) : new Color32(246, 232, 204, 255);
    }

    public void OnClick()
    {
        if (tablon == null)
        {
            Debug.LogError("TablonView no asignado en CaballoButtonUI");
            return;
        }

        Debug.Log("Click en caballo: " + caballo.nombre);

        tablon.SeleccionarCaballo(caballo);
    }
}
