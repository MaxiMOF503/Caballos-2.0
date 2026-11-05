using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class EstabloArrastre : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    internal ItemCaballo Item { get; private set; }
    private RectTransform raiz;
    private RectTransform vistaPrevia;
    private System.Action seleccionar;

    internal void Configurar(RectTransform contenedor, ItemCaballo item, System.Action alArrastrar)
    {
        raiz = contenedor;
        Item = item;
        seleccionar = alArrastrar;
    }

    public void OnBeginDrag(PointerEventData evento)
    {
        if (evento.button != PointerEventData.InputButton.Left)
            return;

        seleccionar();
        var objeto = new GameObject("VistaPreviaEquipo", typeof(RectTransform));
        objeto.layer = raiz.gameObject.layer;
        vistaPrevia = (RectTransform)objeto.transform;
        vistaPrevia.SetParent(raiz, false);
        vistaPrevia.sizeDelta = new Vector2(205, 64);
        vistaPrevia.localScale = Vector3.one;

        var cara = objeto.AddComponent<Image>();
        cara.color = new Color32(81, 45, 23, 240);
        cara.raycastTarget = false;

        var icono = new GameObject("Icono", typeof(RectTransform)).AddComponent<Image>();
        icono.gameObject.layer = objeto.layer;
        icono.rectTransform.SetParent(vistaPrevia, false);
        icono.rectTransform.anchorMin = new Vector2(.02f, .04f);
        icono.rectTransform.anchorMax = new Vector2(.31f, .96f);
        icono.rectTransform.offsetMin = icono.rectTransform.offsetMax = Vector2.zero;
        icono.sprite = Item.sprite;
        icono.enabled = Item.sprite != null;
        icono.preserveAspect = true;
        icono.raycastTarget = false;

        var etiqueta = new GameObject("Nombre", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        etiqueta.gameObject.layer = objeto.layer;
        etiqueta.rectTransform.SetParent(vistaPrevia, false);
        etiqueta.rectTransform.anchorMin = new Vector2(.33f, 0);
        etiqueta.rectTransform.anchorMax = new Vector2(.98f, 1);
        etiqueta.rectTransform.offsetMin = etiqueta.rectTransform.offsetMax = Vector2.zero;
        etiqueta.font = GetComponentInChildren<TMP_Text>().font;
        etiqueta.text = Item.nombre;
        etiqueta.richText = false;
        etiqueta.alignment = TextAlignmentOptions.MidlineLeft;
        etiqueta.enableAutoSizing = true;
        etiqueta.fontSizeMin = 12;
        etiqueta.fontSizeMax = 20;
        etiqueta.raycastTarget = false;

        OnDrag(evento);
    }

    public void OnDrag(PointerEventData evento)
    {
        if (vistaPrevia != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(raiz, evento.position, evento.pressEventCamera, out Vector2 punto))
            vistaPrevia.anchoredPosition = punto + new Vector2(18, -19);
    }

    public void OnEndDrag(PointerEventData evento) => QuitarVistaPrevia();
    private void OnDisable() => QuitarVistaPrevia();

    private void QuitarVistaPrevia()
    {
        if (vistaPrevia == null)
            return;

        vistaPrevia.gameObject.SetActive(false);
        Destroy(vistaPrevia.gameObject);
        vistaPrevia = null;
    }
}
