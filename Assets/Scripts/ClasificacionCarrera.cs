using System;
using System.Collections.Generic;

[Serializable]
public sealed class LlegadaCarrera
{
    public string idCaballo;
    public int posicion;
    public double tiempo;
    public string incidencia;
    public float progreso;
    public bool Finalizo => string.IsNullOrEmpty(incidencia);

    internal LlegadaCarrera Copia() => (LlegadaCarrera)MemberwiseClone();
}

public readonly struct AvanceCarrera
{
    public readonly string id;
    public readonly float anterior, actual;
    public AvanceCarrera(string id, float anterior, float actual)
    { this.id = id; this.anterior = anterior; this.actual = actual; }
}

// One batch per simulation frame: crossing order must not depend on iteration order.
public sealed class ClasificacionCarrera
{
    sealed class Seguimiento
    {
        internal float progreso;
        internal double ultimoAvance;
        internal LlegadaCarrera resultado;
    }

    readonly Dictionary<string, Seguimiento> participantes = new Dictionary<string, Seguimiento>(StringComparer.Ordinal);
    readonly List<LlegadaCarrera> llegadas = new List<LlegadaCarrera>();
    readonly Action<string> registrar;
    readonly float distancia;
    public double Tiempo { get; private set; }
    public double LimiteTiempo { get; }
    public int Resueltos { get; private set; }
    public bool Completa => Resueltos == participantes.Count;

    public ClasificacionCarrera(IEnumerable<string> ids, float distancia, Action<string> registrarProblema = null)
    {
        if (!Finito(distancia) || distancia <= 0) throw new ArgumentOutOfRangeException(nameof(distancia));
        this.distancia = distancia;
        registrar = registrarProblema;
        foreach (string id in ids)
        {
            if (string.IsNullOrEmpty(id) || participantes.ContainsKey(id))
                throw new ArgumentException("La parrilla necesita identificadores únicos y no vacíos.");
            participantes.Add(id, new Seguimiento());
        }
        if (participantes.Count == 0) throw new ArgumentException("La parrilla está vacía.");
        // Minimum normal pace is 8 * .30 units/s. Eight times that duration,
        // plus a full minute, comfortably exceeds legitimate long races.
        LimiteTiempo = Math.Max(300, distancia / 2.4 * 8 + 60);
    }

    public bool Resuelto(string id) => participantes.TryGetValue(id, out var estado) && estado.resultado != null;

    public IReadOnlyList<LlegadaCarrera> Resultados
    {
        get
        {
            var copia = new List<LlegadaCarrera>(llegadas.Count);
            foreach (var llegada in llegadas) copia.Add(llegada.Copia());
            return copia.AsReadOnly();
        }
    }

    public void Avanzar(double dt, IReadOnlyList<AvanceCarrera> avances)
    {
        if (Completa || double.IsNaN(dt) || double.IsInfinity(dt) || dt <= 0) return;
        double inicio = Tiempo;
        Tiempo += dt;
        var cruces = new List<LlegadaCarrera>();
        var vistos = new HashSet<string>(StringComparer.Ordinal);
        foreach (var avance in avances)
        {
            if (!participantes.TryGetValue(avance.id, out var estado) || estado.resultado != null || !vistos.Add(avance.id)) continue;
            if (!Finito(avance.anterior) || !Finito(avance.actual) || avance.anterior < 0 ||
                avance.actual < avance.anterior || avance.anterior < estado.progreso)
            {
                Incidencia(avance.id, estado, "Progreso inválido de simulación");
                continue;
            }
            if (avance.actual > estado.progreso)
            {
                estado.progreso = Math.Min(distancia, avance.actual);
                estado.ultimoAvance = Tiempo;
            }
            if (avance.actual >= distancia)
            {
                double tramo = (double)avance.actual - avance.anterior;
                double fraccion = tramo > 0 ? (distancia - avance.anterior) / tramo : 1;
                cruces.Add(new LlegadaCarrera { idCaballo = avance.id, progreso = distancia,
                    tiempo = inicio + dt * Math.Max(0, Math.Min(1, fraccion)) });
            }
        }
        cruces.Sort((a, b) =>
        {
            int tiempo = a.tiempo.CompareTo(b.tiempo);
            // An exact simulation tie uses the stable ID, never the grid index.
            return tiempo != 0 ? tiempo : StringComparer.Ordinal.Compare(a.idCaballo, b.idCaballo);
        });
        foreach (var cruce in cruces)
        {
            cruce.posicion = llegadas.Count + 1;
            participantes[cruce.idCaballo].resultado = cruce;
            llegadas.Add(cruce);
            Resueltos++;
        }
        foreach (var par in participantes)
            if (par.Value.resultado == null && (Tiempo - par.Value.ultimoAvance >= 60 || Tiempo >= LimiteTiempo))
                Incidencia(par.Key, par.Value, Tiempo >= LimiteTiempo ? "Límite excepcional de simulación" : "Sin avanzar durante 60 segundos de simulación");
        if (!Completa) return;
        var noFinalizaron = new List<LlegadaCarrera>();
        foreach (var estado in participantes.Values)
            if (!estado.resultado.Finalizo) noFinalizaron.Add(estado.resultado);
        noFinalizaron.Sort((a, b) =>
        {
            int progreso = b.progreso.CompareTo(a.progreso);
            return progreso != 0 ? progreso : StringComparer.Ordinal.Compare(a.idCaballo, b.idCaballo);
        });
        foreach (var fallo in noFinalizaron)
        {
            fallo.posicion = llegadas.Count + 1;
            llegadas.Add(fallo);
        }
    }

    void Incidencia(string id, Seguimiento estado, string motivo)
    {
        estado.resultado = new LlegadaCarrera { idCaballo = id, tiempo = -1, incidencia = motivo, progreso = estado.progreso };
        Resueltos++;
        registrar?.Invoke($"Carrera: participante {id}: {motivo}. Se registra como No finalizó.");
    }

    static bool Finito(float valor) => !float.IsNaN(valor) && !float.IsInfinity(valor);

    public static bool Validar(CarreraProgramada carrera, IReadOnlyList<LlegadaCarrera> resultados)
    {
        if (carrera?.participantes == null || resultados == null || resultados.Count == 0 || resultados.Count != carrera.participantes.Count) return false;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var participante in carrera.participantes)
            if (string.IsNullOrEmpty(participante?.caballo?.id) || !ids.Add(participante.caballo.id)) return false;
        double anterior = 0;
        string idAnterior = null;
        bool fallo = false;
        for (int i = 0; i < resultados.Count; i++)
        {
            var llegada = resultados[i];
            if (llegada == null || !ids.Remove(llegada.idCaballo) || llegada.posicion != i + 1) return false;
            if (!llegada.Finalizo) { fallo = true; if (llegada.tiempo != -1) return false; continue; }
            if (fallo || double.IsNaN(llegada.tiempo) || double.IsInfinity(llegada.tiempo) || llegada.tiempo < anterior ||
                (llegada.tiempo == anterior && idAnterior != null && StringComparer.Ordinal.Compare(idAnterior, llegada.idCaballo) > 0)) return false;
            anterior = llegada.tiempo;
            idAnterior = llegada.idCaballo;
        }
        return ids.Count == 0;
    }
}

[Serializable]
public sealed class ResultadoCarrera
{
    public CarreraProgramada carrera;
    public string idCaballoJugador;
    public bool Valido => !string.IsNullOrEmpty(carrera?.id) && ClasificacionCarrera.Validar(carrera, carrera.clasificacionFinal);
}
