using System;
using UnityEngine;

[Serializable]
public sealed class TarifaCarrera
{
    public int distanciaMetros;
    [Min(0)] public int inicial;
    [Min(0)] public int desdeDia8;
}

[Serializable]
public sealed class ProgresoInicioCarreras
{
    public int version;
    public int completadas;
    public bool ganoPrimera;
    public bool IniciacionPendiente => version == 1 && completadas == 1 && !ganoPrimera;
    public static ProgresoInicioCarreras Nuevo() => new ProgresoInicioCarreras { version = 1 };
    // Missing fields in old saves must never re-enable the welcome script.
    public static ProgresoInicioCarreras Anterior() => new ProgresoInicioCarreras { version = 1, completadas = 2 };
    public void Completar(bool gano)
    {
        if (completadas == 0) ganoPrimera = gano;
        completadas = Math.Min(2, completadas + 1);
    }
}

[Serializable]
public sealed class BandaRivalesCarrera
{
    [Range(0, 5)] public int dispersion = 1;
    [Range(-3, 3)] public int ajusteInicial = -2;
    [Range(-3, 3)] public int ajusteAvanzado = 1;
    [Min(2)] public int diaEtapaAvanzada = 15;
    [Range(0, 2)] public int especialidadDistancia = 1;

    public void Aplicar(Caballo rival, CaballoData activo, int metros, int dia, System.Random azar)
    {
        int etapa = dia >= diaEtapaAvanzada ? ajusteAvanzado : ajusteInicial;
        float larga = Mathf.InverseLerp(1200, 2400, metros);
        int especialidad = Mathf.RoundToInt(Mathf.Lerp(-especialidadDistancia, especialidadDistancia, larga));
        int velocidad = activo.velocidad + etapa - especialidad;
        int stamina = activo.stamina + etapa + especialidad;
        int limite = Mathf.Clamp(dispersion, 0, 5);
        rival.velocidad = Caballo.NormalizarEstadistica(velocidad + azar.Next(-limite, limite + 1));
        rival.stamina = Caballo.NormalizarEstadistica(stamina + azar.Next(-limite, limite + 1));
        rival.inteligencia = Caballo.NormalizarEstadistica(activo.inteligencia + etapa + azar.Next(-limite, limite + 1));
        rival.suerte = Caballo.NormalizarEstadistica(activo.suerte + azar.Next(-limite, limite + 1));
    }
}
