using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TablonLayout;

public sealed class SeleccionCarreraView : MonoBehaviour
{
    [SerializeField] Sprite marco, madera, trofeo, caballo;
    [SerializeField] Sprite maderaPanel, marcoBanner;
    [SerializeField] TMP_FontAsset fuente;

    static readonly Vector2 Lienzo = new(1672, 941);
    static readonly Vector2 Fila = new(1536, 134);
    static readonly Color Crema = new Color32(255, 235, 198, 255);
    static readonly Color Oro = new Color32(236, 177, 79, 255);
    static readonly Color MarcoFila = new Color32(190, 124, 60, 255);
    static readonly Color Tinta = new Color32(56, 28, 16, 255);
    static readonly CultureInfo Formato = CultureInfo.GetCultureInfo("es-AR");

    readonly List<(DefinicionCarrera carrera, Image borde, TMP_Text marca)> filas = new();
    RectTransform lista;
    ScrollRect scroll;
    TMP_Text tiempo, resumen, aviso;
    Button participar;
    DefinicionCarrera elegida;

    void OnEnable()
    {
        if (lista == null) Construir();
        Refrescar();
    }

    void Construir()
    {
        var raiz = (RectTransform)transform;
        var baseOpaca = Surface("Base", raiz, new Rect(Vector2.zero, Lienzo), Lienzo, Tinta).GetComponent<Image>();
        baseOpaca.raycastTarget = true;
        Madera("Madera", raiz, new Rect(Vector2.zero, Lienzo), Lienzo, madera);
        Marco("Marco", raiz, new Rect(Vector2.zero, Lienzo), Lienzo, marco);
        Surface("SeparadorCabecera", raiz, new Rect(12, 147, 1648, 2), Lienzo,
            new Color32(123, 69, 29, 255));
        Surface("SeparadorPie", raiz, new Rect(12, 781, 1648, 2), Lienzo,
            new Color32(176, 110, 50, 255));

        Texto("Titulo", raiz, new Rect(360, 16, 952, 72), Lienzo, "ELEGIR CARRERA", 69);
        tiempo = Texto("DiaSemana", raiz, new Rect(550, 93, 572, 52), Lienzo, "", 38);
        var volver = CrearBoton("Volver", raiz, new Rect(1417, 41, 194, 62), "‹  Volver", 34, Crema, Tinta);
        volver.onClick.AddListener(() => GameManager.Instancia.VolverAlHub());

        var zona = Rect("Ofertas", raiz, new Rect(66, 165, 1540, 597), Lienzo);
        var viewport = Surface("Viewport", zona, new Rect(0, 0, 1, 1), Vector2.one, Color.clear);
        viewport.GetComponent<Image>().raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        lista = Rect("Lista", viewport, new Rect(0, 0, 1, 1), Vector2.one);
        lista.anchorMin = new Vector2(0, 1);
        lista.anchorMax = Vector2.one;
        lista.pivot = new Vector2(.5f, 1);
        lista.anchoredPosition = Vector2.zero;
        lista.sizeDelta = Vector2.zero;
        var layout = lista.gameObject.AddComponent<FilasSeleccionCarrera>();
        layout.padding = new RectOffset(2, 2, 2, 2);
        layout.childControlHeight = layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        lista.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll = zona.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = lista;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 34;

        Madera("Pie", raiz, new Rect(28, 789, 1616, 126), Lienzo, maderaPanel);
        Icono("Caballo", raiz, new Rect(77, 814, 92, 77), Lienzo, caballo);
        Surface("Separador", raiz, new Rect(199, 816, 2, 68), Lienzo, Oro);
        resumen = Texto("CarreraSeleccionada", raiz, new Rect(232, 806, 918, 63), Lienzo, "", 43, true);
        aviso = Texto("Aviso", raiz, new Rect(232, 870, 918, 29), Lienzo, "", 22, true);
        participar = CrearBoton("Participar", raiz, new Rect(1186, 803, 414, 90), "Seleccionar", 49,
            Color.white, Crema, conMadera: true);
        participar.onClick.AddListener(Participar);
    }

    void Refrescar()
    {
        var juego = GameManager.Instancia;
        tiempo.text = $"Día {juego.dia} · Semana {GameManager.ObtenerSemana(juego.dia)}";
        for (int i = lista.childCount - 1; i >= 0; i--)
        {
            var fila = lista.GetChild(i);
            fila.gameObject.SetActive(false);
            fila.SetParent(null, false);
            Destroy(fila.gameObject);
        }
        filas.Clear();

        var catalogo = juego.CatalogoDeCarreras;
        var oferta = juego.ObtenerOfertaCarreras();
        var guardada = juego.ObtenerCarreraProgramada();
        foreach (var carrera in oferta)
            CrearFila(carrera, catalogo?.atlasBanners);

        aviso.text = juego.campanaCompletada ? "Campaña completada." :
            guardada?.SeleccionBloqueada == true ? "La parrilla ya está confirmada para esta carrera." :
            oferta.Count == 0 ? "No hay carreras disponibles." : "";
        Seleccionar(guardada == null ? null : oferta.FirstOrDefault(c => c.id == guardada.idDefinicion));
        Canvas.ForceUpdateCanvases();
        scroll.verticalNormalizedPosition = 1;
    }

    void CrearFila(DefinicionCarrera carrera, Texture2D atlas)
    {
        var rect = Surface("Carrera_" + (carrera.id ?? "guardada"), lista, new Rect(0, 0, 1, 1), Vector2.one, Color.clear);
        rect.gameObject.AddComponent<LayoutElement>();
        rect.GetComponent<Image>().raycastTarget = true;

        var maderaFila = Madera("Tabla", rect, new Rect(3, 3, 1530, 128), Fila, maderaPanel);

        if (carrera.bannerAlternativo != null || (atlas != null && carrera.indiceBanner >= 0 && carrera.indiceBanner < 16))
        {
            var ilustracion = Rect("Paisaje", rect, new Rect(835, 4, 697, 126), Fila).gameObject.AddComponent<BannerCarreraGraphic>();
            ilustracion.Configurar(atlas, carrera.indiceBanner, carrera.bannerAlternativo);
            ilustracion.raycastTarget = false;
        }

        Icono("Trofeo", rect, new Rect(45, 23, 92, 86), Fila, trofeo);
        string distancia = carrera.distanciaMetros > 0 ? $" · {carrera.distanciaMetros} m" : "";
        Texto("NombreDistancia", rect, new Rect(166, 10, 747, 59), Fila, carrera.nombre + distancia, 50, true);
        string tipo = carrera.distanciaMetros > 0 ? $"Tipo: {carrera.TipoVisible}   │   " : "";
        Texto("TipoPremio", rect, new Rect(170, 73, 663, 46), Fila,
            tipo + $"Premio: ${carrera.premio.ToString("N0", Formato)}", 30, true);
        var marca = Texto("MarcaSeleccion", rect, new Rect(8, 33, 35, 62), Fila, "›", 43);
        marca.color = Oro;
        var borde = Marco("Borde", rect, new Rect(Vector2.zero, Fila), Fila, marcoBanner);
        borde.color = MarcoFila;
        var brillo = Marco("Hover", rect, new Rect(Vector2.zero, Fila), Fila, marcoBanner);
        brillo.color = new Color32(255, 225, 162, 220);
        var halo = brillo.gameObject.AddComponent<Outline>();
        halo.effectColor = new Color32(255, 186, 76, 100);
        halo.effectDistance = new Vector2(2, -2);

        var boton = rect.gameObject.AddComponent<BotonBannerCarrera>();
        boton.Configurar(brillo);
        boton.targetGraphic = maderaFila;
        boton.transition = Selectable.Transition.None;
        boton.onClick.AddListener(() => Seleccionar(carrera));
        filas.Add((carrera, borde, marca));
    }

    void Seleccionar(DefinicionCarrera carrera)
    {
        elegida = carrera;
        foreach (var fila in filas)
        {
            bool marcada = ReferenceEquals(carrera, fila.carrera);
            fila.borde.color = marcada ? new Color32(255, 234, 168, 255) : MarcoFila;
            fila.marca.enabled = marcada;
        }
        resumen.text = carrera == null ? "Elegí una carrera" : "Carrera seleccionada: " + carrera.nombre;
        participar.interactable = carrera != null;
    }

    void Participar()
    {
        if (elegida == null) return;
        if (!GameManager.Instancia.ElegirCarrera(elegida.id, out string mensaje))
        {
            aviso.text = mensaje;
            return;
        }

        UIManager.instance.AbrirTablon();
    }

    TMP_Text Texto(string nombre, RectTransform padre, Rect posicion, Vector2 medida, string texto, float tamano,
        bool izquierda = false, Color? color = null)
    {
        var etiqueta = Label(nombre, padre, posicion, medida, fuente, texto, tamano,
            izquierda ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center, color ?? Crema);
        etiqueta.textWrappingMode = TextWrappingModes.NoWrap;
        etiqueta.overflowMode = TextOverflowModes.Ellipsis;
        etiqueta.richText = false;
        return etiqueta;
    }

    static void Icono(string nombre, RectTransform padre, Rect posicion, Vector2 medida, Sprite sprite)
    {
        if (sprite == null) return;
        var image = Rect(nombre, padre, posicion, medida).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    Button CrearBoton(string nombre, RectTransform padre, Rect posicion, string texto, float tamano,
        Color cara, Color tinta, bool conMadera = false)
    {
        var rect = Surface(nombre, padre, posicion, Lienzo, MarcoFila);
        var borde = rect.GetComponent<Image>();
        borde.raycastTarget = true;
        var interior = new Rect(3, 3, posicion.width - 6, posicion.height - 6);
        var frente = conMadera
            ? Madera("Cara", rect, interior, posicion.size, maderaPanel)
            : Surface("Cara", rect, interior, posicion.size, cara).GetComponent<Graphic>();
        frente.color = cara;
        borde.color = Color.clear;
        Marco("Borde", rect, new Rect(Vector2.zero, posicion.size), posicion.size, marcoBanner).color = MarcoFila;
        var boton = rect.gameObject.AddComponent<Button>();
        boton.targetGraphic = frente;
        var colores = ColorBlock.defaultColorBlock;
        colores.normalColor = Color.white;
        colores.selectedColor = colores.normalColor;
        colores.highlightedColor = Color.white;
        colores.disabledColor = new Color32(128, 125, 122, 255);
        boton.colors = colores;
        Texto("Etiqueta", rect, new Rect(9, 4, posicion.width - 18, posicion.height - 8), posicion.size, texto, tamano, color: tinta);
        return boton;
    }

    static Graphic Madera(string nombre, RectTransform padre, Rect posicion, Vector2 medida, Sprite sprite)
    {
        var imagen = Rect(nombre, padre, posicion, medida).gameObject.AddComponent<MaderaSelectorGraphic>();
        imagen.texture = sprite != null ? sprite.texture : null;
        imagen.raycastTarget = false;
        return imagen;
    }

    static Image Marco(string nombre, RectTransform padre, Rect posicion, Vector2 medida, Sprite sprite)
    {
        var imagen = Rect(nombre, padre, posicion, medida).gameObject.AddComponent<Image>();
        imagen.sprite = sprite;
        imagen.type = Image.Type.Sliced;
        imagen.fillCenter = false;
        imagen.raycastTarget = false;
        return imagen;
    }
}

internal sealed class FilasSeleccionCarrera : VerticalLayoutGroup
{
    public override void CalculateLayoutInputVertical()
    {
        float alto = ((RectTransform)transform.parent).rect.height;
        spacing = alto * 15 / 597;
        float altoFila = Mathf.Max(1, (alto - 3 * spacing - padding.vertical) / 4);
        foreach (var fila in rectChildren)
            fila.GetComponent<LayoutElement>().minHeight = altoFila;
        base.CalculateLayoutInputVertical();
    }
}
