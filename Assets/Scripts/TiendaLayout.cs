using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static TablonLayout;

internal static class TiendaLayout
{
    static readonly Vector2 Lienzo = new(1672, 941);
    static readonly Color Tinta = new Color32(55, 29, 13, 255);
    static readonly Color Crema = new Color32(255, 236, 197, 255);

    internal static void Construir(TiendaView view, RectTransform raiz, Sprite fondo, Sprite estanteria,
        Sprite pergamino, Sprite casilla, Sprite cartel, Sprite marcoBoton, Sprite vendedor)
    {
        var anteriores = raiz.Cast<Transform>().Where(t => t != view.transform).ToArray();
        var originales = raiz.Find("PanelItems").GetComponentsInChildren<Button>(true)
            .Where(b => b.transform.parent == raiz.Find("PanelItems")).ToArray();
        var personaje = raiz.GetComponentsInChildren<Image>(true).FirstOrDefault(i => i.sprite == vendedor);
        var fuente = view.textoNombre.font;
        var lienzo = Rect("TiendaRustica", raiz, new Rect(Vector2.zero, Lienzo), Lienzo);
        var fondoUI = Imagen("Ambiente", lienzo, new Rect(Vector2.zero, Lienzo), fondo);
        fondoUI.raycastTarget = true;

        var contenido = Rect("ContenidoTienda", lienzo, new Rect(Vector2.zero, Lienzo), Lienzo);
        view.contenido = contenido.gameObject.AddComponent<CanvasGroup>();
        if (personaje != null)
        {
            // Preserve the supplied portrait's aspect and bottom crop, with its face clear of the controls.
            Colocar(personaje.rectTransform, contenido, new Rect(1242, 235.5f, 751.4f, 705.5f));
            personaje.preserveAspect = true;
            personaje.color = Color.white;
            personaje.raycastTarget = false;
            personaje.gameObject.SetActive(true);
            personaje.gameObject.name = "VendedorOriginal";
        }

        var cabecera = Rect("CartelTienda", contenido, new Rect(307, 26, 610, 128), Lienzo);
        var marco = cabecera.gameObject.AddComponent<Image>();
        marco.sprite = cartel;
        marco.raycastTarget = false;
        Texto("Titulo", cabecera, new Rect(.05f, .08f, .9f, .84f), Vector2.one, fuente, "Tienda", 70, oscuro: false);
        var subasta = BotonNuevo("Subasta", contenido, new Rect(1000, 44, 339, 83), "Subasta", fuente, pergamino, null);
        subasta.interactable = false;
        subasta.image.color = new Color32(112, 97, 83, 255);
        Etiqueta(subasta).color = new Color32(211, 193, 163, 255);

        var volver = BotonNuevo("Volver", contenido, new Rect(1474, 48, 180, 68), "‹ Volver", fuente, pergamino, view.Volver);
        Etiqueta(volver).fontSizeMax = 35;
        Imagen("Pergamino", contenido, new Rect(121, 157, 970, 250), pergamino, sliced: true);
        MoverTexto(view.textoNombre, contenido, new Rect(173, 181, 862, 61), 43, fuente, true);
        MoverTexto(view.textoDescripcion, contenido, new Rect(176, 246, 851, 105), 26, fuente);
        view.textoDescripcion.textWrappingMode = TextWrappingModes.Normal;
        MoverTexto(view.textoPrecio, contenido, new Rect(176, 352, 850, 35), 28, fuente, true);

        Imagen("Estanteria", contenido, new Rect(110, 406, 935, 466), estanteria);
        for (int i = 0; i < originales.Length && i < 6; i++)
        {
            var boton = originales[i];
            // Replace the old decorative children with an independent icon and interaction states.
            foreach (Transform child in boton.transform) child.gameObject.SetActive(false);
            Colocar((RectTransform)boton.transform, contenido, new Rect(209 + i % 3 * 260, 459 + i / 3 * 222, 219, 171));
            boton.gameObject.name = "Oferta" + i;
            boton.gameObject.SetActive(true);
            var cara = boton.GetComponent<Image>();
            cara.sprite = casilla;
            cara.color = Color.white;
            cara.type = Image.Type.Simple;
            boton.transition = Selectable.Transition.None;
            var rect = (RectTransform)boton.transform;
            var icono = ImagenLocal("IconoProducto", rect, new Rect(.055f, .055f, .89f, .87f), null);
            icono.preserveAspect = true;
            var brillo = ImagenLocal("Hover", rect, new Rect(0, 0, 1, 1), marcoBoton);
            brillo.type = Image.Type.Sliced;
            brillo.fillCenter = false;
            brillo.color = new Color32(255, 214, 111, 255);
            var halo = brillo.gameObject.AddComponent<Outline>();
            halo.effectColor = new Color32(255, 173, 50, 110);
            halo.effectDistance = new Vector2(2, -2);
            var seleccion = Surface("Seleccion", rect, new Rect(.1f, .80f, .8f, .14f), Vector2.one,
                new Color32(55, 28, 9, 230));
            Texto("Etiqueta", seleccion, new Rect(0, 0, 1, 1), Vector2.one, fuente, "Seleccionado", 17, oscuro: false);
            var oferta = boton.gameObject.AddComponent<TiendaProductoUI>();
            oferta.Configurar(boton, icono, brillo, seleccion.gameObject, view.SeleccionarProducto);
            view.ofertas.Add(oferta);
        }

        view.botonReroll = BotonNuevo("RerollTienda", contenido, new Rect(1039, 620, 237, 75),
            "", fuente, pergamino, view.EjecutarRerollManual);
        EstiloBoton(view.botonComprar.GetComponent<Button>(), contenido, new Rect(1039, 720, 237, 75),
            "Comprar", fuente, pergamino, view.MostrarConfirmacion);
        view.mensaje = Texto("MensajeTienda", contenido, new Rect(1036, 802, 244, 58), Lienzo, fuente, "", 22, oscuro: false);
        view.mensaje.textWrappingMode = TextWrappingModes.Normal;

        var velo = Surface("ConfirmacionCompra", lienzo, new Rect(Vector2.zero, Lienzo), Lienzo,
            new Color32(0, 0, 0, 135));
        velo.GetComponent<Image>().raycastTarget = true;
        view.confirmacion = velo.gameObject;
        Imagen("Panel", velo, new Rect(226, 270, 807, 348), pergamino, sliced: true);
        Texto("Titulo", velo, new Rect(283, 305, 692, 67), Lienzo, fuente, "¿Confirmar compra?", 43);
        view.textoConfirmacion = Texto("ProductoPrecio", velo, new Rect(283, 389, 692, 78), Lienzo, fuente, "", 35);
        EstiloBoton(view.botonNo.GetComponent<Button>(), velo, new Rect(296, 509, 260, 71),
            "Cancelar", fuente, pergamino, view.CancelarCompra);
        EstiloBoton(view.botonSi.GetComponent<Button>(), velo, new Rect(703, 509, 260, 71),
            "Confirmar", fuente, pergamino, view.ConfirmarCompra);
        view.cancelar = view.botonNo.GetComponent<Button>();
        view.confirmacion.SetActive(false);
        foreach (var antiguo in anteriores)
            if (antiguo.parent == raiz) antiguo.gameObject.SetActive(false);
    }

    static Button BotonNuevo(string nombre, RectTransform padre, Rect bounds, string titulo, TMP_FontAsset fuente,
        Sprite pergamino, UnityAction accion)
    {
        var rect = Rect(nombre, padre, bounds, Lienzo);
        rect.gameObject.AddComponent<Image>();
        var boton = rect.gameObject.AddComponent<Button>();
        EstiloBoton(boton, padre, bounds, titulo, fuente, pergamino, accion);
        return boton;
    }

    static void EstiloBoton(Button boton, RectTransform padre, Rect bounds, string titulo, TMP_FontAsset fuente,
        Sprite pergamino, UnityAction accion)
    {
        foreach (Transform anterior in boton.transform) anterior.gameObject.SetActive(false);
        Colocar((RectTransform)boton.transform, padre, bounds);
        boton.gameObject.SetActive(true);
        var cara = boton.GetComponent<Image>();
        cara.sprite = pergamino;
        cara.type = Image.Type.Sliced;
        cara.pixelsPerUnitMultiplier = 5;
        cara.color = Color.white;
        cara.raycastTarget = true;
        boton.targetGraphic = cara;
        boton.transition = Selectable.Transition.ColorTint;
        var colores = ColorBlock.defaultColorBlock;
        colores.highlightedColor = new Color32(255, 232, 175, 255);
        colores.selectedColor = Color.white;
        colores.pressedColor = new Color32(221, 191, 136, 255);
        colores.disabledColor = new Color32(143, 137, 125, 255);
        boton.colors = colores;
        boton.onClick = new Button.ButtonClickedEvent();
        if (accion != null) boton.onClick.AddListener(accion);
        Texto("EtiquetaTienda", (RectTransform)boton.transform, new Rect(.07f, .07f, .86f, .86f), Vector2.one,
            fuente, titulo, 32);
    }

    static TMP_Text Etiqueta(Button boton) => boton.GetComponentsInChildren<TMP_Text>(true)
        .First(t => t.gameObject.activeSelf);

    static void MoverTexto(TMP_Text texto, RectTransform padre, Rect bounds, float tamano, TMP_FontAsset fuente, bool negrita = false)
    {
        Colocar(texto.rectTransform, padre, bounds);
        Formatear(texto, tamano, fuente, negrita);
        texto.alignment = TextAlignmentOptions.TopLeft;
        texto.gameObject.SetActive(true);
    }

    static TMP_Text Texto(string nombre, RectTransform padre, Rect bounds, Vector2 size, TMP_FontAsset fuente,
        string contenido, float tamano, bool oscuro = true)
    {
        var texto = Label(nombre, padre, bounds, size, fuente, contenido, tamano, TextAlignmentOptions.Center);
        Formatear(texto, tamano, fuente, true);
        texto.color = oscuro ? Tinta : Crema;
        return texto;
    }

    static void Formatear(TMP_Text texto, float tamano, TMP_FontAsset fuente, bool negrita)
    {
        texto.font = fuente;
        texto.fontStyle = negrita ? FontStyles.Bold : FontStyles.Normal;
        texto.fontSize = texto.fontSizeMax = tamano;
        texto.fontSizeMin = tamano * .60f;
        texto.enableAutoSizing = true;
        texto.color = Tinta;
        texto.richText = false;
        texto.raycastTarget = false;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.overflowMode = TextOverflowModes.Ellipsis;
    }

    static Image Imagen(string nombre, RectTransform padre, Rect bounds, Sprite sprite, bool sliced = false)
    {
        var image = ImagenLocal(nombre, padre, new Rect(bounds.x / Lienzo.x, bounds.y / Lienzo.y,
            bounds.width / Lienzo.x, bounds.height / Lienzo.y), sprite);
        if (sliced)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2;
        }
        return image;
    }

    static Image ImagenLocal(string nombre, RectTransform padre, Rect bounds, Sprite sprite)
    {
        var image = Rect(nombre, padre, bounds, Vector2.one).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    static void Colocar(RectTransform rect, RectTransform padre, Rect bounds)
    {
        rect.SetParent(padre, false);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = new Vector2(bounds.xMin / Lienzo.x, 1 - bounds.yMax / Lienzo.y);
        rect.anchorMax = new Vector2(bounds.xMax / Lienzo.x, 1 - bounds.yMin / Lienzo.y);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
