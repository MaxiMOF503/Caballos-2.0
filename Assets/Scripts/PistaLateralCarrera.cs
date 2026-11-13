using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class MapaPistaCarrera
{
    public readonly float distancia, unidadesPorProgreso, largada, meta, longitud;
    public MapaPistaCarrera(float distancia, float unidadesPorProgreso = 24, float largada = 320, float salidaMeta = 600)
    {
        this.distancia = Mathf.Max(1, distancia);
        this.unidadesPorProgreso = Mathf.Max(1, unidadesPorProgreso);
        this.largada = largada;
        meta = X(this.distancia);
        longitud = meta + salidaMeta;
    }
    // Both the finish line and every participant use this sole conversion.
    public float X(float progreso) => largada + Mathf.Clamp(progreso, 0, distancia) * unidadesPorProgreso;
    public float Encuadre(float jugadorX, float ancho) => Mathf.Clamp(jugadorX - ancho * .38f, 0, Mathf.Max(0, longitud - ancho));
}

internal sealed class PistaLateralCarrera
{
    static readonly Vector2 Lienzo = new Vector2(1920, 1080);
    float AltoMundo = 900;
    float altoPista = 720;
    internal readonly RectTransform raiz, viewport, mundo, caballos;
    internal MapaPistaCarrera mapa;
    internal float CamaraX { get; private set; }
    internal bool FlechaVisible => flecha != null && flecha.gameObject.activeSelf;
    RectTransform tribuna, flecha;
    AmbienteCarreraGraphic paisaje, ambiente;
    TMP_Text informacion, tituloMeta;
    readonly CarreraUI owner;
    readonly Texture2D textura;
    float reloj;
    bool iniciada;
    CarreraProgramada programada;

    internal PistaLateralCarrera(CarreraUI owner)
    {
        this.owner = owner;
        var fuente = owner.track.GetComponent<Image>()?.sprite;
        if (fuente != null)
        {
            var original = fuente.texture;
            // GPU copy also works with non-readable imported textures. Do not
            // change their importer or filtering in other views which share them.
            textura = new Texture2D(original.width, original.height, original.format, false);
            textura.Apply(false, true);
            Graphics.CopyTexture(original, 0, 0, textura, 0, 0);
            textura.filterMode = FilterMode.Point;
            textura.wrapMode = TextureWrapMode.Clamp;
            textura.hideFlags = HideFlags.DontSave;
        }
        owner.track.gameObject.SetActive(false);
        raiz = TablonLayout.Surface("CarreraLateral2D", (RectTransform)UIManager.instance.viewCarrera.transform,
            new Rect(0, 0, 1920, 1080), Lienzo, new Color32(27, 37, 30, 255));
        raiz.GetComponent<Image>().raycastTarget = true;
        viewport = TablonLayout.Rect("ViewportPista", raiz, new Rect(24, 110, 1872, 890), Lienzo);
        viewport.gameObject.AddComponent<RectMask2D>();
        var paisajeRect = TablonLayout.Rect("PaisajeParallax", viewport, new Rect(0, 0, 1, 1), Vector2.one);
        paisajeRect.pivot = Vector2.zero;
        paisaje = paisajeRect.gameObject.AddComponent<AmbienteCarreraGraphic>();
        paisaje.fondo = true; paisaje.raycastTarget = false;
        mundo = CrearPlano("Recorrido", viewport);
        caballos = CrearPlano("Participantes", mundo);
        Colocar(owner.textoEstado.rectTransform, raiz, new Rect(44, 64, 1480, 42));
        owner.textoEstado.fontSizeMax = 28;
        owner.textoEstado.enableAutoSizing = true;
        owner.textoEstado.alignment = TextAlignmentOptions.MidlineLeft;
        owner.textoEstado.color = new Color32(250, 237, 210, 255);
        Colocar(owner.botonVolver.GetComponent<RectTransform>(), raiz, new Rect(1640, 33, 235, 76));
        Colocar(owner.botonIniciar.GetComponent<RectTransform>(), raiz, new Rect(700, 1010, 520, 60));
        TablonLayout.Label("AyudaIdentificacion", raiz, new Rect(40, 1010, 620, 60), Lienzo, owner.textoEstado.font,
            "Mantené A para identificar a tu caballo", 25, TextAlignmentOptions.MidlineLeft);
        informacion = TablonLayout.Label("Preparacion", raiz, new Rect(1250, 1005, 625, 65), Lienzo, owner.textoEstado.font,
            "", 23, TextAlignmentOptions.MidlineRight);
        viewport.gameObject.AddComponent<DimensionesPista>().cambio = AdaptarGeometria;
    }

    internal void Preparar(CarreraProgramada carrera, float distancia)
    {
        programada = carrera;
        altoPista = 32 + Mathf.Max(1, carrera?.participantes?.Count ?? 6) * PresentacionCaballoCarrera.SeparacionCarriles;
        AltoMundo = altoPista + 160;
        if (tribuna != null) UnityEngine.Object.Destroy(tribuna.gameObject);
        // Keep only the participant container; terrain is rebuilt for the frozen distance.
        for (int i = mundo.childCount - 1; i >= 0; i--)
            if (mundo.GetChild(i) != caballos) { mundo.GetChild(i).gameObject.SetActive(false); UnityEngine.Object.Destroy(mundo.GetChild(i).gameObject); }
        paisaje.alturaMundo = AltoMundo; paisaje.basePista = altoPista;
        mapa = new MapaPistaCarrera(distancia);
        reloj = 0; iniciada = false; CamaraX = 0; flecha = null;
        mundo.sizeDelta = new Vector2(mapa.longitud, AltoMundo);
        caballos.sizeDelta = mundo.sizeDelta;
        var cesped = CrearPlano("TramosCesped", mundo); cesped.sizeDelta = mundo.sizeDelta;
        CrearMosaicos(cesped, new Rect(0, 0, 1, .84f), 0, altoPista + 4);
        cesped.SetAsFirstSibling();
        var baranda = CrearPlano("Baranda", mundo); baranda.sizeDelta = mundo.sizeDelta;
        for (float x = 0; x < mapa.longitud; x += 90)
            Placa(baranda, "Poste", new Rect(x, altoPista, 7, 45), new Color32(210, 199, 160, 255));
        Placa(baranda, "RielSuperior", new Rect(0, altoPista + 36, mapa.longitud, 7), new Color32(243, 230, 191, 255));
        Placa(baranda, "RielInferior", new Rect(0, altoPista + 12, mapa.longitud, 5), new Color32(160, 157, 124, 255));
        tribuna = CrearPlano("TribunaParallax", viewport); tribuna.sizeDelta = mundo.sizeDelta;
        CrearMosaicos(tribuna, new Rect(0, .85f, 1, .15f), altoPista + 32, 90);
        tribuna.SetSiblingIndex(1); // landscape, audience, running surface/horses
        var ambienteRect = TablonLayout.Rect("PublicoYBanderines", tribuna, new Rect(0, 0, 1, 1), Vector2.one);
        ambienteRect.pivot = Vector2.zero;
        ambiente = ambienteRect.gameObject.AddComponent<AmbienteCarreraGraphic>();
        ambiente.raycastTarget = false; ambiente.basePista = altoPista;
        DibujarLinea(mapa.largada, false); DibujarLinea(mapa.meta, true);
        caballos.SetAsLastSibling();
        if (carrera != null)
        {
            ActualizarInscripcion();
            var texto = owner.botonIniciar.GetComponentInChildren<TMP_Text>();
            if (texto != null) texto.text = carrera.iniciada ? "Reanudar carrera" : $"Iniciar · {Moneda.Texto(carrera.InscripcionPendiente)}";
        }
        Actualizar(0, mapa.largada, false, false);
    }

    internal void VincularJugador(RectTransform participante)
    {
        flecha = CrearPlano("IndicadorJugador", participante);
        flecha.anchorMin = flecha.anchorMax = new Vector2(.5f, 1);
        flecha.pivot = Vector2.zero;
        flecha.sizeDelta = new Vector2(34, 40); flecha.anchoredPosition = new Vector2(49, 4);
        flecha.localScale = Vector3.one * .5f;
        flecha.gameObject.AddComponent<FlechaJugadorGraphic>().raycastTarget = false;
        flecha.gameObject.SetActive(false);
    }
    internal void MarcarReferencia(RectTransform participante, bool propio)
    {
        var referencia = CrearPlano("PuntoCompetitivo", participante);
        referencia.anchorMin = referencia.anchorMax = new Vector2(.5f, 0);
        referencia.pivot = new Vector2(.5f, 0);
        referencia.sizeDelta = new Vector2(5, 5);

    }
    internal void Iniciar() { iniciada = true; reloj = 0; ActualizarInscripcion(); }
    void ActualizarInscripcion()
    {
        if (programada == null) return;
        informacion.text = $"Inscripción: {Moneda.Texto(programada.inscripcion)}" + (programada.inscripcionCobrada ? " · abonada" : " · al largar") +
            $"\nPremio: {Moneda.Texto(programada.PremioGanador)}" + (programada.iniciacion ? " · Iniciación sin apuestas" : " · Apuesta opcional");
    }
    internal void Actualizar(float dt, float xJugador, bool terminoJugador, bool teclaA)
    {
        if (mapa == null) return;
        if (iniciada) reloj += Mathf.Max(0, dt);
        float escala = Mathf.Max(.01f, viewport.rect.height / AltoMundo);
        float ancho = viewport.rect.width / escala;
        float objetivo = mapa.Encuadre(terminoJugador ? mapa.meta : xJugador, ancho);
        CamaraX = dt > 0 ? Mathf.Lerp(CamaraX, objetivo, 1 - Mathf.Exp(-6 * dt)) : objetivo;
        AdaptarGeometria();
        paisaje.camara = CamaraX; paisaje.tiempo = reloj; paisaje.ancho = ancho; paisaje.SetVerticesDirty();
        ambiente.camara = CamaraX * .7f; ambiente.tiempo = reloj; ambiente.ancho = mapa.longitud; ambiente.SetVerticesDirty();
        if (flecha != null) flecha.gameObject.SetActive(iniciada && (reloj < 2 || teclaA));
    }
    void AdaptarGeometria()
    {
        if (mapa == null || mundo == null || tribuna == null) return;
        float escala = Mathf.Max(.01f, viewport.rect.height / AltoMundo);
        CamaraX = Mathf.Clamp(CamaraX, 0, Mathf.Max(0, mapa.longitud - viewport.rect.width / escala));
        mundo.localScale = Vector3.one * escala;
        mundo.anchoredPosition = new Vector2(-CamaraX * escala, 0);
        tribuna.localScale = mundo.localScale;
        tribuna.anchoredPosition = new Vector2(-CamaraX * .7f * escala, 0);
        paisaje.ancho = viewport.rect.width / escala;
        paisaje.SetVerticesDirty();
    }
    internal void Finalizar() { if (flecha != null) flecha.gameObject.SetActive(false); iniciada = false; }
    internal void Liberar() { if (textura != null) UnityEngine.Object.Destroy(textura); }

    void CrearMosaicos(RectTransform padre, Rect uv, float y, float alto)
    {
        float ancho = textura != null ? alto * textura.width * uv.width / (textura.height * uv.height) : 1400;
        for (float x = 0; x < mapa.longitud + ancho; x += ancho)
        {
            var rect = CrearPlano("Tramo", padre); rect.pivot = Vector2.zero;
            rect.sizeDelta = new Vector2(ancho + 1, alto); rect.anchoredPosition = new Vector2(x, y);
            var image = rect.gameObject.AddComponent<RawImage>(); image.texture = textura; image.uvRect = uv; image.raycastTarget = false;
            if (textura == null) image.color = new Color32(69, 113, 45, 255);
        }
    }
    void DibujarLinea(float x, bool meta)
    {
        var linea = CrearPlano(meta ? "Meta" : "Largada", mundo);
        linea.anchoredPosition = new Vector2(x, 0); linea.sizeDelta = new Vector2(24, altoPista);
        if (meta)
            for (int fila = 0; fila < Mathf.CeilToInt(altoPista / 12); fila++)
                for (int columna = 0; columna < 2; columna++)
                    Placa(linea, "Cuadro", new Rect(columna * 12 - 12, fila * 12, 12, 12), (fila + columna) % 2 == 0 ? Color.white : new Color32(27, 30, 26, 255));
        else Placa(linea, "PinturaLargada", new Rect(-3, 0, 6, altoPista), new Color32(245, 235, 189, 255));
        var letrero = Placa(linea, "Cartel", new Rect(-98, altoPista + 124, 196, 35), new Color32(39, 37, 24, 255));
        var text = TablonLayout.Label("Texto", letrero, new Rect(0, 0, 196, 35), new Vector2(196, 35), owner.textoEstado.font,
            meta ? "META" : "LARGADA", 26, TextAlignmentOptions.Center);
        if (meta) tituloMeta = text;
    }
    static RectTransform CrearPlano(string nombre, Transform padre)
    {
        var r = (RectTransform)new GameObject(nombre, typeof(RectTransform)).transform;
        r.SetParent(padre, false); r.anchorMin = r.anchorMax = Vector2.zero; r.pivot = Vector2.zero; return r;
    }
    static RectTransform Placa(RectTransform padre, string nombre, Rect area, Color color)
    {
        var r = CrearPlano(nombre, padre); r.anchoredPosition = area.position; r.sizeDelta = area.size;
        var image = r.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return r;
    }
    static void Colocar(RectTransform r, RectTransform padre, Rect area)
    {
        r.SetParent(padre, false); r.localScale = Vector3.one;
        r.anchorMin = new Vector2(area.xMin / Lienzo.x, 1 - area.yMax / Lienzo.y);
        r.anchorMax = new Vector2(area.xMax / Lienzo.x, 1 - area.yMin / Lienzo.y); r.offsetMin = r.offsetMax = Vector2.zero;
    }
}

[RequireComponent(typeof(CanvasRenderer))]
internal sealed class FlechaJugadorGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        PixelCarrera.Rect(vh, new Rect(9, 16, 16, 24), new Color32(50, 39, 13, 255));
        PixelCarrera.Triangulo(vh, new Vector2(0, 21), new Vector2(34, 21), new Vector2(17, 0), new Color32(50, 39, 13, 255));
        PixelCarrera.Rect(vh, new Rect(12, 18, 10, 20), new Color32(255, 219, 50, 255));
        PixelCarrera.Triangulo(vh, new Vector2(5, 19), new Vector2(29, 19), new Vector2(17, 5), new Color32(255, 219, 50, 255));
    }
}

// Code-native pixel scenery. Flags and cheering cycles animate even with a stationary camera.
// Reuses the original audience/turf image; bespoke multilayer scenery remains provisional.
[RequireComponent(typeof(CanvasRenderer))]
internal sealed class AmbienteCarreraGraphic : MaskableGraphic
{
    internal float camara, tiempo, ancho;
    internal float alturaMundo = 900, basePista = 720;
    internal bool fondo;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); float escala = fondo ? rectTransform.rect.height / alturaMundo : 1;
        if (fondo)
        {
            PixelCarrera.Rect(vh, new Rect(0, 0, rectTransform.rect.width, rectTransform.rect.height), new Color32(131, 193, 211, 255));
            int inicio = Mathf.FloorToInt(camara * .12f / 80) - 2;
            for (int i = inicio; i < inicio + ancho / 80 + 6; i++)
            {
                float x = i * 80 - camara * .12f;
                float alto = 44 + 22 * Mathf.Sin(i * .7f);
                PixelCarrera.Rect(vh, new Rect(x * escala, (basePista + 112) * escala, 84 * escala, alto * escala), new Color32(99, 147, 131, 255));
            }
            return;
        }
        for (int i = 0; i < ancho / 300 + 1; i++)
        {
            float x = i * 300 + 140;
            PixelCarrera.Rect(vh, new Rect(x, basePista + 75, 4, 80), new Color32(67, 65, 51, 255));
            float movimiento = Mathf.Round(Mathf.Sin(tiempo * 5 + i) * 4);
            Color color = i % 2 == 0 ? new Color32(196, 67, 47, 255) : new Color32(237, 193, 67, 255);
            for (int franja = 0; franja < 6; franja++)
                PixelCarrera.Rect(vh, new Rect(x + 4 + franja * 6, basePista + 130 + Mathf.Round(Mathf.Sin(tiempo * 5 + i + franja * .65f) * 3), 6, 19 + movimiento), color);
            // Small additional audience sprites, with independent two-pose cheering arms.
            float persona = x + 85;
            PixelCarrera.Rect(vh, new Rect(persona, basePista + 55, 12, 16), color);
            PixelCarrera.Rect(vh, new Rect(persona + 2, basePista + 72, 8, 8), new Color32(231, 184, 137, 255));
            float brazo = Mathf.FloorToInt(tiempo * 3 + i) % 2 == 0 ? basePista + 68 : basePista + 77;
            PixelCarrera.Rect(vh, new Rect(persona - 5, brazo, 5, 9), new Color32(231, 184, 137, 255));
            PixelCarrera.Rect(vh, new Rect(persona + 12, brazo, 5, 9), new Color32(231, 184, 137, 255));
        }
    }
}

internal static class PixelCarrera
{
    internal static void Rect(VertexHelper vh, Rect r, Color32 c)
    {
        int i = vh.currentVertCount;
        vh.AddVert(new Vector3(r.xMin, r.yMin), c, Vector2.zero); vh.AddVert(new Vector3(r.xMin, r.yMax), c, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMax), c, Vector2.zero); vh.AddVert(new Vector3(r.xMax, r.yMin), c, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
    }
    internal static void Triangulo(VertexHelper vh, Vector2 a, Vector2 b, Vector2 d, Color32 c)
    {
        int i = vh.currentVertCount; vh.AddVert(a, c, Vector2.zero); vh.AddVert(b, c, Vector2.zero); vh.AddVert(d, c, Vector2.zero); vh.AddTriangle(i, i + 1, i + 2);
    }
}

internal sealed class DimensionesPista : MonoBehaviour
{
    internal Action cambio;
    void OnRectTransformDimensionsChange() => cambio?.Invoke();
}
