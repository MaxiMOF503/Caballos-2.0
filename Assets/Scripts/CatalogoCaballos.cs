using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class IdentidadCaballo
{
    public string id;
    public string nombre;
    public string raza;
    public string visualProfileId;
    public string splashRecurso;
    public bool visualDefinitivo;
    public Rect presentationBounds;
    public string enfoque;
    public int ajusteVelocidad, ajusteStamina, ajusteSuerte, ajusteInteligencia;
    public int edadMinima, edadMaxima, potencialMinimo, potencialMaximo;
}

public static class CatalogoCaballos
{
    [Serializable]
    sealed class Archivo { public IdentidadCaballo[] caballos; }

    static IdentidadCaballo[] identidades;
    public static IReadOnlyList<IdentidadCaballo> Todos => identidades ??= Cargar();

    static IdentidadCaballo[] Cargar()
    {
        var archivo = Resources.Load<TextAsset>("Catalogos/Caballos");
        if (archivo == null) throw new InvalidOperationException("Falta el catálogo Catalogos/Caballos.json.");
        return JsonUtility.FromJson<Archivo>(archivo.text).caballos;
    }

    public static IdentidadCaballo Buscar(string id) => Todos.FirstOrDefault(c => c.id == id);
    public static IdentidadCaballo PorNombre(string nombre) => Todos.FirstOrDefault(c => c.nombre == nombre);

    public static void Asignar(Caballo caballo, IdentidadCaballo identidad)
    {
        caballo.catalogIdentityId = identidad.id;
        caballo.nombre = identidad.nombre;
        caballo.raza = identidad.raza;
        caballo.visualProfileId = identidad.visualProfileId;
        var catalog = HorseVisualCatalog.Default;
        caballo.sprite = catalog.catalogAnimationsEnabled ? RecursosCatalogo.Cargar(identidad.splashRecurso)
            : catalog.Resolve(identidad.visualProfileId).baseSprite;
    }
}
