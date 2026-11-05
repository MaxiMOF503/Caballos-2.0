using System;
using System.Collections.Generic;
using UnityEngine;

public enum TipoProductoTienda
{
    Equipable,
    Consumible
}

public sealed class DefinicionProductoTienda
{
    public string idDefinicion;
    public string nombre;
    public string descripcion;
    public int precio;
    public TipoProductoTienda tipoProducto;
    public bool tieneSlotEquipable;
    public TipoSlotItem slotPermitido;
    public Sprite sprite;
    public TipoEstadisticaCaballo estadistica;
    public int bonificacion;

    public DefinicionProductoTienda(string id, string nombre, string descripcion,
        int precio, TipoProductoTienda tipo, bool tieneSlot, TipoSlotItem slot, Sprite sprite)
    {
        idDefinicion = id;
        this.nombre = nombre;
        this.descripcion = descripcion;
        this.precio = precio;
        tipoProducto = tipo;
        tieneSlotEquipable = tieneSlot;
        slotPermitido = slot;
        this.sprite = sprite;
    }
}

public static class SistemaTienda
{
    public const int OfertasIniciales = 3;
    public const int DiasPorRotacion = 4;
    public const int CostoRerollManual = 250;

    // Índices de los seis sprites ya asignados a GameManager en la escena.
    public static List<DefinicionProductoTienda> CrearCatalogo(Sprite[] sprites)
    {
        return new List<DefinicionProductoTienda>
        {
            new DefinicionProductoTienda("herradura_plata", "Herradura de Plata", "+1 resistencia.", 200,
                TipoProductoTienda.Equipable, true, TipoSlotItem.Patas, SpriteEn(sprites, 0)) { estadistica = TipoEstadisticaCaballo.Stamina, bonificacion = 1 },
            new DefinicionProductoTienda("manzana_dorada", "Manzana Dorada", "Aumenta considerablemente la energía.", 350,
                TipoProductoTienda.Consumible, false, default, SpriteEn(sprites, 2)),
            new DefinicionProductoTienda("herradura_oro", "Herradura de Oro", "+2 velocidad.", 500,
                TipoProductoTienda.Equipable, true, TipoSlotItem.Patas, SpriteEn(sprites, 1)) { estadistica = TipoEstadisticaCaballo.Velocidad, bonificacion = 2 },
            new DefinicionProductoTienda("zapatillas", "Zapatillas", "+1 velocidad.", 150,
                TipoProductoTienda.Equipable, true, TipoSlotItem.Patas, SpriteEn(sprites, 5)) { estadistica = TipoEstadisticaCaballo.Velocidad, bonificacion = 1 },
            new DefinicionProductoTienda("sombrero_copa", "Sombrero de Copa", "+1 suerte.", 300,
                TipoProductoTienda.Equipable, true, TipoSlotItem.Cabeza, SpriteEn(sprites, 3)) { estadistica = TipoEstadisticaCaballo.Suerte, bonificacion = 1 },
            new DefinicionProductoTienda("sombrero_noble", "Sombrero Noble", "+2 suerte.", 450,
                TipoProductoTienda.Equipable, true, TipoSlotItem.Cabeza, SpriteEn(sprites, 4)) { estadistica = TipoEstadisticaCaballo.Suerte, bonificacion = 2 }
        };
    }

    private static Sprite SpriteEn(Sprite[] sprites, int indice)
    {
        return sprites != null && indice < sprites.Length ? sprites[indice] : null;
    }

    public static DefinicionProductoTienda Buscar(IList<DefinicionProductoTienda> catalogo, string id)
    {
        if (catalogo == null || string.IsNullOrEmpty(id))
            return null;

        foreach (DefinicionProductoTienda producto in catalogo)
            if (producto != null && producto.idDefinicion == id)
                return producto;
        return null;
    }

    public static List<string> GenerarStock(IList<DefinicionProductoTienda> catalogo, int cantidad)
    {
        List<string> candidatos = new List<string>();
        HashSet<string> ids = new HashSet<string>();
        if (catalogo != null)
            foreach (DefinicionProductoTienda producto in catalogo)
                if (producto != null && !string.IsNullOrEmpty(producto.idDefinicion) && ids.Add(producto.idDefinicion))
                    candidatos.Add(producto.idDefinicion);

        int total = Mathf.Min(Mathf.Max(0, cantidad), candidatos.Count);
        for (int i = 0; i < total; i++)
        {
            int elegido = UnityEngine.Random.Range(i, candidatos.Count);
            string temporal = candidatos[i];
            candidatos[i] = candidatos[elegido];
            candidatos[elegido] = temporal;
        }
        return candidatos.GetRange(0, total);
    }

    public static bool StockValido(EstadoTiendaData estado, IList<DefinicionProductoTienda> catalogo, int cantidad)
    {
        if (estado == null || estado.idsProductosStock == null || catalogo == null ||
            estado.idsProductosStock.Count != Mathf.Min(Mathf.Max(0, cantidad), catalogo.Count) ||
            estado.diaUltimaRotacion < 1 || estado.diaProximaRotacion <= estado.diaUltimaRotacion)
            return false;

        HashSet<string> ids = new HashSet<string>();
        foreach (string id in estado.idsProductosStock)
            if (!ids.Add(id) || Buscar(catalogo, id) == null)
                return false;
        return true;
    }

    public static EstadoTiendaData CrearEstado(IList<DefinicionProductoTienda> catalogo,
        int cantidad, int dia, int intervalo)
    {
        int periodo = Mathf.Max(1, intervalo);
        int ultimo = 1 + ((Mathf.Max(1, dia) - 1) / periodo) * periodo;
        return new EstadoTiendaData
        {
            idsProductosStock = GenerarStock(catalogo, cantidad),
            diaUltimaRotacion = ultimo,
            diaProximaRotacion = ultimo + periodo
        };
    }

    public static bool Sincronizar(EstadoTiendaData estado, IList<DefinicionProductoTienda> catalogo,
        int cantidad, int dia, int intervalo)
    {
        if (estado == null || dia < estado.diaProximaRotacion)
            return false;

        int periodo = Mathf.Max(1, intervalo);
        int saltos = ((dia - estado.diaProximaRotacion) / periodo) + 1;
        List<string> anterior = estado.idsProductosStock;
        estado.diaUltimaRotacion = estado.diaProximaRotacion + (saltos - 1) * periodo;
        estado.diaProximaRotacion = estado.diaUltimaRotacion + periodo;
        estado.idsProductosStock = GenerarStockDistinto(catalogo, cantidad, anterior);
        return true;
    }

    public static List<string> GenerarStockDistinto(IList<DefinicionProductoTienda> catalogo,
        int cantidad, IList<string> anterior)
    {
        List<string> nuevo = GenerarStock(catalogo, cantidad);
        if (nuevo.Count == 0 || anterior == null || catalogo == null || catalogo.Count <= nuevo.Count ||
            !MismoConjunto(nuevo, anterior))
            return nuevo;

        foreach (DefinicionProductoTienda producto in catalogo)
            if (producto != null && !nuevo.Contains(producto.idDefinicion))
            {
                nuevo[0] = producto.idDefinicion;
                break;
            }
        return nuevo;
    }

    private static bool MismoConjunto(IList<string> a, IList<string> b)
    {
        if (a.Count != b.Count)
            return false;
        for (int i = 0; i < a.Count; i++)
            if (!b.Contains(a[i]))
                return false;
        return true;
    }
}
