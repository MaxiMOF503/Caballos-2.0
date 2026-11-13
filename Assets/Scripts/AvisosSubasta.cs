using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Avisos no modales. El seguimiento y su lectura se conservan en el JSON de la partida.
public sealed class AvisosSubasta : MonoBehaviour
{
    GameObject panel;
    TMP_Text texto;
    string idVisible;
    double hasta;
    EstadoSubastasData estadoVisible;

    void Update()
    {
        Actualizar(Time.realtimeSinceStartupAsDouble);
    }

    internal void Actualizar(double ahora)
    {
        var juego = GameManager.Instancia;
        bool hub = juego != null && juego.estadoActual == GameState.Hub && !MenuPausa.Activa &&
            UIManager.instance != null && !UIManager.instance.HayInterfazAbierta;
        // A load/new game replaces the notice state. Never carry an old panel
        // into another campaign, even if its five seconds have not elapsed.
        if (!hub || estadoVisible != juego.subastas)
        {
            Ocultar();
            estadoVisible = juego?.subastas;
            if (!hub) return;
        }
        if (idVisible != null && ahora < hasta) return;
        Ocultar();

        var aviso = juego.subastas?.avisos?.FirstOrDefault(a => !a.leido &&
            (a.diaLimite == 0 || juego.dia <= a.diaLimite));
        // Persist consumption at first display, not at dismissal. If saving
        // fails, keep the notice pending rather than showing it repeatedly.
        if (aviso == null || !juego.MarcarAvisoSubasta(aviso.id)) return;
        if (panel == null) Crear();
        idVisible = aviso.id;
        texto.text = aviso.texto;
        hasta = ahora + 5;
        panel.SetActive(true);
    }

    void Ocultar()
    {
        if (panel != null) panel.SetActive(false);
        idVisible = null;
    }

    void Crear()
    {
        var canvas = new GameObject("AvisosSubastas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvas.transform.SetParent(transform,false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = 30;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600,900);
        scaler.matchWidthOrHeight = .5f;
        var rect = new GameObject("Aviso", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rect.SetParent(canvas.transform,false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f,0);
        rect.pivot = new Vector2(.5f,0);
        rect.anchoredPosition = new Vector2(0,124);
        rect.sizeDelta = new Vector2(740,82);
        var image = rect.GetComponent<Image>();
        image.color = new Color32(47,28,15,235);
        image.raycastTarget = false;
        texto = TablonLayout.Label("Texto",rect,new Rect(18,8,704,66),new Vector2(740,82),
            TMP_Settings.defaultFontAsset,"",25,TextAlignmentOptions.Center,new Color32(255,231,188,255));
        texto.richText = false;
        texto.textWrappingMode = TextWrappingModes.Normal;
        panel = rect.gameObject;
    }
}
