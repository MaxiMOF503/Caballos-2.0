using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class GameManager
{
    CarreraProgramada carreraProgramada;
    int semillaOfertaCarreras;
    bool procesandoResultado;
    ProgresoInicioCarreras inicioCarreras = ProgresoInicioCarreras.Anterior();
    public ProgresoInicioCarreras ProgresoInicio => inicioCarreras;
    public string ErrorInicioCarrera { get; private set; }
    public ApuestaCarrera UltimaApuesta { get; private set; }
    public ResultadoCarrera UltimoResultadoCarrera { get; private set; }
    public bool ResultadoCarreraGuardado { get; private set; } = true;

    public int EstadisticaEfectiva(Caballo caballo, TipoEstadisticaCaballo tipo)
    {
        int valor = tipo switch
        {
            TipoEstadisticaCaballo.Velocidad => caballo.velocidad,
            TipoEstadisticaCaballo.Stamina => caballo.stamina,
            TipoEstadisticaCaballo.Suerte => caballo.suerte,
            _ => caballo.inteligencia
        };
        if (!EsCaballoJugador(caballo)) return valor;
        valor += Preparacion(caballo, tipo);
        var catalogo = CatalogoTienda();
        foreach (var ranura in SistemaEquipamiento.Ranuras)
        {
            var item = ObtenerItemEquipado(caballo, ranura);
            var producto = item == null ? null : SistemaTienda.Buscar(catalogo, item.idDefinicion);
            if (producto != null && producto.tieneSlotEquipable &&
                SistemaEquipamiento.EsCompatible(producto.slotPermitido, ranura))
                valor += producto.BonoPara(tipo);
        }
        return Caballo.NormalizarEstadistica(valor);
    }

    public string MostrarEstadistica(Caballo caballo, TipoEstadisticaCaballo tipo, int valorBase)
    {
        int efectiva = EstadisticaEfectiva(caballo, tipo);
        string rango = RangoCaballo.ObtenerRango(efectiva);
        return efectiva == valorBase ? rango : $"{rango} ({valorBase} → {efectiva}, {efectiva - valorBase:+0;-0})";
    }

    public CatalogoCarreras CatalogoDeCarreras => Resources.Load<CatalogoCarreras>("Carreras/Catalogo");
    public int PrecioInscripcionOferta(DefinicionCarrera carrera) => ObtenerCarreraProgramada()?.idDefinicion == carrera.id
        ? Math.Max(0, carreraProgramada.inscripcion) : CatalogoDeCarreras.Inscripcion(carrera, dia);

    public CarreraProgramada ObtenerCarreraProgramada() =>
        !campanaCompletada && carreraProgramada?.dia == dia ? carreraProgramada : null;

    public IReadOnlyList<DefinicionCarrera> ObtenerOfertaCarreras()
    {
        if (campanaCompletada || Derrotado) return Array.Empty<DefinicionCarrera>();
        var catalogo = CatalogoDeCarreras;
        var guardada = ObtenerCarreraProgramada();
        if (guardada?.SeleccionBloqueada == true)
        {
            var arte = catalogo?.Buscar(guardada.idDefinicion);
            return new[] { new DefinicionCarrera
            {
                id = guardada.idDefinicion, nombre = guardada.NombreVisible,
                distanciaMetros = guardada.distanciaMetros, premio = guardada.PremioGanador, tipo = guardada.tipo,
                indiceBanner = arte?.indiceBanner ?? -1, bannerAlternativo = arte?.bannerAlternativo
            }};
        }

        var oferta = catalogo?.Oferta(dia, semillaOfertaCarreras) ?? Array.Empty<DefinicionCarrera>();
        if (inicioCarreras.IniciacionPendiente && ObtenerDiaDeSemana(dia) != 7 &&
            !oferta.Any(c => catalogo.Inscripcion(c, dia) <= dinero))
        {
            var accesible = catalogo.carreras.Where(c => c.id != catalogo.carreraEspecialSemanal)
                .OrderBy(c => catalogo.Inscripcion(c, dia)).ThenBy(c => c.id, StringComparer.Ordinal).FirstOrDefault();
            if (accesible != null && !oferta.Any(c => c.id == accesible.id)) return oferta.Concat(new[] { accesible }).ToArray();
        }
        return oferta;
    }

    public bool ElegirCarrera(string id, out string mensaje)
    {
        mensaje = null;
        if (campanaCompletada || Derrotado)
        {
            mensaje = "La campaña terminó.";
            return false;
        }
        var guardada = ObtenerCarreraProgramada();
        if (guardada?.SeleccionBloqueada == true)
        {
            if (guardada.idDefinicion == id) return true;
            mensaje = "La carrera ya tiene una apuesta o está iniciada.";
            return false;
        }
        var definicion = ObtenerOfertaCarreras().FirstOrDefault(c => c.id == id);
        var jugador = CaballoActivo;
        if (definicion == null || jugador == null)
        {
            mensaje = "Esta carrera no está disponible.";
            return false;
        }
        if (guardada?.idDefinicion == id) return true;

        var anterior = carreraProgramada;
        carreraProgramada = PrepararParrilla(definicion, jugador);
        if (GuardarPartida())
        {
            RestaurarRivalesDeCarrera();
            return true;
        }
        carreraProgramada = anterior;
        mensaje = "No se pudo guardar la selección.";
        return false;
    }

    CarreraProgramada PrepararParrilla(DefinicionCarrera definicion, Caballo jugador)
    {
        var carrera = new CarreraProgramada
        {
            dia = dia, idDefinicion = definicion.id, nombreCarrera = definicion.nombre,
            tipo = definicion.tipo, distanciaMetros = definicion.distanciaMetros, premio = definicion.premio,
            versionReglas = 1, inscripcion = CatalogoDeCarreras.Inscripcion(definicion, dia),
            iniciacion = inicioCarreras.IniciacionPendiente
        };
        int semilla = CatalogoCarreras.Semilla(semillaOfertaCarreras, dia, definicion.id);
        var azar = new System.Random(semilla);
        var datos = CaballoData.DesdeCaballo(jugador, ObtenerIndiceSprite(jugador.sprite));
        datos.velocidad = EstadisticaEfectiva(jugador, TipoEstadisticaCaballo.Velocidad);
        datos.stamina = EstadisticaEfectiva(jugador, TipoEstadisticaCaballo.Stamina);
        datos.suerte = EstadisticaEfectiva(jugador, TipoEstadisticaCaballo.Suerte);
        datos.inteligencia = EstadisticaEfectiva(jugador, TipoEstadisticaCaballo.Inteligencia);
        carrera.participantes.Add(new ParticipanteCarrera { caballo = datos, semilla = azar.Next(1, int.MaxValue) });

        // Name and visual generation already use Unity random. Isolate their sequence from the rest of the game.
        var estado = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(semilla);
            nombresDisponibles = new List<string>(nombres);
            nombresDisponibles.Remove(jugador.nombre);
            ResetSprites();
            spritesDisponibles.Remove(jugador.sprite);
            var persistente = rivalPersistente != null && !EsCaballoJugador(rivalPersistente) ? rivalPersistente : null;
            if (persistente != null)
            {
                nombresDisponibles.Remove(persistente.nombre);
                spritesDisponibles.Remove(persistente.sprite);
            }

            int cantidad = definicion.dificultad >= 3 ? 5 : 4;
            for (int i = 0; i < cantidad; i++)
            {
                var rival = CrearRivalParaCarrera(definicion, i == 0 ? persistente : null, datos, azar);
                carrera.participantes.Add(new ParticipanteCarrera
                {
                    caballo = CaballoData.DesdeCaballo(rival, ObtenerIndiceSprite(rival.sprite)),
                    semilla = azar.Next(1, int.MaxValue)
                });
            }
        }
        finally { UnityEngine.Random.state = estado; }

        if (carrera.iniciacion)
        {
            float ritmo = Mathf.Lerp(8, 18, Mathf.InverseLerp(1, 16, datos.velocidad)) * .8f;
            foreach (var participante in carrera.participantes)
                participante.ritmoIniciacion = participante.caballo.id == jugador.id ? ritmo :
                    ritmo * Mathf.Lerp(Mathf.Clamp(CatalogoDeCarreras.ritmoRivalIniciacionMin, .5f, .95f),
                        Mathf.Clamp(CatalogoDeCarreras.ritmoRivalIniciacionMax, .5f, .95f), (float)azar.NextDouble());
        }
        carrera.FijarCuotas();
        return carrera;
    }

    Caballo CrearRivalParaCarrera(DefinicionCarrera carrera, Caballo anterior, CaballoData activo, System.Random azar)
    {
        Caballo rival;
        if (anterior != null)
        {
            rival = CaballoData.DesdeCaballo(anterior, ObtenerIndiceSprite(anterior.sprite)).CrearCaballo();
            rival.sprite = anterior.sprite;
        }
        else
        {
            var bytes = new byte[16];
            azar.NextBytes(bytes);
            rival = new Caballo
            {
                id = new Guid(bytes).ToString("N"), nombre = ObtenerNombreUnico(),
                edad = azar.Next(EdadMinima, EdadMaximaExclusiva),
                potencial = azar.Next(PotencialRivalMinimo, PotencialRivalMaximoExclusivo)
            };
            AsignarVisualNuevo(rival);
        }

        var identidad = CatalogoCaballos.Buscar(rival.catalogIdentityId);
        if (anterior == null && identidad != null)
            rival.potencial = Mathf.Clamp(rival.potencial, identidad.potencialMinimo, identidad.potencialMaximo);
        (CatalogoDeCarreras.bandasRivales ?? new BandaRivalesCarrera()).Aplicar(rival, activo, carrera.distanciaMetros, dia, azar);
        rival.esDelJugador = false;
        rival.estres = 0;
        rival.relacion = 0;
        rival.ValidarDatos();
        return rival;
    }

    // The available money already excludes auction escrow. During the first race,
    // protect the cheapest next entry as well, so an optional bet cannot soft-lock the welcome race.
    public int ReservaSiguienteIniciacion(CarreraProgramada carrera) => carrera?.versionReglas > 0 && inicioCarreras.completadas == 0
        ? CatalogoDeCarreras.carreras.Where(c => c.id != CatalogoDeCarreras.carreraEspecialSemanal)
            .Select(c => CatalogoDeCarreras.Inscripcion(c, dia + 1)).DefaultIfEmpty(0).Min() : 0;
    public int MaximoApuestaCarrera(CarreraProgramada carrera) => (int)Math.Max(0L,
        (long)dinero - (carrera?.InscripcionPendiente ?? 0) - ReservaSiguienteIniciacion(carrera));

    public Caballo CrearVistaParticipante(ParticipanteCarrera participante)
    {
        var caballo = participante.caballo.CrearCaballo();
        RestaurarVisual(caballo, participante.caballo.spriteIndex);
        return caballo;
    }

    public bool Apostar(string idCarrera, string idCaballo, int cantidad, out string mensaje)
    {
        var carrera = ObtenerCarreraProgramada();
        if (carrera == null || carrera.id != idCarrera || carrera.iniciada || carrera.apuesta != null || carrera.iniciacion)
        {
            mensaje = carrera?.iniciacion == true ? "Carrera de iniciación: apuestas deshabilitadas." : "Las apuestas están cerradas para esta carrera.";
            return false;
        }
        var participante = carrera.Buscar(idCaballo);
        if (participante == null || cantidad <= 0 || cantidad > MaximoApuestaCarrera(carrera))
        {
            mensaje = $"Elegí un participante. Máximo para apostar: ${MaximoApuestaCarrera(carrera)}; se conserva el dinero de inscripción.";
            return false;
        }
        var apuesta = new ApuestaCarrera
        {
            idCarrera = carrera.id, idCaballo = idCaballo, nombreCaballo = participante.caballo.nombre,
            cantidad = cantidad, cuotaDecimas = participante.cuotaDecimas
        };
        if ((long)dinero - cantidad + apuesta.Pago + carrera.PremioGanador > int.MaxValue)
        {
            mensaje = "El pago supera el saldo máximo admitido.";
            return false;
        }
        dinero -= cantidad;
        carrera.apuesta = apuesta;
        if (!GuardarPartida())
        {
            dinero += cantidad;
            carrera.apuesta = null;
            mensaje = "No se pudo guardar la apuesta. El saldo se conservó.";
            return false;
        }
        ActualizarHUD();
        mensaje = apuesta.Resultado;
        return true;
    }

    public bool IniciarCarreraProgramada(CarreraProgramada carrera)
    {
        ErrorInicioCarrera = null;
        if (carrera == null || carrera != carreraProgramada || campanaCompletada || Derrotado) return false;
        if (carrera.iniciada) return true; // Reanudar una carrera guardada con la misma parrilla.
        if (carrera.participantes == null || carrera.participantes.Count == 0 ||
            carrera.participantes.Any(p => string.IsNullOrEmpty(p?.caballo?.id)) ||
            carrera.participantes.Select(p => p.caballo.id).Distinct().Count() != carrera.participantes.Count)
        { ErrorInicioCarrera = "Parrilla inválida: no se cobró la inscripción."; return false; }
        int costo = carrera.InscripcionPendiente;
        if (costo > 0 && dinero < costo)
        { ErrorInicioCarrera = $"Inscripción: {Moneda.Texto(costo)}. Faltan {Moneda.Texto((long)costo - dinero)} de saldo disponible."; return false; }
        int anterior = dinero;
        bool cobradaAntes = carrera.inscripcionCobrada;
        dinero -= costo;
        if (carrera.versionReglas > 0) carrera.inscripcionCobrada = true;
        carrera.iniciada = true;
        if (GuardarPartida()) { ActualizarHUD(); return true; }
        dinero = anterior;
        carrera.inscripcionCobrada = cobradaAntes;
        carrera.iniciada = false;
        ErrorInicioCarrera = "No se pudo guardar el inicio: no se cobró la inscripción.";
        return false;
    }

    public string FinalizarCarreraProgramada(CarreraProgramada carrera, Caballo ganador, Caballo mejorRival)
    {
        if (carrera == null || carrera != carreraProgramada || !carrera.iniciada ||
            !ClasificacionCarrera.Validar(carrera, carrera.clasificacionFinal)) return "";
        var primero = carrera.clasificacionFinal[0];
        bool anulada = !primero.Finalizo;
        if (!anulada && (ganador == null || ganador.id != primero.idCaballo)) return "";
        var apuesta = carrera.apuesta;
        var jugador = caballosJugador.Find(c => c != null && carrera.Buscar(c.id) != null);
        bool ganoJugador = !anulada && jugador != null && jugador.id == primero.idCaballo;
        mejorRival = null;
        foreach (var llegada in carrera.clasificacionFinal)
            if (llegada.Finalizo && llegada.idCaballo != jugador?.id)
            {
                mejorRival = CrearVistaParticipante(carrera.Buscar(llegada.idCaballo));
                break;
            }
        var resultadoFinal = new ResultadoCarrera { carrera = carrera, idCaballoJugador = jugador?.id };
        ResultadoCarreraGuardado = false;
        // Los guardados habituales de XP y cambio de día forman una sola escritura.
        procesandoResultado = true;
        try
        {
            long pago = anulada ? apuesta?.Anular() ?? 0 : apuesta?.Liquidar(primero.idCaballo) ?? 0;
            dinero = (int)Math.Min(int.MaxValue - DineroReservado, (long)dinero + pago + (anulada ? 0 : ganoJugador ? carrera.PremioGanador : carrera.versionReglas > 0 ? 0 : -200));
            EstablecerRivalPersistente(mejorRival);
            if (jugador != null && !anulada) ProgresionCaballo.AgregarExperiencia(jugador, ganoJugador ? 100 : 25);
            if (!anulada) inicioCarreras.Completar(ganoJugador);
            carreraProgramada = null;
            resultadosAntesDerrota = true;
            SiguienteDia();
            resultadosAntesDerrota = Derrotado;
            if (apuesta != null) UltimaApuesta = apuesta;
            UltimoResultadoCarrera = resultadoFinal;
            ResultadoCarreraGuardado = false;
        }
        finally { procesandoResultado = false; }
        string resultado = apuesta?.Resultado ?? "";
        return GuardarPartida() ? resultado : resultado + "\nNo se pudo guardar el resultado. La partida en disco conserva la carrera anterior.";
    }

    void CargarCarreraProgramada(GameData datos)
    {
        inicioCarreras = datos.inicioCarreras?.version == 1 ? datos.inicioCarreras : ProgresoInicioCarreras.Anterior();
        semillaOfertaCarreras = datos.semillaOfertaCarreras;
        carreraProgramada = datos.carreraProgramada;
        if (carreraProgramada?.participantes != null)
            foreach (var participante in carreraProgramada.participantes)
                MigrarIdentidadVisual(participante?.caballo);
        if (carreraProgramada?.participantes == null || carreraProgramada.participantes.Count == 0 ||
            carreraProgramada.dia != dia) carreraProgramada = null;
        if (carreraProgramada != null && string.IsNullOrEmpty(carreraProgramada.apuesta?.idCarrera))
            carreraProgramada.apuesta = null;
        UltimaApuesta = string.IsNullOrEmpty(datos.ultimaApuesta?.idCarrera) ? null : datos.ultimaApuesta;
        UltimoResultadoCarrera = datos.ultimoResultadoCarrera != null && datos.ultimoResultadoCarrera.Valido ? datos.ultimoResultadoCarrera : null;
        ResultadoCarreraGuardado = true;
        RestaurarRivalesDeCarrera();
    }

    void RestaurarRivalesDeCarrera()
    {
        caballosDelDia.Clear();
        if (carreraProgramada != null)
            foreach (var participante in carreraProgramada.participantes)
                if (!participante.caballo.esDelJugador) caballosDelDia.Add(CrearVistaParticipante(participante));
    }
}
