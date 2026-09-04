using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EstabloView : MonoBehaviour
{
    [Header("Ventanas")]
    public GameObject panelInfoCaballo;
    public GameObject panelUps;
    public GameObject panelProcrear;
    public GameObject panelItems;

    [Header("Stats")]
    public TMP_Text textoNombre;
    public TMP_Text textoEdad;
    public TMP_Text textoNivel;
    public TMP_Text textoExperiencia;
    public TMP_Text textoVelocidad;
    public TMP_Text textoStamina;
    public TMP_Text textoSuerte;
    public TMP_Text textoInteligencia;
    public TMP_Text textoPotencial;
    public TMP_Text textoRelacion;
    public TMP_Text textoEstres;
    public Image imagenCaballo;

    [Header("Items")]
    public Sprite spriteManiquiItems;

    private readonly List<Button> botonesNavegacion = new List<Button>();

    private TMP_Text textoNombreUp;
    private TMP_Text textoPuntosUp;
    private TMP_Text textoVelocidadUp;
    private TMP_Text textoStaminaUp;
    private TMP_Text textoSuerteUp;
    private TMP_Text textoInteligenciaUp;
    private TMP_Text textoPotencialUp;

    private Button botonMejorarVelocidad;
    private Button botonMejorarStamina;
    private Button botonMejorarSuerte;
    private Button botonMejorarInteligencia;

    private readonly List<Button> botonesInventarioItems = new List<Button>();
    private readonly Dictionary<TipoSlotItem, TMP_Text> textosSlotsItems =
        new Dictionary<TipoSlotItem, TMP_Text>();
    private readonly Dictionary<TipoSlotItem, Image> imagenesSlotsItems =
        new Dictionary<TipoSlotItem, Image>();

    private RectTransform contenidoInventarioItems;
    private TMP_Text textoInventarioVacio;
    private TMP_Text textoDetalleItem;
    private TMP_Text textoCaballoItems;
    private Button botonAccionItem;
    private TMP_Text textoAccionItem;
    private Image imagenManiquiItems;
    private Button plantillaBotonItems;
    private TMP_Text plantillaTextoItems;
    private ItemCaballo itemSeleccionado;

    private int indiceCaballoSeleccionado;
    private bool interfazConfigurada;

    private void Start()
    {
        AsegurarConfiguracionInterfaz();
        RefrescarCaballoSeleccionado();
    }

    public void AbrirCaballo()
    {
        AsegurarConfiguracionInterfaz();
        MostrarStats();
    }

    private Caballo ObtenerCaballoSeleccionado()
    {
        if (GameManager.Instancia == null ||
            GameManager.Instancia.caballosJugador == null ||
            GameManager.Instancia.caballosJugador.Count == 0)
        {
            indiceCaballoSeleccionado = 0;
            return null;
        }

        int cantidadCaballos = GameManager.Instancia.caballosJugador.Count;
        indiceCaballoSeleccionado = CalcularIndiceCircular(
            indiceCaballoSeleccionado,
            0,
            cantidadCaballos);

        return GameManager.Instancia.caballosJugador[indiceCaballoSeleccionado];
    }

    private static int CalcularIndiceCircular(int indiceActual, int desplazamiento, int cantidad)
    {
        if (cantidad <= 0)
            return 0;

        int indice = (indiceActual + desplazamiento) % cantidad;
        return indice < 0 ? indice + cantidad : indice;
    }

    public void SeleccionarAnterior()
    {
        CambiarSeleccion(-1);
    }

    public void SeleccionarSiguiente()
    {
        CambiarSeleccion(1);
    }

    private void CambiarSeleccion(int desplazamiento)
    {
        int cantidadCaballos = GameManager.Instancia?.caballosJugador?.Count ?? 0;

        if (cantidadCaballos > 1)
        {
            indiceCaballoSeleccionado = CalcularIndiceCircular(
                indiceCaballoSeleccionado,
                desplazamiento,
                cantidadCaballos);
        }

        RefrescarCaballoSeleccionado();
    }

    private void RefrescarCaballoSeleccionado()
    {
        Caballo caballo = ObtenerCaballoSeleccionado();
        MostrarCaballo(caballo);
        MostrarMejoras(caballo);
        MostrarItemsCaballo(caballo);

        int cantidadCaballos = GameManager.Instancia?.caballosJugador?.Count ?? 0;
        bool puedeNavegar = cantidadCaballos > 1;

        foreach (Button boton in botonesNavegacion)
        {
            if (boton != null)
                boton.interactable = puedeNavegar;
        }
    }

    private void MostrarCaballo(Caballo caballo)
    {
        if (caballo == null)
        {
            AsignarTexto(textoNombre, "Sin caballo");
            AsignarTexto(textoEdad, "Edad: -");
            AsignarTexto(textoNivel, "Nivel: -");
            AsignarTexto(textoExperiencia, "XP: - / -");
            AsignarTexto(textoVelocidad, "Velocidad: -");
            AsignarTexto(textoStamina, "Stamina: -");
            AsignarTexto(textoSuerte, "Suerte: -");
            AsignarTexto(textoInteligencia, "Inteligencia: -");
            AsignarTexto(textoPotencial, "Potencial: -");
            AsignarTexto(textoRelacion, "Relación: -");
            AsignarTexto(textoEstres, "Estrés: -");

            if (imagenCaballo != null)
            {
                imagenCaballo.sprite = null;
                imagenCaballo.enabled = false;
            }

            return;
        }

        AsignarTexto(textoNombre, caballo.nombre);
        AsignarTexto(textoEdad, "Edad: " + caballo.edad);
        AsignarTexto(textoNivel, "Nivel: " + caballo.nivel);

        int experienciaNecesaria =
            ProgresionCaballo.ExperienciaNecesariaParaSiguienteNivel(caballo.nivel);

        AsignarTexto(
            textoExperiencia,
            "XP: " + caballo.experiencia + " / " + experienciaNecesaria);

        AsignarTexto(textoVelocidad, "Velocidad: " + RangoCaballo.ObtenerRango(caballo.velocidad));
        AsignarTexto(textoStamina, "Stamina: " + RangoCaballo.ObtenerRango(caballo.stamina));
        AsignarTexto(textoSuerte, "Suerte: " + RangoCaballo.ObtenerRango(caballo.suerte));
        AsignarTexto(textoInteligencia, "Inteligencia: " + RangoCaballo.ObtenerRango(caballo.inteligencia));
        AsignarTexto(textoPotencial, "Potencial: " + RangoCaballo.ObtenerRango(caballo.potencial));
        AsignarTexto(textoRelacion, "Relación: " + caballo.relacion);
        AsignarTexto(textoEstres, "Estrés: " + caballo.estres);

        if (imagenCaballo != null)
        {
            imagenCaballo.sprite = caballo.sprite;
            imagenCaballo.enabled = caballo.sprite != null;
        }
    }

    private void MostrarMejoras(Caballo caballo)
    {
        if (caballo == null)
        {
            AsignarTexto(textoNombreUp, "Sin caballo");
            AsignarTexto(textoPuntosUp, "Puntos disponibles: 0");
            AsignarTexto(textoVelocidadUp, "Velocidad: -");
            AsignarTexto(textoStaminaUp, "Stamina: -");
            AsignarTexto(textoSuerteUp, "Suerte: -");
            AsignarTexto(textoInteligenciaUp, "Inteligencia: -");
            AsignarTexto(textoPotencialUp, "Potencial: -");
            ActualizarBotonesMejora(null);
            return;
        }

        AsignarTexto(textoNombreUp, caballo.nombre);
        AsignarTexto(textoPuntosUp, "Puntos disponibles: " + caballo.puntosMejoraPendientes);
        AsignarTexto(textoVelocidadUp, "Velocidad: " + RangoCaballo.ObtenerRango(caballo.velocidad));
        AsignarTexto(textoStaminaUp, "Stamina: " + RangoCaballo.ObtenerRango(caballo.stamina));
        AsignarTexto(textoSuerteUp, "Suerte: " + RangoCaballo.ObtenerRango(caballo.suerte));
        AsignarTexto(textoInteligenciaUp, "Inteligencia: " + RangoCaballo.ObtenerRango(caballo.inteligencia));
        AsignarTexto(textoPotencialUp, "Potencial: " + RangoCaballo.ObtenerRango(caballo.potencial));
        ActualizarBotonesMejora(caballo);
    }

    public void MejorarVelocidad()
    {
        MejorarEstadisticaSeleccionada(TipoEstadisticaCaballo.Velocidad);
    }

    public void MejorarStamina()
    {
        MejorarEstadisticaSeleccionada(TipoEstadisticaCaballo.Stamina);
    }

    public void MejorarSuerte()
    {
        MejorarEstadisticaSeleccionada(TipoEstadisticaCaballo.Suerte);
    }

    public void MejorarInteligencia()
    {
        MejorarEstadisticaSeleccionada(TipoEstadisticaCaballo.Inteligencia);
    }

    private void MejorarEstadisticaSeleccionada(TipoEstadisticaCaballo estadistica)
    {
        Caballo caballo = ObtenerCaballoSeleccionado();

        if (ProgresionCaballo.MejorarEstadistica(caballo, estadistica))
            RefrescarCaballoSeleccionado();
    }

    private void ActualizarBotonesMejora(Caballo caballo)
    {
        bool tienePuntos = caballo != null && caballo.puntosMejoraPendientes > 0;

        ConfigurarInteractuable(
            botonMejorarVelocidad,
            tienePuntos && caballo.velocidad < Caballo.EstadisticaMaxima);
        ConfigurarInteractuable(
            botonMejorarStamina,
            tienePuntos && caballo.stamina < Caballo.EstadisticaMaxima);
        ConfigurarInteractuable(
            botonMejorarSuerte,
            tienePuntos && caballo.suerte < Caballo.EstadisticaMaxima);
        ConfigurarInteractuable(
            botonMejorarInteligencia,
            tienePuntos && caballo.inteligencia < Caballo.EstadisticaMaxima);
    }

    private static void ConfigurarInteractuable(Button boton, bool interactuable)
    {
        if (boton != null)
            boton.interactable = interactuable;
    }

    private void AsegurarConfiguracionInterfaz()
    {
        if (interfazConfigurada ||
            panelInfoCaballo == null ||
            panelUps == null ||
            panelItems == null)
            return;

        Transform raizEstablo = panelInfoCaballo.transform.parent;
        Button plantillaBoton = null;

        foreach (Button boton in raizEstablo.GetComponentsInChildren<Button>(true))
        {
            if (boton.name == "Butonanterior")
            {
                boton.onClick.AddListener(SeleccionarAnterior);
                botonesNavegacion.Add(boton);
            }
            else if (boton.name == "Butonsiguiente")
            {
                boton.onClick.AddListener(SeleccionarSiguiente);
                botonesNavegacion.Add(boton);

                if (boton.transform.parent == panelUps.transform)
                    plantillaBoton = boton;

                if (boton.transform.parent == panelItems.transform)
                    plantillaBotonItems = boton;
            }
        }

        TMP_Text textoRelacionUp = null;
        TMP_Text textoEstresUp = null;

        foreach (TMP_Text texto in panelUps.GetComponentsInChildren<TMP_Text>(true))
        {
            switch (texto.name)
            {
                case "TextoNombre":
                    textoNombreUp = texto;
                    break;
                case "TextoEdad":
                    textoPuntosUp = texto;
                    break;
                case "TextoVelocidad":
                    textoVelocidadUp = texto;
                    break;
                case "TextoStamina":
                    textoStaminaUp = texto;
                    break;
                case "TextoSuerte":
                    textoSuerteUp = texto;
                    break;
                case "TextoInteligencia":
                    textoInteligenciaUp = texto;
                    break;
                case "TextoPotencial":
                    textoPotencialUp = texto;
                    break;
                case "TextoRelacion":
                    textoRelacionUp = texto;
                    break;
                case "TextoEstres":
                    textoEstresUp = texto;
                    break;
            }
        }

        AsignarTexto(textoRelacionUp, string.Empty);
        AsignarTexto(textoEstresUp, string.Empty);

        if (plantillaBoton == null)
        {
            Debug.LogWarning("No se encontró una plantilla de botón para las mejoras del Establo.");
        }
        else
        {
            botonMejorarVelocidad = CrearBotonMejora(
                plantillaBoton,
                "MejorarVelocidad",
                textoVelocidadUp,
                MejorarVelocidad);
            botonMejorarStamina = CrearBotonMejora(
                plantillaBoton,
                "MejorarStamina",
                textoStaminaUp,
                MejorarStamina);
            botonMejorarSuerte = CrearBotonMejora(
                plantillaBoton,
                "MejorarSuerte",
                textoSuerteUp,
                MejorarSuerte);
            botonMejorarInteligencia = CrearBotonMejora(
                plantillaBoton,
                "MejorarInteligencia",
                textoInteligenciaUp,
                MejorarInteligencia);
        }

        ConfigurarPanelItems();
        interfazConfigurada = true;
    }

    private void ConfigurarPanelItems()
    {
        if (plantillaBotonItems == null)
        {
            Debug.LogWarning("No se encontró una plantilla de botón para Items del Establo.");
            return;
        }

        plantillaTextoItems = plantillaBotonItems.GetComponentInChildren<TMP_Text>(true);
        if (plantillaTextoItems == null)
        {
            Debug.LogWarning("No se encontró una plantilla de texto para Items del Establo.");
            return;
        }

        RectTransform raizItems = CrearRectTransform(
            "ContenidoFuncionalItems",
            panelItems.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
        raizItems.SetSiblingIndex(1);

        RectTransform panelInventario = CrearRectTransform(
            "PanelInventarioItems",
            raizItems,
            new Vector2(0.05f, 0.10f),
            new Vector2(0.36f, 0.84f),
            Vector2.zero,
            Vector2.zero);

        RectTransform panelManiqui = CrearRectTransform(
            "PanelManiquiItems",
            raizItems,
            new Vector2(0.40f, 0.10f),
            new Vector2(0.96f, 0.84f),
            Vector2.zero,
            Vector2.zero);

        CrearTextoItems(
            "TituloInventarioItems",
            panelInventario,
            "INVENTARIO",
            new Vector2(0f, 0.89f),
            new Vector2(1f, 1f),
            34f,
            TextAlignmentOptions.Center);

        CrearListaInventario(panelInventario);

        textoDetalleItem = CrearTextoItems(
            "DetalleItemSeleccionado",
            panelInventario,
            "Selecciona un item",
            new Vector2(0.04f, 0.10f),
            new Vector2(0.96f, 0.27f),
            22f,
            TextAlignmentOptions.TopLeft);

        botonAccionItem = Instantiate(plantillaBotonItems, panelInventario);
        botonAccionItem.name = "AccionItemSeleccionado";
        botonAccionItem.onClick.RemoveAllListeners();
        botonAccionItem.onClick.AddListener(EjecutarAccionItemSeleccionado);

        RectTransform rectAccion = botonAccionItem.GetComponent<RectTransform>();
        rectAccion.anchorMin = new Vector2(0.5f, 0f);
        rectAccion.anchorMax = new Vector2(0.5f, 0f);
        rectAccion.pivot = new Vector2(0.5f, 0f);
        rectAccion.anchoredPosition = new Vector2(0f, 8f);
        rectAccion.sizeDelta = new Vector2(430f, 62f);

        textoAccionItem = botonAccionItem.GetComponentInChildren<TMP_Text>(true);
        if (textoAccionItem != null)
        {
            textoAccionItem.enableAutoSizing = true;
            textoAccionItem.fontSizeMin = 14f;
            textoAccionItem.fontSizeMax = 25f;
        }

        textoCaballoItems = CrearTextoItems(
            "NombreCaballoItems",
            panelManiqui,
            "Sin caballo seleccionado",
            new Vector2(0f, 0.89f),
            new Vector2(1f, 1f),
            34f,
            TextAlignmentOptions.Center);

        GameObject imagenManiquiObjeto = new GameObject(
            "CaballoManiqui",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imagenManiquiObjeto.transform.SetParent(panelManiqui, false);
        RectTransform rectManiqui = imagenManiquiObjeto.GetComponent<RectTransform>();
        rectManiqui.anchorMin = new Vector2(0.18f, 0.16f);
        rectManiqui.anchorMax = new Vector2(0.82f, 0.78f);
        rectManiqui.offsetMin = Vector2.zero;
        rectManiqui.offsetMax = Vector2.zero;
        imagenManiquiItems = imagenManiquiObjeto.GetComponent<Image>();
        imagenManiquiItems.preserveAspect = true;
        imagenManiquiItems.raycastTarget = false;

        CrearSlotItems(
            panelManiqui,
            TipoSlotItem.Cabeza,
            new Vector2(0.5f, 0.82f),
            "CABEZA ▼");
        CrearSlotItems(
            panelManiqui,
            TipoSlotItem.Patas,
            new Vector2(0.16f, 0.17f),
            "PATAS ▶");
        CrearSlotItems(
            panelManiqui,
            TipoSlotItem.Espalda,
            new Vector2(0.84f, 0.43f),
            "◀ ESPALDA");
    }

    private void CrearListaInventario(RectTransform panelInventario)
    {
        GameObject scrollObjeto = new GameObject(
            "ListaItems",
            typeof(RectTransform),
            typeof(ScrollRect));
        scrollObjeto.transform.SetParent(panelInventario, false);
        RectTransform rectScroll = scrollObjeto.GetComponent<RectTransform>();
        rectScroll.anchorMin = new Vector2(0.04f, 0.29f);
        rectScroll.anchorMax = new Vector2(0.96f, 0.88f);
        rectScroll.offsetMin = Vector2.zero;
        rectScroll.offsetMax = Vector2.zero;

        GameObject viewportObjeto = new GameObject(
            "Viewport",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Mask));
        viewportObjeto.transform.SetParent(rectScroll, false);
        RectTransform viewport = viewportObjeto.GetComponent<RectTransform>();
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = Vector2.zero;
        viewport.offsetMax = Vector2.zero;
        viewportObjeto.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.28f);
        viewportObjeto.GetComponent<Mask>().showMaskGraphic = true;

        GameObject contenidoObjeto = new GameObject(
            "ContenidoInventario",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        contenidoObjeto.transform.SetParent(viewport, false);
        contenidoInventarioItems = contenidoObjeto.GetComponent<RectTransform>();
        contenidoInventarioItems.anchorMin = new Vector2(0f, 1f);
        contenidoInventarioItems.anchorMax = new Vector2(1f, 1f);
        contenidoInventarioItems.pivot = new Vector2(0.5f, 1f);
        contenidoInventarioItems.anchoredPosition = Vector2.zero;
        contenidoInventarioItems.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = contenidoObjeto.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = contenidoObjeto.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = scrollObjeto.GetComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = contenidoInventarioItems;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        textoInventarioVacio = CrearTextoItems(
            "InventarioVacio",
            viewport,
            "Inventario vacío",
            Vector2.zero,
            Vector2.one,
            25f,
            TextAlignmentOptions.Center);
    }

    private void CrearSlotItems(
        RectTransform panelManiqui,
        TipoSlotItem slot,
        Vector2 ancla,
        string etiqueta)
    {
        GameObject slotObjeto = new GameObject(
            "Slot" + slot,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Outline));
        slotObjeto.transform.SetParent(panelManiqui, false);

        RectTransform rectSlot = slotObjeto.GetComponent<RectTransform>();
        rectSlot.anchorMin = ancla;
        rectSlot.anchorMax = ancla;
        rectSlot.pivot = new Vector2(0.5f, 0.5f);
        rectSlot.anchoredPosition = Vector2.zero;
        rectSlot.sizeDelta = new Vector2(230f, 105f);

        slotObjeto.GetComponent<Image>().color = new Color(0.18f, 0.10f, 0.04f, 0.88f);
        Outline outline = slotObjeto.GetComponent<Outline>();
        outline.effectColor = new Color(0.75f, 0.48f, 0.18f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);

        TMP_Text textoSlot = CrearTextoItems(
            "TextoSlot" + slot,
            rectSlot,
            etiqueta + "\nVACÍO",
            Vector2.zero,
            Vector2.one,
            23f,
            TextAlignmentOptions.Center);
        textosSlotsItems[slot] = textoSlot;

        GameObject iconoObjeto = new GameObject(
            "IconoSlot" + slot,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        iconoObjeto.transform.SetParent(rectSlot, false);
        RectTransform rectIcono = iconoObjeto.GetComponent<RectTransform>();
        rectIcono.anchorMin = new Vector2(0.5f, 0.5f);
        rectIcono.anchorMax = new Vector2(0.5f, 0.5f);
        rectIcono.pivot = new Vector2(0.5f, 0.5f);
        rectIcono.anchoredPosition = Vector2.zero;
        rectIcono.sizeDelta = new Vector2(72f, 72f);

        Image imagenSlot = iconoObjeto.GetComponent<Image>();
        imagenSlot.preserveAspect = true;
        imagenSlot.raycastTarget = false;
        imagenSlot.enabled = false;
        imagenesSlotsItems[slot] = imagenSlot;
    }

    private TMP_Text CrearTextoItems(
        string nombre,
        Transform padre,
        string contenido,
        Vector2 anclaMin,
        Vector2 anclaMax,
        float tamanoFuente,
        TextAlignmentOptions alineacion)
    {
        TMP_Text texto = Instantiate(plantillaTextoItems, padre);
        texto.name = nombre;
        texto.text = contenido;
        texto.fontSize = tamanoFuente;
        texto.alignment = alineacion;
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 12f;
        texto.fontSizeMax = tamanoFuente;
        texto.raycastTarget = false;

        RectTransform rect = texto.rectTransform;
        rect.anchorMin = anclaMin;
        rect.anchorMax = anclaMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        texto.gameObject.SetActive(true);
        return texto;
    }

    private static RectTransform CrearRectTransform(
        string nombre,
        Transform padre,
        Vector2 anclaMin,
        Vector2 anclaMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(padre, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = anclaMin;
        rect.anchorMax = anclaMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    private void MostrarItemsCaballo(Caballo caballo)
    {
        if (contenidoInventarioItems == null)
            return;

        AsignarTexto(
            textoCaballoItems,
            caballo != null ? "EQUIPAMIENTO — " + caballo.nombre : "Sin caballo seleccionado");

        if (imagenManiquiItems != null)
        {
            imagenManiquiItems.sprite = spriteManiquiItems != null
                ? spriteManiquiItems
                : caballo != null
                    ? caballo.sprite
                    : null;
            imagenManiquiItems.enabled = imagenManiquiItems.sprite != null;
        }

        ActualizarListaInventario();
        ActualizarSlotItem(caballo, TipoSlotItem.Cabeza, "CABEZA ▼");
        ActualizarSlotItem(caballo, TipoSlotItem.Espalda, "◀ ESPALDA");
        ActualizarSlotItem(caballo, TipoSlotItem.Patas, "PATAS ▶");
        ActualizarDetalleItem(caballo);
    }

    private void ActualizarListaInventario()
    {
        foreach (Button boton in botonesInventarioItems)
        {
            if (boton != null)
            {
                boton.gameObject.SetActive(false);
                Destroy(boton.gameObject);
            }
        }

        botonesInventarioItems.Clear();

        List<ItemCaballo> items = GameManager.Instancia?.itemsJugador;
        bool inventarioVacio = items == null || items.Count == 0;

        if (textoInventarioVacio != null)
            textoInventarioVacio.gameObject.SetActive(inventarioVacio);

        if (inventarioVacio)
        {
            itemSeleccionado = null;
            return;
        }

        itemSeleccionado = ResolverItemInventario(itemSeleccionado, items);

        foreach (ItemCaballo item in items)
        {
            if (item == null)
                continue;

            Button boton = Instantiate(plantillaBotonItems, contenidoInventarioItems);
            boton.name = "Item_" + item.id;
            boton.onClick.RemoveAllListeners();
            ItemCaballo itemEntrada = item;
            boton.onClick.AddListener(() => SeleccionarItem(itemEntrada));

            LayoutElement layout = boton.GetComponent<LayoutElement>();
            if (layout == null)
                layout = boton.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 82f;

            TMP_Text etiqueta = boton.GetComponentInChildren<TMP_Text>(true);
            if (etiqueta != null)
            {
                string estado = string.IsNullOrEmpty(item.caballoEquipadoId)
                    ? "Disponible"
                    : "Equipado: " + ObtenerNombreCaballo(item.caballoEquipadoId);
                string seleccion = ReferenceEquals(item, itemSeleccionado) ? "▶ " : string.Empty;
                etiqueta.text = seleccion + item.nombre + "\n" + item.slotPermitido + " — " + estado;
                etiqueta.alignment = TextAlignmentOptions.Center;
                etiqueta.enableAutoSizing = true;
                etiqueta.fontSizeMin = 12f;
                etiqueta.fontSizeMax = 22f;
            }

            boton.gameObject.SetActive(true);
            botonesInventarioItems.Add(boton);
        }
    }

    private void ActualizarSlotItem(Caballo caballo, TipoSlotItem slot, string etiqueta)
    {
        if (!textosSlotsItems.TryGetValue(slot, out TMP_Text textoSlot) ||
            !imagenesSlotsItems.TryGetValue(slot, out Image imagenSlot))
        {
            return;
        }

        ItemCaballo equipado = caballo != null && GameManager.Instancia != null
            ? GameManager.Instancia.ObtenerItemEquipado(caballo, slot)
            : null;

        if (equipado == null)
        {
            textoSlot.text = etiqueta + "\nVACÍO";
            imagenSlot.sprite = null;
            imagenSlot.enabled = false;
            return;
        }

        textoSlot.text = equipado.sprite == null
            ? etiqueta + "\n" + equipado.nombre
            : etiqueta;
        imagenSlot.sprite = equipado.sprite;
        imagenSlot.enabled = equipado.sprite != null;
    }

    private void SeleccionarItem(ItemCaballo item)
    {
        itemSeleccionado = item;
        MostrarItemsCaballo(ObtenerCaballoSeleccionado());
    }

    private void ActualizarDetalleItem(Caballo caballo)
    {
        GameManager gameManager = GameManager.Instancia;
        ItemCaballo item = ResolverItemInventario(itemSeleccionado, gameManager?.itemsJugador);
        itemSeleccionado = item;

        if (item == null)
        {
            AsignarTexto(textoDetalleItem, "Selecciona un item");
            ConfigurarAccionItem("Selecciona un item", false);
            return;
        }

        string estado = string.IsNullOrEmpty(item.caballoEquipadoId)
            ? "Disponible"
            : "Equipado en: " + ObtenerNombreCaballo(item.caballoEquipadoId);
        AsignarTexto(
            textoDetalleItem,
            item.nombre + "\n" + item.slotPermitido + " — " + estado + "\n" + item.descripcion);

        if (gameManager == null || caballo == null)
        {
            ConfigurarAccionItem("Sin caballo seleccionado", false);
            return;
        }

        if (item.caballoEquipadoId == caballo.id)
        {
            bool puedePagar = gameManager.dinero >= SistemaEquipamiento.CostoDesequiparItem;
            ConfigurarAccionItem(
                "Desequipar ($" + SistemaEquipamiento.CostoDesequiparItem + ")" +
                (puedePagar ? string.Empty : " — dinero insuficiente"),
                puedePagar);
            return;
        }

        if (!string.IsNullOrEmpty(item.caballoEquipadoId))
        {
            ConfigurarAccionItem("Equipado en: " + ObtenerNombreCaballo(item.caballoEquipadoId), false);
            return;
        }

        ItemCaballo itemEnSlot = gameManager.ObtenerItemEquipado(caballo, item.slotPermitido);
        if (itemEnSlot != null)
        {
            ConfigurarAccionItem("Slot " + item.slotPermitido + " ocupado", false);
            return;
        }

        bool dineroSuficiente = gameManager.dinero >= SistemaEquipamiento.CostoEquiparItem;
        ConfigurarAccionItem(
            "Equipar ($" + SistemaEquipamiento.CostoEquiparItem + ")" +
            (dineroSuficiente ? string.Empty : " — dinero insuficiente"),
            dineroSuficiente);
    }

    private void ConfigurarAccionItem(string texto, bool interactuable)
    {
        AsignarTexto(textoAccionItem, texto);
        if (botonAccionItem != null)
            botonAccionItem.interactable = interactuable;
    }

    private void EjecutarAccionItemSeleccionado()
    {
        GameManager gameManager = GameManager.Instancia;
        Caballo caballo = ObtenerCaballoSeleccionado();
        ItemCaballo item = ResolverItemInventario(itemSeleccionado, gameManager?.itemsJugador);

        if (gameManager == null || caballo == null || item == null)
            return;

        bool cambioRealizado = item.caballoEquipadoId == caballo.id
            ? gameManager.DesequiparItem(caballo, item)
            : gameManager.EquiparItem(caballo, item);

        if (cambioRealizado)
            MostrarItemsCaballo(caballo);
    }

    private static ItemCaballo ResolverItemInventario(
        ItemCaballo item,
        IList<ItemCaballo> items)
    {
        if (item == null || items == null)
            return null;

        foreach (ItemCaballo itemInventario in items)
        {
            if (ReferenceEquals(itemInventario, item) ||
                (itemInventario != null && itemInventario.id == item.id))
            {
                return itemInventario;
            }
        }

        return null;
    }

    private static string ObtenerNombreCaballo(string caballoId)
    {
        List<Caballo> caballos = GameManager.Instancia?.caballosJugador;
        if (caballos == null)
            return "Desconocido";

        foreach (Caballo caballo in caballos)
        {
            if (caballo != null && caballo.id == caballoId)
                return caballo.nombre;
        }

        return "Desconocido";
    }

    private Button CrearBotonMejora(
        Button plantilla,
        string nombre,
        TMP_Text textoEstadistica,
        UnityAction accion)
    {
        Button boton = Instantiate(plantilla, panelUps.transform);
        boton.name = nombre;
        boton.onClick.RemoveAllListeners();
        boton.onClick.AddListener(accion);

        RectTransform rectBoton = boton.GetComponent<RectTransform>();
        RectTransform rectTexto = textoEstadistica != null
            ? textoEstadistica.rectTransform
            : null;

        rectBoton.anchorMin = new Vector2(0.5f, 0.5f);
        rectBoton.anchorMax = new Vector2(0.5f, 0.5f);
        rectBoton.pivot = new Vector2(0.5f, 0.5f);
        rectBoton.sizeDelta = new Vector2(60f, 50f);
        rectBoton.anchoredPosition = rectTexto != null
            ? new Vector2(
                rectTexto.anchoredPosition.x + rectTexto.sizeDelta.x * 0.5f + 50f,
                rectTexto.anchoredPosition.y)
            : Vector2.zero;

        TMP_Text etiqueta = boton.GetComponentInChildren<TMP_Text>(true);
        if (etiqueta != null)
            etiqueta.text = "+";

        boton.gameObject.SetActive(true);
        return boton;
    }

    private static void AsignarTexto(TMP_Text texto, string contenido)
    {
        if (texto != null)
            texto.text = contenido;
    }

    public void CerrarCaballo()
    {
        panelInfoCaballo.SetActive(false);
        panelUps.SetActive(false);
        panelProcrear.SetActive(false);
        panelItems.SetActive(false);
    }

    public void MostrarStats()
    {
        AsegurarConfiguracionInterfaz();
        panelInfoCaballo.SetActive(true);
        panelUps.SetActive(false);
        panelProcrear.SetActive(false);
        panelItems.SetActive(false);
        RefrescarCaballoSeleccionado();
    }

    public void MostrarUps()
    {
        AsegurarConfiguracionInterfaz();
        panelInfoCaballo.SetActive(false);
        panelUps.SetActive(true);
        panelProcrear.SetActive(false);
        panelItems.SetActive(false);
        RefrescarCaballoSeleccionado();
    }

    public void MostrarProcrear()
    {
        AsegurarConfiguracionInterfaz();
        panelInfoCaballo.SetActive(false);
        panelUps.SetActive(false);
        panelProcrear.SetActive(true);
        panelItems.SetActive(false);
        RefrescarCaballoSeleccionado();
    }

    public void MostrarItems()
    {
        AsegurarConfiguracionInterfaz();
        panelInfoCaballo.SetActive(false);
        panelUps.SetActive(false);
        panelProcrear.SetActive(false);
        panelItems.SetActive(true);
        RefrescarCaballoSeleccionado();
    }
}
