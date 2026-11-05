using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public partial class EstabloView
{
    private static readonly Vector2 Lienzo = new(1672, 941);
    private static readonly Color Crema = new Color32(255, 239, 212, 255);
    private static readonly Color Oro = new Color32(224, 157, 67, 255);
    private static readonly Color Borde = new Color32(115, 62, 30, 255);
    private static readonly Color Roble = new Color32(145, 111, 83, 255);
    private static readonly Color Sombra = new Color32(105, 72, 50, 255);
    private static readonly Color Dorado = new Color32(255, 186, 88, 255);
    private static readonly Color Rojo = new Color32(206, 83, 58, 255);

    private RectTransform raiz;
    private readonly RectTransform[] paneles = new RectTransform[4];
    private readonly Button[] pestanas = new Button[4];
    private readonly TMP_Text[] valoresStats = new TMP_Text[10];
    private readonly TMP_Text[] resumenUp = new TMP_Text[4];
    private readonly int[] indicesResumen = { 1, 2, 0, 8 };
    private readonly TMP_Text[] mejorasUp = new TMP_Text[4];
    private readonly TMP_Text[] pendientesUp = new TMP_Text[4];
    private readonly Button[] botonesUp = new Button[4];
    private TMP_Text nombreStats, nombreUp, puntosUp, potencialUp, mensajeUp;
    private TMP_Text cantidadCaballos, padreCriadero;
    private TMP_Text nombreItems, tituloItems, inventarioVacio, mensajeItems;
    private TMP_Text tituloModal, cuerpoModal;
    private Image retratoEnStats, retratoEnUp, imagenPadre, maniquiItems;
    private Button confirmarUp, descripcionItems, confirmarModal;
    private Button cerrarModal;
    private RectTransform listaCaballos, listaInventario;
    private CanvasGroup grupoContenido, grupoNavegacion;
    private GameObject focoAnterior;
    private GameObject modal;

    private void ConstruirInterfaz()
    {
        if (raiz != null)
            return;

        Transform vista = transform.parent;
        foreach (Transform anterior in vista)
            if (anterior != transform)
                anterior.gameObject.SetActive(false);

        raiz = RectUI("EstabloRustico", vista, new Rect(Vector2.zero, Lienzo), Lienzo);
        var baseOpaca = Imagen("Base", raiz, new Rect(Vector2.zero, Lienzo), Lienzo,
            null, new Color32(28, 15, 9, 255));
        baseOpaca.raycastTarget = true;

        if (marco != null)
        {
            // Omit the transparent gutter on the source sprite before slicing the corners.
            Rect rect = marco.rect;
            rect.x += rect.width * 25 / 1536;
            rect.y += rect.height * 34 / 1024;
            rect.width *= 1486f / 1536;
            rect.height *= 945f / 1024;
            marcoRecortado = Sprite.Create(marco.texture, rect, new Vector2(.5f, .5f),
                100, 0, SpriteMeshType.FullRect, new Vector4(96, 96, 96, 96));
            var exterior = Imagen("MarcoExterior", raiz, new Rect(Vector2.zero, Lienzo), Lienzo, marcoRecortado, Color.white);
            exterior.type = Image.Type.Sliced;
            exterior.pixelsPerUnitMultiplier = 1.4f;
        }

        var cuerpo = RectUI("Contenido", raiz, new Rect(Vector2.zero, Lienzo), Lienzo);
        grupoContenido = cuerpo.gameObject.AddComponent<CanvasGroup>();
        string[] nombres = { "Stats", "Up", "Procrear", "Items" };
        for (int i = 0; i < paneles.Length; i++)
            paneles[i] = RectUI(nombres[i], cuerpo, new Rect(Vector2.zero, Lienzo), Lienzo);

        var barra = RectUI("NavegacionEstablo", raiz, new Rect(574, 54, 1044, 74), Lienzo);
        grupoNavegacion = barra.gameObject.AddComponent<CanvasGroup>();
        var medidaBarra = new Vector2(1044, 74);
        for (int i = 0; i < pestanas.Length; i++)
        {
            var pestana = (Pestana)i;
            pestanas[i] = Boton("Pestana" + nombres[i], barra, new Rect(i * 235, 0, 222, 74),
                medidaBarra, nombres[i], 34, () => Mostrar(pestana), Sombra);
        }

        Boton("Volver", barra, new Rect(940, 0, 104, 74), medidaBarra, "←", 51, CerrarCaballo, Rojo);
        ConstruirStats();
        ConstruirUp();
        ConstruirCriadero();
        ConstruirItems();
        ConstruirModal();
    }

    private void ConstruirStats()
    {
        var panel = paneles[(int)Pestana.Stats];
        (nombreStats, retratoEnStats) = CabeceraCaballo(panel);
        Placa("FichaStats", panel, new Rect(574, 145, 1044, 734), Lienzo, true);
        for (int i = 0; i < NombresStats.Length; i++)
        {
            float y = 181 + i * 63;
            IconoStat(panel, i, new Rect(663, y, 88, 57));
            Texto(NombresStats[i], panel, new Rect(782, y, 330, 52), Lienzo, NombresStats[i], 36, TextAlignmentOptions.MidlineLeft);
            Linea(panel, new Rect(1130, y + 9, 2, 38), Oro);
            valoresStats[i] = Texto("Valor" + NombresStats[i], panel, new Rect(1182, y, 357, 52), Lienzo,
                "", 36, TextAlignmentOptions.MidlineLeft);
            if (i < NombresStats.Length - 1)
                Linea(panel, new Rect(623, y + 59, 944, 1.5f), Borde);
        }
    }

    private void ConstruirUp()
    {
        var panel = paneles[(int)Pestana.Up];
        (nombreUp, retratoEnUp) = CabeceraCaballo(panel);
        Placa("Resumen", panel, new Rect(87, 680, 431, 171), Lienzo, true);
        BotonPlaca("TituloResumen", panel, new Rect(188, 660, 229, 43), Lienzo, "Resumen", 27);
        for (int i = 0; i < indicesResumen.Length; i++)
        {
            int indice = indicesResumen[i];
            float y = 704 + i * 34;
            IconoStat(panel, indice, new Rect(110, y + 2, 48, 30));
            Texto("Resumen" + NombresStats[indice], panel, new Rect(173, y, 154, 32), Lienzo,
                NombresStats[indice], 25, TextAlignmentOptions.MidlineLeft);
            resumenUp[i] = Texto("ValorResumen" + indice, panel, new Rect(331, y, 158, 32), Lienzo,
                "", 25, TextAlignmentOptions.MidlineRight);
            if (i < indicesResumen.Length - 1)
                Linea(panel, new Rect(113, y + 32, 376, 1), Borde);
        }

        Placa("FichaUp", panel, new Rect(574, 145, 1044, 734), Lienzo, true);
        Texto("TituloUp", panel, new Rect(782, 155, 631, 68), Lienzo, "Mejora de atributos", 47);
        Linea(panel, new Rect(630, 190, 142, 2), Oro);
        Linea(panel, new Rect(1420, 190, 142, 2), Oro);
        puntosUp = BotonPlaca("PuntosDisponibles", panel, new Rect(830, 230, 530, 65), Lienzo, "", 37);
        for (int i = 0; i < mejorasUp.Length; i++)
        {
            var estadistica = (TipoEstadisticaCaballo)i;
            float y = 316 + i * 78;
            Placa("Fila" + estadistica, panel, new Rect(625, y, 942, 69), Lienzo, true);
            IconoStat(panel, i + 3, new Rect(655, y + 6, 87, 56));
            mejorasUp[i] = Texto("Mejora" + estadistica, panel, new Rect(778, y + 5, 516, 58), Lienzo,
                "", 35, TextAlignmentOptions.MidlineLeft);
            pendientesUp[i] = Texto("Pendiente" + estadistica, panel, new Rect(1305, y + 5, 97, 58), Lienzo, "", 35);
            pendientesUp[i].color = Oro;
            botonesUp[i] = Boton("Sumar" + estadistica, panel, new Rect(1458, y + 5, 84, 59), Lienzo,
                "+", 40, () => PrepararMejora(estadistica), Dorado);
        }

        Placa("FilaPotencial", panel, new Rect(625, 628, 942, 69), Lienzo, true);
        IconoStat(panel, 7, new Rect(655, 634, 87, 56));
        potencialUp = Texto("Potencial", panel, new Rect(778, 634, 680, 57), Lienzo, "", 35, TextAlignmentOptions.MidlineLeft);
        confirmarUp = Boton("ConfirmarMejoras", panel, new Rect(887, 728, 430, 86), Lienzo, "Confirmar", 43, ConfirmarMejoras, Roble);
        mensajeUp = Texto("MensajeMejoras", panel, new Rect(660, 820, 880, 38), Lienzo, "", 25);
    }

    private (TMP_Text, Image) CabeceraCaballo(RectTransform panel)
    {
        Placa("FichaCaballo", panel, new Rect(51, 54, 502, 825), Lienzo);
        var nombre = BotonPlaca("NombreCaballo", panel, new Rect(102, 106, 400, 75), Lienzo, "", 51);
        var retrato = Imagen("RetratoGris", panel, new Rect(82, 200, 439, 438), Lienzo, retratoStats, Color.white);
        retrato.preserveAspect = true;
        Flechas(panel, 183);
        return (nombre, retrato);
    }

    private void Flechas(RectTransform panel, float y)
    {
        flechasCaballos.Add(Boton("CaballoAnterior", panel, new Rect(58, y, 43, 46), Lienzo, "‹", 32, SeleccionarAnterior, Sombra));
        flechasCaballos.Add(Boton("CaballoSiguiente", panel, new Rect(503, y, 43, 46), Lienzo, "›", 32, SeleccionarSiguiente, Sombra));
    }

    private void ConstruirCriadero()
    {
        var panel = paneles[(int)Pestana.Procrear];
        Placa("MisCaballos", panel, new Rect(51, 54, 502, 825), Lienzo);
        cantidadCaballos = BotonPlaca("CantidadCaballos", panel, new Rect(84, 76, 435, 67), Lienzo, "", 38);
        listaCaballos = Lista(panel, "ListaCaballos", new Rect(81, 161, 441, 678));

        Placa("FondoCriadero", panel, new Rect(574, 145, 1044, 734), Lienzo);
        Madera("InteriorEstablo", panel, new Rect(580, 151, 1032, 722), Lienzo, fondoCriadero, new Color32(156, 133, 116, 255));
        Texto("TituloCriadero", panel, new Rect(800, 157, 588, 61), Lienzo, "Criadero", 51);
        Texto("InstruccionCriadero", panel, new Rect(625, 221, 943, 42), Lienzo,
            "La crianza todavía no está disponible", 27);

        Placa("Padre", panel, new Rect(644, 281, 392, 371), Lienzo, true);
        BotonPlaca("TituloPadre", panel, new Rect(699, 271, 285, 56), Lienzo, "Padre / Madre", 29);
        imagenPadre = Imagen("SpritePadre", panel, new Rect(670, 343, 340, 237), Lienzo, null, Color.white);
        imagenPadre.preserveAspect = true;
        padreCriadero = BotonPlaca("NombrePadre", panel, new Rect(691, 592, 300, 47), Lienzo, "", 29);
        Texto("Union", panel, new Rect(1042, 392, 111, 108), Lienzo, "+", 68).color = Oro;
        Placa("Pareja", panel, new Rect(1161, 281, 392, 371), Lienzo, true);
        BotonPlaca("TituloPareja", panel, new Rect(1216, 271, 285, 56), Lienzo, "Pareja", 29);
        var silueta = Imagen("SiluetaPareja", panel, new Rect(1187, 343, 340, 237), Lienzo,
            spriteManiquiItems, new Color32(105, 67, 36, 255));
        silueta.preserveAspect = true;
        silueta.enabled = spriteManiquiItems != null;
        Boton("SeleccionarPareja", panel, new Rect(1197, 590, 319, 49), Lienzo,
            "Seleccionar caballo", 27, null, Sombra).interactable = false;

        BotonPlaca("Compatibilidad", panel, new Rect(721, 666, 752, 68), Lienzo, "Compatibilidad  ·  — %", 33);
        Boton("Procrear", panel, new Rect(909, 755, 374, 81), Lienzo, "Procrear", 39, null, Dorado).interactable = false;
    }

    private void ConstruirItems()
    {
        var panel = paneles[(int)Pestana.Items];
        Placa("PanelInventario", panel, new Rect(51, 54, 502, 825), Lienzo);
        nombreItems = BotonPlaca("CaballoInventario", panel, new Rect(102, 76, 400, 70), Lienzo, "", 49);
        Flechas(panel, 89);
        Placa("MarcoListaItems", panel, new Rect(70, 169, 463, 601), Lienzo, true);
        listaInventario = Lista(panel, "Inventario", new Rect(82, 184, 437, 572));
        inventarioVacio = Texto("InventarioVacio", panel, new Rect(84, 187, 434, 111), Lienzo, "Inventario vacío", 31);
        descripcionItems = Boton("DescripcionItem", panel, new Rect(88, 790, 428, 64), Lienzo,
            "Selecciona un ítem", 32, MostrarDescripcion, Dorado);

        Placa("Equipamiento", panel, new Rect(574, 145, 1044, 734), Lienzo, true);
        tituloItems = Texto("TituloEquipamiento", panel, new Rect(627, 159, 938, 78), Lienzo, "", 48);
        maniquiItems = Imagen("CaballoEquipamiento", panel, new Rect(817, 235, 628, 556), Lienzo, spriteManiquiItems, Color.white);
        maniquiItems.preserveAspect = true;
        Ranura(panel, TipoSlotItem.Cabeza, new Rect(628, 264, 289, 108), new Vector2(999, 420), false);
        Ranura(panel, TipoSlotItem.Espalda, new Rect(1301, 331, 264, 108), new Vector2(1216, 480), true);
        Ranura(panel, TipoSlotItem.PatasDelanteras, new Rect(628, 627, 289, 108), new Vector2(1043, 685), false);
        Ranura(panel, TipoSlotItem.PatasTraseras, new Rect(1301, 627, 264, 108), new Vector2(1243, 685), true);

        Texto("AyudaEquipamiento", panel, new Rect(613, 779, 965, 36), Lienzo,
            $"Arrastra un ítem a su ranura · Equipar ${SistemaEquipamiento.CostoEquiparItem} · Reemplazar ${SistemaEquipamiento.CostoEquiparItem + SistemaEquipamiento.CostoDesequiparItem}", 23);
        mensajeItems = Texto("MensajeItems", panel, new Rect(613, 825, 965, 36), Lienzo, "", 25);
        mensajeItems.color = Oro;
    }

    private void Ranura(RectTransform panel, TipoSlotItem ranura, Rect bounds, Vector2 punto, bool derecha)
    {
        var borde = new Vector2(derecha ? bounds.xMin : bounds.xMax, bounds.center.y);
        var codo = new Vector2(Mathf.Lerp(borde.x, punto.x, .4f), borde.y);
        Segmento(panel, borde, codo);
        Segmento(panel, codo, punto);
        Imagen("Punto" + ranura, panel, new Rect(punto.x - 5, punto.y - 5, 10, 10), Lienzo, null, Oro);
        var tarjeta = Placa("Ranura" + ranura, panel, bounds, Lienzo);
        tarjeta.GetComponent<Image>().raycastTarget = true;
        tarjeta.gameObject.AddComponent<EstabloDestinoEquipo>().Configurar(item => EquiparEnRanura(ranura, item));
        Texto("Titulo", tarjeta, new Rect(5, 5, bounds.width - 10, 47), bounds.size,
            SistemaEquipamiento.NombreRanura(ranura), 30);
        var boton = Boton("Estado", tarjeta, new Rect(10, 51, bounds.width - 20, 46), bounds.size,
            "Vacío", 25, () => ActivarRanura(ranura), Sombra);
        botonesRanura.Add(ranura, boton);
    }

    private void ConstruirModal()
    {
        var velo = Imagen("DetalleItemModal", raiz, new Rect(Vector2.zero, Lienzo), Lienzo,
            null, new Color32(0, 0, 0, 195));
        velo.raycastTarget = true;
        modal = velo.gameObject;
        Placa("MarcoDetalle", velo.rectTransform, new Rect(423, 245, 826, 450), Lienzo);
        tituloModal = BotonPlaca("TituloDetalle", velo.rectTransform, new Rect(478, 279, 716, 75), Lienzo, "", 44);
        cuerpoModal = Texto("ContenidoDetalle", velo.rectTransform, new Rect(500, 367, 672, 200), Lienzo,
            "", 31, TextAlignmentOptions.MidlineLeft);
        cuerpoModal.textWrappingMode = TextWrappingModes.Normal;
        cerrarModal = Boton("CerrarDetalle", velo.rectTransform, new Rect(515, 599, 261, 62), Lienzo,
            "Cerrar", 30, OcultarModal, Sombra);
        confirmarModal = Boton("ConfirmarDetalle", velo.rectTransform, new Rect(892, 599, 261, 62), Lienzo,
            "Desequipar", 30, null, Dorado);
        modal.SetActive(false);
    }

    private void MostrarModal(string titulo, string cuerpo, UnityAction accion = null, bool habilitada = true)
    {
        tituloModal.text = titulo;
        cuerpoModal.text = cuerpo;
        confirmarModal.gameObject.SetActive(accion != null);
        confirmarModal.interactable = habilitada;
        confirmarModal.onClick.RemoveAllListeners();
        if (accion != null)
        {
            confirmarModal.onClick.AddListener(() =>
            {
                OcultarModal();
                accion();
            });
        }

        focoAnterior = EventSystem.current?.currentSelectedGameObject;
        grupoContenido.interactable = grupoNavegacion.interactable = false;
        modal.SetActive(true);
        cerrarModal.Select();
    }

    private void OcultarModal()
    {
        if (!modal.activeSelf)
            return;

        modal.SetActive(false);
        grupoContenido.interactable = grupoNavegacion.interactable = true;
        if (focoAnterior != null && focoAnterior.activeInHierarchy)
            EventSystem.current?.SetSelectedGameObject(focoAnterior);
        focoAnterior = null;
    }

    private RectTransform Lista(RectTransform panel, string nombre, Rect bounds)
    {
        var scrollRoot = RectUI(nombre, panel, bounds, Lienzo);
        var vista = Imagen("Viewport", scrollRoot, new Rect(0, 0, bounds.width - 18, bounds.height),
            bounds.size, null, Color.clear);
        vista.raycastTarget = true;
        vista.gameObject.AddComponent<RectMask2D>();

        var contenido = RectUI("Filas", vista.rectTransform, new Rect(0, 0, 1, 1), Vector2.one);
        contenido.anchorMin = new Vector2(0, 1);
        contenido.anchorMax = Vector2.one;
        contenido.pivot = new Vector2(.5f, 1);
        contenido.anchoredPosition = Vector2.zero;
        contenido.sizeDelta = Vector2.zero;
        var vertical = contenido.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.spacing = 9;
        vertical.padding = new RectOffset(4, 4, 4, 4);
        vertical.childControlHeight = vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false;
        contenido.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var pista = Imagen("Pista", scrollRoot, new Rect(bounds.width - 13, 0, 11, bounds.height), bounds.size, null, Borde);
        pista.raycastTarget = true;
        var tirador = Imagen("Tirador", pista.rectTransform, new Rect(0, 0, 1, 1), Vector2.one, null, Oro);
        tirador.raycastTarget = true;
        var barra = pista.gameObject.AddComponent<Scrollbar>();
        barra.handleRect = tirador.rectTransform;
        barra.targetGraphic = tirador;
        barra.direction = Scrollbar.Direction.BottomToTop;

        var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = vista.rectTransform;
        scroll.content = contenido;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 39;
        scroll.verticalScrollbar = barra;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        return contenido;
    }

    private Button Fila(string nombre, RectTransform lista, string texto, Sprite sprite)
    {
        var boton = Boton(nombre, lista, new Rect(0, 0, 1, 1), Vector2.one, texto, 28, null, Sombra);
        boton.gameObject.AddComponent<LayoutElement>().preferredHeight = 74;
        var icono = Imagen("Icono", (RectTransform)boton.transform, new Rect(.03f, .08f, .18f, .84f),
            Vector2.one, sprite, Color.white);
        icono.preserveAspect = true;
        icono.enabled = sprite != null;
        var etiqueta = Etiqueta(boton);
        etiqueta.rectTransform.anchorMin = new Vector2(.25f, .04f);
        etiqueta.rectTransform.anchorMax = new Vector2(.96f, .96f);
        etiqueta.rectTransform.offsetMin = etiqueta.rectTransform.offsetMax = Vector2.zero;
        etiqueta.alignment = TextAlignmentOptions.MidlineLeft;
        return boton;
    }

    private void QuitarFilas(RectTransform lista)
    {
        for (int i = lista.childCount - 1; i >= 0; i--)
        {
            var fila = lista.GetChild(i);
            fila.gameObject.SetActive(false);
            fila.SetParent(null, false);
            Destroy(fila.gameObject);
        }
    }

    private Button Boton(string nombre, RectTransform padre, Rect bounds, Vector2 medida, string titulo,
        float tamano, UnityAction accion, Color normal)
    {
        var borde = Imagen(nombre, padre, bounds, medida, null, Borde);
        var cara = Madera("Madera", borde.rectTransform, new Rect(.01f, .045f, .98f, .91f),
            Vector2.one, madera, Color.white, bounds.width / bounds.height);
        cara.raycastTarget = true;

        var boton = borde.gameObject.AddComponent<Button>();
        boton.targetGraphic = cara;
        Colores(boton, normal, false);
        Texto("Etiqueta", borde.rectTransform, new Rect(.04f, .05f, .92f, .9f), Vector2.one, titulo, tamano);
        if (accion != null)
            boton.onClick.AddListener(accion);
        return boton;
    }

    private static void Colores(Button boton, Color normal, bool seleccionado)
    {
        var colores = ColorBlock.defaultColorBlock;
        colores.normalColor = normal;
        colores.highlightedColor = Color.Lerp(normal, new Color32(255, 215, 140, 255), .43f);
        colores.pressedColor = Color.Lerp(normal, Color.black, .18f);
        colores.selectedColor = normal;
        colores.disabledColor = new Color32(93, 83, 76, 255);
        colores.fadeDuration = .08f;
        boton.colors = colores;
        boton.GetComponent<Image>().color = seleccionado ? Oro : Borde;
    }

    private static void Resaltar(Button boton, bool seleccionado) => Colores(boton, seleccionado ? Dorado : Sombra, seleccionado);
    private static TMP_Text Etiqueta(Button boton) => boton.GetComponentInChildren<TMP_Text>(true);

    private TMP_Text BotonPlaca(string nombre, RectTransform padre, Rect bounds, Vector2 medida, string texto, float tamano)
    {
        var placa = Placa(nombre, padre, bounds, medida);
        return Texto("Etiqueta", placa, new Rect(8, 2, bounds.width - 16, bounds.height - 4), bounds.size, texto, tamano);
    }

    private RectTransform Placa(string nombre, RectTransform padre, Rect bounds, Vector2 medida, bool oscura = false)
    {
        var borde = Imagen(nombre, padre, bounds, medida, null, Borde).rectTransform;
        Imagen("Bisel", borde, new Rect(2, 2, bounds.width - 4, bounds.height - 4), bounds.size,
            null, new Color32(61, 33, 18, 255));
        Madera("Tabla", borde, new Rect(4, 4, bounds.width - 8, bounds.height - 8), bounds.size,
            madera, oscura ? new Color32(66, 48, 35, 255) : Roble);
        return borde;
    }

    private void IconoStat(RectTransform panel, int indice, Rect bounds)
    {
        if (iconosStats == null)
            return;

        var icono = Madera("Icono" + NombresStats[indice], panel, bounds, Lienzo, iconosStats, Color.white);
        icono.uvRect = new Rect(indice / 10f, 0, .1f, 1);
    }

    private static void Linea(RectTransform panel, Rect bounds, Color color) =>
        Imagen("Separador", panel, bounds, Lienzo, null, color);

    private static void Segmento(RectTransform panel, Vector2 inicio, Vector2 fin)
    {
        Vector2 delta = fin - inicio;
        Vector2 centro = (inicio + fin) * .5f;
        var linea = Imagen("Conector", panel, new Rect(centro.x - delta.magnitude * .5f, centro.y - 1.5f,
            delta.magnitude, 3), Lienzo, null, Oro);
        linea.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(-delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private TMP_Text Texto(string nombre, RectTransform padre, Rect bounds, Vector2 medida,
        string contenido, float tamano, TextAlignmentOptions alineacion = TextAlignmentOptions.Center)
    {
        var texto = RectUI(nombre, padre, bounds, medida).gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null)
            texto.font = fuente;
        texto.text = contenido;
        texto.fontStyle = FontStyles.Bold;
        texto.fontSize = tamano;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = tamano * .52f;
        texto.fontSizeMax = tamano;
        texto.color = Crema;
        texto.alignment = alineacion;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.overflowMode = TextOverflowModes.Ellipsis;
        texto.richText = false;
        texto.raycastTarget = false;
        return texto;
    }

    private static void AsignarSprite(Image imagen, Sprite sprite)
    {
        imagen.sprite = sprite;
        imagen.enabled = sprite != null;
    }

    private static Image Imagen(string nombre, RectTransform padre, Rect bounds, Vector2 medida, Sprite sprite, Color color)
    {
        var imagen = RectUI(nombre, padre, bounds, medida).gameObject.AddComponent<Image>();
        imagen.sprite = sprite;
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    private static RawImage Madera(string nombre, RectTransform padre, Rect bounds, Vector2 medida,
        Sprite sprite, Color color, float aspecto = 0)
    {
        var imagen = RectUI(nombre, padre, bounds, medida).gameObject.AddComponent<RawImage>();
        imagen.texture = sprite != null ? sprite.texture : null;
        imagen.color = color;
        imagen.raycastTarget = false;
        if (sprite != null)
        {
            float relacion = (aspecto > 0 ? aspecto : bounds.width / bounds.height) / (sprite.rect.width / sprite.rect.height);
            var recorte = relacion > 1 ? new Vector2(1, 1 / relacion) : new Vector2(relacion, 1);
            imagen.uvRect = new Rect((Vector2.one - recorte) * .5f, recorte);
        }
        return imagen;
    }

    private static RectTransform RectUI(string nombre, Transform padre, Rect bounds, Vector2 medida)
    {
        var objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.layer = padre.gameObject.layer;
        var rect = (RectTransform)objeto.transform;
        rect.SetParent(padre, false);
        rect.anchorMin = new Vector2(bounds.xMin / medida.x, 1 - bounds.yMax / medida.y);
        rect.anchorMax = new Vector2(bounds.xMax / medida.x, 1 - bounds.yMin / medida.y);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }
}
