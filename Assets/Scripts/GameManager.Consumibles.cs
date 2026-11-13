using UnityEngine;

public partial class GameManager
{
    static int Preparacion(Caballo caballo, TipoEstadisticaCaballo tipo) => tipo switch
    {
        TipoEstadisticaCaballo.Velocidad => caballo.preparacionVelocidad,
        TipoEstadisticaCaballo.Stamina => caballo.preparacionStamina,
        TipoEstadisticaCaballo.Suerte => caballo.preparacionSuerte,
        _ => caballo.preparacionInteligencia
    };

    public bool PuedeUsarConsumible(Caballo caballo, ItemCaballo item, out string mensaje)
    {
        mensaje = "";
        caballo = caballosJugador.Find(c => c != null && c.id == caballo?.id);
        item = itemsJugador.Find(i => i != null && i.id == item?.id);
        if (caballo == null || item == null ||
            !string.IsNullOrEmpty(item.caballoEquipadoId) || campanaCompletada)
        {
            mensaje = "Seleccioná un caballo propio y un consumible del inventario.";
            return false;
        }
        var producto = SistemaTienda.Buscar(CatalogoTienda(), item.idDefinicion);
        if (producto?.tipoProducto != TipoProductoTienda.Consumible)
        {
            mensaje = "Este artículo se utiliza como equipamiento.";
            return false;
        }
        if (caballo.ultimoDiaConsumible == dia)
        {
            mensaje = "Este caballo ya utilizó un consumible hoy.";
            return false;
        }
        if (ObtenerCarreraProgramada()?.Buscar(caballo.id) != null)
        {
            mensaje = "Prepará al caballo antes de seleccionar su carrera.";
            return false;
        }
        return true;
    }

    public bool UsarConsumible(Caballo caballo, ItemCaballo item, out string mensaje)
    {
        if (!PuedeUsarConsumible(caballo, item, out mensaje)) return false;
        caballo = caballosJugador.Find(c => c != null && c.id == caballo.id);
        item = itemsJugador.Find(i => i != null && i.id == item.id);
        var producto = SistemaTienda.Buscar(CatalogoTienda(), item.idDefinicion);
        string anterior = JsonUtility.ToJson(CaballoData.DesdeCaballo(caballo, -1));
        var azarAnterior = Random.state;
        int indice = itemsJugador.IndexOf(item);
        caballo.preparacionVelocidad = Mathf.Clamp(producto.BonoPara(TipoEstadisticaCaballo.Velocidad), 0, 2);
        caballo.preparacionStamina = Mathf.Clamp(producto.BonoPara(TipoEstadisticaCaballo.Stamina), 0, 2);
        caballo.preparacionSuerte = Mathf.Clamp(producto.BonoPara(TipoEstadisticaCaballo.Suerte), 0, 2);
        caballo.preparacionInteligencia = Mathf.Clamp(producto.BonoPara(TipoEstadisticaCaballo.Inteligencia), 0, 2);
        caballo.ultimoDiaConsumible = dia;
        if (producto.experiencia > 0) ProgresionCaballo.AgregarExperiencia(caballo, producto.experiencia, guardar: false);
        itemsJugador.RemoveAt(indice);
        if (!GuardarPartida())
        {
            JsonUtility.FromJsonOverwrite(anterior, caballo);
            Random.state = azarAnterior;
            itemsJugador.Insert(indice, item);
            mensaje = "No se pudo guardar el uso. El artículo sigue en el inventario.";
            return false;
        }
        mensaje = producto.nombre + " utilizado. " + producto.EfectoVisible;
        return true;
    }
}
