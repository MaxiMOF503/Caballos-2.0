using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class GameManager
{
    CarreraProgramada carreraProgramada;
    int semillaOfertaCarreras;
    bool procesandoResultado;
    public ApuestaCarrera UltimaApuesta { get; private set; }

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
        var catalogo = CatalogoTienda();
        foreach (var ranura in SistemaEquipamiento.Ranuras)
        {
            var item = ObtenerItemEquipado(caballo, ranura);
            var producto = item == null ? null : SistemaTienda.Buscar(catalogo, item.idDefinicion);
            if (producto != null && producto.tieneSlotEquipable &&
                SistemaEquipamiento.EsCompatible(producto.slotPermitido, ranura) &&
                producto.estadistica == tipo)
                valor += producto.bonificacion;
        }
        return Caballo.NormalizarEstadistica(valor);
    }

    public string MostrarEstadistica(Caballo caballo, TipoEstadisticaCaballo tipo, int valorBase)
    {
        int efectiva = EstadisticaEfectiva(caballo, tipo);
        string rango = RangoCaballo.ObtenerRango(efectiva);
        return efectiva == valorBase ? rango : $"{rango} ({valorBase} → {efectiva}, +{efectiva - valorBase})";
    }

    public CatalogoCarreras CatalogoDeCarreras => Resources.Load<CatalogoCarreras>("Carreras/Catalogo");

    public CarreraProgramada ObtenerCarreraProgramada() =>
        !campanaCompletada && carreraProgramada?.dia == dia ? carreraProgramada : null;

    public IReadOnlyList<DefinicionCarrera> ObtenerOfertaCarreras()
    {
        if (campanaCompletada) return Array.Empty<DefinicionCarrera>();
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

        return catalogo?.Oferta(dia, semillaOfertaCarreras) ?? Array.Empty<DefinicionCarrera>();
    }

    public bool ElegirCarrera(string id, out string mensaje)
    {
        mensaje = null;
        if (campanaCompletada)
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
        var jugador = caballosJugador.FirstOrDefault(c => c != null);
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
            tipo = definicion.tipo, distanciaMetros = definicion.distanciaMetros, premio = definicion.premio
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
                var rival = CrearRivalParaCarrera(definicion, i == 0 ? persistente : null, azar);
                carrera.participantes.Add(new ParticipanteCarrera
                {
                    caballo = CaballoData.DesdeCaballo(rival, ObtenerIndiceSprite(rival.sprite)),
                    semilla = azar.Next(1, int.MaxValue)
                });
            }
        }
        finally { UnityEngine.Random.state = estado; }

        carrera.FijarCuotas();
        return carrera;
    }

    Caballo CrearRivalParaCarrera(DefinicionCarrera carrera, Caballo anterior, System.Random azar)
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

        int distancia = Mathf.Clamp(Mathf.RoundToInt((carrera.distanciaMetros - 1200) / 400f), 0, 3);
        int dificultad = Mathf.Clamp(carrera.dificultad, 1, 5);
        int velocidad = 5 + dificultad / 2 + (distancia == 0 ? 1 : 0);
        int stamina = 2 + distancia * 2 + dificultad / 2;
        int tecnica = 3 + dificultad / 2 + (distancia == 0 ? 1 : 0);
        rival.velocidad = AjustarAtributo(velocidad, anterior?.velocidad, azar);
        rival.stamina = AjustarAtributo(stamina, anterior?.stamina, azar);
        rival.inteligencia = AjustarAtributo(tecnica, anterior?.inteligencia, azar);
        rival.suerte = AjustarAtributo(3 + dificultad / 2, anterior?.suerte, azar);
        rival.esDelJugador = false;
        rival.estres = 0;
        rival.relacion = 0;
        rival.ValidarDatos();
        return rival;
    }

    static int AjustarAtributo(int objetivo, int? anterior, System.Random azar) => anterior.HasValue
        ? Mathf.Clamp(anterior.Value, objetivo - 2, objetivo + 2)
        : objetivo + azar.Next(-2, 3);

    public Caballo CrearVistaParticipante(ParticipanteCarrera participante)
    {
        var caballo = participante.caballo.CrearCaballo();
        RestaurarVisual(caballo, participante.caballo.spriteIndex);
        return caballo;
    }

    public bool Apostar(string idCarrera, string idCaballo, int cantidad, out string mensaje)
    {
        var carrera = ObtenerCarreraProgramada();
        if (carrera == null || carrera.id != idCarrera || carrera.iniciada || carrera.apuesta != null)
        {
            mensaje = "Las apuestas están cerradas para esta carrera.";
            return false;
        }
        var participante = carrera.Buscar(idCaballo);
        if (participante == null || cantidad <= 0 || cantidad > dinero)
        {
            mensaje = "Elegí un participante e ingresá un importe positivo que puedas pagar.";
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
        if (carrera == null || carrera != carreraProgramada || campanaCompletada) return false;
        if (carrera.iniciada) return true; // Reanudar una carrera guardada con la misma parrilla.
        carrera.iniciada = true;
        if (GuardarPartida()) return true;
        carrera.iniciada = false;
        return false;
    }

    public string FinalizarCarreraProgramada(CarreraProgramada carrera, Caballo ganador, Caballo mejorRival)
    {
        if (carrera == null || ganador == null || carrera != carreraProgramada ||
            !carrera.iniciada || carrera.Buscar(ganador.id) == null) return "";
        var apuesta = carrera.apuesta;
        var jugador = caballosJugador.Find(c => c != null && carrera.Buscar(c.id) != null);
        bool ganoJugador = jugador != null && jugador.id == ganador.id;
        // Los guardados habituales de XP y cambio de día forman una sola escritura.
        procesandoResultado = true;
        try
        {
            long pago = apuesta?.Liquidar(ganador.id) ?? 0;
            dinero = (int)Math.Min(int.MaxValue, (long)dinero + pago + (ganoJugador ? carrera.PremioGanador : -200));
            EstablecerRivalPersistente(mejorRival);
            if (jugador != null) ProgresionCaballo.AgregarExperiencia(jugador, ganoJugador ? 100 : 25);
            carreraProgramada = null;
            SiguienteDia();
            if (apuesta != null) UltimaApuesta = apuesta;
        }
        finally { procesandoResultado = false; }
        string resultado = apuesta?.Resultado ?? "";
        return GuardarPartida() ? resultado : resultado + "\nNo se pudo guardar el resultado. La partida en disco conserva la carrera anterior.";
    }

    void CargarCarreraProgramada(GameData datos)
    {
        semillaOfertaCarreras = datos.semillaOfertaCarreras;
        carreraProgramada = datos.carreraProgramada;
        if (carreraProgramada?.participantes == null || carreraProgramada.participantes.Count == 0 ||
            carreraProgramada.dia != dia) carreraProgramada = null;
        if (carreraProgramada != null && string.IsNullOrEmpty(carreraProgramada.apuesta?.idCarrera))
            carreraProgramada.apuesta = null;
        UltimaApuesta = string.IsNullOrEmpty(datos.ultimaApuesta?.idCarrera) ? null : datos.ultimaApuesta;
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
