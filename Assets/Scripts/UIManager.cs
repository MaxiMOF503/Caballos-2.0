using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    public GameObject viewCarrera;
    public GameObject viewTienda;
    public GameObject viewEstablo;
    public GameObject viewTablon;
    [SerializeField] GameObject viewSeleccionCarrera;
    GameObject avisoHub;
    Coroutine ocultarAviso;

    internal bool HayInterfazAbierta => GameManager.Instancia?.Derrotado == true || viewCarrera.activeInHierarchy || viewTienda.activeInHierarchy ||
        viewEstablo.activeInHierarchy || viewTablon.activeInHierarchy ||
        (viewSeleccionCarrera != null && viewSeleccionCarrera.activeInHierarchy);

    private void Awake()
    {
        instance = this;
    }

    internal bool ConsumirEscape()
    {
        var foco = EventSystem.current?.currentSelectedGameObject;
        if (foco != null && (foco.GetComponent<TMP_InputField>()?.isFocused == true ||
            foco.GetComponent<InputField>()?.isFocused == true))
            return true;

        var tienda = viewTienda.GetComponentInChildren<TiendaView>();
        if (tienda?.subasta?.CerrarPedigri() == true) return true;
        if (tienda != null && tienda.confirmacion != null && tienda.confirmacion.activeInHierarchy)
        {
            tienda.CancelarCompra();
            return true;
        }

        return viewEstablo.GetComponentInChildren<EstabloView>()?.CancelarModalConEscape() == true ||
            viewTablon.GetComponentInChildren<TablonView>()?.CancelarApuestaConEscape() == true;
    }

    public void CerrarTodo()
    {
        if (ocultarAviso != null) StopCoroutine(ocultarAviso);
        ocultarAviso = null;
        if (avisoHub != null) avisoHub.SetActive(false);
        viewCarrera.SetActive(false);
        viewTienda.SetActive(false);
        viewEstablo.SetActive(false);
        viewTablon.SetActive(false);
        if (viewSeleccionCarrera != null) viewSeleccionCarrera.SetActive(false);
    }

    public void AbrirSeleccionCarrera()
    {
        if (GameManager.Instancia.ObtenerCarreraProgramada()?.SeleccionBloqueada == true)
        {
            AbrirTablon();
            return;
        }
        CerrarTodo();
        viewSeleccionCarrera.SetActive(true);
    }

    public void AbrirCarrera()
    {
        if (GameManager.Instancia.ObtenerCarreraProgramada() == null)
        {
            GameManager.Instancia.VolverAlHub();
            MostrarAvisoHub("Primero elegí una carrera en el tablón de apuestas.");
            return;
        }
        CerrarTodo();
        viewCarrera.SetActive(true);
    }

    public void AbrirTienda()
    {
        CerrarTodo();
        viewTienda.SetActive(true);
    }

    public void AbrirEstablo()
    {
        CerrarTodo();
        viewEstablo.SetActive(true);
    }

    public void AbrirTablon()
    {
        if (GameManager.Instancia.ObtenerCarreraProgramada() == null)
        {
            AbrirSeleccionCarrera();
            return;
        }
        CerrarTodo();
        viewTablon.SetActive(true);
    }

    void MostrarAvisoHub(string mensaje)
    {
        if (avisoHub == null)
        {
            avisoHub = new GameObject("AvisoHub", typeof(RectTransform), typeof(Image));
            var rect = avisoHub.GetComponent<RectTransform>();
            rect.SetParent(viewTablon.transform.parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0);
            rect.sizeDelta = new Vector2(920, 76);
            rect.anchoredPosition = new Vector2(0, 46);
            var fondo = avisoHub.GetComponent<Image>();
            fondo.color = new Color32(47, 27, 13, 238);
            fondo.raycastTarget = false;
            var texto = new GameObject("Mensaje", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            texto.rectTransform.SetParent(rect, false);
            texto.rectTransform.anchorMin = Vector2.zero;
            texto.rectTransform.anchorMax = Vector2.one;
            texto.rectTransform.offsetMin = new Vector2(18, 8);
            texto.rectTransform.offsetMax = new Vector2(-18, -8);
            texto.font = TMP_Settings.defaultFontAsset;
            texto.fontSize = texto.fontSizeMax = 29;
            texto.fontSizeMin = 20;
            texto.enableAutoSizing = true;
            texto.alignment = TextAlignmentOptions.Center;
            texto.color = new Color32(255, 236, 197, 255);
            texto.raycastTarget = false;
        }
        avisoHub.GetComponentInChildren<TMP_Text>().text = mensaje;
        avisoHub.SetActive(true);
        avisoHub.transform.SetAsLastSibling();
        ocultarAviso = StartCoroutine(OcultarAvisoHub());
    }

    IEnumerator OcultarAvisoHub()
    {
        yield return new WaitForSecondsRealtime(4);
        avisoHub.SetActive(false);
        ocultarAviso = null;
    }
}
