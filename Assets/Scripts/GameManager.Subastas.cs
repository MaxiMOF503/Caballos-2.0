using System;
using System.Linq;
using UnityEngine;

public partial class GameManager
{
    public EstadoSubastasData subastas = new();
    public string caballoActivoId;
    public event Action SubastasActualizadas;

    public Caballo CaballoActivo => caballosJugador.Find(c => c != null && c.id == caballoActivoId) ??
        caballosJugador.FirstOrDefault(c => c != null);
    public SubastaData SubastaActual => subastas?.actual;
    public int DineroReservado => SubastaActual?.reserva ?? 0;
    public bool PuedeCambiarCaballo => ObtenerCarreraProgramada() == null;

    public Caballo CrearVistaSubasta()
    {
        var caballo = SubastaActual.caballo.CrearCaballo();
        RestaurarVisual(caballo, SubastaActual.caballo.spriteIndex);
        return caballo;
    }

    void InicializarSubastas()
    {
        subastas ??= new EstadoSubastasData();
        subastas.historial ??= new();
        subastas.avisos ??= new();
        caballoActivoId = CaballoActivo?.id;
        if (string.IsNullOrEmpty(subastas.actual?.id))
            subastas.actual = GenerarSubasta(dia, subastas.historial.LastOrDefault());
        MigrarIdentidadVisual(subastas.actual?.caballo);
        foreach (var oferta in subastas.historial) MigrarIdentidadVisual(oferta?.caballo);
    }

    SubastaData GenerarSubasta(int diaOferta, SubastaData anterior)
    {
        int ciclo = (diaOferta - 1) / SistemaSubastas.DiasPorSubasta;
        string fundador = caballosJugador.FirstOrDefault(c => c != null)?.id ?? "subasta";
        int semilla = CatalogoCarreras.Semilla(semillaOfertaCarreras, ciclo, fundador);
        return SistemaSubastas.Crear(diaOferta, nombres, HorseVisualCatalog.Default, anterior, semilla);
    }

    public bool SeleccionarCaballoActivo(string id, out string mensaje)
    {
        mensaje = "";
        var caballo = caballosJugador.Find(c => c != null && c.id == id);
        if (caballo == null) { mensaje = "El caballo no pertenece al establo."; return false; }
        if (CaballoActivo?.id == id) return true;
        if (!PuedeCambiarCaballo)
        {
            mensaje = "Ya hay una carrera preparada. Podrás cambiar el caballo al terminarla.";
            return false;
        }
        string anterior = caballoActivoId;
        caballoActivoId = id;
        if (GuardarPartida()) return true;
        caballoActivoId = anterior;
        mensaje = "No se pudo guardar la selección.";
        return false;
    }

    public bool PujarSubasta(string id, int cantidad, out string mensaje)
    {
        mensaje = "";
        var actual = SubastaActual;
        if (campanaCompletada || actual == null || actual.id != id || actual.resultado != ResultadoSubasta.Abierta ||
            dia < actual.diaInicio || dia > actual.diaFin)
        { mensaje = "La subasta está cerrada."; return false; }
        if (cantidad <= actual.pujaActual || cantidad < SistemaSubastas.Minima(actual) ||
            (long)cantidad > (long)dinero + actual.reserva)
        { mensaje = "La oferta debe alcanzar la puja mínima y tu saldo disponible."; return false; }

        string anterior = JsonUtility.ToJson(subastas);
        int saldoAnterior = dinero;
        dinero -= cantidad - actual.reserva;
        actual.reserva = cantidad;
        SistemaSubastas.Registrar(actual, dia, SistemaSubastas.Jugador, cantidad);
        SistemaSubastas.OfrecerIA(actual, dia, true, subastas, ref dinero);
        if (!GuardarPartida())
        {
            subastas = JsonUtility.FromJson<EstadoSubastasData>(anterior);
            dinero = saldoAnterior;
            mensaje = "No se pudo guardar la puja. Intentá de nuevo.";
            return false;
        }
        mensaje = actual.LideraJugador ? "Tu oferta quedó reservada. Liderás la subasta." :
            "Otro postor superó tu oferta. Tu reserva fue liberada.";
        ActualizarHUD();
        SubastasActualizadas?.Invoke();
        return true;
    }

    public bool SeguirSubasta(bool seguir)
    {
        var actual = SubastaActual;
        if (actual == null) return false;
        bool anterior = actual.siguiendo;
        actual.siguiendo = seguir;
        if (!GuardarPartida()) { actual.siguiendo = anterior; return false; }
        SubastasActualizadas?.Invoke();
        return true;
    }

    public bool MarcarAvisoSubasta(string id)
    {
        var aviso = subastas.avisos.Find(a => a.id == id);
        if (aviso == null || aviso.leido) return true;
        aviso.leido = true;
        if (GuardarPartida()) return true;
        aviso.leido = false;
        return false;
    }

    void AvanzarSubasta(int nuevoDia)
    {
        InicializarSubastas();
        var actual = SubastaActual;
        if (nuevoDia > actual.diaFin)
        {
            CerrarSubasta();
            subastas.actual = GenerarSubasta(nuevoDia, actual);
            actual = SubastaActual;
            SubastasActualizadas?.Invoke();
        }
        if (nuevoDia <= actual.ultimoDiaProcesado) return;
        for (int diaPuja = actual.ultimoDiaProcesado + 1; diaPuja <= nuevoDia; diaPuja++)
            SistemaSubastas.OfrecerIA(actual, diaPuja, false, subastas, ref dinero);
        actual.ultimoDiaProcesado = nuevoDia;
        if (actual.DiaDelCiclo(nuevoDia) == 4 && actual.siguiendo)
            SistemaSubastas.Avisar(subastas, actual.id + ":ultimo", "¡La subasta de " + actual.caballo.nombre + " termina hoy!", actual.diaFin);
        SubastasActualizadas?.Invoke();
    }

    void CerrarSubasta()
    {
        var actual = SubastaActual;
        if (actual == null || actual.resultado != ResultadoSubasta.Abierta) return;
        if (actual.LideraJugador && actual.reserva == actual.pujaActual && actual.reserva > 0)
        {
            actual.resultado = ResultadoSubasta.Ganada;
            if (!caballosJugador.Exists(c => c.id == actual.caballo.id))
            {
                var caballo = actual.caballo.CrearCaballo();
                caballo.esDelJugador = true;
                RestaurarVisual(caballo, actual.caballo.spriteIndex);
                caballosJugador.Add(caballo);
            }
            SistemaSubastas.Avisar(subastas, actual.id + ":resultado",
                "¡Ganaste la subasta! " + actual.caballo.nombre + " se añadió a tu establo por $" + actual.pujaActual + ".");
        }
        else
        {
            dinero += actual.reserva;
            actual.resultado = actual.pujaActual > 0 ? ResultadoSubasta.Perdida : ResultadoSubasta.SinOfertas;
            SistemaSubastas.Avisar(subastas, actual.id + ":resultado", actual.pujaActual > 0
                ? actual.NombrePostor + " ganó la subasta de " + actual.caballo.nombre + "."
                : "La subasta de " + actual.caballo.nombre + " finalizó sin ofertas.");
        }
        actual.reserva = 0;
        if (!subastas.historial.Exists(s => s.id == actual.id)) subastas.historial.Add(actual);
    }

    bool CancelarSubastaAlTerminarCampana()
    {
        var actual = SubastaActual;
        if (actual == null || actual.resultado != ResultadoSubasta.Abierta) return false;
        dinero += actual.reserva;
        actual.reserva = 0;
        actual.resultado = ResultadoSubasta.Cancelada;
        if (!subastas.historial.Exists(s => s.id == actual.id)) subastas.historial.Add(actual);
        SistemaSubastas.Avisar(subastas, actual.id + ":resultado",
            "La campaña terminó. La subasta pendiente se canceló y tu reserva fue liberada.");
        return true;
    }
}
