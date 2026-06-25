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