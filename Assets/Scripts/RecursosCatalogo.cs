using System.Collections.Generic;
using UnityEngine;

public static class RecursosCatalogo
{
    static readonly Dictionary<string, Sprite> cargados = new();
    static readonly HashSet<string> ausentes = new();

    public static bool EsCaballo(Sprite sprite)
    {
        if (sprite == null) return false;
        foreach (var entry in cargados)
            if (entry.Key.StartsWith("Catalogos/Caballos/") && entry.Value == sprite) return true;
        return false;
    }

    public static Sprite Cargar(string ruta)
    {
        if (string.IsNullOrEmpty(ruta)) return null;
        if (cargados.TryGetValue(ruta, out var sprite) && sprite != null) return sprite;
        sprite = Resources.Load<Sprite>(ruta);
        if (sprite != null) cargados[ruta] = sprite;
        else if (ausentes.Add(ruta)) Debug.LogWarning("Ilustración de catálogo pendiente: " + ruta);
        return sprite;
    }
}
