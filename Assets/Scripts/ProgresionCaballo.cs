using System.Collections.Generic;
using UnityEngine;

public enum TipoEstadisticaCaballo
{
    Velocidad,
    Stamina,
    Suerte,
    Inteligencia
}

public static class ProgresionCaballo
{
    public static int ExperienciaNecesariaParaSiguienteNivel(int nivel)
    {
        int nivelValido = Mathf.Max(1, nivel);
        return 100 + ((nivelValido - 1) * 50);
    }

    public static int AgregarExperiencia(Caballo caballo, int cantidad)
    {
        if (caballo == null)
        {
            Debug.LogWarning("No se puede agregar experiencia a un caballo nulo.");
            return 0;
        }

        if (cantidad <= 0)
            return 0;

        caballo.ValidarDatos();

        long experienciaAcumulada = (long)caballo.experiencia + cantidad;
        int nivelesSubidos = 0;

        while (experienciaAcumulada >= ExperienciaNecesariaParaSiguienteNivel(caballo.nivel))
        {
            experienciaAcumulada -= ExperienciaNecesariaParaSiguienteNivel(caballo.nivel);
            caballo.nivel++;
            nivelesSubidos++;
            caballo.puntosMejoraPendientes += CalcularPuntosPorNivel(caballo.potencial);
        }

        caballo.experiencia = experienciaAcumulada > int.MaxValue
            ? int.MaxValue
            : (int)experienciaAcumulada;

        caballo.ValidarDatos();

        if (nivelesSubidos > 0)
        {
            Debug.Log(caballo.nombre + " subió " + nivelesSubidos +
                      " nivel(es) y alcanzó el nivel " + caballo.nivel + ".");
        }

        if (GameManager.Instancia != null)
            GameManager.Instancia.GuardarPartida();

        return nivelesSubidos;
    }

    public static bool MejorarEstadistica(Caballo caballo, TipoEstadisticaCaballo estadistica)
    {
        caballo?.ValidarDatos();
        return AplicarMejoras(caballo, new Dictionary<TipoEstadisticaCaballo, int> { [estadistica] = 1 });
    }

    public static int ValorBase(Caballo caballo, TipoEstadisticaCaballo estadistica) => estadistica switch
    {
        TipoEstadisticaCaballo.Velocidad => caballo.velocidad,
        TipoEstadisticaCaballo.Stamina => caballo.stamina,
        TipoEstadisticaCaballo.Suerte => caballo.suerte,
        TipoEstadisticaCaballo.Inteligencia => caballo.inteligencia,
        _ => Caballo.EstadisticaMaxima
    };

    public static bool AplicarMejoras(Caballo caballo, IReadOnlyDictionary<TipoEstadisticaCaballo, int> mejoras)
    {
        if (caballo == null || mejoras == null || mejoras.Count == 0)
            return false;

        int disponibles = caballo.puntosMejoraPendientes;
        foreach (var mejora in mejoras)
        {
            if (mejora.Value <= 0 || mejora.Value > disponibles ||
                mejora.Value > Caballo.EstadisticaMaxima - ValorBase(caballo, mejora.Key))
                return false;

            disponibles -= mejora.Value;
        }

        int puntosAnteriores = caballo.puntosMejoraPendientes;
        AjustarValores(caballo, mejoras, 1);
        caballo.puntosMejoraPendientes = disponibles;
        if (GameManager.Instancia == null || GameManager.Instancia.GuardarPartida())
            return true;

        AjustarValores(caballo, mejoras, -1);
        caballo.puntosMejoraPendientes = puntosAnteriores;
        return false;
    }

    private static void AjustarValores(Caballo caballo, IReadOnlyDictionary<TipoEstadisticaCaballo, int> mejoras, int signo)
    {
        foreach (var mejora in mejoras)
        {
            int cantidad = signo * mejora.Value;
            switch (mejora.Key)
            {
                case TipoEstadisticaCaballo.Velocidad: caballo.velocidad += cantidad; break;
                case TipoEstadisticaCaballo.Stamina: caballo.stamina += cantidad; break;
                case TipoEstadisticaCaballo.Suerte: caballo.suerte += cantidad; break;
                case TipoEstadisticaCaballo.Inteligencia: caballo.inteligencia += cantidad; break;
            }
        }
    }

    public static int CalcularPuntosPorNivel(int potencial)
    {
        int potencialValido = Caballo.NormalizarEstadistica(potencial);

        if (potencialValido == Caballo.EstadisticaMaxima)
            return 2;

        float probabilidadSegundaMejora = 0f;

        if (potencialValido >= 13)
            probabilidadSegundaMejora = 0.75f;
        else if (potencialValido >= 9)
            probabilidadSegundaMejora = 0.50f;
        else if (potencialValido >= 5)
            probabilidadSegundaMejora = 0.25f;

        if (probabilidadSegundaMejora <= 0f)
            return 1;

        return 1 + (Random.value < probabilidadSegundaMejora ? 1 : 0);
    }
}
