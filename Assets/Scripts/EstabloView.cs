using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class EstabloView : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset fuente;
    [SerializeField] private Sprite madera;
    [SerializeField] private Sprite marco;
    [SerializeField] private Sprite fondoCriadero;
    [SerializeField] private Sprite retratoStats;
    [SerializeField] private Sprite iconosStats;
    [SerializeField] private Sprite spriteManiquiItems;
    internal Sprite IconosStats => iconosStats;
    internal Sprite TexturaMadera => madera;

    private enum Pestana { Stats, Up, Procrear, Items }

    private static readonly string[] NombresStats =
    {
        "Edad", "Nivel", "XP", "Velocidad", "Stamina", "Suerte", "Inteligencia",
        "Potencial", "Relación", "Estrés"
    };

    private readonly Dictionary<TipoEstadisticaCaballo, int> mejorasPendientes = new();
    private readonly List<Button> flechasCaballos = new();
    private readonly Dictionary<TipoSlotItem, Button> botonesRanura = new();
    private readonly Dictionary<string, Button> botonesInventario = new();
    private Pestana pestanaActual;
    private int indiceCaballo;
    private string grupoSeleccionado;
    private Sprite marcoRecortado;

    private GameManager Juego => GameManager.Instancia;
    private List<Caballo> Caballos => Juego?.caballosJugador;
    private Caballo CaballoActual => Caballos != null && indiceCaballo < Caballos.Count
        ? Caballos[indiceCaballo] : null;

    private void OnEnable()
    {
        ConstruirInterfaz();
        indiceCaballo = Mathf.Max(0, Caballos?.IndexOf(Juego.CaballoActivo) ?? 0);
        AbrirCaballo();
    }

    private void OnDisable()
    {
        mejorasPendientes.Clear();
        grupoSeleccionado = null;
        if (modal != null)
            OcultarModal();
    }

    private void OnDestroy()
    {
        if (marcoRecortado != null)
            Destroy(marcoRecortado);
    }

    // These entry points also preserve bindings in existing scenes.
    public void AbrirCaballo() => MostrarStats();
    public void MostrarStats() => Mostrar(Pestana.Stats);
    public void MostrarUps() => Mostrar(Pestana.Up);
    public void MostrarProcrear() => Mostrar(Pestana.Procrear);
    public void MostrarItems() => Mostrar(Pestana.Items);
    public void CerrarCaballo() => Juego.VolverAlHub();

    private void Mostrar(Pestana pestana)
    {
        if (pestana != pestanaActual)
        {
            mejorasPendientes.Clear();
            grupoSeleccionado = null;
        }

        OcultarModal();
        pestanaActual = pestana;
        mensajeUp.text = "";
        mensajeItems.text = "";
        for (int i = 0; i < paneles.Length; i++)
        {
            bool activa = i == (int)pestana;
            paneles[i].gameObject.SetActive(activa);
            Resaltar(pestanas[i], activa);
        }

        Refrescar();
    }

    public void SeleccionarAnterior() => CambiarCaballo(-1);
    public void SeleccionarSiguiente() => CambiarCaballo(1);

    private void CambiarCaballo(int paso)
    {
        int cantidad = Caballos?.Count ?? 0;
        if (cantidad > 1)
            SeleccionarCaballo((indiceCaballo + paso + cantidad) % cantidad);
    }

    private void SeleccionarCaballo(int indice)
    {
        if (indice == indiceCaballo)
            return;

        indiceCaballo = indice;
        mejorasPendientes.Clear();
        grupoSeleccionado = null;
        OcultarModal();
        mensajeUp.text = "";
        mensajeItems.text = "";
        Refrescar();
    }

    private void Refrescar()
    {
        int cantidad = Caballos?.Count ?? 0;
        indiceCaballo = Mathf.Clamp(indiceCaballo, 0, Mathf.Max(0, cantidad - 1));
        foreach (var flecha in flechasCaballos)
            flecha.interactable = cantidad > 1;
        selectorCaballo.text = NombreActual;
        bool activo = CaballoActual?.id == Juego.CaballoActivo?.id;
        Etiqueta(activarCaballo).text = activo ? "Activo" : "Hacer activo";
        activarCaballo.interactable = !activo && Juego.PuedeCambiarCaballo;
        estadoSeleccion.text = Juego.PuedeCambiarCaballo ? "" : "Caballo fijado para la carrera preparada";

        switch (pestanaActual)
        {
            case Pestana.Stats: RefrescarStats(); break;
            case Pestana.Up: RefrescarUp(); break;
            case Pestana.Procrear: RefrescarCriadero(); break;
            case Pestana.Items: RefrescarItems(); break;
        }
    }

    private string NombreActual => CaballoActual?.nombre ?? "Sin caballo";

    private string[] ValoresStats(Caballo caballo)
    {
        if (caballo == null)
            return Enumerable.Repeat("—", NombresStats.Length).ToArray();

        return new[]
        {
            caballo.edad.ToString(), caballo.nivel.ToString(),
            $"{caballo.experiencia} / {ProgresionCaballo.ExperienciaNecesariaParaSiguienteNivel(caballo.nivel)}",
            Estadistica(caballo, TipoEstadisticaCaballo.Velocidad),
            Estadistica(caballo, TipoEstadisticaCaballo.Stamina),
            Estadistica(caballo, TipoEstadisticaCaballo.Suerte),
            Estadistica(caballo, TipoEstadisticaCaballo.Inteligencia),
            RangoCaballo.ObtenerRango(caballo.potencial),
            caballo.relacion.ToString(), caballo.estres.ToString()
        };
    }

    private string Estadistica(Caballo caballo, TipoEstadisticaCaballo tipo) =>
        Juego.MostrarEstadistica(caballo, tipo, ProgresionCaballo.ValorBase(caballo, tipo));

    private void RefrescarStats()
    {
        nombreStats.text = NombreActual;
        AsignarSprite(retratoEnStats, HorseVisualCatalog.Default?.Splash(CaballoActual) ?? CaballoActual?.sprite);
        var valores = ValoresStats(CaballoActual);
        for (int i = 0; i < valores.Length; i++)
            valoresStats[i].text = valores[i];
    }

    private int PuntosDisponibles =>
        (CaballoActual?.puntosMejoraPendientes ?? 0) - mejorasPendientes.Values.Sum();

    private int Pendientes(TipoEstadisticaCaballo estadistica) =>
        mejorasPendientes.TryGetValue(estadistica, out int cantidad) ? cantidad : 0;

    private bool PuedePreparar(TipoEstadisticaCaballo estadistica) =>
        CaballoActual != null && PuntosDisponibles > 0 &&
        ProgresionCaballo.ValorBase(CaballoActual, estadistica) + Pendientes(estadistica) < Caballo.EstadisticaMaxima;

    private void PrepararMejora(TipoEstadisticaCaballo estadistica)
    {
        if (pestanaActual != Pestana.Up || !PuedePreparar(estadistica))
            return;

        mejorasPendientes[estadistica] = Pendientes(estadistica) + 1;
        mensajeUp.text = "";
        RefrescarUp();
    }

    private void ConfirmarMejoras()
    {
        if (pestanaActual != Pestana.Up || mejorasPendientes.Count == 0)
            return;

        bool aplicado = ProgresionCaballo.AplicarMejoras(CaballoActual, mejorasPendientes);
        if (aplicado)
            mejorasPendientes.Clear();

        mensajeUp.text = aplicado ? "Mejoras guardadas" : "No se pudieron aplicar las mejoras";
        RefrescarUp();
    }

    private void RefrescarUp()
    {
        nombreUp.text = NombreActual;
        AsignarSprite(retratoEnUp, HorseVisualCatalog.Default?.Splash(CaballoActual) ?? CaballoActual?.sprite);
        puntosUp.text = $"Puntos disponibles: {Mathf.Max(0, PuntosDisponibles)}";
        var valores = ValoresStats(CaballoActual);
        for (int i = 0; i < indicesResumen.Length; i++)
            resumenUp[i].text = valores[indicesResumen[i]];

        for (int i = 0; i < mejorasUp.Length; i++)
        {
            var estadistica = (TipoEstadisticaCaballo)i;
            int cantidad = Pendientes(estadistica);
            mejorasUp[i].text = NombresStats[i + 3] + ": " + valores[i + 3];
            pendientesUp[i].text = cantidad > 0 ? "+" + cantidad : "";
            botonesUp[i].interactable = PuedePreparar(estadistica);
        }

        potencialUp.text = "Potencial: " + valores[7];
        confirmarUp.interactable = mejorasPendientes.Count > 0 && PuntosDisponibles >= 0;
    }

    private void RefrescarCriadero()
    {
        QuitarFilas(listaCaballos);
        var caballos = Caballos;
        cantidadCaballos.text = $"Mis Caballos ({caballos?.Count ?? 0})";
        padreCriadero.text = NombreActual;
        AsignarSprite(imagenPadre, CaballoActual?.sprite);

        if (caballos == null)
            return;

        for (int i = 0; i < caballos.Count; i++)
        {
            int indice = i;
            var caballo = caballos[i];
            if (caballo == null)
                continue;

            var boton = Fila("Caballo_" + caballo.id, listaCaballos,
                caballo.nombre + "\nNv. " + caballo.nivel, caballo.sprite);
            boton.onClick.AddListener(() => SeleccionarCaballo(indice));
            Resaltar(boton, i == indiceCaballo);
        }
    }

    private static string ClaveGrupo(ItemCaballo item) =>
        string.IsNullOrEmpty(item.idDefinicion) ? item.id : item.idDefinicion;

    private IEnumerable<ItemCaballo> Disponibles() => Juego?.itemsJugador?
        .Where(item => item != null && string.IsNullOrEmpty(item.caballoEquipadoId))
        ?? Enumerable.Empty<ItemCaballo>();

    private ItemCaballo ItemSeleccionado() => Disponibles().FirstOrDefault(item => ClaveGrupo(item) == grupoSeleccionado);

    private void RefrescarItems()
    {
        tituloItems.text = "Equipamiento: " + NombreActual;
        nombreItems.text = NombreActual;
        AsignarSprite(maniquiItems, spriteManiquiItems);
        QuitarFilas(listaInventario);
        botonesInventario.Clear();

        var grupos = Disponibles().GroupBy(ClaveGrupo).ToArray();
        inventarioVacio.gameObject.SetActive(grupos.Length == 0);
        foreach (var grupo in grupos)
        {
            var item = grupo.First();
            string clave = grupo.Key;
            var boton = Fila("Item_" + clave, listaInventario, item.nombre + $"   × {grupo.Count()}", item.sprite);
            botonesInventario.Add(clave, boton);
            boton.onClick.AddListener(() => SeleccionarItem(clave));
            if (Producto(item)?.tipoProducto != TipoProductoTienda.Consumible)
                boton.gameObject.AddComponent<EstabloArrastre>().Configurar(raiz, item, () => SeleccionarItem(clave));
        }

        if (ItemSeleccionado() == null)
            grupoSeleccionado = null;
        ActualizarSeleccionItem();
    }

    private void SeleccionarItem(string clave)
    {
        grupoSeleccionado = clave;
        mensajeItems.text = "";
        ActualizarSeleccionItem();
    }

    private void ActualizarSeleccionItem()
    {
        foreach (var boton in botonesInventario)
            Resaltar(boton.Value, boton.Key == grupoSeleccionado);

        var seleccionado = ItemSeleccionado();
        descripcionItems.interactable = seleccionado != null;
        Etiqueta(descripcionItems).text = seleccionado == null ? "Selecciona un ítem" :
            Producto(seleccionado)?.tipoProducto == TipoProductoTienda.Consumible ? "Usar consumible" : "Descripción";

        foreach (var entrada in botonesRanura)
        {
            var equipado = Juego?.ObtenerItemEquipado(CaballoActual, entrada.Key);
            var boton = entrada.Value;
            Etiqueta(boton).text = equipado == null ? "Vacío" : equipado.nombre;
            boton.interactable = equipado != null || (CaballoActual != null && seleccionado != null &&
                Producto(seleccionado)?.tipoProducto != TipoProductoTienda.Consumible &&
                SistemaEquipamiento.EsCompatible(seleccionado.slotPermitido, entrada.Key));
        }
    }

    private DefinicionProductoTienda Producto(ItemCaballo item) =>
        Juego == null ? null : SistemaTienda.Buscar(Juego.CatalogoTienda(), item.idDefinicion);

    private void MostrarDescripcion()
    {
        var item = ItemSeleccionado();
        if (item == null)
            return;

        var producto = Producto(item);
        if (producto?.tipoProducto == TipoProductoTienda.Consumible)
        {
            bool puede = Juego.PuedeUsarConsumible(CaballoActual, item, out string motivo);
            string duracion = producto.bonificacion != 0 || producto.bonificacionSecundaria != 0
                ? "Preparación de este día. Usar antes de elegir carrera." : "Usar antes de elegir carrera.";
            if (producto.experiencia > 0) duracion += " La XP obtenida es permanente.";
            MostrarModal(producto.nombre, producto.descripcion + "\n" + producto.EfectoVisible +
                "\n" + duracion + "\n" + motivo,
                () =>
                {
                    Juego.UsarConsumible(CaballoActual, item, out string resultado);
                    mensajeItems.text = resultado;
                    RefrescarItems();
                }, puede);
            Etiqueta(confirmarModal).text = "Usar";
            return;
        }
        string efecto = producto?.EfectoVisible ?? "Sin bonificación registrada";
        MostrarModal(producto?.nombre ?? item.nombre,
            $"{producto?.descripcion ?? item.descripcion}\n\n{efecto}\nRanura: {SistemaEquipamiento.NombreRanura(producto?.slotPermitido ?? item.slotPermitido)}");
    }

    private void ActivarRanura(TipoSlotItem ranura)
    {
        var caballo = CaballoActual;
        var equipado = Juego?.ObtenerItemEquipado(caballo, ranura);
        if (equipado == null)
        {
            if (ItemSeleccionado() is ItemCaballo item)
                EquiparEnRanura(ranura, item);
            return;
        }

        MostrarModal(equipado.nombre, (Producto(equipado)?.EfectoVisible ?? "") +
            $"\nDesequipar de {SistemaEquipamiento.NombreRanura(ranura)} cuesta {Moneda.Texto(SistemaEquipamiento.CostoDesequiparItem)}.",
            () =>
            {
                bool retirado = Juego.DesequiparItem(caballo, equipado);
                mensajeItems.text = retirado ? "Ítem devuelto al inventario" : "No se pudo desequipar";
                RefrescarItems();
            }, Juego.dinero >= SistemaEquipamiento.CostoDesequiparItem);
        Etiqueta(confirmarModal).text = "Desequipar";
    }

    private void EquiparEnRanura(TipoSlotItem ranura, ItemCaballo item)
    {
        if (pestanaActual != Pestana.Items || CaballoActual == null || item == null)
            return;

        int nivelNecesario = Producto(item)?.nivelMinimo ?? 1;
        if (CaballoActual.nivel < nivelNecesario)
        {
            mensajeItems.text = "Requiere nivel " + nivelNecesario;
            return;
        }

        if (!SistemaEquipamiento.EsCompatible(item.slotPermitido, ranura))
        {
            mensajeItems.text = "Ítem incompatible con " + SistemaEquipamiento.NombreRanura(ranura);
            return;
        }

        int costo = SistemaEquipamiento.CostoEquiparItem;
        if (Juego.ObtenerItemEquipado(CaballoActual, ranura) != null)
            costo += SistemaEquipamiento.CostoDesequiparItem;

        if (Juego.dinero < costo)
        {
            mensajeItems.text = "Saldo insuficiente";
            return;
        }

        bool equipado = Juego.EquiparItem(CaballoActual, item, ranura, true);
        mensajeItems.text = equipado ? "Equipo guardado" : "No se pudo equipar";
        if (equipado)
            RefrescarItems();
    }
}
