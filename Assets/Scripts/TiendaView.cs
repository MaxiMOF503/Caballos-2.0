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

    [Header("Presentación")]
    [SerializeField] Sprite fondo, estanteria, pergamino, casilla, marcoCartel, marcoBoton;
    [SerializeField] Sprite vendedor;

    internal readonly List<TiendaProductoUI> ofertas = new();
    internal Button botonReroll;
    internal TMP_Text mensaje, textoConfirmacion;
    internal CanvasGroup contenido;
    internal GameObject confirmacion;
    internal Button cancelar;
    string productoSeleccionadoId;
    string compraPendienteId;
    int precioConfirmado;
    GameObject focoAnterior;
    bool interfazConfigurada;

    void OnEnable()
    {
        if (!interfazConfigurada)
        {
            TiendaLayout.Construir(this, (RectTransform)transform.parent,
                fondo, estanteria, pergamino, casilla, marcoCartel, marcoBoton, vendedor);
            interfazConfigurada = true;
        }
        RefrescarTienda(false);
    }

    void OnDisable()
    {
        CerrarConfirmacion();
    }

    internal DefinicionProductoTienda ProductoDisponible(string id)
    {
        var juego = GameManager.Instancia;
        if (string.IsNullOrEmpty(id) || juego?.estadoTienda?.idsProductosStock == null ||
            !juego.estadoTienda.idsProductosStock.Contains(id))
            return null;

        return SistemaTienda.Buscar(juego.CatalogoTienda(), id);
    }

    void RefrescarTienda(bool conservarSeleccion)
    {
        CerrarConfirmacion();
        var juego = GameManager.Instancia;
        if (juego == null) return;

        if (juego.SincronizarTienda())
            juego.GuardarPartida();

        if (!conservarSeleccion || ProductoDisponible(productoSeleccionadoId) == null)
            productoSeleccionadoId = null;

        var stock = juego.estadoTienda.idsProductosStock;
        for (int i = 0; i < ofertas.Count; i++)
            ofertas[i].Mostrar(i < stock.Count ? ProductoDisponible(stock[i]) : null);

        mensaje.text = "";
        ActualizarReroll();
        MostrarDetalle();
    }

    public void RefrescarPorCambioDeStock() => RefrescarTienda(true);

    internal void SeleccionarProducto(string id)
    {
        if (confirmacion.activeSelf || ProductoDisponible(id) == null) return;
        productoSeleccionadoId = id;
        mensaje.text = "";
        MostrarDetalle();
    }

    void SeleccionarOferta(int indice)
    {
        var stock = GameManager.Instancia?.estadoTienda?.idsProductosStock;
        if (stock != null && indice >= 0 && indice < stock.Count)
            SeleccionarProducto(stock[indice]);
    }

    // Preserve the original entry points for existing scene bindings.
    public void SeleccionarHerraduraPlata() => SeleccionarOferta(0);
    public void SeleccionarManzanaDorada() => SeleccionarOferta(1);
    public void SeleccionarHerraduraOro() => SeleccionarOferta(2);
    public void SeleccionarZapatillas() => SeleccionarOferta(3);
    public void SeleccionarSombreroCopa() => SeleccionarOferta(4);
    public void SeleccionarSombreroNoble() => SeleccionarOferta(5);

    static bool PuedeComprar(DefinicionProductoTienda producto)
    {
        return producto != null && GameManager.Instancia != null &&
            producto.tipoProducto == TipoProductoTienda.Equipable && producto.tieneSlotEquipable &&
            producto.sprite != null && GameManager.Instancia.dinero >= producto.precio;
    }

    void MostrarDetalle()
    {
        var producto = ProductoDisponible(productoSeleccionadoId);
        textoNombre.text = producto?.nombre ?? "Selecciona un ítem";
        textoDescripcion.text = "";
        textoPrecio.text = "";
        if (producto != null)
        {
            textoDescripcion.text = producto.descripcion;
            if (producto.tipoProducto == TipoProductoTienda.Equipable)
            {
                textoDescripcion.text += $"\nBonificación: +{producto.bonificacion} {producto.estadistica}";
                textoDescripcion.text += "\nRanura: " + SistemaEquipamiento.NombreRanura(producto.slotPermitido);
            }
            else
                textoDescripcion.text += "\nConsumible · Próximamente";

            textoPrecio.text = $"Precio: ${producto.precio}";
            if (GameManager.Instancia.dinero < producto.precio)
                textoPrecio.text += " · Saldo insuficiente";
        }

        foreach (var oferta in ofertas)
            oferta.Marcar(producto?.idDefinicion);

        botonComprar.GetComponent<Button>().interactable = PuedeComprar(producto);
    }

    public void MostrarConfirmacion()
    {
        var producto = ProductoDisponible(productoSeleccionadoId);
        if (confirmacion.activeSelf || !PuedeComprar(producto)) return;
        compraPendienteId = producto.idDefinicion;
        precioConfirmado = producto.precio;
        textoConfirmacion.text = producto.nombre + $" — ${producto.precio}";
        foreach (var oferta in ofertas) oferta.QuitarHover();
        focoAnterior = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
        contenido.interactable = false;
        contenido.blocksRaycasts = false;
        confirmacion.SetActive(true);
        cancelar.Select();
    }

    public void CancelarCompra() => CerrarConfirmacion();

    void CerrarConfirmacion()
    {
        compraPendienteId = null;
        if (confirmacion == null) return;
        confirmacion.SetActive(false);
        contenido.interactable = true;
        contenido.blocksRaycasts = true;
        if (focoAnterior != null && focoAnterior.activeInHierarchy)
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(focoAnterior);
        focoAnterior = null;
    }

    public void ConfirmarCompra()
    {
        if (string.IsNullOrEmpty(compraPendienteId)) return;
        var producto = ProductoDisponible(compraPendienteId);
        bool valida = PuedeComprar(producto) && producto.precio == precioConfirmado;
        string id = compraPendienteId;
        // Consume this confirmation before calling the existing synchronous transaction.
        CerrarConfirmacion();
        bool comprada = valida && GameManager.Instancia.IntentarComprarItem(id);
        mensaje.text = comprada ? "Compra guardada" : "No se pudo comprar. Revisá la oferta y el saldo.";
        ActualizarReroll();
        MostrarDetalle();
    }

    internal void EjecutarRerollManual()
    {
        if (confirmacion.activeSelf) return;
        if (GameManager.Instancia.IntentarRerollTiendaManual())
            RefrescarTienda(true);
        else
            mensaje.text = "No se pudo renovar la oferta.";
    }

    void ActualizarReroll()
    {
        botonReroll.GetComponentInChildren<TMP_Text>().text = "Reroll $" + SistemaTienda.CostoRerollManual;
        botonReroll.interactable = GameManager.Instancia.dinero >= SistemaTienda.CostoRerollManual;
    }

    internal void Volver()
    {
        CerrarConfirmacion();
        GameManager.Instancia.VolverAlHub();
    }
}
