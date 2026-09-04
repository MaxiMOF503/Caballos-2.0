using System;
using System.Collections.Generic;
using UnityEngine;

public enum TipoSlotItem
{
    Cabeza,
    Espalda,
    Patas
}

[Serializable]
public class ItemCaballo
{
    public string id = Guid.NewGuid().ToString("N");
    public string nombre;
    public string descripcion;
    public TipoSlotItem slotPermitido;
    public string caballoEquipadoId;
    public Sprite sprite;

    public void ValidarDatos()
    {
        if (!Guid.TryParseExact(id, "N", out _))
            id = Guid.NewGuid().ToString("N");

        if (string.IsNullOrWhiteSpace(nombre))
            nombre = "Item";

        descripcion ??= string.Empty;

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

    public static bool EquiparItem(
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

        if (!string.IsNullOrEmpty(itemJugador.caballoEquipadoId))
        {
            mensaje = "El item ya está equipado en otro caballo.";
            return false;
        }

        if (ObtenerItemEquipado(itemsJugador, caballoJugador.id, itemJugador.slotPermitido) != null)
        {
            mensaje = "Slot " + itemJugador.slotPermitido + " ocupado.";
            return false;
        }

        if (dinero < CostoEquiparItem)
        {
            mensaje = "Dinero insuficiente para equipar el item.";
            return false;
        }

        dinero -= CostoEquiparItem;
        itemJugador.caballoEquipadoId = caballoJugador.id;
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

        foreach (ItemCaballo item in itemsJugador)
        {
            if (item != null &&
                item.caballoEquipadoId == caballoId &&
                item.slotPermitido == slot)
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

        string claveSlot = item.caballoEquipadoId + ":" + (int)item.slotPermitido;
        if (slotsOcupados == null || !slotsOcupados.Add(claveSlot))
        {
            advertencia =
                "Dos items intentaban ocupar el slot " + item.slotPermitido +
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
    public int saveVersion = 1;
    public int dinero;
    public int dia;
    public bool campanaCompletada;
    public List<CaballoData> caballosJugador = new List<CaballoData>();
    public CaballoData rivalPersistente;
    public List<ItemCaballoData> itemsJugador = new List<ItemCaballoData>();
}

[Serializable]
public class ItemCaballoData
{
    public string id;
    public string nombre;
    public string descripcion;
    public TipoSlotItem slotPermitido;
    public string caballoEquipadoId;
    public int spriteIndex = -1;

    public static ItemCaballoData DesdeItem(ItemCaballo item, int indiceSprite)
    {
        return new ItemCaballoData
        {
            id = item.id,
            nombre = item.nombre,
            descripcion = item.descripcion,
            slotPermitido = item.slotPermitido,
            caballoEquipadoId = item.caballoEquipadoId,
            spriteIndex = indiceSprite
        };
    }

    public ItemCaballo CrearItem()
    {
        ItemCaballo item = new ItemCaballo
        {
            id = id,
            nombre = nombre,
            descripcion = descripcion,
            slotPermitido = slotPermitido,
            caballoEquipadoId = caballoEquipadoId
        };

        item.ValidarDatos();
        return item;
    }
}

[Serializable]
public class CaballoData
{
    public string id;
    public string nombre;
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
            nombre = caballo.nombre,
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
            nombre = nombre,
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
