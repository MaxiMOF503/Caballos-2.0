using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BetaJugableValidation
{
    const string Activa = "BetaJugableValidation.Activa";
    static double inicio;
    static int saldoAntes;
    static ApuestaCarrera apuestaEnCurso;
    static int premioEnCurso;
    static CarreraProgramada carreraSinApuesta;
    static int saldoSinApuesta;
    static string resultadoAnterior;

    static BetaJugableValidation() => EditorApplication.update += Verificar;

    public static void Run()
    {
        string ruta = Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH");
        if (!Application.isBatchMode || string.IsNullOrEmpty(ruta) ||
            !Path.GetFullPath(ruta).StartsWith(Path.GetFullPath("../ValidationSave") + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase) || File.Exists(SaveSystem.RutaArchivo))
            throw new InvalidOperationException("Se requiere batch y una carpeta nueva debajo de ../ValidationSave.");
        EditorSceneManager.OpenScene(GameLaunch.HubScenePath);
        SessionState.SetBool(Activa, true);
        EditorApplication.isPlaying = true;
    }

    static void Verificar()
    {
        if (!SessionState.GetBool(Activa, false) || !EditorApplication.isPlaying) return;
        try
        {
            if (inicio == 0) inicio = EditorApplication.timeSinceStartup;
            Exigir(EditorApplication.timeSinceStartup - inicio < 120, "Tiempo máximo de validación");
            var juego = GameManager.Instancia;
            if (juego == null || juego.caballosJugador.Count == 0) return;
            if (apuestaEnCurso == null)
            {
                VerificarEquipamiento(juego);
                VerificarSeleccion(juego);
                VerificarTablonYLiquidacion(juego);
                PrepararCarreraReal(juego);
                return;
            }
            if (carreraSinApuesta != null)
            {
                if (juego.dia != 4) return;
                VerificarResultadoSinApuesta(juego);
                VerificarCarreraAnterior(juego);
                Debug.Log("BETA PASS: escenarios A–G, con apuesta y sin apuesta, persistencia, cambio de carrera y liquidación única.");
                Salir(0);
                return;
            }
            if (juego.dia != 3) return;
            var resultado = juego.UltimaApuesta;
            Exigir(resultado.idCarrera == apuestaEnCurso.idCarrera && resultado.estado != EstadoApuesta.Pendiente,
                "La simulación liquidó la apuesta");
            var vista = UIManager.instance.viewCarrera.GetComponentInChildren<CarreraUI>();
            int balanceCarrera = vista.textoEstado.text.Contains("Premio de carrera") ? premioEnCurso : -200;
            long pago = resultado.estado == EstadoApuesta.Ganada ? resultado.Pago : 0;
            Exigir(juego.dinero == saldoAntes + balanceCarrera + pago, "Balance real de carrera y apuesta");
            Recargar(juego);
            Exigir(juego.UltimaApuesta.estado == resultado.estado && juego.dinero == saldoAntes + balanceCarrera + pago,
                "Resultado persistente sin segundo pago");
            PrepararCarreraSinApuesta(juego);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            Salir(1);
        }
    }

    static void VerificarEquipamiento(GameManager juego)
    {
        juego.dinero = 10000;
        var catalogo = juego.CatalogoTienda().Where(p => p.tipoProducto == TipoProductoTienda.Equipable).ToArray();
        Exigir(catalogo.Length == 5, "Cinco equipables");
        foreach (var producto in catalogo)
        {
            juego.estadoTienda.idsProductosStock = new System.Collections.Generic.List<string> { producto.idDefinicion };
            int saldo = juego.dinero;
            int inventario = juego.itemsJugador.Count;
            Exigir(juego.IntentarComprarItem(producto.idDefinicion), "Compra " + producto.nombre);
            Exigir(juego.dinero == saldo - producto.precio && juego.itemsJugador.Count == inventario + 1, "Un cargo por compra");
            var caballo = juego.caballosJugador[0];
            int valor = juego.EstadisticaEfectiva(caballo, producto.estadistica);
            var item = juego.itemsJugador.Last();
            Exigir(juego.EquiparItem(caballo, item), "Equipar " + producto.nombre);
            Exigir(!juego.EquiparItem(caballo, item), "Rechazar equipo repetido");
            int efectiva = Caballo.NormalizarEstadistica(valor + producto.bonificacion);
            Exigir(juego.EstadisticaEfectiva(caballo, producto.estadistica) == efectiva, "Bonificación efectiva");
            Exigir(juego.MostrarEstadistica(caballo, producto.estadistica, valor).Contains(RangoCaballo.ObtenerRango(efectiva)),
                "Clasificación del establo");
            string id = item.id;
            Recargar(juego);
            caballo = juego.caballosJugador[0];
            Exigir(juego.itemsJugador.Count == inventario + 1 && juego.EstadisticaEfectiva(caballo, producto.estadistica) == efectiva,
                "Carga conserva el equipo sin duplicar la bonificación");
            Exigir(juego.DesequiparItem(caballo, juego.itemsJugador.Find(i => i.id == id)), "Desequipar");
            Exigir(juego.EstadisticaEfectiva(caballo, producto.estadistica) == valor, "Valor base restaurado");
        }
        var zapatillas = juego.itemsJugador.Find(i => i.idDefinicion == "zapatillas");
        Exigir(juego.EquiparItem(juego.caballosJugador[0], zapatillas), "Zapatillas para carrera");
    }

    static void VerificarSeleccion(GameManager juego)
    {
        var catalogo = juego.CatalogoDeCarreras;
        Exigir(catalogo.atlasBanners != null && catalogo.carreras.Count == 16 &&
            catalogo.carreras.GroupBy(c => c.distanciaMetros).All(grupo => grupo.Count() == 4) &&
            catalogo.carreras.Select(c => c.id).Distinct().Count() == 16 &&
            catalogo.carreras.Select(c => c.indiceBanner).Distinct().Count() == 16, "Catálogo y atlas de 16 carreras");
        Exigir(Enumerable.Range(1, 6).Select(dia => catalogo.Oferta(dia, 123).Count).Distinct().Count() > 1 &&
            catalogo.Oferta(7, 123).Single().id == catalogo.carreraEspecialSemanal, "Ofertas variables y cierre semanal");
        Exigir(juego.ObtenerCarreraProgramada() == null && juego.caballosDelDia.Count == 0, "Sin parrilla antes de elegir");
        int semilla = Enumerable.Range(1, 30).First(valor => catalogo.Oferta(juego.dia, valor).Count == 4);
        typeof(GameManager).GetField("semillaOfertaCarreras", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(juego, semilla);
        var ofertas = juego.ObtenerOfertaCarreras();
        var camara = CameraController.instancia.transform;
        var posicion = camara.position;
        var rotacion = camara.rotation;
        var televisor = UnityEngine.Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None)
            .First(objeto => objeto.tipo == Interactable.Tipo.TV);
        televisor.Interact();
        Exigir(juego.estadoActual == GameState.Hub && juego.ObtenerCarreraProgramada() == null &&
            !UIManager.instance.viewCarrera.activeSelf && camara.position == posicion && camara.rotation == rotacion,
            "C: TV sin selección conserva el Hub y la cámara");
        Exigir(UIManager.instance.viewTablon.transform.parent.Find("AvisoHub").GetComponentInChildren<TMPro.TMP_Text>()
            .text.Contains("tablón de apuestas"), "C: aviso visible para elegir primero en el tablón");
        juego.SetState(Interactable.Tipo.Tablon);
        var selector = UnityEngine.Object.FindFirstObjectByType<SeleccionCarreraView>();
        Exigir(selector != null && !UIManager.instance.viewTablon.activeSelf &&
            !UIManager.instance.viewCarrera.activeSelf, "A: el tablón abre el selector cuando falta una carrera");
        var participar = selector.transform.Find("Participar").GetComponent<UnityEngine.UI.Button>();
        Exigir(!participar.interactable, "Participar exige selección");
        var primera = selector.transform.Find("Ofertas/Viewport/Lista/Carrera_" + ofertas[0].id);
        var brillo = primera.Find("Hover").GetComponent<UnityEngine.UI.Image>();
        var boton = primera.GetComponent<UnityEngine.UI.Button>();
        var evento = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        Exigir(!brillo.enabled, "Brillo inicial apagado");
        boton.OnPointerEnter(evento);
        Exigir(brillo.enabled, "Hover enciende el brillo");
        boton.OnPointerExit(evento);
        boton.onClick.Invoke();
        Exigir(!brillo.enabled && participar.interactable && selector.transform.Find("CarreraSeleccionada")
            .GetComponent<TMPro.TMP_Text>().text.Contains(ofertas[0].nombre), "Selección separada del hover");
        Exigir(juego.ObtenerCarreraProgramada() == null, "Seleccionar sin Participar no genera rivales");
        participar.onClick.Invoke();
        var primeraParrilla = juego.ObtenerCarreraProgramada();
        Exigir(UIManager.instance.viewTablon.activeSelf && !UIManager.instance.viewCarrera.activeSelf && !selector.gameObject.activeSelf &&
            primeraParrilla.idDefinicion == ofertas[0].id && primeraParrilla.premio == ofertas[0].premio,
            "A: Seleccionar prepara la carrera y abre el tablón");
        string parrillaGuardada = JsonUtility.ToJson(primeraParrilla);
        Recargar(juego);
        Exigir(JsonUtility.ToJson(juego.ObtenerCarreraProgramada()) == parrillaGuardada, "D: selección y cuotas sobreviven a carga");
        juego.VolverAlHub();
        televisor.Interact();
        Exigir(UIManager.instance.viewCarrera.activeSelf &&
            JsonUtility.ToJson(juego.ObtenerCarreraProgramada()) == parrillaGuardada, "TV utiliza exactamente la carrera preparada");
        juego.VolverAlHub();
        juego.SetState(Interactable.Tipo.Tablon);
        var tablon = UIManager.instance.viewTablon.GetComponentInChildren<TablonView>();
        tablon.CambiarCarrera();
        Exigir(selector.gameObject.activeSelf, "E: volver al selector sin apostar");
        var ids = primeraParrilla.participantes.Skip(1).Select(p => p.caballo.id).ToArray();
        var alternativa = ofertas.Last();
        selector.transform.Find("Ofertas/Viewport/Lista/Carrera_" + alternativa.id).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        participar.onClick.Invoke();
        var segunda = juego.ObtenerCarreraProgramada();
        Exigir(!ids.SequenceEqual(segunda.participantes.Skip(1).Select(p => p.caballo.id)) &&
            segunda.participantes.Skip(1).Average(p => p.caballo.stamina) >
            primeraParrilla.participantes.Skip(1).Average(p => p.caballo.stamina), "E: otra distancia adapta rivales");
        Exigir(juego.ElegirCarrera(ofertas[0].id, out _) &&
            ids.SequenceEqual(juego.ObtenerCarreraProgramada().participantes.Skip(1).Select(p => p.caballo.id)),
            "Volver a una carrera conserva su parrilla determinista");
    }

    static void VerificarTablonYLiquidacion(GameManager juego)
    {
        juego.caballosJugador[0].nombre = "Umbral 41";
        var carrera = juego.ObtenerCarreraProgramada();
        carrera.participantes[0].caballo.nombre = juego.caballosJugador[0].nombre;
        var primero = carrera.participantes[1].caballo;
        var segundo = carrera.participantes[2].caballo;
        primero.nombre = segundo.nombre = "Réplica 52";
        primero.edad = 3;
        segundo.edad = 7;
        primero.velocidad = 2;
        segundo.velocidad = 12;
        primero.nivel = 2;
        segundo.nivel = 4;
        if (carrera.participantes.Count == 5)
        {
            var adicional = CaballoData.DesdeCaballo(juego.caballosDelDia[0], -1);
            adicional.id = Guid.NewGuid().ToString("N");
            adicional.nombre = "Borde 63";
            carrera.participantes.Add(new ParticipanteCarrera { caballo = adicional, semilla = 31 });
        }
        carrera.FijarCuotas();
        UIManager.instance.AbrirTablon();
        var vista = UIManager.instance.viewTablon.GetComponentInChildren<TablonView>();
        var jugador = carrera.participantes[0];
        Exigir(jugador.caballo.esDelJugador && vista.content.childCount == carrera.participantes.Count &&
            carrera.participantes.Count == 6, "Parrilla completa en tablón");
        VerificarGeometriaTablon(vista);
        Exigir(!vista.transform.parent.GetComponentsInChildren<Transform>(true).Any(t => t.name == "BotonAgregarCaballo"),
            "Sin botón Añadir caballo");
        Exigir(jugador.caballo.velocidad == juego.caballosJugador[0].velocidad + 1, "Carrera usa velocidad efectiva");
        Exigir(carrera.participantes.Count(p => p.cuotaDecimas == 30) == 1 &&
            carrera.participantes.Single(p => p.cuotaDecimas == 30).Rendimiento == carrera.participantes.Min(p => p.Rendimiento),
            "Cuota 3 para el participante más débil");
        var filas = vista.content.Cast<Transform>().Select(t => t.GetComponent<CaballoButtonUI>()).ToArray();
        Exigir(filas[0].textoNombre.text.Contains("Umbral 41 (tuyo)") &&
            filas[0].textoNombre.text.Contains("×"), "Fila dinámica del caballo propio");
        filas[1].OnClick();
        Exigir(vista.textoEdad.text.Contains("Edad: 3") && vista.textoNivel.text.Contains("Nivel: 2") &&
            vista.textoVelocidad.text.Contains("F+"), "Datos del primer homónimo");
        filas[2].OnClick();
        Exigir(vista.textoEdad.text.Contains("Edad: 7") && vista.textoNivel.text.Contains("Nivel: 4") &&
            vista.textoVelocidad.text.Contains("S+") && !filas[1].textoNombre.text.StartsWith(">") &&
            filas[2].textoNombre.text.StartsWith(">"), "Selección por ID actualiza datos y marca");
        string parrillaAntesDeVolver = JsonUtility.ToJson(carrera);
        var volver = vista.transform.parent.Find("TablonRustico/Header/BackButton")
            .GetComponent<UnityEngine.UI.Button>();
        volver.onClick.Invoke();
        Exigir(!UIManager.instance.viewTablon.activeSelf &&
            JsonUtility.ToJson(juego.ObtenerCarreraProgramada()) == parrillaAntesDeVolver, "Volver conserva parrilla y cuotas");
        UIManager.instance.AbrirTablon();
        Exigir(vista.textoEdad.text.Contains("Edad: 7"), "Reabrir conserva la selección por ID");
        Exigir(!juego.Apostar(carrera.id, jugador.caballo.id, juego.dinero + 1, out _), "Bloquea saldo insuficiente");
        Exigir(!juego.Apostar(carrera.id, "ausente", 1, out _), "Bloquea participante ajeno");
        vista.content.Cast<Transform>().First(t => t.gameObject.activeSelf).GetComponent<CaballoButtonUI>().OnClick();
        vista.importe.text = "100";
        Exigir(vista.textoPotencial.text == $"Pago estimado: ${jugador.cuotaDecimas * 10}", "Pago total dinámico");
        int saldo = juego.dinero;
        vista.apostar.onClick.Invoke();
        Exigir(vista.confirmar.gameObject.activeSelf && juego.dinero == saldo, "Vista previa sin cargo");
        vista.confirmar.onClick.Invoke();
        Exigir(carrera.apuesta?.cantidad == 100 && juego.dinero == saldo - 100, "Un cargo al confirmar");
        vista.CambiarCarrera();
        Exigir(UIManager.instance.viewTablon.activeSelf &&
            !vista.transform.parent.Find("TablonRustico/Header/ChangeRaceButton").GetComponent<UnityEngine.UI.Button>().interactable,
            "F: una apuesta confirmada bloquea el control de cambio");
        UIManager.instance.AbrirSeleccionCarrera();
        Exigir(UIManager.instance.viewTablon.activeSelf, "F: el acceso directo al selector respeta el bloqueo");
        Exigir(juego.ObtenerOfertaCarreras().Single().id == carrera.idDefinicion &&
            !juego.ElegirCarrera("distinta", out _), "La apuesta bloquea cambios de carrera");
        Exigir(!juego.Apostar(carrera.id, jugador.caballo.id, 100, out _), "Bloquea segunda apuesta");
        string parrilla = JsonUtility.ToJson(carrera);
        Recargar(juego);
        carrera = juego.ObtenerCarreraProgramada();
        Exigir(JsonUtility.ToJson(carrera) == parrilla && juego.dinero == saldo - 100, "Apuesta, cuotas y parrilla sobreviven a carga");
        Exigir(juego.IniciarCarreraProgramada(carrera), "Inicio guardado");
        var ganador = juego.CrearVistaParticipante(carrera.participantes[0]);
        var rival = juego.CrearVistaParticipante(carrera.participantes[1]);
        long esperado = saldo - 100 + carrera.apuesta.Pago + carrera.PremioGanador;
        juego.FinalizarCarreraProgramada(carrera, ganador, rival);
        Exigir(juego.dinero == esperado && juego.UltimaApuesta.estado == EstadoApuesta.Ganada, "Premio y apuesta separados");
        juego.FinalizarCarreraProgramada(carrera, ganador, rival);
        Exigir(juego.dinero == esperado, "Liquidación idempotente");
        Recargar(juego);
        Exigir(juego.dinero == esperado && juego.UltimaApuesta.estado == EstadoApuesta.Ganada, "Sin segundo pago al cargar");
    }

    static void VerificarGeometriaTablon(TablonView vista)
    {
        var root = (RectTransform)vista.transform.parent;
        var board = (RectTransform)root.Find("TablonRustico");
        var marco = board.GetComponent<UnityEngine.UI.Image>();
        var madera = board.Find("WoodenBackground/Planks").GetComponent<UnityEngine.UI.RawImage>();
        Exigir(marco.type == UnityEngine.UI.Image.Type.Sliced && marco.sprite.border.sqrMagnitude > 0,
            "Marco con esquinas protegidas");
        Exigir(madera.texture == UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>("Assets/Art/RaceSelection/MaderaFondo.png").texture &&
            madera.material.shader.name == "UI/Default", "Madera compartida con el selector y proporciones conservadas");
        var content = (RectTransform)vista.content;
        var scroll = content.parent.GetComponent<UnityEngine.UI.ScrollRect>();
        var anteriorMin = root.anchorMin;
        var anteriorMax = root.anchorMax;
        var anteriorOffsetMin = root.offsetMin;
        var anteriorOffsetMax = root.offsetMax;
        var ultimaFila = content.GetChild(content.childCount - 1).gameObject;
        try
        {
            var resoluciones = new[] {new Vector2(1920, 1080), new Vector2(1600, 900),
                new Vector2(1366, 768), new Vector2(1280, 720), new Vector2(2560, 1080)};
            foreach (var resolucion in resoluciones)
            {
                float escala = Mathf.Sqrt(resolucion.x / 1920f * resolucion.y / 1080f);
                root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
                root.anchoredPosition = Vector2.zero;
                root.sizeDelta = resolucion / escala;
                Canvas.ForceUpdateCanvases();
                Exigir(Mathf.Abs(board.rect.width - root.rect.width) < .1f &&
                    Mathf.Abs(board.rect.height - root.rect.height) < .1f &&
                    board.anchorMin == Vector2.zero && board.anchorMax == Vector2.one,
                    "Tablón ocupa todo el viewport a " + resolucion);
                foreach (string nombre in new[] {"Header", "ParticipantsPanel", "HorseDetailsPanel", "BettingFooter"})
                {
                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(board, board.Find(nombre));
                    Exigir(board.rect.Contains(bounds.min) && board.rect.Contains(bounds.max), nombre + " dentro del marco");
                }
                ultimaFila.SetActive(false);
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                Exigir(content.rect.height <= scroll.viewport.rect.height, "Cinco filas visibles");
                ultimaFila.SetActive(true);
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                Exigir(scroll.vertical && content.rect.height > scroll.viewport.rect.height, "Seis filas con desplazamiento");
            }
        }
        finally
        {
            ultimaFila.SetActive(true);
            root.anchorMin = anteriorMin;
            root.anchorMax = anteriorMax;
            root.offsetMin = anteriorOffsetMin;
            root.offsetMax = anteriorOffsetMax;
            Canvas.ForceUpdateCanvases();
        }
        Debug.Log("TABLÓN PASS: marco de borde a borde en 1920×1080, 1600×900, 1366×768, 1280×720 y 2560×1080.");
    }

    static void PrepararCarreraReal(GameManager juego)
    {
        Exigir(juego.ElegirCarrera(juego.ObtenerOfertaCarreras().Last().id, out _), "Elegir carrera para simulación");
        var carrera = juego.ObtenerCarreraProgramada();
        Exigir(juego.Apostar(carrera.id, carrera.participantes[0].caballo.id, 100, out _), "Apuesta para carrera real");
        saldoAntes = juego.dinero;
        apuestaEnCurso = carrera.apuesta;
        premioEnCurso = carrera.PremioGanador;
        UIManager.instance.AbrirCarrera();
        var vista = UIManager.instance.viewCarrera.GetComponentInChildren<CarreraUI>();
        vista.IniciarCarrera();
        Exigir(carrera.iniciada, "Arranque de simulación original");
    }

    static void VerificarCarreraAnterior(GameManager juego)
    {
        Exigir(juego.ElegirCarrera(juego.ObtenerOfertaCarreras()[0].id, out _), "Preparar fixture anterior");
        var carrera = juego.ObtenerCarreraProgramada();
        Exigir(juego.Apostar(carrera.id, carrera.participantes[0].caballo.id, 1, out _), "Guardar la apuesta de la fixture");
        Exigir(SaveSystem.IntentarCargar(out var datos), "Leer guardado para simular campos ausentes");
        datos.carreraProgramada.idDefinicion = null;
        datos.carreraProgramada.nombreCarrera = null;
        datos.carreraProgramada.distanciaMetros = 0;
        datos.carreraProgramada.premio = 0;
        Exigir(SaveSystem.Guardar(datos), "Escribir fixture anterior");
        Recargar(juego);
        var anterior = juego.ObtenerCarreraProgramada();
        var opcion = juego.ObtenerOfertaCarreras().Single();
        Exigir(anterior.id == carrera.id && anterior.apuesta.idCaballo == carrera.apuesta.idCaballo &&
            anterior.PremioGanador == 500 && juego.ElegirCarrera(opcion.id, out _) &&
            ReferenceEquals(anterior, juego.ObtenerCarreraProgramada()), "Apuesta y premio de guardado anterior");
    }

    static void PrepararCarreraSinApuesta(GameManager juego)
    {
        Exigir(juego.ObtenerCarreraProgramada() == null && juego.caballosDelDia.Count == 0,
            "G: la carrera finalizada deja libre el nuevo día");
        juego.SetState(Interactable.Tipo.TV);
        Exigir(juego.estadoActual == GameState.Hub && !UIManager.instance.viewCarrera.activeSelf,
            "G: el televisor no repite la carrera terminada");
        juego.SetState(Interactable.Tipo.Tablon);
        var selector = UnityEngine.Object.FindFirstObjectByType<SeleccionCarreraView>();
        var oferta = juego.ObtenerOfertaCarreras()[0];
        selector.transform.Find("Ofertas/Viewport/Lista/Carrera_" + oferta.id).GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        selector.transform.Find("Participar").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        carreraSinApuesta = juego.ObtenerCarreraProgramada();
        saldoSinApuesta = juego.dinero;
        resultadoAnterior = JsonUtility.ToJson(juego.UltimaApuesta);
        var tablon = UIManager.instance.viewTablon.GetComponentInChildren<TablonView>();
        Exigir(carreraSinApuesta.apuesta == null && juego.dinero == saldoSinApuesta &&
            !tablon.transform.parent.Find("TablonRustico/BettingFooter/FeedbackText").GetComponent<TMPro.TMP_Text>().text.Contains("Apuesta confirmada"),
            "B: consultar sin apostar y sin mensajes pendientes de la carrera anterior");
        juego.VolverAlHub();
        juego.SetState(Interactable.Tipo.TV);
        var carrera = UIManager.instance.viewCarrera.GetComponentInChildren<CarreraUI>();
        carrera.IniciarCarrera();
        Exigir(carreraSinApuesta.iniciada && carreraSinApuesta.apuesta == null, "B: carrera real sin apuesta");
    }

    static void VerificarResultadoSinApuesta(GameManager juego)
    {
        var pantalla = UIManager.instance.viewCarrera.GetComponentInChildren<CarreraUI>();
        int balance = pantalla.textoEstado.text.Contains("Premio de carrera") ? carreraSinApuesta.PremioGanador : -200;
        Exigir(juego.dinero == saldoSinApuesta + balance &&
            JsonUtility.ToJson(juego.UltimaApuesta) == resultadoAnterior, "B: resultado sin débito ni pago de apuesta");
        int saldoFinal = juego.dinero;
        var ganador = juego.CrearVistaParticipante(carreraSinApuesta.participantes[0]);
        juego.FinalizarCarreraProgramada(carreraSinApuesta, ganador, null);
        Exigir(juego.dinero == saldoFinal && !juego.IniciarCarreraProgramada(carreraSinApuesta) &&
            juego.ObtenerCarreraProgramada() == null, "G: no se repiten premios ni la carrera anterior");
        Recargar(juego);
        Exigir(juego.dinero == saldoFinal && juego.ObtenerCarreraProgramada() == null &&
            juego.ObtenerOfertaCarreras().Count > 0, "G: nueva oferta y resultado guardados");
    }

    static void Recargar(GameManager juego) =>
        Exigir((bool)typeof(GameManager).GetMethod("CargarPartida", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(juego, null), "Carga JSON");

    static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new InvalidOperationException(mensaje);
    }

    static void Salir(int codigo)
    {
        SessionState.SetBool(Activa, false);
        EditorApplication.Exit(codigo);
    }
}
