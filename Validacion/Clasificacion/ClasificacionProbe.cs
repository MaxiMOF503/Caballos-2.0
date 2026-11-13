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
public static class ClasificacionProbe
{
    const string Key = "ClasificacionProbe.Active";
    const string Output = "C:/Caballos/Validation/Clasificacion/";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly List<string> checks = new();
    static readonly List<string> unexpected = new();
    static IEnumerator tests;
    static GameManager game;
    static CarreraUI race;
    static ClasificacionProbe() => EditorApplication.update += Step;
    public static void Run()
    {
        if (!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH")) || File.Exists(SaveSystem.RutaArchivo))
            throw new InvalidOperationException("Guardado aislado requerido");
        Modelo();
        EditorSceneManager.OpenScene(GameLaunch.HubScenePath);
        EditorApplication.delayCall += () => EditorApplication.delayCall += () => { SessionState.SetBool(Key, true); EditorApplication.isPlaying = true; };
    }
    static void Step()
    {
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying) return;
        try { tests ??= Tests(); if (!tests.MoveNext()) Finish(0); }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    static void Modelo()
    {
        var model = new ClasificacionCarrera(new[] { "a", "b", "c" }, 100);
        model.Avanzar(1, new[] { new AvanceCarrera("a", 0, 101), new AvanceCarrera("b", 0, 120), new AvanceCarrera("c", 0, 99) });
        Check(!model.Completa && model.Resultados.Select(x => x.idCaballo).SequenceEqual(new[] { "b", "a" }), "Mismo frame: interpolación vence al orden de la lista");
        Check(Math.Abs(model.Resultados[0].tiempo - 100d / 120) < 1e-12, "Tiempo subframe exacto");
        var mutable = model.Resultados[0]; mutable.posicion = 900;
        model.Avanzar(1, new[] { new AvanceCarrera("b", 120, 200), new AvanceCarrera("c", 99, 101) });
        Check(model.Completa && model.Resultados.Select(x => x.posicion).SequenceEqual(new[] { 1, 2, 3 }), "Llegadas únicas, fijas y protegidas de mutaciones externas");
        model.Avanzar(100, new[] { new AvanceCarrera("a", 101, 1000) });
        Check(model.Resultados.Count == 3 && model.Tiempo == 2, "No repite llegadas ni avanza una carrera resuelta");
        var tie = new ClasificacionCarrera(new[] { "z", "a" }, 100);
        tie.Avanzar(.1, new[] { new AvanceCarrera("z", 0, 101), new AvanceCarrera("a", 0, 101) });
        Check(tie.Resultados[0].idCaballo == "a", "Empate exacto: desempate por ID estable");
        var close = new ClasificacionCarrera(new[] { "a", "b" }, 100);
        close.Avanzar(.02, new[] { new AvanceCarrera("a", 0, 100.00001f), new AvanceCarrera("b", 0, 100.00002f) });
        Check(close.Resultados[0].idCaballo == "b" && close.Resultados[0].tiempo < close.Resultados[1].tiempo, "Cruces muy próximos no se redondean para decidir el ganador");
        var errors = new List<string>();
        var fault = new ClasificacionCarrera(new[] { "stuck", "nan", "ok" }, 100, errors.Add);
        fault.Avanzar(1, new[] { new AvanceCarrera("stuck", 0, 0), new AvanceCarrera("nan", 0, float.NaN), new AvanceCarrera("ok", 0, 101) });
        Check(!fault.Completa && fault.Resultados.Count == 1 && fault.Resuelto("nan"), "Dato corrupto se aísla sin parar a un corredor válido");
        fault.Avanzar(60, new[] { new AvanceCarrera("stuck", 0, 0) });
        Check(fault.Completa && fault.Resultados[0].idCaballo == "ok" && fault.Resultados.Skip(1).All(x => !x.Finalizo && x.tiempo == -1) && errors.Count == 2,
            "Bloqueados detrás de todos los finalistas, con diagnóstico e ID");
        var paused = new ClasificacionCarrera(new[] { "a" }, 100);
        paused.Avanzar(0, Array.Empty<AvanceCarrera>());
        paused.Avanzar(double.NaN, Array.Empty<AvanceCarrera>());
        Check(paused.Tiempo == 0 && !paused.Completa, "Pausa y delta inválido no consumen el plazo");
        var endless = new ClasificacionCarrera(new[] { "a" }, 100);
        for (int i = 0; i < endless.LimiteTiempo + 2 && !endless.Completa; i++)
            endless.Avanzar(1, new[] { new AvanceCarrera("a", i * .001f, (i + 1) * .001f) });
        Check(endless.Completa && !endless.Resultados[0].Finalizo, "Progreso patológico mínimo también tiene límite determinista");
        var random = new System.Random(7721);
        for (int trial = 0; trial < 400; trial++)
        {
            var samples = Enumerable.Range(0, 6).Select(i => new AvanceCarrera("id-" + i, 90, 100.001f + (float)random.NextDouble() * 30)).OrderBy(_ => random.Next()).ToArray();
            var sorted = new ClasificacionCarrera(samples.Select(x => x.id), 100);
            sorted.Avanzar(.016, samples);
            Check(sorted.Resultados.Select(x => x.idCaballo).SequenceEqual(samples.OrderByDescending(x => x.actual).ThenBy(x => x.id, StringComparer.Ordinal).Select(x => x.id)), "Orden independiente en parrilla aleatoria " + trial);
        }
        // Exercise the unmodified engine at the longest supported distance with weak stamina.
        for (int speed = 1; speed <= 16; speed++)
        {
            var horse = new Caballo { velocidad = speed, stamina = 1, suerte = 1, inteligencia = 1, potencial = 16 };
            var state = new EstadoCaballoCarrera(horse, speed);
            var longRace = new ClasificacionCarrera(new[] { horse.id }, 400);
            for (int frame = 0; frame < 30000 && !longRace.Completa; frame++)
            {
                float before = state.distanciaRecorrida; state.Actualizar(.02f, 400);
                longRace.Avanzar(.02f, new[] { new AvanceCarrera(horse.id, before, state.distanciaRecorrida) });
            }
            Check(longRace.Completa && longRace.Resultados[0].Finalizo && longRace.Tiempo < longRace.LimiteTiempo, "Motor real: carrera larga válida con velocidad " + speed);
        }
        bool rejected = false;
        try { new ClasificacionCarrera(new[] { "a", "a" }, 100); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "Rechaza parrillas con identidad duplicada");
        File.WriteAllLines(Output + "modelo-pruebas.txt", checks);
    }
    static IEnumerator Tests()
    {
        while (GameManager.Instancia == null || GameManager.Instancia.caballosJugador.Count == 0) yield return null;
        double ready = EditorApplication.timeSinceStartup + 1; while (EditorApplication.timeSinceStartup < ready) yield return null;
        game = GameManager.Instancia;
        race = UIManager.instance.viewCarrera.GetComponentInChildren<CarreraUI>(true);
        Application.logMessageReceived += OnLog;
        // Real UI and production simulation: player first, last, 1/2/6 runners and long race.
        foreach (var scenario in new[] { (6, true, 1200), (6, false, 3200), (2, true, 1600), (1, true, 1200) })
        {
            game.IniciarNuevaPartida(); game.dinero = 100000;
            race.enabled = true;
            Check(game.ElegirCarrera(game.ObtenerOfertaCarreras()[0].id, out _), "Elige carrera del calendario");
            var scheduled = game.ObtenerCarreraProgramada();
            AjustarParrilla(scheduled, scenario.Item1, scenario.Item2, scenario.Item3);
            string player = game.CaballoActivo.id;
            int baseSpeed = game.CaballoActivo.velocidad;
            int day = game.dia, money = game.dinero;
            Check(game.Apostar(scheduled.id, player, 100, out _), "Apuesta confirmada por ID real");
            int odds = scheduled.apuesta.cuotaDecimas;
            game.SetState(Interactable.Tipo.TV); yield return null;
            race.enabled = false;
            race.IniciarCarrera();
            string initialSave = File.ReadAllText(SaveSystem.RutaArchivo);
            var tracker = (ClasificacionCarrera)Get(race, "clasificacion");
            var states = (List<EstadoCaballoCarrera>)Get(race, "estadosCarrera");
            var actors = (List<HorseVisualAnimator>)Get(race, "visualAnimators");
            var positions = (List<RectTransform>)Get(race, "caballosUI");
            bool firstObserved = false;
            for (int frame = 0; frame < 25000 && EnCurso(); frame++)
            {
                Call(race, "AvanzarCarrera", .02f);
                if (!firstObserved && tracker.Resultados.Count > 0 && !tracker.Completa)
                {
                    firstObserved = true;
                    Check(EnCurso() && game.dia == day && scheduled.apuesta.estado == EstadoApuesta.Pendiente, "Primer cruce no liquida ni adelanta el día");
                    Check(File.ReadAllText(SaveSystem.RutaArchivo) == initialSave, "Primer cruce no escribe un resultado parcial");
                    string firstId = tracker.Resultados[0].idCaballo;
                    Check((firstId == player) == scenario.Item2, "Escenario controlado de jugador primero/último");
                    int finishedIndex = states.FindIndex(x => x.caballo.id == firstId);
                    float finishedDistance = states[finishedIndex].distanciaRecorrida;
                    var running = states.Find(x => !tracker.Resuelto(x.caballo.id));
                    float runningBefore = running.distanciaRecorrida;
                    Check(!actors[finishedIndex].IsRunning && actors.Where((_, i) => !tracker.Resuelto(states[i].caballo.id)).All(x => x.IsRunning), "Sólo se detiene la animación del que llegó");
                    Check(positions[finishedIndex].anchoredPosition.x == 1000, "Finalista queda en meta, sin salir del recorrido");
                    Call(race, "AvanzarCarrera", .02f);
                    Check(states[finishedIndex].distanciaRecorrida == finishedDistance && running.distanciaRecorrida > runningBefore, "Finalista no avanza y los demás continúan");
                    Check(Raiz() == null, "No hay pantalla final antes del último");
                }
            }
            Check(!EnCurso() && tracker.Completa && ClasificacionCarrera.Validar(scheduled, scheduled.clasificacionFinal), "Carrera real completa y clasificación válida");
            var ranking = scheduled.clasificacionFinal;
            Check(ranking.Select(x => x.posicion).SequenceEqual(Enumerable.Range(1, scenario.Item1)) && ranking.All(x => x.Finalizo), "Todas las posiciones son únicas y todas las llegadas válidas");
            Check((ranking[0].idCaballo == player) == scenario.Item2 && (scenario.Item2 || ranking.Last().idCaballo == player), "Jugador ocupa el extremo previsto de la clasificación");
            Check(scenario.Item1 == 1 || firstObserved, "Se observó la carrera continuar tras el primer cruce");
            Check(game.dia == day + 1 && game.CaballoActivo.velocidad == baseSpeed, "Un avance de día y estadísticas base conservadas");
            Check(scheduled.apuesta.cuotaDecimas == odds && scheduled.apuesta.estado == (scenario.Item2 ? EstadoApuesta.Ganada : EstadoApuesta.Perdida), "Cuotas congeladas y apuesta liquidada con ganador definitivo");
            Check(game.dinero == money - 100 + (scenario.Item2 ? scheduled.PremioGanador + 100 * odds / 10 : -200), "Premio y apuesta conservan los importes existentes");
            Check(game.CaballoActivo.nivel == (scenario.Item2 ? 2 : 1) && game.CaballoActivo.experiencia == (scenario.Item2 ? 0 : 25), "XP única según resultado definitivo");
            Check(actors.All(x => !x.IsRunning && x.GetComponentInChildren<Image>().sprite == x.Profile.baseSprite), "Cada animador restaura su base correspondiente");
            ValidarUI(scheduled, player);
            int settledMoney = game.dinero, settledDay = game.dia, xp = game.CaballoActivo.experiencia;
            game.FinalizarCarreraProgramada(scheduled, game.CrearVistaParticipante(scheduled.Buscar(ranking[0].idCaballo)), null);
            Call(race, "AvanzarCarrera", 2f);
            Check(game.dinero == settledMoney && game.dia == settledDay && game.CaballoActivo.experiencia == xp, "No duplica premio, apuesta, XP ni día");
            Check(SaveSystem.IntentarCargar(out var saved) && saved.ultimoResultadoCarrera.Valido && saved.ultimoResultadoCarrera.carrera.clasificacionFinal.Count == scenario.Item1, "JSON contiene clasificación completa");
            Check((bool)Call(game, "CargarPartida") && game.UltimoResultadoCarrera.Valido && game.ObtenerCarreraProgramada() == null, "Carga no reabre ni vuelve a liquidar una carrera terminada");
            if (scenario.Item1 == 6 && scenario.Item2) { Render("podio", true); yield return null; }
            else { Render("podio-" + scenario.Item1 + (scenario.Item2 ? "-primero" : "-ultimo"), false); yield return null; }
            var back = Raiz().Find("Volver").GetComponent<Button>(); back.onClick.Invoke();
            Check(!UIManager.instance.viewCarrera.activeInHierarchy && Cursor.visible, "Volver al Hub funciona y restaura cursor");
            yield return null;
        }
        IntegracionExcepciones();
        Check(unexpected.Count == 0, "Sin errores de UI ni excepciones inesperadas: " + string.Join(" | ", unexpected));
        Debug.Log("CLASIFICACION PASS: " + checks.Count + " comprobaciones");
    }
    static void AjustarParrilla(CarreraProgramada scheduled, int count, bool playerFirst, int meters)
    {
        while (scheduled.participantes.Count > count) scheduled.participantes.RemoveAt(scheduled.participantes.Count - 1);
        while (scheduled.participantes.Count < count)
        {
            var horse = new Caballo(); CatalogoCaballos.Asignar(horse, CatalogoCaballos.Todos[50 + scheduled.participantes.Count]);
            scheduled.participantes.Add(new ParticipanteCarrera { caballo = CaballoData.DesdeCaballo(horse, -1), semilla = scheduled.participantes.Count + 901 });
        }
        foreach (var p in scheduled.participantes)
        {
            bool fast = p.caballo.esDelJugador == playerFirst;
            p.caballo.velocidad = fast ? 16 : 1; p.caballo.stamina = fast ? 16 : 1;
            p.caballo.inteligencia = fast ? 16 : 1; p.caballo.suerte = 1; p.semilla = 442 + scheduled.participantes.IndexOf(p);
        }
        scheduled.distanciaMetros = meters; scheduled.FijarCuotas();
    }
    static void ValidarUI(CarreraProgramada scheduled, string player)
    {
        var root = Raiz();
        Check(root != null, "Pantalla de resultados presente sólo al completar");
        var texts = root.GetComponentsInChildren<TMP_Text>(true);
        var playerResult = scheduled.clasificacionFinal.Single(x => x.idCaballo == player);
        Check(texts.Single(x => x.name == "ResultadoPersonal").text == $"Tu caballo: {playerResult.posicion}.º de {scheduled.participantes.Count} participantes", "Puesto personal coincide con la clasificación");
        Check(texts.Count(x => x.name == "Posicion") == scheduled.participantes.Count, "Ranking incluye a todos, sin filas ficticias");
        Check(texts.Count(x => x.name == "Puesto") == Math.Min(3, scheduled.participantes.Count), "Podio muestra sólo los puestos existentes");
        for (int i = 0; i < Math.Min(3, scheduled.participantes.Count); i++)
        {
            var entry = scheduled.clasificacionFinal[i]; var horse = scheduled.Buscar(entry.idCaballo).caballo;
            var portrait = root.GetComponentsInChildren<Image>().Single(x => x.name == "Splash_" + entry.idCaballo);
            var identity = CatalogoCaballos.Buscar(horse.catalogIdentityId);
            Check(portrait.sprite.texture == RecursosCatalogo.Cargar(identity.splashRecurso).texture && portrait.preserveAspect && !portrait.raycastTarget, "Podio: splash propio y proporciones para " + horse.nombre);
            Check(portrait.transform.parent.Find("Nombre").GetComponent<TMP_Text>().text == horse.nombre, "Podio: nombre real para " + entry.idCaballo);
        }
        Check(root.GetComponentsInChildren<Button>(true).All(x => x.transition == Selectable.Transition.None), "Sin efectos decorativos de hover");
    }
    static void IntegracionExcepciones()
    {
        race.enabled = true;
        game.IniciarNuevaPartida(); game.dinero = 100000;
        Check(game.ElegirCarrera(game.ObtenerOfertaCarreras()[0].id, out _), "Parrilla para protección de liquidación");
        var scheduled = game.ObtenerCarreraProgramada(); string player = game.CaballoActivo.id;
        Check(game.IniciarCarreraProgramada(scheduled), "Inicio de protección guardado");
        int money = game.dinero, day = game.dia;
        var winner = game.CrearVistaParticipante(scheduled.participantes[0]);
        game.FinalizarCarreraProgramada(scheduled, winner, null);
        Check(game.dinero == money && game.dia == day && game.ObtenerCarreraProgramada() == scheduled, "API rechaza liquidación al primer ganador sin clasificación completa");
        scheduled.clasificacionFinal = Fixture(scheduled);
        scheduled.clasificacionFinal[1].idCaballo = scheduled.clasificacionFinal[0].idCaballo;
        game.FinalizarCarreraProgramada(scheduled, winner, null);
        Check(game.dinero == money && game.dia == day, "API rechaza ranking duplicado");
        scheduled.clasificacionFinal = null;
        game.SetState(Interactable.Tipo.TV);
        race.enabled = false;
        race.IniciarCarrera();
        // Safe write failure: use a file as the isolated save directory, never the personal save.
        string originalPath = Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH");
        string oldFile = File.ReadAllText(SaveSystem.RutaArchivo);
        string invalidPath = Path.Combine(Output, "directorio-no-disponible"); File.WriteAllText(invalidPath, "fixture");
        Environment.SetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH", invalidPath);
        for (int frame = 0; frame < 25000 && EnCurso(); frame++) Call(race, "AvanzarCarrera", .02f);
        Check(!game.ResultadoCarreraGuardado && game.dia == day + 1, "Fallo de guardado se comunica y conserva resultado en memoria");
        var retry = Raiz().Find("ReintentarGuardado").GetComponent<Button>();
        var back = Raiz().Find("Volver").GetComponent<Button>();
        Check(retry.gameObject.activeInHierarchy && !back.gameObject.activeInHierarchy, "UI ofrece reintentar y bloquea salida con guardado pendiente");
        Render("guardado-pendiente", false);
        int afterFailure = game.dinero;
        game.FinalizarCarreraProgramada(scheduled, winner, null);
        Check(game.dinero == afterFailure && game.dia == day + 1, "Fallo de escritura no permite reliquidar");
        retry.onClick.Invoke();
        Check(!game.ResultadoCarreraGuardado && game.dinero == afterFailure, "Reintento todavía fallido conserva saldo y pantalla");
        Environment.SetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH", originalPath);
        Check(File.ReadAllText(SaveSystem.RutaArchivo) == oldFile, "Fallos de escritura conservaron la copia anterior");
        retry.onClick.Invoke();
        Check(game.ResultadoCarreraGuardado && back.gameObject.activeInHierarchy && !retry.gameObject.activeInHierarchy, "Botón reintentar guarda correctamente y habilita volver");
        Check(game.dinero == afterFailure && game.dia == day + 1, "Reintento de guardado no altera economía ni calendario");
        back.onClick.Invoke();
        // Legacy schema: missing optional results is accepted without a fabricated result.
        Check(SaveSystem.IntentarCargar(out var legacy), "Lee fixture para compatibilidad");
        legacy.ultimoResultadoCarrera = null;
        Check(SaveSystem.Guardar(legacy) && (bool)Call(game, "CargarPartida") && game.UltimoResultadoCarrera == null, "Guardado anterior sin clasificación se carga correctamente");
        Environment.SetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH", invalidPath);
        Check(!game.GuardarPartida() && game.ResultadoCarreraGuardado, "Un fallo general sin premio pendiente no bloquea el retorno de la carrera");
        Environment.SetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH", originalPath);
        // Every entrant faulty: no fabricated winner, no prize or XP, one bet refund.
        game.IniciarNuevaPartida(); game.dinero = 100000;
        game.ElegirCarrera(game.ObtenerOfertaCarreras()[0].id, out _); scheduled = game.ObtenerCarreraProgramada();
        money = game.dinero; day = game.dia;
        Check(game.Apostar(scheduled.id, game.CaballoActivo.id, 100, out _) && game.IniciarCarreraProgramada(scheduled), "Apuesta para carrera sin llegadas");
        var invalid = new ClasificacionCarrera(scheduled.participantes.Select(x => x.caballo.id), 100);
        invalid.Avanzar(60, scheduled.participantes.Select(x => new AvanceCarrera(x.caballo.id, 0, 0)).ToArray());
        scheduled.clasificacionFinal = new List<LlegadaCarrera>(invalid.Resultados);
        game.FinalizarCarreraProgramada(scheduled, null, null);
        Check(game.dinero == money && game.dia == day + 1 && game.CaballoActivo.experiencia == 0 && scheduled.apuesta.estado == EstadoApuesta.Anulada,
            "Sin llegadas: devolución, sin premio/penalización/XP inventados, un día");
        game.FinalizarCarreraProgramada(scheduled, null, null);
        Check(game.dinero == money && scheduled.apuesta.Anular() == 0, "Reembolso no se duplica");
        // Weekly fee and last campaign day still use the pre-existing calendar exactly once.
        foreach (int calendarDay in new[] { 7, 42 })
        {
            game.IniciarNuevaPartida(); game.dia = calendarDay; game.dinero = 1000000;
            game.ElegirCarrera(game.ObtenerOfertaCarreras()[0].id, out _); scheduled = game.ObtenerCarreraProgramada();
            Check(game.IniciarCarreraProgramada(scheduled), "Inicia carrera del día " + calendarDay);
            scheduled.clasificacionFinal = Fixture(scheduled); money = game.dinero;
            winner = game.CrearVistaParticipante(scheduled.Buscar(scheduled.clasificacionFinal[0].idCaballo));
            game.FinalizarCarreraProgramada(scheduled, winner, null);
            Check(game.dinero == money + scheduled.PremioGanador - GameManager.ObtenerCuotaSemana(calendarDay / 7), "Cuota semanal y premio una sola vez día " + calendarDay);
            Check(game.dia == (calendarDay == 42 ? 42 : 8) && game.campanaCompletada == (calendarDay == 42), "Calendario preservado al terminar día " + calendarDay);
            Check(game.UltimoResultadoCarrera.Valido && SaveSystem.IntentarCargar(out var result) && result.ultimoResultadoCarrera.Valido, "Guarda clasificación en transición semanal/final");
        }
    }
    internal static List<LlegadaCarrera> Fixture(CarreraProgramada race) => race.participantes.Select((p, i) => new LlegadaCarrera { idCaballo = p.caballo.id, posicion = i + 1, tiempo = i + 10, progreso = 100 }).ToList();
    static void Render(string name, bool all)
    {
        var canvas = race.GetComponentInParent<Canvas>(); var mode = canvas.renderMode; var previousCamera = canvas.worldCamera;
        var render = new GameObject("RenderResultadosPrueba").AddComponent<Camera>();
        render.transform.position = new Vector3(0, 0, -100); render.clearFlags = CameraClearFlags.SolidColor; render.backgroundColor = Color.black;
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = render; canvas.planeDistance = 1;
        var sizes = all ? new[] { new Vector2Int(1920, 1080), new Vector2Int(1600, 900), new Vector2Int(1366, 768), new Vector2Int(1280, 720) } : new[] { new Vector2Int(1280, 720) };
        foreach (var size in sizes)
        {
            var rt = new RenderTexture(size.x, size.y, 24); render.targetTexture = rt; Canvas.ForceUpdateCanvases(); render.Render(); RenderTexture.active = rt;
            var screenshot = new Texture2D(size.x, size.y, TextureFormat.RGB24, false); screenshot.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); screenshot.Apply();
            File.WriteAllBytes(Output + name + "-" + size.x + "x" + size.y + ".png", screenshot.EncodeToPNG()); RenderTexture.active = null; Object.Destroy(screenshot);
            var root = Raiz().GetComponent<RectTransform>();
            var corners = new Vector3[4]; root.GetWorldCorners(corners);
            var a = render.WorldToScreenPoint(corners[0]); var b = render.WorldToScreenPoint(corners[2]);
            Check(Math.Abs(a.x) < 2 && Math.Abs(a.y) < 2 && Math.Abs(b.x - size.x) < 2 && Math.Abs(b.y - size.y) < 2, "Resultados cubren viewport " + size);
            Check(root.GetComponentsInChildren<TMP_Text>().All(x => !x.isTextOverflowing), "Sin texto cortado en " + name + " " + size);
            render.targetTexture = null; Object.Destroy(rt);
        }
        canvas.renderMode = mode; canvas.worldCamera = previousCamera; Object.Destroy(render.gameObject);
    }
    static void OnLog(string message, string stack, LogType type)
    {
        if ((type == LogType.Error || type == LogType.Exception) && !message.StartsWith("No se pudo guardar la partida")) unexpected.Add(message);
    }
    static bool EnCurso() => (bool)Get(race, "carreraActiva");
    static Transform Raiz() => UIManager.instance.viewCarrera.transform.Cast<Transform>().LastOrDefault(t => t.name == "ResultadosCarrera");
    static object Get(object target, string name) => target.GetType().GetField(name, Flags).GetValue(target);
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Flags).Invoke(target, args);
    static void Check(bool condition, string text) { if (!condition) throw new InvalidOperationException(text); checks.Add(text); }
    static void Finish(int code)
    {
        SessionState.SetBool(Key, false); Application.logMessageReceived -= OnLog;
        File.WriteAllLines(Output + "pruebas.txt", checks); EditorApplication.Exit(code);
    }
}
