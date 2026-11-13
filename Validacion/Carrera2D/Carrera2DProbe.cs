using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class Carrera2DProbe
{
    const string Key = "Carrera2DProbe.Active", Output = "C:/Caballos/Validation/Carrera2D/";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static List<string> checks = new List<string>();
    static readonly List<string> errors = new List<string>();
    static IEnumerator tests;
    static GameManager game;
    static CarreraUI ui;
    static Carrera2DProbe() => EditorApplication.update += Step;
    public static void Run()
    {
        if (!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH")) || File.Exists(SaveSystem.RutaArchivo)) throw new InvalidOperationException("Se requiere guardado aislado nuevo.");
        Modelos();
        EditorSceneManager.OpenScene(GameLaunch.HubScenePath);
        EditorApplication.delayCall += () => EditorApplication.delayCall += () => { SessionState.SetBool(Key, true); EditorApplication.isPlaying = true; };
    }
    static void Step()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        try { tests ??= Tests(); if (!tests.MoveNext()) Finish(0); } catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void Modelos()
    {
        var catalogo = Resources.Load<CatalogoCarreras>("Carreras/Catalogo");
        foreach (var pair in new[] { (1200, 200), (1600, 300), (2100, 400), (2400, 500) })
        {
            var definition = catalogo.carreras.First(c => c.distanciaMetros == pair.Item1 && c.id != catalogo.carreraEspecialSemanal);
            Check(catalogo.Inscripcion(definition, 7) == pair.Item2 && catalogo.Inscripcion(definition, 8) == pair.Item2 * 2, "Tarifa exacta día 7/8 para " + pair.Item1);
            Check(catalogo.Inscripcion(definition, 15) == pair.Item2 * 2, "No baja la tarifa al reiniciar la semana " + pair.Item1);
            var mapa = new MapaPistaCarrera(100f * pair.Item1 / 1200);
            Check(mapa.X(mapa.distancia) == mapa.meta && mapa.X(0) == mapa.largada && mapa.longitud > mapa.meta, "Meta y conversión única " + pair.Item1);
            Check(mapa.Encuadre(mapa.meta, 1800) <= mapa.longitud - 1800 && mapa.Encuadre(0, 1800) == 0, "Límites de cámara " + pair.Item1);
        }
        Check(catalogo.Inscripcion(catalogo.Buscar(catalogo.carreraEspecialSemanal), 8) == 1000, "Tarifa propia de carrera especial");
        var custom = new DefinicionCarrera { distanciaMetros = 3000, inscripcionInicial = 750, inscripcionDesdeDia8 = 1100 };
        Check(catalogo.Inscripcion(custom, 1) == 750 && catalogo.Inscripcion(custom, 14) == 1100, "Nueva distancia con tarifa propia configurable");
        var activo = new CaballoData { velocidad = 5, stamina = 4, inteligencia = 5, suerte = 3 };
        int victorias = 0;
        for (int seed = 0; seed < 60; seed++)
        {
            var own = new Caballo { velocidad = 5, stamina = 4, inteligencia = 5, suerte = 3 };
            var runners = new List<EstadoCaballoCarrera> { new EstadoCaballoCarrera(own, seed * 7 + 3) };
            for (int i = 0; i < 5; i++)
            {
                var rival = new Caballo(); catalogo.bandasRivales.Aplicar(rival, activo, 1200, 1, new System.Random(seed * 11 + i));
                Check(rival.velocidad >= 3 && rival.velocidad <= own.velocidad && rival.stamina <= own.stamina, "Banda inicial acotada " + seed + "/" + i);
                runners.Add(new EstadoCaballoCarrera(rival, seed * 100 + i));
            }
            var result = Simular(runners, 100, null);
            if (result[0].idCaballo == own.id) victorias++;
        }
        Check(victorias >= 12 && victorias < 60, "Carreras normales conservan victoria y derrota: " + victorias + "/60 victorias iniciales");
        for (int stat = 1; stat <= 16; stat++)
            foreach (int meters in new[] { 1200, 2400 })
            {
                var runners = Enumerable.Range(0, 6).Select(i => new EstadoCaballoCarrera(new Caballo { velocidad = i == 0 ? stat : 16, stamina = i == 0 ? 1 : 16 }, stat * 20 + i)).ToList();
                float speed = Mathf.Lerp(8, 18, Mathf.InverseLerp(1, 16, stat)) * .8f;
                var speeds = Enumerable.Range(0, 6).Select(i => speed * (i == 0 ? 1 : .95f - i * .01f)).ToArray();
                var result = Simular(runners, 100f * meters / 1200, speeds);
                Check(result[0].idCaballo == runners[0].caballo.id && result.All(x => x.Finalizo), "Guion real sin azar ni salto, atributo " + stat + ", " + meters + "m");
            }
        var before = new ProgresoInicioCarreras { version = 1 };
        before.Completar(false); Check(before.IniciacionPendiente, "Primera derrota activa segunda de iniciación");
        before.Completar(true); Check(!before.IniciacionPendiente && before.completadas == 2, "Tercera normal");
        before = ProgresoInicioCarreras.Nuevo(); before.Completar(true);
        Check(!before.IniciacionPendiente && !ProgresoInicioCarreras.Anterior().IniciacionPendiente, "Primera victoria y migración nunca activan guion");
        File.WriteAllLines(Output + "modelos.txt", checks);
    }
    static IReadOnlyList<LlegadaCarrera> Simular(List<EstadoCaballoCarrera> runners, float distance, float[] speeds)
    {
        var order = new ClasificacionCarrera(runners.Select(s => s.caballo.id), distance);
        for (int tick = 0; tick < 30000 && !order.Completa; tick++)
        {
            var advances = new List<AvanceCarrera>();
            for (int i = 0; i < runners.Count; i++)
            {
                var r = runners[i]; if (order.Resuelto(r.caballo.id)) continue;
                float prior = r.distanciaRecorrida; r.Actualizar(.02f, distance, speeds?[i] ?? 0);
                advances.Add(new AvanceCarrera(r.caballo.id, prior, r.distanciaRecorrida));
            }
            order.Avanzar(.02f, advances);
        }
        if (!order.Completa) throw new InvalidOperationException("Simulación infinita de prueba");
        return order.Resultados;
    }
    static IEnumerator Tests()
    {
        while (GameManager.Instancia == null || GameManager.Instancia.caballosJugador.Count == 0) yield return null;
        double ready = EditorApplication.timeSinceStartup + 1; while (EditorApplication.timeSinceStartup < ready) yield return null;
        game = GameManager.Instancia; ui = UIManager.instance.viewCarrera.GetComponentInChildren<CarreraUI>(true);
        Application.logMessageReceived += OnLog;
        game.IniciarNuevaPartida();
        // Initial price + existing auction escrow + optional bet: never spend the entries.
        var auction = game.SubastaActual; auction.precioInicial = 100;
        foreach (var bidder in auction.postores) bidder.retirado = true;
        Check(game.PujarSubasta(auction.id, 400, out _) && game.dinero == 600 && game.DineroReservado == 400, "Reserva de subasta separada del disponible");
        UIManager.instance.AbrirSeleccionCarrera(); yield return null;
        Render("selector-inscripciones", new Vector2Int(1280, 720));
        var race = Preparar(1200); string playerId = game.CaballoActivo.id;
        AjustarNormal(race, false);
        int cash = game.dinero;
        Check(!race.iniciacion && race.inscripcion == 200 && game.ProgresoInicio.completadas == 0, "Primera normal con inscripción de $200");
        Check(game.MaximoApuestaCarrera(race) == 200 && !game.Apostar(race.id, playerId, 201, out _), "Apuesta respeta inscripción y siguiente entrada mínima, además de reserva de subasta");
        Check(game.Apostar(race.id, playerId, 200, out _), "Apuesta permitida sin comprometer las entradas");
        string frozen = JsonUtility.ToJson(race);
        UIManager.instance.AbrirTablon(); yield return null;
        Check(game.dinero == cash - 200 && JsonUtility.ToJson(race) == frozen, "Consultar participantes no cobra ni regenera");
        Render("tablon-inscripcion", new Vector2Int(1280, 720));
        game.VolverAlHub(); game.SetState(Interactable.Tipo.TV); yield return null;
        ui.enabled = false;
        Check(game.dinero == cash - 200 && JsonUtility.ToJson(race) == frozen, "Abrir preparación del televisor no cobra ni regenera");
        Render("preparacion", new Vector2Int(1280, 720));
        ui.IniciarCarrera();
        Check(game.dinero == 200 && race.inscripcionCobrada && race.iniciada, "Largada cobra exactamente una vez $200");
        ui.IniciarCarrera(); game.IniciarCarreraProgramada(race);
        Check(game.dinero == 200 && game.DineroReservado == 400, "Repetir inicio no vuelve a cobrar ni consume reserva");
        Check(game.GuardarPartida(), "Guarda carrera iniciada con inscripción y apuesta");
        game.VolverAlHub(); Load(); race = game.ObtenerCarreraProgramada();
        Check(race.iniciada && race.inscripcionCobrada && race.apuesta.cantidad == 200 && !race.iniciacion, "Carga conserva inicio, apuesta y condiciones normales");
        Abrir(); yield return null; ui.enabled = false; ui.IniciarCarrera();
        Check(game.dinero == 200, "Abandono/reanudación no cobra otra inscripción");
        CorrerHastaFinal(race, false, true);
        Check(game.dia == 2 && game.dinero == 200 && game.ProgresoInicio.IniciacionPendiente && race.apuesta.estado == EstadoApuesta.Perdida, "Derrota real conserva segunda entrada y activa guion una vez");
        Check(game.UltimoResultadoCarrera.carrera.clasificacionFinal.Last().idCaballo == playerId, "Jugador último y ranking real completo");
        game.VolverAlHub(); yield return null; Load();
        Check(game.ProgresoInicio.IniciacionPendiente && game.dinero == 200, "Derrota y guion pendiente sobreviven al guardado/carga");

        // Second race: configured before any UI can accept a bet; shuffled player not in first/central lane.
        var affordable = game.ObtenerOfertaCarreras().First(c => game.PrecioInscripcionOferta(c) <= game.dinero);
        Check(game.ElegirCarrera(affordable.id, out _), "Oferta conserva una segunda carrera pagable después de perder con apuesta");
        race = game.ObtenerCarreraProgramada();
        Check(race.iniciacion && race.participantes.All(p => p.ritmoIniciacion > 0), "Ritmos de bienvenida fijados en la parrilla");
        var own = race.participantes.Single(p => p.caballo.id == playerId);
        race.participantes.Remove(own); race.participantes.Insert(1, own);
        // Even hypothetical stronger rival stats/luck do not override the explicitly guided pace.
        foreach (var p in race.participantes.Where(p => p != own)) { p.caballo.velocidad = 16; p.caballo.stamina = 16; p.caballo.suerte = 16; }
        Check(game.GuardarPartida(), "Guarda parámetros y orden no central de prueba"); Load(); race = game.ObtenerCarreraProgramada();
        frozen = JsonUtility.ToJson(race); cash = game.dinero;
        UIManager.instance.AbrirTablon(); yield return null;
        var board = UIManager.instance.viewTablon.GetComponentInChildren<TablonView>();
        Check(!board.importe.interactable && !board.apostar.interactable && !game.Apostar(race.id, playerId, 1, out string message) && message.Contains("iniciación"), "Apuestas bloqueadas antes de comprometer dinero, en UI y API");
        Check(game.dinero == cash && JsonUtility.ToJson(race) == frozen, "Consultar iniciación no cobra ni cambia el guion");
        game.VolverAlHub(); Abrir(); yield return null; ui.enabled = false;
        ui.IniciarCarrera(); Check(game.dinero == 0, "Segunda inscripción normal, sin dinero ficticio añadido");
        string guidedGrid = JsonUtility.ToJson(race);
        game.VolverAlHub(); yield return null; Load(); race = game.ObtenerCarreraProgramada();
        Check(JsonUtility.ToJson(race) == guidedGrid && game.ProgresoInicio.IniciacionPendiente, "Abandonar/cargar iniciación conserva parrilla y guion pendiente");
        Abrir(); yield return null; ui.enabled = false; ui.IniciarCarrera();
        Check(game.dinero == 0 && race.iniciacion, "Reanudar iniciación no vuelve a cobrar ni pierde la garantía");
        var actors = (List<RectTransform>)Get(ui, "caballosUI");
        var arrow = actors[1].Find("IndicadorJugador");
        Check(arrow != null && actors.Where((r, i) => i != 1).All(r => r.Find("IndicadorJugador") == null), "Flecha pertenece exclusivamente al ID real, índice 1 fuera del centro");
        Presentar(0, false); Check(arrow.gameObject.activeSelf, "Flecha inicial visible");
        Presentar(1.99f, false); Check(arrow.gameObject.activeSelf, "Visible durante casi dos segundos");
        Presentar(.02f, false); Check(!arrow.gameObject.activeSelf, "Se oculta tras dos segundos");
        Presentar(.1f, true); Check(arrow.gameObject.activeSelf, "A mantenida muestra flecha");
        Render("flecha-jugador", new Vector2Int(1280, 720));
        var previousArrow = arrow.position;
        actors[1].anchoredPosition += new Vector2(10, 20);
        Check(arrow.position != previousArrow && arrow.parent == actors[1], "Flecha sigue posición y cambio de carril del mismo actor");
        Presentar(.1f, false); Check(!arrow.gameObject.activeSelf, "Soltar A oculta flecha");
        var states = (List<EstadoCaballoCarrera>)Get(ui, "estadosCarrera");
        float initialProgress = states[1].distanciaRecorrida;
        Presentar(.1f, true); Presentar(.1f, false);
        Check(states[1].distanciaRecorrida == initialProgress, "Identificar no mueve ni altera rendimiento");
        CorrerHastaFinal(race, true, false);
        Check(game.dinero == race.PremioGanador && game.dia == 3 && game.ProgresoInicio.completadas == 2, "Segunda gana realmente; inscripción/premio/día únicos");
        Check(!arrow.gameObject.activeSelf, "Flecha oculta en resultados");
        int paid = game.dinero; game.FinalizarCarreraProgramada(race, game.CrearVistaParticipante(race.Buscar(playerId)), null);
        Check(game.dinero == paid && game.dia == 3, "Repetir resultado no duplica pagos ni día");
        ValidarPodio(race);
        Render("podio", new Vector2Int(1280, 720));
        game.VolverAlHub(); yield return null; Load();
        race = Preparar(1200);
        Check(!race.iniciacion && race.participantes.All(p => p.ritmoIniciacion == 0), "Desde tercera no hay guion tras guardar/cargar");

        // Winning the first is not followed by a forced second.
        game.IniciarNuevaPartida(); race = Preparar(1200); AjustarNormal(race, true);
        Abrir(); yield return null; ui.enabled = false; ui.IniciarCarrera(); CorrerHastaFinal(race, true, false);
        Check(game.ProgresoInicio.ganoPrimera && game.ProgresoInicio.completadas == 1 && !game.ProgresoInicio.IniciacionPendiente, "Primera victoria real no activa iniciación");
        game.VolverAlHub(); yield return null; Load(); race = Preparar(1600);
        Check(!race.iniciacion, "Segunda normal después de primera victoria y recarga");

        foreach (bool first in new[] { true, false })
        {
            game.VolverAlHub(); yield return null;
            game.IniciarNuevaPartida(); Set(game, "inicioCarreras", ProgresoInicioCarreras.Anterior()); game.dinero = 100000;
            race = Preparar(2400); AjustarNormal(race, first); int before = game.dinero;
            Abrir(); yield return null; ui.enabled = false; ui.IniciarCarrera();
            CorrerHastaFinal(race, first, true);
            Check(game.dinero == before - race.inscripcion + (first ? race.PremioGanador : 0), "Larga: inscripción y premio correctos, jugador " + (first ? "primero" : "último"));
            ValidarPodio(race);
        }

        ProbarErroresYMigracion();
        Check(errors.Count == 0, "Sin errores de UI/excepciones inesperadas: " + string.Join(" | ", errors));
        Debug.Log("CARRERA 2D PASS: " + checks.Count + " comprobaciones de integración");
    }
    static CarreraProgramada Preparar(int meters)
    {
        for (int seed = 1; seed < 500; seed++)
        {
            Set(game, "semillaOfertaCarreras", seed);
            var candidate = game.ObtenerOfertaCarreras().FirstOrDefault(c => c.distanciaMetros == meters);
            if (candidate != null) { Check(game.ElegirCarrera(candidate.id, out _), "Selecciona " + meters + " m"); return game.ObtenerCarreraProgramada(); }
        }
        throw new InvalidOperationException("No hay oferta de prueba " + meters);
    }
    static void AjustarNormal(CarreraProgramada race, bool first)
    {
        foreach (var p in race.participantes)
        { bool fast = p.caballo.esDelJugador == first; p.caballo.velocidad = fast ? 16 : 1; p.caballo.stamina = fast ? 16 : 1; p.caballo.inteligencia = fast ? 16 : 1; p.caballo.suerte = 1; }
        Check(game.GuardarPartida(), "Guarda fixture normal, sin guion");
    }
    static void CorrerHastaFinal(CarreraProgramada race, bool playerFirst, bool longRender)
    {
        var order = (ClasificacionCarrera)Get(ui, "clasificacion");
        var actors = (List<RectTransform>)Get(ui, "caballosUI");
        var states = (List<EstadoCaballoCarrera>)Get(ui, "estadosCarrera");
        int playerIndex = states.FindIndex(s => s.caballo.esDelJugador);
        var view = Get(ui, "pista"); var map = (MapaPistaCarrera)Get(view, "mapa");
        var world = (RectTransform)Get(view, "mundo"); var finish = (RectTransform)world.Find("Meta");
        Check(finish.anchoredPosition.x == map.meta, "Línea dibujada coincide con meta de simulación " + race.distanciaMetros);
        bool firstSeen = false, halfway = false;
        bool photographedFinish = false;
        int priorCount = 0;
        for (int tick = 0; tick < 30000 && EnCurso(); tick++)
        {
            Tick(.02f); Presentar(.02f, false);
            for (int i = 0; i < states.Count; i++)
                if (order.Resuelto(states[i].caballo.id))
                {
                    Check(Math.Abs(actors[i].anchoredPosition.x - map.meta) < .01f, "Punto competitivo coincide con meta, actor " + i);
                    Check(Math.Abs(actors[i].Find("PuntoCompetitivo").position.x - finish.position.x) < .01f, "Cruce visible coincide en coordenadas del Canvas, actor " + i);
                }
            if (order.Resultados.Count > priorCount)
            {
                priorCount = order.Resultados.Count;
                Check(order.Resultados.Select(x => x.posicion).Distinct().Count() == priorCount, "Puestos únicos al registrar cruce");
                if (!order.Completa)
                {
                    firstSeen = true;
                    Check(EnCurso() && game.ObtenerCarreraProgramada() == race, "Simulación sigue tras una llegada");
                    if (playerFirst && !photographedFinish)
                    {
                        photographedFinish = true;
                        Presentar(.1f, false);
                        Render("meta-" + race.distanciaMetros, new Vector2Int(1280, 720));
                    }
                }
            }
            if (!halfway && states[playerIndex].distanciaRecorrida > map.distancia * .48f && !order.Completa)
            {
                halfway = true;
                Presentar(0, false); // align the camera once; then animate a stationary view
                float camera = (float)Get(view, "CamaraX");
                string a = Render("recorrido-" + race.distanciaMetros + "-" + (playerFirst ? "primero" : "ultimo"), new Vector2Int(1280, 720));
                camera = (float)Get(view, "CamaraX");
                Presentar(.4f, false);
                string b = Render("ambiente-" + race.distanciaMetros + "-" + (playerFirst ? "primero" : "ultimo"), new Vector2Int(1280, 720));
                Debug.Log("AMBIENTE: cambia=" + (a != b) + " camaraAntes=" + camera + " despues=" + Get(view, "CamaraX"));
                Check(a != b && Math.Abs((float)Get(view, "CamaraX") - camera) < .001f, "Ambiente realmente animado con cámara quieta");
                if (longRender)
                    foreach (var size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1600, 900), new Vector2Int(1366, 768) }) Render("pista-" + race.distanciaMetros, size);
            }
        }
        Check(!EnCurso() && order.Completa && race.clasificacionFinal.All(x => x.Finalizo), "Todos completan corta/larga y hay clasificación final");
        Check((race.clasificacionFinal[0].idCaballo == game.CaballoActivo.id) == playerFirst, "Ganador real esperado, sin cambiar resultados");
        Check(playerFirst || race.clasificacionFinal.Last().idCaballo == game.CaballoActivo.id, "Jugador último cuando corresponde");
        Check(firstSeen && halfway, "Se observó recorrido y continuidad después del primer finalista");
        Check(ClasificacionCarrera.Validar(race, race.clasificacionFinal), "Clasificación completa válida");
    }
    static void ProbarErroresYMigracion()
    {
        game.IniciarNuevaPartida(); var race = Preparar(1200); game.dinero = 199;
        Check(!game.IniciarCarreraProgramada(race) && game.dinero == 199 && !race.iniciada && game.ErrorInicioCarrera.Contains("Faltan $1"), "Saldo insuficiente informa faltante y no cobra");
        game.dinero = 400; Check(game.GuardarPartida(), "Guarda saldo para fallo de inicio");
        string path = Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH"), previous = File.ReadAllText(SaveSystem.RutaArchivo);
        string invalid = Output + "no-directorio"; File.WriteAllText(invalid, "fixture"); Environment.SetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH", invalid);
        Check(!game.IniciarCarreraProgramada(race) && game.dinero == 400 && !race.iniciada && !race.inscripcionCobrada, "Fallo al guardar inicio revierte cobro y flags");
        Environment.SetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH", path);
        Check(File.ReadAllText(SaveSystem.RutaArchivo) == previous, "Fallo de inicio conserva JSON previo");
        // Persisted legacy pending grid, confirmed bet and active race: grandfather its terms.
        Check(SaveSystem.IntentarCargar(out var data), "Lee fixture legacy");
        data.dia = 20; data.inicioCarreras = null; data.dinero = 750;
        data.carreraProgramada.dia = 20; data.carreraProgramada.versionReglas = 0; data.carreraProgramada.inscripcion = 0;
        data.carreraProgramada.iniciada = true; data.carreraProgramada.inscripcionCobrada = false;
        data.carreraProgramada.apuesta = new ApuestaCarrera { idCarrera = race.id, idCaballo = game.CaballoActivo.id, cantidad = 50, cuotaDecimas = 27 };
        string grid = JsonUtility.ToJson(data.carreraProgramada);
        Check(SaveSystem.Guardar(data), "Escribe partida avanzada sin progreso nuevo"); Load(); race = game.ObtenerCarreraProgramada();
        Check(game.ProgresoInicio.completadas == 2 && !race.iniciacion && game.IniciarCarreraProgramada(race) && game.dinero == 750 && JsonUtility.ToJson(race) == grid,
            "Migración avanzada conserva parrilla, apuesta y condiciones, sin nuevo cobro ni guion");
        Abrir(); ui.enabled = false; ui.IniciarCarrera();
        var legacyStates = (List<EstadoCaballoCarrera>)Get(ui, "estadosCarrera");
        var original = new EstadoCaballoCarrera(legacyStates[0].caballo, race.participantes[0].semilla);
        float distance = (float)Get(ui, "DistanciaSimulada");
        original.Actualizar(.037f, distance); Tick(.037f);
        Check(legacyStates[0].distanciaRecorrida == original.distanciaRecorrida && game.dinero == 750,
            "Carrera legacy conserva su integración anterior, sin nueva tarifa ni parámetros guiados");
        game.VolverAlHub();
        race.iniciada = false; game.dinero = -50;
        Check(game.IniciarCarreraProgramada(race) && game.dinero == -50,
            "Carrera antigua sin inscripción conserva posibilidad de iniciar con saldo negativo anterior");
        game.IniciarNuevaPartida(); game.dia = 8; race = Preparar(1200);
        Check(race.inscripcion == 400 && game.IniciarCarreraProgramada(race) && game.dinero == 600, "Día global 8 cobra corta $400 una vez");
        game.VolverAlHub(); Load(); race = game.ObtenerCarreraProgramada();
        Check(game.IniciarCarreraProgramada(race) && game.dinero == 600, "Día 8 reanudado no cobra otra vez");
    }
    static void ValidarPodio(CarreraProgramada race)
    {
        var root = UIManager.instance.viewCarrera.transform.Find("ResultadosCarrera");
        foreach (var r in race.clasificacionFinal.Take(3))
        {
            var image = root.GetComponentsInChildren<Image>().Single(x => x.name == "Splash_" + r.idCaballo);
            var horse = race.Buscar(r.idCaballo).caballo;
            Check(image.sprite.texture == RecursosCatalogo.Cargar(CatalogoCaballos.Buscar(horse.catalogIdentityId).splashRecurso).texture, "Podio conserva identidad " + horse.nombre);
        }
        Check(root.GetComponentsInChildren<TMP_Text>().Count(x => x.name == "Posicion") == race.participantes.Count, "Ranking incluye toda la parrilla");
    }
    static string Render(string name, Vector2Int size)
    {
        var canvas = ui.GetComponentInParent<Canvas>(); var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera;
        var camera = new GameObject("RenderCarrera2DPrueba").AddComponent<Camera>(); camera.transform.position = new Vector3(0, 0, -100);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        var rt = new RenderTexture(size.x, size.y, 24); camera.targetTexture = rt;
        Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = rt;
        var image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); image.Apply();
        byte[] png = image.EncodeToPNG(); File.WriteAllBytes(Output + name + "-" + size.x + "x" + size.y + ".png", png);
        string hash = Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(png));
        RenderTexture.active = null; camera.targetTexture = null; canvas.renderMode = oldMode; canvas.worldCamera = oldCamera;
        Object.Destroy(camera.gameObject); Object.Destroy(rt); Object.Destroy(image);
        Check(true, "Render fuera de pantalla " + name + " " + size); return hash;
    }
    static void Abrir() { ui.enabled = true; game.SetState(Interactable.Tipo.TV); }
    static void Load() => Check((bool)Call(game, "CargarPartida"), "Carga de campaña aislada");
    static void Tick(float dt) => Call(ui, "AvanzarCarrera", dt);
    static void Presentar(float dt, bool held) => Call(ui, "ActualizarPresentacion", dt, held);
    static bool EnCurso() => (bool)Get(ui, "EnCurso");
    static object Get(object o, string name) => o.GetType().GetField(name, Flags)?.GetValue(o) ?? o.GetType().GetProperty(name, Flags).GetValue(o);
    static void Set(object o, string name, object value) => o.GetType().GetField(name, Flags).SetValue(o, value);
    static object Call(object o, string name, params object[] args) => o.GetType().GetMethod(name, Flags).Invoke(o, args);
    static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks.Add(message); }
    static void OnLog(string message, string stack, LogType type)
    { if ((type == LogType.Error || type == LogType.Exception) && !message.StartsWith("No se pudo guardar la partida")) errors.Add(message); }
    static void Finish(int code)
    {
        SessionState.SetBool(Key, false); Application.logMessageReceived -= OnLog;
        File.WriteAllLines(Output + "pruebas.txt", checks); EditorApplication.Exit(code);
    }
}
