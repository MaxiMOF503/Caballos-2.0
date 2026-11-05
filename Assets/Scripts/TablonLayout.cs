using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Layout scoped to View_Tablon. The existing controls keep their input bindings.
internal static class TablonLayout
{
    static readonly Vector2 BoardSize = new Vector2(1672, 940.5f);
    static readonly Color Cream = new Color32(246, 232, 204, 255);
    static readonly Color Ink = new Color32(65, 36, 21, 255);
    static readonly Color Rail = new Color32(71, 39, 22, 255);

    public static Sprite Construir(TablonView view, RectTransform root, Sprite marco, Sprite madera)
    {
        var antiguos = new List<GameObject>();
        foreach (Transform child in root)
            if (child != view.transform) antiguos.Add(child.gameObject);

        root.GetComponent<Image>().color = new Color32(32, 17, 10, 255);
        var board = Rect("TablonRustico", root, new Rect(0, 0, BoardSize.x, BoardSize.y), BoardSize);

        var frame = board.gameObject.AddComponent<Image>();
        Sprite recorte = ConfigurarMarco(frame, marco);

        var font = view.textoNombre.font;
        Panel("WoodenBackground", board, new Rect(48, 36, 1576, 868), BoardSize, madera);
        var headerSize = new Vector2(1560, 82);
        var header = Panel("Header", board, new Rect(56, 42, headerSize.x, headerSize.y), BoardSize, madera);
        Label("MainTitle", header, new Rect(286, 0, 988, 68), headerSize, font,
            "TABLÓN DE APUESTAS", 54, TextAlignmentOptions.Center);
        var volver = Boton("BackButton", header, new Rect(1396, 12, 146, 44), headerSize, font, "Volver");
        volver.onClick.AddListener(() => GameManager.Instancia.VolverAlHub());

        var leftSize = new Vector2(794, 542);
        var left = Panel("ParticipantsPanel", board, new Rect(68, 136, leftSize.x, leftSize.y), BoardSize, madera);
        Label("Title_Participants", left, new Rect(38, 12, 718, 57), leftSize, font,
            "Participantes", 41, TextAlignmentOptions.Center);
        var scroll = Rect("ParticipantsScrollView", left, new Rect(64, 96, 682, 406), leftSize);
        var hit = scroll.gameObject.AddComponent<Image>();
        hit.color = Color.clear;
        scroll.gameObject.AddComponent<RectMask2D>();
        var scrolling = scroll.gameObject.AddComponent<ScrollRect>();
        scrolling.viewport = scroll;
        scrolling.horizontal = false;
        scrolling.movementType = ScrollRect.MovementType.Clamped;
        scrolling.scrollSensitivity = 38;
        var content = Rect("ParticipantsContent", scroll, new Rect(0, 0, 682, 406), new Vector2(682, 406));
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(.5f, 1);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var rows = content.gameObject.AddComponent<TablonRowsLayout>();
        rows.childControlWidth = rows.childControlHeight = true;
        rows.childForceExpandWidth = true;
        rows.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrolling.content = content;
        var track = Surface("ScrollTrack", left, new Rect(760, 96, 9, 406), leftSize, Rail);
        track.GetComponent<Image>().raycastTarget = true;
        var thumb = Surface("Thumb", track, new Rect(0, 0, 9, 406), new Vector2(9, 406), Cream);
        thumb.GetComponent<Image>().raycastTarget = true;
        var bar = track.gameObject.AddComponent<Scrollbar>();
        bar.handleRect = thumb;
        bar.targetGraphic = thumb.GetComponent<Image>();
        bar.direction = Scrollbar.Direction.BottomToTop;
        scrolling.verticalScrollbar = bar;
        scrolling.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        view.content = content;

        var rightSize = new Vector2(732, 542);
        var right = Panel("HorseDetailsPanel", board, new Rect(878, 136, rightSize.x, rightSize.y), BoardSize, madera);
        Label("Title_HorseDetails", right, new Rect(40, 12, 652, 57), rightSize, font,
            "Datos del caballo", 41, TextAlignmentOptions.Center);
        MoveText(view.textoNombre, right, new Rect(132, 84, 540, 56), rightSize, 40, true);
        Surface("DetailsRule", right, new Rect(132, 143, 523, 2), rightSize, new Color32(187, 132, 79, 255));
        var details = new[] {view.textoEdad, view.textoNivel, view.textoVelocidad, view.textoStamina,
            view.textoSuerte, view.textoInteligencia};
        for (int i = 0; i < details.Length; i++)
            MoveText(details[i], right, new Rect(144, 157 + i * 58, 532, 48), rightSize, 34);

        var footerSize = new Vector2(1560, 180);
        var footer = Panel("BettingFooter", board, new Rect(56, 704, footerSize.x, footerSize.y), BoardSize, madera);
        view.saldo = Label("BalanceText", footer, new Rect(72, 44, 320, 60), footerSize, font,
            "", 40, TextAlignmentOptions.MidlineLeft);
        Surface("BalanceDivider", footer, new Rect(402, 26, 3, 116), footerSize, new Color32(177, 121, 74, 255));
        Label("AmountLabel", footer, new Rect(464, 25, 248, 50), footerSize, font,
            "Monto a apostar:", 29, TextAlignmentOptions.MidlineLeft);
        Colocar(view.importe.GetComponent<RectTransform>(), footer, new Rect(720, 25, 358, 54), footerSize);
        EstiloImporte(view.importe);
        MoveText(view.textoPotencial, footer, new Rect(464, 90, 620, 44), footerSize, 30);
        view.textoPotencial.gameObject.name = "EstimatedPayoutText";
        Surface("BetDivider", footer, new Rect(1122, 26, 3, 116), footerSize, new Color32(177, 121, 74, 255));
        Colocar(view.apostar.GetComponent<RectTransform>(), footer, new Rect(1168, 24, 345, 103), footerSize);
        var betFace = view.apostar.GetComponent<Image>();
        betFace.sprite = null;
        betFace.color = new Color32(111, 64, 32, 255);
        Wood("WoodFace", view.apostar.GetComponent<RectTransform>(), new Rect(4, 4, 337, 95), new Vector2(345, 103), madera)
            .SetAsFirstSibling();
        TextoBoton(view.apostar, "Apostar", 42, Cream);
        EstiloBoton(view.apostar);
        Colocar(view.confirmar.GetComponent<RectTransform>(), footer, new Rect(1168, 36, 165, 68), footerSize);
        Colocar(view.cancelar.GetComponent<RectTransform>(), footer, new Rect(1346, 36, 167, 68), footerSize);
        foreach (var button in new[] {view.confirmar, view.cancelar})
        {
            button.GetComponent<Image>().color = Cream;
            TextoBoton(button, button == view.confirmar ? "Confirmar" : "Cancelar", 24, Ink);
            EstiloBoton(button);
        }
        view.feedback = Label("FeedbackText", footer, new Rect(464, 146, 1049, 31), footerSize, font,
            "", 21, TextAlignmentOptions.MidlineLeft);

        foreach (var anterior in antiguos) anterior.SetActive(false);
        return recorte;
    }

    public static void EstiloFila(CaballoButtonUI fila)
    {
        var button = fila.GetComponent<Button>();
        var image = fila.GetComponent<Image>();
        image.color = Cream;
        if (fila.GetComponent<LayoutElement>() == null)
            fila.gameObject.AddComponent<LayoutElement>();
        EstiloBoton(button);
        var texto = fila.textoNombre;
        Formato(texto, 31, Ink, TextAlignmentOptions.Center, true);
        texto.rectTransform.anchorMin = Vector2.zero;
        texto.rectTransform.anchorMax = Vector2.one;
        texto.rectTransform.offsetMin = new Vector2(16, 4);
        texto.rectTransform.offsetMax = new Vector2(-16, -4);
    }

    static void EstiloImporte(TMP_InputField input)
    {
        var image = input.GetComponent<Image>();
        image.color = new Color32(250, 245, 235, 255);
        Formato(input.textComponent, 26, Ink, TextAlignmentOptions.MidlineLeft, false);
        if (input.placeholder is TMP_Text placeholder)
        {
            Formato(placeholder, 24, new Color32(112, 93, 80, 255), TextAlignmentOptions.MidlineLeft, false);
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.text = "Ingresar monto...";
        }
    }

    static RectTransform Panel(string name, RectTransform parent, Rect bounds, Vector2 size, Sprite madera)
    {
        var panel = Surface(name, parent, bounds, size, Rail);
        Wood("Planks", panel, new Rect(3, 3, bounds.width - 6, bounds.height - 6), bounds.size, madera);
        return panel;
    }

    internal static Sprite ConfigurarMarco(Image imagen, Sprite marco)
    {
        imagen.raycastTarget = false;
        if (marco == null)
        {
            imagen.color = Rail;
            return null;
        }

        var rect = marco.rect;
        rect.x += rect.width * 25 / 1536;
        rect.y += rect.height * 34 / 1024;
        rect.width *= 1486f / 1536;
        rect.height *= 945f / 1024;
        var recorte = Sprite.Create(marco.texture, rect, new Vector2(.5f, .5f),
            100, 0, SpriteMeshType.FullRect, new Vector4(96, 96, 96, 96));
        imagen.sprite = recorte;
        imagen.type = Image.Type.Sliced;
        imagen.pixelsPerUnitMultiplier = 1.4f;
        return recorte;
    }

    internal static RectTransform Wood(string name, RectTransform parent, Rect bounds, Vector2 size, Sprite madera)
    {
        var rect = Rect(name, parent, bounds, size);
        var wood = rect.gameObject.AddComponent<TablonWoodGraphic>();
        wood.texture = madera != null ? madera.texture : null;
        if (madera != null)
            wood.material = Resources.Load<Material>("Tablon/MaderaCalida");
        else
            wood.color = new Color32(121, 69, 38, 255);
        wood.raycastTarget = false;
        return rect;
    }

    internal static RectTransform Surface(string name, RectTransform parent, Rect bounds, Vector2 size, Color color)
    {
        var rect = Rect(name, parent, bounds, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    static Button Boton(string name, RectTransform parent, Rect bounds, Vector2 size, TMP_FontAsset font, string label)
    {
        var rect = Surface(name, parent, bounds, size, Cream);
        var image = rect.GetComponent<Image>();
        image.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        EstiloBoton(button);
        Label("Label", rect, new Rect(0, 0, bounds.width, bounds.height), bounds.size, font,
            label, 25, TextAlignmentOptions.Center, Ink);
        return button;
    }

    static void EstiloBoton(Button button)
    {
        button.transition = Selectable.Transition.ColorTint;
        var colors = ColorBlock.defaultColorBlock;
        colors.highlightedColor = new Color(.96f, .89f, .8f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.79f, .68f, .55f);
        colors.disabledColor = new Color(.6f, .56f, .51f);
        button.colors = colors;
    }

    static void TextoBoton(Button button, string label, float size, Color color)
    {
        var text = button.GetComponentInChildren<TMP_Text>(true);
        text.text = label;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(6, 4);
        text.rectTransform.offsetMax = new Vector2(-6, -4);
        Formato(text, size, color, TextAlignmentOptions.Center, true);
    }

    static void MoveText(TMP_Text text, RectTransform parent, Rect bounds, Vector2 size, float fontSize, bool bold = false)
    {
        Colocar(text.rectTransform, parent, bounds, size);
        Formato(text, fontSize, Cream, TextAlignmentOptions.MidlineLeft, bold);
    }

    internal static TMP_Text Label(string name, RectTransform parent, Rect bounds, Vector2 size, TMP_FontAsset font,
        string value, float fontSize, TextAlignmentOptions alignment, Color? color = null)
    {
        var rect = Rect(name, parent, bounds, size);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        Formato(text, fontSize, color ?? Cream, alignment, true);
        return text;
    }

    static void Formato(TMP_Text text, float size, Color color, TextAlignmentOptions alignment, bool bold)
    {
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        text.enableAutoSizing = true;
        text.fontSize = text.fontSizeMax = size;
        text.fontSizeMin = 15;
        text.raycastTarget = false;
        text.margin = Vector4.zero;
    }

    internal static RectTransform Rect(string name, RectTransform parent, Rect bounds, Vector2 size)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        Colocar(rect, parent, bounds, size);
        return rect;
    }

    static void Colocar(RectTransform rect, RectTransform parent, Rect bounds, Vector2 size)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(bounds.xMin / size.x, 1 - bounds.yMax / size.y);
        rect.anchorMax = new Vector2(bounds.xMax / size.x, 1 - bounds.yMin / size.y);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}

// Keeps five complete rows in view as the available height changes.
internal sealed class TablonRowsLayout : VerticalLayoutGroup
{
    public override void CalculateLayoutInputVertical()
    {
        float height = ((RectTransform)transform.parent).rect.height;
        spacing = height * 21 / 406;
        foreach (var row in rectChildren)
        {
            var element = row.GetComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height * 61 / 406;
        }
        base.CalculateLayoutInputVertical();
    }
}

// Rotates the original plank texture in UV space and crops to cover each panel.
internal sealed class TablonWoodGraphic : RawImage
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        base.OnPopulateMesh(mesh);
        if (texture == null || rectTransform.rect.height <= 0) return;
        float target = rectTransform.rect.width / rectTransform.rect.height;
        float source = (float)texture.height / texture.width;
        float u = Mathf.Min(1, source / target);
        float v = Mathf.Min(1, target / source);
        for (int i = 0; i < mesh.currentVertCount; i++)
        {
            var vertex = UIVertex.simpleVert;
            mesh.PopulateUIVertex(ref vertex, i);
            vertex.uv0 = new Vector2(.5f + (vertex.uv0.y - .5f) * u, .5f + (.5f - vertex.uv0.x) * v);
            mesh.SetUIVertex(vertex, i);
        }
    }
}
