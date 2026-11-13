using System;
using System.Collections.Generic;
using UnityEngine;

public enum TipoProductoTienda
{
    Equipable,
    Consumible
}

[Serializable]
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
    public TipoEstadisticaCaballo estadisticaSecundaria;
    public int bonificacionSecundaria;
    public int nivelMinimo = 1;
    public int experiencia;
    public int indiceLegacy = -1;
    public string iconoRecurso;
    public bool visualDefinitivo;

    public int BonoPara(TipoEstadisticaCaballo tipo) =>
        (estadistica == tipo ? bonificacion : 0) + (estadisticaSecundaria == tipo ? bonificacionSecundaria : 0);

    public string EfectoVisible
    {
        get
        {
            var efectos = new List<string>();
            if (bonificacion != 0) efectos.Add(bonificacion.ToString("+0;-0") + " " + estadistica);
            if (bonificacionSecundaria != 0) efectos.Add(bonificacionSecundaria.ToString("+0;-0") + " " + estadisticaSecundaria);
            if (experiencia > 0) efectos.Add("+" + experiencia + " XP");
            return string.Join(" · ", efectos);
        }
    }

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
    [Serializable]
    sealed class Archivo { public DefinicionProductoTienda[] productos; }
    static DefinicionProductoTienda[] definiciones;
    public const int OfertasIniciales = 3;
    public const int DiasPorRotacion = 4;
    public const int CostoRerollManual = 250;

    // Índices de los seis sprites ya asignados a GameManager en la escena.
    public static List<DefinicionProductoTienda> CrearCatalogo(Sprite[] sprites)
    {
        if (definiciones == null)
        {
            var archivo = Resources.Load<TextAsset>("Catalogos/Items");
            if (archivo == null) throw new InvalidOperationException("Falta Catalogos/Items.json.");
            definiciones = JsonUtility.FromJson<Archivo>(archivo.text).productos;
        }
        var catalogo = new List<DefinicionProductoTienda>(definiciones.Length);
        foreach (var datos in definiciones)
        {
            var icono = datos.indiceLegacy >= 0 ? SpriteEn(sprites, datos.indiceLegacy) :
                RecursosCatalogo.Cargar(datos.iconoRecurso) ?? RecursosCatalogo.Cargar("Catalogos/IconoPendiente");
            catalogo.Add(new DefinicionProductoTienda(datos.idDefinicion, datos.nombre, datos.descripcion,
                datos.precio, datos.tipoProducto, datos.tieneSlotEquipable, datos.slotPermitido, icono)
            {
                estadistica = datos.estadistica, bonificacion = datos.bonificacion,
                estadisticaSecundaria = datos.estadisticaSecundaria, bonificacionSecundaria = datos.bonificacionSecundaria,
                nivelMinimo = datos.nivelMinimo, experiencia = datos.experiencia,
                indiceLegacy = datos.indiceLegacy, iconoRecurso = datos.iconoRecurso, visualDefinitivo = datos.visualDefinitivo
            });
        }
        return catalogo;
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
