using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum TipoCarrera { Corta, Media, Intermedia, Larga }

[Serializable]
public sealed class DefinicionCarrera
{
    public string id;
    public string nombre;
    public TipoCarrera tipo;
    [Min(1)] public int distanciaMetros;
    [Min(1)] public int premio;
    [Range(1, 5)] public int dificultad = 1;
    [Range(0, 15)] public int indiceBanner;
    public Sprite bannerAlternativo;

    public string TipoVisible => tipo == TipoCarrera.Intermedia ? "Media / intermedia" : tipo.ToString();
}

[CreateAssetMenu(menuName = "Caballos/Catálogo de carreras")]
public sealed class CatalogoCarreras : ScriptableObject
{
    public Texture2D atlasBanners;
    [Min(1)] public int minimoOfertas = 2;
    [Min(1)] public int maximoOfertas = 4;
    public string carreraEspecialSemanal;
    public List<DefinicionCarrera> carreras = new();

    public DefinicionCarrera Buscar(string id) => carreras.Find(c => c != null && c.id == id);

    public IReadOnlyList<DefinicionCarrera> Oferta(int dia, int semilla)
    {
        var especial = string.IsNullOrEmpty(carreraEspecialSemanal) ? null : Buscar(carreraEspecialSemanal);
        if (especial != null && GameManager.ObtenerDiaDeSemana(dia) == 7)
            return new[] { especial };

        var categorias = carreras.Where(c => c != null && c != especial && !string.IsNullOrWhiteSpace(c.id))
            .GroupBy(c => c.tipo).Select(grupo => grupo.ToArray()).ToList();
        if (categorias.Count == 0)
            return Array.Empty<DefinicionCarrera>();

        var azar = new System.Random(Semilla(semilla, dia, "oferta"));
        int maximo = Mathf.Clamp(maximoOfertas, 1, categorias.Sum(grupo => grupo.Length));
        int minimo = Mathf.Clamp(minimoOfertas, 1, maximo);
        int cantidad = azar.Next(minimo, maximo + 1);
        var oferta = new List<DefinicionCarrera>(cantidad);
        var restantes = new List<DefinicionCarrera>();
        while (oferta.Count < cantidad && categorias.Count > 0)
        {
            int indice = azar.Next(categorias.Count);
            var opciones = categorias[indice];
            var elegida = opciones[azar.Next(opciones.Length)];
            oferta.Add(elegida);
            restantes.AddRange(opciones.Where(c => c != elegida));
            categorias.RemoveAt(indice);
        }
        while (oferta.Count < cantidad)
        {
            int indice = azar.Next(restantes.Count);
            oferta.Add(restantes[indice]);
            restantes.RemoveAt(indice);
        }

        oferta.Sort((a, b) => a.distanciaMetros.CompareTo(b.distanciaMetros));
        return oferta;
    }

    internal static int Semilla(int partida, int dia, string id)
    {
        unchecked
        {
            int hash = partida * 397 ^ dia;
            foreach (char letra in id)
                hash = hash * 31 + letra;
            return hash;
        }
    }
}
