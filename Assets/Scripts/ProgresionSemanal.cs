using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public static class Moneda
{
    public static string Texto(long saldo) => "$" + saldo.ToString("N0", CultureInfo.GetCultureInfo("es-AR"));
}
[Serializable] public sealed class DerrotaCampana { public string motivo; }
[Serializable] public sealed class MetasSemanales
{
    public int[] primeras = { 3000, 6000, 15000 };
    public int incremento = 9000;
    static MetasSemanales actual;
    public static MetasSemanales Actual => actual ??= JsonUtility.FromJson<MetasSemanales>(Resources.Load<TextAsset>("MetasSemanales").text);
    public int Meta(int semana) => primeras[Math.Min(primeras.Length - 1, Math.Max(0, semana - 1))] + Math.Max(0, semana - primeras.Length) * incremento;
}
public partial class GameManager
{
    DerrotaCampana derrota;
    long ultimoTotalConfirmado;
    internal bool resultadosAntesDerrota;
    public bool Derrotado => !string.IsNullOrEmpty(derrota?.motivo);
    public long SaldoTotal => (long)dinero + DineroReservado;
    public void CerrarResultadosDerrota() { resultadosAntesDerrota = false; }
    ProgresionSemanalVista semanaVista;
    void LateUpdate()
    {
        if (UIManager.instance == null || caballosJugador.Count == 0) return;
        semanaVista ??= new ProgresionSemanalVista(this);
        if (textoDinero != null) textoDinero.gameObject.SetActive(false);
        semanaVista.Actualizar(Derrotado && !resultadosAntesDerrota, derrota?.motivo);
    }
    public void MenuDesdeDerrota()
    {
        if (!Derrotado || !GuardarPartida()) return;
        Time.timeScale = 1;
        SceneManager.LoadScene(GameLaunch.MenuScenePath);
    }
}
internal sealed class ProgresionSemanalVista
{
    readonly GameManager game;
    readonly GameObject canvas, derrota;
    readonly TMP_Text saldo, motivo;
    readonly RectTransform barra;
    internal ProgresionSemanalVista(GameManager game)
    {
        this.game = game;
        canvas = new GameObject("ProgresionSemanalHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(game.transform, false);
        var c = canvas.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 500;
        var scale = canvas.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1920,1080);
        var root = (RectTransform)canvas.transform;
        var panel = TablonLayout.Surface("IndicadorSemanal", root, new Rect(30, 8, 1010, 58), new Vector2(1920,1080), new Color32(38,29,18,238));
        saldo = TablonLayout.Label("SaldoMeta",panel,new Rect(12,0,980,38),new Vector2(1010,58),TMP_Settings.defaultFontAsset,"",25,TextAlignmentOptions.MidlineLeft);
        var fondo = TablonLayout.Surface("BarraFondo",panel,new Rect(12,43,980,8),new Vector2(1010,58),new Color32(74,62,43,255));
        barra = TablonLayout.Surface("Progreso",fondo,new Rect(0,0,1,1),Vector2.one,new Color32(203,155,75,255));
        derrota = new GameObject("GameOver",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
        derrota.transform.SetParent(root,false);
        var rect=(RectTransform)derrota.transform; rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var dc=derrota.GetComponent<Canvas>();dc.overrideSorting=true;dc.sortingOrder=2000;
        TablonLayout.Surface("Bloqueo",rect,new Rect(0,0,1,1),Vector2.one,new Color(0,0,0,.85f)).GetComponent<Image>().raycastTarget=true;
        var madera=UIManager.instance.viewEstablo.GetComponentInChildren<EstabloView>(true).TexturaMadera;
        var cuadro=TablonLayout.Wood("Madera",rect,new Rect(440,260,1040,560),new Vector2(1920,1080),madera);
        TablonLayout.Label("Titulo",cuadro,new Rect(20,25,1000,100),new Vector2(1040,560),TMP_Settings.defaultFontAsset,"GAME OVER",66,TextAlignmentOptions.Center);
        var papel=TablonLayout.Surface("Pergamino",cuadro,new Rect(45,145,950,190),new Vector2(1040,560),new Color32(238,215,173,255));
        var imagenPapel = papel.GetComponent<Image>();
        imagenPapel.sprite = UIManager.instance.viewTienda.GetComponentInChildren<TiendaView>(true).PapelInterfaz;
        imagenPapel.type = Image.Type.Sliced;
        imagenPapel.color = Color.white;
        motivo=TablonLayout.Label("Motivo",papel,new Rect(100,25,750,140),new Vector2(950,190),TMP_Settings.defaultFontAsset,"",31,TextAlignmentOptions.Center,new Color32(58,35,22,255));
        Boton(cuadro,"Nueva partida",70,()=>{UnityEngine.Object.FindFirstObjectByType<MenuPausa>()?.CerrarParaReinicio();Time.timeScale=1;game.IniciarNuevaPartida();UIManager.instance.CerrarTodo();game.VolverAlHub();});
        Boton(cuadro,"Menú principal",550,game.MenuDesdeDerrota);
        var opciones = derrota.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < opciones.Length; i++)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnUp = opciones[1-i], selectOnDown = opciones[1-i],
                selectOnLeft = opciones[1-i], selectOnRight = opciones[1-i] };
            opciones[i].navigation = nav;
        }
        derrota.SetActive(false);
    }
    void Boton(RectTransform parent,string label,float x,UnityEngine.Events.UnityAction action)
    {
        var r=TablonLayout.Surface(label,parent,new Rect(x,390,420,105),new Vector2(1040,560),new Color32(240,218,177,255));
        var im=r.GetComponent<Image>();im.raycastTarget=true;
        var button=r.gameObject.AddComponent<Button>();button.targetGraphic=im;button.transition=Selectable.Transition.None;button.onClick.AddListener(action);
        TablonLayout.Label("Texto",r,new Rect(4,4,412,97),new Vector2(420,105),TMP_Settings.defaultFontAsset,label,33,TextAlignmentOptions.Center,new Color32(58,35,22,255));
    }
    internal void Actualizar(bool perdida,string mensaje)
    {
        int semana=GameManager.ObtenerSemana(game.dia),meta=MetasSemanales.Actual.Meta(semana);
        saldo.text=$"Semana {semana} · Saldo {Moneda.Texto(game.SaldoTotal)}     Meta {Moneda.Texto(meta)}";
        barra.anchorMax=new Vector2(Mathf.Clamp01((float)game.SaldoTotal/meta),1);
        // Other menus keep their approved layout; the shared indicator appears in Hub and race.
        bool podio = UIManager.instance.viewCarrera.transform.Find("ResultadosCarrera")?.gameObject.activeInHierarchy == true;
        saldo.transform.parent.gameObject.SetActive(!podio && (game.estadoActual==GameState.Hub || UIManager.instance.viewCarrera.activeInHierarchy));
        motivo.text=mensaje??"";
        if (perdida && !derrota.activeSelf)
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(derrota.GetComponentsInChildren<Button>(true)[0].gameObject);
        derrota.SetActive(perdida);
    }
}
