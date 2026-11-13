using System;
using System.Collections.Generic;

[Serializable]
public sealed class CarreraProgramada
{
    public string id = Guid.NewGuid().ToString("N");
    public int dia;
    public string idDefinicion;
    public string nombreCarrera;
    public int distanciaMetros;
    public TipoCarrera tipo;
    public int premio;
    public bool iniciada;
    public int versionReglas;
    public int inscripcion;
    public bool inscripcionCobrada;
    public bool iniciacion;
    public int InscripcionPendiente => versionReglas > 0 && !inscripcionCobrada ? Math.Max(0, inscripcion) : 0;
    public List<ParticipanteCarrera> participantes = new List<ParticipanteCarrera>();
    public ApuestaCarrera apuesta;
    public List<LlegadaCarrera> clasificacionFinal;

    public bool SeleccionBloqueada => iniciada || apuesta != null;
    public int PremioGanador => premio > 0 ? premio : 500;
    public string NombreVisible => string.IsNullOrEmpty(nombreCarrera) ? "Carrera pendiente" : nombreCarrera;

    public ParticipanteCarrera Buscar(string idCaballo)
    {
        return participantes.Find(p => p.caballo.id == idCaballo);
    }

    public void FijarCuotas()
    {
        var orden = new List<ParticipanteCarrera>(participantes);
        orden.Sort((a, b) =>
        {
            int comparacion = a.Rendimiento.CompareTo(b.Rendimiento);
            return comparacion != 0 ? comparacion : StringComparer.Ordinal.Compare(a.caballo.id, b.caballo.id);
        });
        for (int i = 0; i < orden.Count; i++)
            orden[i].cuotaDecimas = i == 0 ? 30 : 28 - 13 * (i - 1) / Math.Max(1, orden.Count - 2);
    }
}

[Serializable]
public sealed class ParticipanteCarrera
{
    public CaballoData caballo;
    public int cuotaDecimas;
    public int semilla;
    public float ritmoIniciacion;
    public int Rendimiento => 5 * caballo.velocidad + 3 * caballo.stamina + caballo.suerte + caballo.inteligencia;
}

public enum EstadoApuesta { Pendiente, Ganada, Perdida, Anulada }

[Serializable]
public sealed class ApuestaCarrera
{
    public string idCarrera;
    public string idCaballo;
    public string nombreCaballo;
    public int cantidad;
    public int cuotaDecimas;
    public EstadoApuesta estado;
    public long Pago => (long)cantidad * cuotaDecimas / 10;
    public string Resultado => estado == EstadoApuesta.Ganada
        ? $"¡Apuesta ganada! Cobraste {Moneda.Texto(Pago)}."
        : estado == EstadoApuesta.Perdida ? "Apuesta perdida."
        : estado == EstadoApuesta.Anulada ? $"Carrera sin llegadas válidas. Apuesta devuelta: {Moneda.Texto(cantidad)}."
        : $"Apuesta confirmada por {nombreCaballo}: {Moneda.Texto(cantidad)}.";

    public long Anular()
    {
        if (estado != EstadoApuesta.Pendiente) return 0;
        estado = EstadoApuesta.Anulada;
        return cantidad;
    }

    public long Liquidar(string idGanador)
    {
        if (estado != EstadoApuesta.Pendiente) return 0;
        estado = idCaballo == idGanador ? EstadoApuesta.Ganada : EstadoApuesta.Perdida;
        return estado == EstadoApuesta.Ganada ? Pago : 0;
    }
}
