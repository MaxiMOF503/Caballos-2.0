using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TiendaView : MonoBehaviour
{
    [Header("Textos")]
    public TMP_Text textoNombre;
    public TMP_Text textoDescripcion;
    public TMP_Text textoPrecio;

    [Header("Botones")]
    public GameObject botonComprar;
    public GameObject botonSi;
    public GameObject botonNo;

    private readonly List<Button> botonesOferta = new List<Button>();
    private DefinicionProductoTienda productoSeleccionado;
    private Button botonReroll;

    private void Start()
    {
        AsegurarBotones();
        RefrescarTienda();
    }

    private void OnEnable()
    {
        AsegurarBotones();
        RefrescarTienda();
    }

    private void AsegurarBotones()
    {
        if (botonesOferta.Count > 0)
            return;

        Transform panel = transform.parent != null ? transform.parent.Find("PanelItems") : null;
        if (panel == null)
            return;

        foreach (Button boton in panel.GetComponentsInChildren<Button>(true))
            if (boton.transform.parent == panel)
                botonesOferta.Add(boton);

        if (botonesOferta.Count > 0)
            CrearBotonReroll();
    }

    private void CrearBotonReroll()
    {
        if (botonReroll != null || botonComprar == null)
            return;
        Button plantilla = botonComprar.GetComponent<Button>();
        if (plantilla == null)
            return;

        botonReroll = Instantiate(plantilla, botonComprar.transform.parent);
        botonReroll.name = "RerollTienda";
        botonReroll.onClick = new Button.ButtonClickedEvent();
        botonReroll.onClick.AddListener(EjecutarRerollManual);

        LayoutElement layout = botonReroll.GetComponent<LayoutElement>();
        if (layout == null)
            layout = botonReroll.gameObject.AddComponent<LayoutElement>();
        layout.ignoreLayout = true;

        RectTransform rect = botonReroll.GetComponent<RectTransform>();
        RectTransform rectComprar = plantilla.GetComponent<RectTransform>();
        rect.anchorMin = rectComprar.anchorMin;
        rect.anchorMax = rectComprar.anchorMax;
        rect.pivot = rectComprar.pivot;
        rect.anchoredPosition = rectComprar.anchoredPosition + new Vector2(0f, 100f);
        rect.sizeDelta = new Vector2(190f, 36f);
        rect.localScale = rectComprar.localScale;

        TMP_Text etiqueta = botonReroll.GetComponentInChildren<TMP_Text>(true);
        if (etiqueta != null)
            etiqueta.text = "Reroll $" + SistemaTienda.CostoRerollManual;
        foreach (Graphic grafico in botonReroll.GetComponentsInChildren<Graphic>(true))
            if (grafico.gameObject != botonReroll.gameObject)
                grafico.raycastTarget = false;
        botonReroll.gameObject.SetActive(true);
    }

    private void RefrescarTienda()
    {
        GameManager juego = GameManager.Instancia;
        if (juego == null || botonesOferta.Count == 0)
            return;

        if (juego.SincronizarTienda())
            juego.GuardarPartida();

        productoSeleccionado = null;
        List<DefinicionProductoTienda> catalogo = juego.CatalogoTienda();
        List<string> stock = juego.estadoTienda.idsProductosStock;

        for (int i = 0; i < botonesOferta.Count; i++)
        {
            Button boton = botonesOferta[i];
            DefinicionProductoTienda producto = i < stock.Count
                ? SistemaTienda.Buscar(catalogo, stock[i])
                : null;
            Image icono = boton.transform.childCount > 0
                ? boton.transform.GetChild(0).GetComponent<Image>()
                : null;
            TMP_Text etiqueta = boton.GetComponentInChildren<TMP_Text>(true);
            if (etiqueta != null)
            {
                etiqueta.text = string.Empty;
                etiqueta.gameObject.SetActive(false);
            }
            if (icono != null)
            {
                icono.sprite = producto != null ? producto.sprite : null;
                icono.preserveAspect = true;
                icono.enabled = icono.sprite != null;
            }

            boton.gameObject.SetActive(producto != null);
            if (producto != null)
                boton.interactable = true;
        }

        if (botonReroll != null)
            botonReroll.interactable = juego.dinero >= SistemaTienda.CostoRerollManual;
        MostrarDetalle();
    }

    public void RefrescarPorCambioDeStock()
    {
        RefrescarTienda();
    }

    private void SeleccionarOferta(int indice)
    {
        GameManager juego = GameManager.Instancia;
        if (juego == null || juego.estadoTienda == null ||
            indice < 0 || indice >= juego.estadoTienda.idsProductosStock.Count ||
            indice >= botonesOferta.Count || !botonesOferta[indice].gameObject.activeSelf)
            return;

        productoSeleccionado = SistemaTienda.Buscar(
            juego.CatalogoTienda(), juego.estadoTienda.idsProductosStock[indice]);
        MostrarDetalle();
    }

    // Los eventos de los seis botones existentes están serializados con estos nombres.
    public void SeleccionarHerraduraPlata() => SeleccionarOferta(0);
    public void SeleccionarManzanaDorada() => SeleccionarOferta(1);
    public void SeleccionarHerraduraOro() => SeleccionarOferta(2);
    public void SeleccionarZapatillas() => SeleccionarOferta(3);
    public void SeleccionarSombreroCopa() => SeleccionarOferta(4);
    public void SeleccionarSombreroNoble() => SeleccionarOferta(5);

    private bool PuedeComprar()
    {
        GameManager juego = GameManager.Instancia;
        return productoSeleccionado != null && juego != null &&
            productoSeleccionado.tipoProducto == TipoProductoTienda.Equipable &&
            productoSeleccionado.tieneSlotEquipable &&
            productoSeleccionado.sprite != null && juego.dinero >= productoSeleccionado.precio;
    }

    private void MostrarDetalle()
    {
        if (textoNombre == null || textoDescripcion == null || textoPrecio == null)
            return;

        if (productoSeleccionado == null)
        {
            textoNombre.text = "Selecciona un item";
            textoDescripcion.text = string.Empty;
            textoPrecio.text = string.Empty;
        }
        else
        {
            textoNombre.text = productoSeleccionado.nombre;
            textoDescripcion.text = productoSeleccionado.descripcion;
            textoPrecio.text = productoSeleccionado.tipoProducto == TipoProductoTienda.Consumible
                ? "Precio: $" + productoSeleccionado.precio + " — Consumible - próximamente"
                : "Precio: $" + productoSeleccionado.precio;
        }

        ReiniciarConfirmacion();
    }

    private void ReiniciarConfirmacion()
    {
        if (botonComprar != null)
        {
            botonComprar.SetActive(true);
            Button boton = botonComprar.GetComponent<Button>();
            if (boton != null)
                boton.interactable = PuedeComprar();
        }
        if (botonSi != null) botonSi.SetActive(false);
        if (botonNo != null) botonNo.SetActive(false);
    }

    public void MostrarConfirmacion()
    {
        if (!PuedeComprar())
            return;
        botonComprar.SetActive(false);
        botonSi.SetActive(true);
        botonNo.SetActive(true);
    }

    public void CancelarCompra()
    {
        ReiniciarConfirmacion();
    }

    public void ConfirmarCompra()
    {
        if (PuedeComprar())
            GameManager.Instancia.IntentarComprarItem(productoSeleccionado.idDefinicion);
        ReiniciarConfirmacion();
        if (botonReroll != null && GameManager.Instancia != null)
            botonReroll.interactable = GameManager.Instancia.dinero >= SistemaTienda.CostoRerollManual;
    }

    private void EjecutarRerollManual()
    {
        if (GameManager.Instancia != null && GameManager.Instancia.IntentarRerollTiendaManual())
            RefrescarTienda();
    }
}
