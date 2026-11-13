using System;
using System.Collections.Generic;
using UnityEngine;

public enum TipoSlotItem
{
    Cabeza,
    Espalda,
    Patas, // Categoría del catálogo y valor serializado de partidas anteriores.
    PatasDelanteras,
    PatasTraseras
}

[Serializable]
public class ItemCaballo
{
    public string id = Guid.NewGuid().ToString("N");
    public string idDefinicion = string.Empty;
    public string nombre;
    public string descripcion;
    public TipoSlotItem slotPermitido;
    public string caballoEquipadoId;
    public TipoSlotItem slotEquipado;
    public Sprite sprite;

    public TipoSlotItem RanuraActual => SistemaEquipamiento.EsCompatible(slotPermitido, slotEquipado)
        ? slotEquipado
        : SistemaEquipamiento.RanuraInicial(slotPermitido);

    public void ValidarDatos()
    {
        if (!Guid.TryParseExact(id, "N", out _))
            id = Guid.NewGuid().ToString("N");

        if (string.IsNullOrWhiteSpace(nombre))
            nombre = "Item";

        descripcion ??= string.Empty;
        idDefinicion ??= string.Empty;

        if (!Enum.IsDefined(typeof(TipoSlotItem), slotPermitido))
            slotPermitido = TipoSlotItem.Cabeza;

        if (string.IsNullOrWhiteSpace(caballoEquipadoId))
        {
            caballoEquipadoId = null;
        }
        else if (!Guid.TryParseExact(caballoEquipadoId, "N", out _))
        {
            caballoEquipadoId = null;
        }
    }
}

public static class SistemaEquipamiento
{
    public const int CostoEquiparItem = 50;
    public const int CostoDesequiparItem = 50;
    public static IReadOnlyList<TipoSlotItem> Ranuras { get; } = Array.AsReadOnly(new[]
    {
        TipoSlotItem.Cabeza, TipoSlotItem.Espalda,
        TipoSlotItem.PatasDelanteras, TipoSlotItem.PatasTraseras
    });

    public static TipoSlotItem RanuraInicial(TipoSlotItem categoria) =>
        categoria == TipoSlotItem.Patas ? TipoSlotItem.PatasDelanteras : categoria;

    public static bool EsCompatible(TipoSlotItem categoria, TipoSlotItem ranura) =>
        categoria == TipoSlotItem.Patas
            ? ranura == TipoSlotItem.PatasDelanteras || ranura == TipoSlotItem.PatasTraseras
            : categoria == ranura && Enum.IsDefined(typeof(TipoSlotItem), ranura);

    public static string NombreRanura(TipoSlotItem ranura) => ranura switch
    {
        TipoSlotItem.PatasDelanteras => "Patas delanteras",
        TipoSlotItem.PatasTraseras => "Patas traseras",
        TipoSlotItem.Patas => "Patas delanteras o traseras",
        _ => ranura.ToString()
    };

    public static bool EquiparItem(
        Caballo caballo,
        ItemCaballo item,
        IList<Caballo> caballosJugador,
        IList<ItemCaballo> itemsJugador,
        ref int dinero,
        out string mensaje)
    {
        return EquiparItem(caballo, item, RanuraInicial(item?.slotPermitido ?? default),
            false, caballosJugador, itemsJugador, ref dinero, out _, out mensaje);
    }

    public static bool EquiparItem(
        Caballo caballo,
        ItemCaballo item,
        TipoSlotItem ranura,
        bool permitirReemplazo,
        IList<Caballo> caballosJugador,
        IList<ItemCaballo> itemsJugador,
        ref int dinero,
        out ItemCaballo reemplazado,
        out string mensaje)
    {
        reemplazado = null;
        Caballo caballoJugador = BuscarCaballo(caballosJugador, caballo);
        if (caballoJugador == null)
        {
            mensaje = "El caballo no pertenece al jugador.";
            return false;
        }

        ItemCaballo itemJugador = BuscarItem(itemsJugador, item);
        if (itemJugador == null)
        {
            mensaje = "El item no pertenece al inventario del jugador.";
            return false;
        }

        caballoJugador.ValidarDatos();
        itemJugador.ValidarDatos();

        if (!string.IsNullOrEmpty(itemJugador.caballoEquipadoId))
        {
            mensaje = "El item ya está equipado en otro caballo.";
            return false;
        }

        if (!EsCompatible(itemJugador.slotPermitido, ranura))
        {
            mensaje = itemJugador.nombre + " no es compatible con " + NombreRanura(ranura) + ".";
            return false;
        }

        ItemCaballo ocupado = ObtenerItemEquipado(itemsJugador, caballoJugador.id, ranura);
        if (ocupado != null && !permitirReemplazo)
        {
            mensaje = "Ranura " + NombreRanura(ranura) + " ocupada.";
            return false;
        }

        int costo = CostoEquiparItem + (ocupado != null ? CostoDesequiparItem : 0);
        if (dinero < costo)
        {
            mensaje = "Dinero insuficiente para equipar el item.";
            return false;
        }

        dinero -= costo;
        if (ocupado != null)
            ocupado.caballoEquipadoId = null;

        reemplazado = ocupado;
        itemJugador.caballoEquipadoId = caballoJugador.id;
        itemJugador.slotEquipado = ranura;
        mensaje = itemJugador.nombre + " equipado en " + caballoJugador.nombre + ".";
        return true;
    }

    public static bool DesequiparItem(
        Caballo caballo,
        ItemCaballo item,
        IList<Caballo> caballosJugador,
        IList<ItemCaballo> itemsJugador,
        ref int dinero,
        out string mensaje)
    {
        Caballo caballoJugador = BuscarCaballo(caballosJugador, caballo);
        if (caballoJugador == null)
        {
            mensaje = "El caballo no pertenece al jugador.";
            return false;
        }

        ItemCaballo itemJugador = BuscarItem(itemsJugador, item);
        if (itemJugador == null)
        {
            mensaje = "El item no pertenece al inventario del jugador.";
            return false;
        }

        caballoJugador.ValidarDatos();
        itemJugador.ValidarDatos();

        if (string.IsNullOrEmpty(itemJugador.caballoEquipadoId))
        {
            mensaje = "El item no está equipado.";
            return false;
        }

        if (itemJugador.caballoEquipadoId != caballoJugador.id)
        {
            mensaje = "El item está equipado en otro caballo.";
            return false;
        }

        if (dinero < CostoDesequiparItem)
        {
            mensaje = "Dinero insuficiente para desequipar el item.";
            return false;
        }

        dinero -= CostoDesequiparItem;
        itemJugador.caballoEquipadoId = null;
        mensaje = itemJugador.nombre + " desequipado de " + caballoJugador.nombre + ".";
        return true;
    }

    public static ItemCaballo ObtenerItemEquipado(
        IList<ItemCaballo> itemsJugador,
        string caballoId,
        TipoSlotItem slot)
    {
        if (itemsJugador == null || string.IsNullOrEmpty(caballoId))
            return null;

        slot = RanuraInicial(slot);
        foreach (ItemCaballo item in itemsJugador)
        {
            if (item != null &&
                item.caballoEquipadoId == caballoId &&
                item.RanuraActual == slot)
            {
                return item;
            }
        }

        return null;
    }

    public static bool ValidarAsignacionPersistida(
        ItemCaballo item,
        ISet<string> idsCaballosJugador,
        ISet<string> slotsOcupados,
        out string advertencia)
    {
        advertencia = null;

        if (item == null || string.IsNullOrEmpty(item.caballoEquipadoId))
            return true;

        if (idsCaballosJugador == null || !idsCaballosJugador.Contains(item.caballoEquipadoId))
        {
            advertencia =
                "El caballo asociado al item " + item.nombre +
                " no existe. El item quedó disponible.";
            item.caballoEquipadoId = null;
            return false;
        }

        item.slotEquipado = item.RanuraActual;
        string claveSlot = item.caballoEquipadoId + ":" + (int)item.slotEquipado;
        if (slotsOcupados == null || !slotsOcupados.Add(claveSlot))
        {
            advertencia =
                "Dos items intentaban ocupar la ranura " + NombreRanura(item.slotEquipado) +
                " del mismo caballo. " + item.nombre + " quedó disponible.";
            item.caballoEquipadoId = null;
            return false;
        }

        return true;
    }

    private static Caballo BuscarCaballo(IList<Caballo> caballosJugador, Caballo buscado)
    {
        if (caballosJugador == null || buscado == null)
            return null;

        foreach (Caballo caballo in caballosJugador)
        {
            if (ReferenceEquals(caballo, buscado) ||
                (caballo != null && !string.IsNullOrEmpty(buscado.id) && caballo.id == buscado.id))
            {
                return caballo;
            }
        }

        return null;
    }

    private static ItemCaballo BuscarItem(IList<ItemCaballo> itemsJugador, ItemCaballo buscado)
    {
        if (itemsJugador == null || buscado == null)
            return null;

        foreach (ItemCaballo item in itemsJugador)
        {
            if (ReferenceEquals(item, buscado) ||
                (item != null && !string.IsNullOrEmpty(buscado.id) && item.id == buscado.id))
            {
                return item;
            }
        }

        return null;
    }
}

[Serializable]
public class GameData
{
    public DerrotaCampana derrota;
    public int saveVersion = 1;
    public int dinero;
    public int dia;
    public bool campanaCompletada;
    public List<CaballoData> caballosJugador = new List<CaballoData>();
    public CaballoData rivalPersistente;
    public List<ItemCaballoData> itemsJugador = new List<ItemCaballoData>();
    public EstadoTiendaData estadoTienda;
    public CarreraProgramada carreraProgramada;
    public int semillaOfertaCarreras;
    public ApuestaCarrera ultimaApuesta;
    public ResultadoCarrera ultimoResultadoCarrera;
    public ProgresoInicioCarreras inicioCarreras;
    public EstadoSubastasData subastas;
    public string caballoActivoId;
}

[Serializable]
public class EstadoTiendaData
{
    public List<string> idsProductosStock = new List<string>();
    public int diaUltimaRotacion;
    public int diaProximaRotacion;
}

[Serializable]
public class ItemCaballoData
{
    public string id;
    public string idDefinicion;
    public string nombre;
    public string descripcion;
    public TipoSlotItem slotPermitido;
    public string caballoEquipadoId;
    public TipoSlotItem slotEquipado;
    public int spriteIndex = -1;

    public static ItemCaballoData DesdeItem(ItemCaballo item, int indiceSprite)
    {
        return new ItemCaballoData
        {
            id = item.id,
            idDefinicion = item.idDefinicion,
            nombre = item.nombre,
            descripcion = item.descripcion,
            slotPermitido = item.slotPermitido,
            caballoEquipadoId = item.caballoEquipadoId,
            slotEquipado = item.RanuraActual,
            spriteIndex = indiceSprite
        };
    }

    public ItemCaballo CrearItem()
    {
        ItemCaballo item = new ItemCaballo
        {
            id = id,
            idDefinicion = idDefinicion ?? string.Empty,
            nombre = nombre,
            descripcion = descripcion,
            slotPermitido = slotPermitido,
            caballoEquipadoId = caballoEquipadoId,
            slotEquipado = slotEquipado
        };

        item.ValidarDatos();
        return item;
    }
}

[Serializable]
public class CaballoData
{
    public string id;
    public string visualProfileId;
    public string catalogIdentityId;
    public int preparacionVelocidad, preparacionStamina, preparacionSuerte, preparacionInteligencia;
    public int ultimoDiaConsumible;
    public string nombre;
    public string raza;
    public int edad;
    public int nivel;
    public int experiencia;
    public int puntosMejoraPendientes;
    public int velocidad;
    public int stamina;
    public int suerte;
    public int inteligencia;
    public int potencial;
    public int relacion;
    public int estres;
    public bool esDelJugador;
    public int spriteIndex = -1;

    public static CaballoData DesdeCaballo(Caballo caballo, int indiceSprite)
    {
        return new CaballoData
        {
            id = caballo.id,
            visualProfileId = caballo.visualProfileId,
            catalogIdentityId = caballo.catalogIdentityId,
            preparacionVelocidad = caballo.preparacionVelocidad,
            preparacionStamina = caballo.preparacionStamina,
            preparacionSuerte = caballo.preparacionSuerte,
            preparacionInteligencia = caballo.preparacionInteligencia,
            ultimoDiaConsumible = caballo.ultimoDiaConsumible,
            nombre = caballo.nombre,
            raza = caballo.raza,
            edad = caballo.edad,
            nivel = caballo.nivel,
            experiencia = caballo.experiencia,
            puntosMejoraPendientes = caballo.puntosMejoraPendientes,
            velocidad = caballo.velocidad,
            stamina = caballo.stamina,
            suerte = caballo.suerte,
            inteligencia = caballo.inteligencia,
            potencial = caballo.potencial,
            relacion = caballo.relacion,
            estres = caballo.estres,
            esDelJugador = caballo.esDelJugador,
            spriteIndex = indiceSprite
        };
    }

    public Caballo CrearCaballo()
    {
        Caballo caballo = new Caballo
        {
            id = id,
            visualProfileId = visualProfileId,
            catalogIdentityId = catalogIdentityId,
            preparacionVelocidad = preparacionVelocidad,
            preparacionStamina = preparacionStamina,
            preparacionSuerte = preparacionSuerte,
            preparacionInteligencia = preparacionInteligencia,
            ultimoDiaConsumible = ultimoDiaConsumible,
            nombre = nombre,
            raza = raza,
            edad = edad,
            nivel = nivel,
            experiencia = experiencia,
            puntosMejoraPendientes = puntosMejoraPendientes,
            velocidad = velocidad,
            stamina = stamina,
            suerte = suerte,
            inteligencia = inteligencia,
            potencial = potencial,
            relacion = relacion,
            estres = estres,
            esDelJugador = esDelJugador
        };

        caballo.ValidarDatos();
        return caballo;
    }
}
