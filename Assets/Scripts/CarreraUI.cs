using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public sealed class EstadoCaballoCarrera
{
    // Conversión de la estadística Velocidad (1-16) a unidades de carrera por segundo.
    const float VELOCIDAD_MAXIMA_F = 8f;
    const float VELOCIDAD_MAXIMA_EX = 18f;
    const float ACELERACION = 7f;
    const float DESACELERACION = 10f;
    const float RITMO_MINIMO = 0.30f;

    // Balance de energía y recuperación.
    const float ENERGIA_MAXIMA = 100f;
    const float RITMO_SOSTENIBLE_F = 0.56f;
    const float RITMO_SOSTENIBLE_EX = 0.84f;
    const float CONSUMO_BASE = 1.2f;
    const float CONSUMO_POR_EXCESO = 14f;
    const float EFICIENCIA_MAXIMA_STAMINA = 0.75f;
    const float EFICIENCIA_MAXIMA_INTELIGENCIA = 0.25f;
    const float RECUPERACION_MINIMA = 7f;
    const float RECUPERACION_EXTRA_STAMINA = 9f;
    const float RECUPERACION_EXTRA_INTELIGENCIA = 4f;
    const float UMBRAL_RECUPERACION_F = 12f;
    const float UMBRAL_RECUPERACION_EX = 28f;
    const float ENERGIA_NECESARIA_PARA_VOLVER = 30f;

    // La inteligencia reduce el esfuerzo inicial y reserva energía para el final.
    const float AHORRO_MAXIMO_POR_INTELIGENCIA = 0.22f;
    const float INICIO_ACELERACION_FINAL = 0.65f;
    const float FIN_ACELERACION_FINAL = 0.95f;

    // Variación de forma por carrera. La distribución triangular concentra los
    // resultados cerca del valor normal y deja actuaciones malas poco frecuentes.
    const float VARIACION_MAXIMA_RITMO = 0.24f;
    const float VARIACION_MAXIMA_CONSUMO = 0.20f;
    const float VARIACION_MAXIMA_ACELERACION = 0.10f;
    const float VARIACION_MAXIMA_UMBRAL = 4f;
    const float VARIACION_MAXIMA_TRAMO_FINAL = 0.04f;

    // Eventos pequeños y espaciados: no se genera azar nuevo en cada frame.
    const float INTERVALO_SUERTE_MINIMO = 2.5f;
    const float INTERVALO_SUERTE_MAXIMO = 5f;
    const float DURACION_SUERTE_MINIMA = 0.7f;
    const float DURACION_SUERTE_MAXIMA = 1.4f;
    const float IMPULSO_SUERTE_MINIMO = 0.02f;
    const float IMPULSO_SUERTE_EXTRA = 0.02f;
    const float PERDIDA_SUERTE_MINIMA = 0.014f;
    const float PERDIDA_SUERTE_EXTRA = 0.016f;
    const float VELOCIDAD_MAXIMA_CON_SUERTE = 1.05f;

    readonly System.Random aleatorio;
    readonly float velocidadNormalizada;
    readonly float staminaNormalizada;
    readonly float suerteNormalizada;
    readonly float inteligenciaNormalizada;
    readonly float factorRitmoCarrera;
    readonly float factorConsumoCarrera;
    readonly float factorAceleracionCarrera;
    readonly float ajusteUmbralRecuperacion;
    readonly float ajusteTramoFinal;

    float tiempoHastaEventoSuerte;
    float duracionEventoSuerte;

    public Caballo caballo { get; private set; }
    public float distanciaRecorrida { get; private set; }
    public float velocidadActual { get; private set; }
    public float energia { get; private set; }
    public bool recuperando { get; private set; }
    public float modificadorTemporalSuerte { get; private set; }
    public float velocidadMaxima { get; private set; }

    public EstadoCaballoCarrera(Caballo caballo, int semillaAleatoria)
    {
        this.caballo = caballo;
        aleatorio = new System.Random(semillaAleatoria);

        velocidadNormalizada = NormalizarEstadistica(caballo.velocidad);
        staminaNormalizada = NormalizarEstadistica(caballo.stamina);
        suerteNormalizada = NormalizarEstadistica(caballo.suerte);
        inteligenciaNormalizada = NormalizarEstadistica(caballo.inteligencia);

        factorRitmoCarrera = 1f - VARIACION_MAXIMA_RITMO * ProductoAleatorio();
        factorConsumoCarrera = 1f + VARIACION_MAXIMA_CONSUMO * DiferenciaAleatoria();
        factorAceleracionCarrera = 1f + VARIACION_MAXIMA_ACELERACION * DiferenciaAleatoria();
        ajusteUmbralRecuperacion = VARIACION_MAXIMA_UMBRAL * DiferenciaAleatoria();
        ajusteTramoFinal = VARIACION_MAXIMA_TRAMO_FINAL * DiferenciaAleatoria();

        velocidadMaxima = Interpolar(VELOCIDAD_MAXIMA_F, VELOCIDAD_MAXIMA_EX, velocidadNormalizada);
        energia = ENERGIA_MAXIMA;
        tiempoHastaEventoSuerte = AleatorioEntre(INTERVALO_SUERTE_MINIMO, INTERVALO_SUERTE_MAXIMO);
    }

    public void Actualizar(float deltaTime, float distanciaCarrera, float ritmoIniciacion = 0)
    {
        if (deltaTime <= 0f || distanciaCarrera <= 0f)
            return;

        if (ritmoIniciacion > 0)
        {
            // The one-time welcome race uses frozen, proportional pace/acceleration
            // from its saved grid. Normal races retain every original formula below.
            velocidadActual = MoverHacia(velocidadActual, ritmoIniciacion, ritmoIniciacion * 2 * deltaTime);
            distanciaRecorrida += velocidadActual * deltaTime;
            return;
        }

        ActualizarEventoSuerte(deltaTime);

        float progresoNormalizado = Limitar01(distanciaRecorrida / distanciaCarrera);
        float ritmoSostenible = Interpolar(RITMO_SOSTENIBLE_F, RITMO_SOSTENIBLE_EX, staminaNormalizada);
        float umbralRecuperacion = Interpolar(
            UMBRAL_RECUPERACION_F,
            UMBRAL_RECUPERACION_EX,
            inteligenciaNormalizada) + ajusteUmbralRecuperacion;

        if (!recuperando && energia <= umbralRecuperacion)
            recuperando = true;
        else if (recuperando && energia >= umbralRecuperacion + ENERGIA_NECESARIA_PARA_VOLVER)
            recuperando = false;

        float esfuerzoObjetivo = CalcularEsfuerzoObjetivo(progresoNormalizado, ritmoSostenible);
        float rendimientoPorEnergia = CalcularRendimientoPorEnergia();
        float velocidadObjetivo = velocidadMaxima * esfuerzoObjetivo * rendimientoPorEnergia;

        velocidadObjetivo += modificadorTemporalSuerte;
        velocidadObjetivo = Limitar(
            velocidadObjetivo,
            velocidadMaxima * RITMO_MINIMO,
            velocidadMaxima * VELOCIDAD_MAXIMA_CON_SUERTE);

        float aceleracionAplicada = ACELERACION * factorAceleracionCarrera;
        float cambioMaximo = (velocidadObjetivo >= velocidadActual ? aceleracionAplicada : DESACELERACION)
            * deltaTime;
        velocidadActual = MoverHacia(velocidadActual, velocidadObjetivo, cambioMaximo);

        ActualizarEnergia(deltaTime, ritmoSostenible);
        distanciaRecorrida += velocidadActual * deltaTime;
    }

    float CalcularEsfuerzoObjetivo(float progresoNormalizado, float ritmoSostenible)
    {
        if (recuperando)
        {
            float ritmoRecuperacion = ritmoSostenible * Interpolar(0.52f, 0.68f, inteligenciaNormalizada);
            return ritmoRecuperacion * factorRitmoCarrera;
        }

        float esfuerzoInicial = 1f - AHORRO_MAXIMO_POR_INTELIGENCIA * inteligenciaNormalizada;
        float tramoFinal = SuavizarPaso(
            INICIO_ACELERACION_FINAL + ajusteTramoFinal,
            FIN_ACELERACION_FINAL + ajusteTramoFinal,
            progresoNormalizado);

        return Interpolar(esfuerzoInicial, 1f, tramoFinal) * factorRitmoCarrera;
    }

    void ActualizarEnergia(float deltaTime, float ritmoSostenible)
    {
        float intensidadActual = velocidadMaxima > 0f
            ? velocidadActual / velocidadMaxima
            : 0f;

        if (recuperando && intensidadActual < ritmoSostenible)
        {
            float margenRecuperacion = 1f - intensidadActual / ritmoSostenible;
            float recuperacionPorSegundo = RECUPERACION_MINIMA
                + RECUPERACION_EXTRA_STAMINA * staminaNormalizada
                + RECUPERACION_EXTRA_INTELIGENCIA * inteligenciaNormalizada;

            energia += recuperacionPorSegundo * (0.15f + margenRecuperacion) * deltaTime;
        }
        else
        {
            float exceso = Maximo(0f, intensidadActual - ritmoSostenible);
            float consumo = CONSUMO_BASE * intensidadActual + CONSUMO_POR_EXCESO * exceso;
            float eficienciaStamina = 1f - EFICIENCIA_MAXIMA_STAMINA * staminaNormalizada;
            float eficienciaInteligencia = 1f - EFICIENCIA_MAXIMA_INTELIGENCIA * inteligenciaNormalizada;

            energia -= consumo
                * eficienciaStamina
                * eficienciaInteligencia
                * factorConsumoCarrera
                * deltaTime;
        }

        energia = Limitar(energia, 0f, ENERGIA_MAXIMA);
    }

    float CalcularRendimientoPorEnergia()
    {
        if (energia >= 60f)
            return 1f;

        if (energia >= 30f)
            return Interpolar(0.78f, 1f, (energia - 30f) / 30f);

        if (energia >= 10f)
            return Interpolar(0.55f, 0.78f, (energia - 10f) / 20f);

        return Interpolar(0.38f, 0.55f, energia / 10f);
    }

    void ActualizarEventoSuerte(float deltaTime)
    {
        if (duracionEventoSuerte > 0f)
        {
            duracionEventoSuerte -= deltaTime;

            if (duracionEventoSuerte <= 0f)
                modificadorTemporalSuerte = 0f;
        }

        tiempoHastaEventoSuerte -= deltaTime;

        if (tiempoHastaEventoSuerte > 0f)
            return;

        float probabilidadPositiva = 0.47f + 0.08f * suerteNormalizada;
        bool eventoPositivo = aleatorio.NextDouble() < probabilidadPositiva;

        if (eventoPositivo)
        {
            float intensidad = IMPULSO_SUERTE_MINIMO + IMPULSO_SUERTE_EXTRA * suerteNormalizada;
            modificadorTemporalSuerte = velocidadMaxima * intensidad * AleatorioEntre(0.7f, 1.3f);
        }
        else
        {
            float intensidad = PERDIDA_SUERTE_MINIMA + PERDIDA_SUERTE_EXTRA * (1f - suerteNormalizada);
            modificadorTemporalSuerte = -velocidadMaxima * intensidad * AleatorioEntre(0.7f, 1.3f);
        }

        duracionEventoSuerte = AleatorioEntre(DURACION_SUERTE_MINIMA, DURACION_SUERTE_MAXIMA);
        tiempoHastaEventoSuerte += AleatorioEntre(INTERVALO_SUERTE_MINIMO, INTERVALO_SUERTE_MAXIMO);
    }

    float AleatorioEntre(float minimo, float maximo)
    {
        return minimo + (float)aleatorio.NextDouble() * (maximo - minimo);
    }

    float ProductoAleatorio()
    {
        return (float)(aleatorio.NextDouble() * aleatorio.NextDouble());
    }

    float DiferenciaAleatoria()
    {
        return (float)(aleatorio.NextDouble() - aleatorio.NextDouble());
    }

    static float NormalizarEstadistica(int valor)
    {
        int limitado = valor < Caballo.EstadisticaMinima
            ? Caballo.EstadisticaMinima
            : valor > Caballo.EstadisticaMaxima
                ? Caballo.EstadisticaMaxima
                : valor;

        return (limitado - Caballo.EstadisticaMinima) /
               (float)(Caballo.EstadisticaMaxima - Caballo.EstadisticaMinima);
    }

    static float SuavizarPaso(float bordeInicial, float bordeFinal, float valor)
    {
        float t = Limitar01((valor - bordeInicial) / (bordeFinal - bordeInicial));
        return t * t * (3f - 2f * t);
    }

    static float MoverHacia(float actual, float objetivo, float cambioMaximo)
    {
        float diferencia = objetivo - actual;

        if (Absoluto(diferencia) <= cambioMaximo)
            return objetivo;

        return actual + (diferencia > 0f ? cambioMaximo : -cambioMaximo);
    }

    static float Interpolar(float desde, float hasta, float t)
    {
        return desde + (hasta - desde) * Limitar01(t);
    }

    static float Limitar01(float valor)
    {
        return Limitar(valor, 0f, 1f);
    }

    static float Limitar(float valor, float minimo, float maximo)
    {
        return valor < minimo ? minimo : valor > maximo ? maximo : valor;
    }

    static float Maximo(float a, float b)
    {
        return a > b ? a : b;
    }

    static float Absoluto(float valor)
    {
        return valor < 0f ? -valor : valor;
    }
}

public class CarreraUI : MonoBehaviour
{
    [Header("Track")]
    public RectTransform track;

    [Header("Prefab UI")]
    public GameObject caballoPrefab;
    public Transform contenedorCaballos;

    [Header("UI")]
    public TextMeshProUGUI textoEstado;
    public GameObject botonIniciar;
    public GameObject botonVolver;

    [Header("Datos")]
    List<Caballo> caballosData = new List<Caballo>();

    List<RectTransform> caballosUI = new List<RectTransform>();
    List<Image> caballosImg = new List<Image>();

    List<EstadoCaballoCarrera> estadosCarrera = new List<EstadoCaballoCarrera>();

    readonly List<HorseVisualAnimator> visualAnimators = new List<HorseVisualAnimator>();
    bool carreraActiva;
    Caballo caballoJugadorParticipante;
    CarreraProgramada carreraPreparada;
    ClasificacionCarrera clasificacion;
    ResultadosCarreraView resultadosView;
    readonly List<AvanceCarrera> avances = new List<AvanceCarrera>();
    PistaLateralCarrera pista;
    float acumuladoSimulacion;
    const float PasoSimulacion = .02f;

    [Header("Simulación")]
    [SerializeField] float distanciaCarrera = 100f;

    void OnEnable()
    {
        PrepararCarrera();
    }

    internal bool EnCurso => carreraActiva;

    void PrepararCarrera()
    {
        carreraActiva = false;
        clasificacion = null;
        resultadosView?.Ocultar();

        textoEstado.text = "Listo para correr";

        botonIniciar.SetActive(true);
        botonVolver.SetActive(true);

        caballosData.Clear();
        caballoJugadorParticipante = null;

        var juego = GameManager.Instancia;
        carreraPreparada = juego.ObtenerCarreraProgramada();
        if (carreraPreparada != null)
        {
            foreach (var participante in carreraPreparada.participantes)
            {
                var caballo = juego.CrearVistaParticipante(participante);
                caballosData.Add(caballo);
                if (caballo.esDelJugador) caballoJugadorParticipante = caballo;
            }
            textoEstado.text = carreraPreparada.iniciada
                ? "Carrera pendiente de reanudar · " + carreraPreparada.NombreVisible
                : carreraPreparada.NombreVisible + " · Listo para correr";
        }
        else
        {
            botonIniciar.SetActive(false);
            textoEstado.text = juego.campanaCompletada ? "Campaña completada" : "Elegí una carrera en el tablón de apuestas";
        }
        CrearCaballosUI();
    }

    void CrearCaballosUI()
    {
        pista ??= new PistaLateralCarrera(this);
        pista.Preparar(carreraPreparada, carreraPreparada == null ? 100 : DistanciaSimulada);
        contenedorCaballos = pista.caballos;
        foreach (Transform child in contenedorCaballos)
            Destroy(child.gameObject);

        visualAnimators.Clear();
        caballosUI.Clear();
        caballosImg.Clear();
        estadosCarrera.Clear();

        for (int i = 0; i < caballosData.Count; i++)
        {
            GameObject obj = Instantiate(caballoPrefab, contenedorCaballos);

            RectTransform ui = obj.GetComponent<RectTransform>();

            //  FIX IMPORTANTE: buscar Image en hijos
            Image img = obj.GetComponentInChildren<Image>(true);

            var visual = obj.AddComponent<HorseVisualAnimator>();
            visual.Bind(caballosData[i], img, true);
            visualAnimators.Add(visual);
            caballosUI.Add(ui);
            caballosImg.Add(img);

            ui.anchorMin = ui.anchorMax = Vector2.zero;
            ui.pivot = new Vector2(.5f, 0);
            ui.localScale = Vector3.one;
            ui.sizeDelta = new Vector2(228, PresentacionCaballoCarrera.AlturaVisible);
            ui.anchoredPosition = new Vector2(pista.mapa.largada,
                24 + (caballosData.Count - 1 - i) * PresentacionCaballoCarrera.SeparacionCarriles);
            if (img != null)
            {
                img.raycastTarget = false;
            }
            if (caballosData[i].id == caballoJugadorParticipante?.id) pista.VincularJugador(ui);
            pista.MarcarReferencia(ui, caballosData[i].id == caballoJugadorParticipante?.id);

            //  ASIGNAR SPRITE SEGURO
            if (img != null)
            {
                if (caballosData[i].sprite != null)
                {
                    visual.StopRun();
                    img.enabled = true;
                }
                else
                {
                    Debug.LogWarning("Caballo sin sprite: " + caballosData[i].nombre);
                }
            }
            else
            {
                Debug.LogError("Prefab no tiene Image en hijos!");
            }
        }
    }

    public void IniciarCarrera()
    {
        if (carreraActiva) return;
        if (GameManager.Instancia.campanaCompletada)
        {
            Debug.LogWarning("La campaña ya está completada. No se puede iniciar una carrera del día 43.");
            return;
        }

        if (carreraPreparada != GameManager.Instancia.ObtenerCarreraProgramada())
            PrepararCarrera();

        if (caballosData.Count == 0)
            return;

        estadosCarrera.Clear();

        for (int i = 0; i < caballosData.Count; i++)
        {
            estadosCarrera.Add(new EstadoCaballoCarrera(
                caballosData[i],
                carreraPreparada.participantes[i].semilla));

            Vector2 posicionInicial = caballosUI[i].anchoredPosition;
            posicionInicial.x = pista.mapa.largada;
            caballosUI[i].anchoredPosition = posicionInicial;
        }

        var ids = new List<string>();
        foreach (var caballo in caballosData) ids.Add(caballo.id);
        clasificacion = new ClasificacionCarrera(ids, DistanciaSimulada, mensaje => Debug.LogWarning(mensaje));
        if (!GameManager.Instancia.IniciarCarreraProgramada(carreraPreparada))
        {
            textoEstado.text = GameManager.Instancia.ErrorInicioCarrera ?? "No se pudo guardar el inicio de la carrera.";
            return;
        }
        acumuladoSimulacion = 0;
        pista.Iniciar();
        foreach (var visual in visualAnimators) visual.StartRun();
        carreraActiva = true;

        botonIniciar.SetActive(false);
        botonVolver.SetActive(false);

        textoEstado.text = "¡Carrera iniciada!";
    }

    float DistanciaSimulada => Mathf.Max(1f, distanciaCarrera *
        (carreraPreparada.distanciaMetros > 0 ? carreraPreparada.distanciaMetros / 1200f : 1f));

    void Update()
    {
        if (!MenuPausa.Activa)
        {
            AvanzarCarrera(Time.deltaTime);
            ActualizarPresentacion(Time.deltaTime, Input.GetKey(KeyCode.A));
        }
        resultadosView?.ActualizarGuardado();
    }

    internal void AvanzarCarrera(float dt)
    {
        if (!carreraActiva || GameManager.Instancia.Derrotado || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
        if (carreraPreparada.versionReglas <= 0)
        {
            // Already prepared legacy races retain their previous integration cadence.
            SimularPaso(dt);
            return;
        }
        acumuladoSimulacion += dt;
        // Fixed simulation ticks make the preconfigured welcome pace and normal
        // crossings independent of rendering FPS. Pause does not add time.
        while (acumuladoSimulacion + .000001f >= PasoSimulacion && carreraActiva)
        {
            acumuladoSimulacion = Mathf.Max(0, acumuladoSimulacion - PasoSimulacion);
            SimularPaso(PasoSimulacion);
        }
    }

    void SimularPaso(float dt)
    {
        avances.Clear();
        for (int i = 0; i < estadosCarrera.Count; i++)
        {
            var estado = estadosCarrera[i];
            if (clasificacion.Resuelto(estado.caballo.id)) continue;
            float anterior = estado.distanciaRecorrida;
            estado.Actualizar(dt, DistanciaSimulada, carreraPreparada.iniciacion ? carreraPreparada.Buscar(estado.caballo.id).ritmoIniciacion : 0);
            avances.Add(new AvanceCarrera(estado.caballo.id, anterior, estado.distanciaRecorrida));
            if (!float.IsNaN(estado.distanciaRecorrida) && !float.IsInfinity(estado.distanciaRecorrida))
            {
                Vector2 posicion = caballosUI[i].anchoredPosition;
                posicion.x = pista.mapa.X(estado.distanciaRecorrida);
                caballosUI[i].anchoredPosition = posicion;
            }
        }
        clasificacion.Avanzar(dt, avances);
        foreach (var avance in avances)
            if (clasificacion.Resuelto(avance.id))
            {
                int indice = caballosData.FindIndex(c => c.id == avance.id);
                visualAnimators[indice].StopRun();
            }
        if (clasificacion.Completa) { Terminar(); return; }
        textoEstado.text = carreraPreparada.NombreVisible + $" · {carreraPreparada.distanciaMetros} m\nLlegadas: {clasificacion.Resultados.Count} de {caballosData.Count}";
        foreach (var llegada in clasificacion.Resultados)
            if (llegada.idCaballo == caballoJugadorParticipante?.id)
                textoEstado.text += $" · Tu caballo: {llegada.posicion}.º · Esperando a los demás";
    }

    internal void ActualizarPresentacion(float dt, bool teclaA)
    {
        if (pista == null || caballoJugadorParticipante == null) return;
        int indice = caballosData.FindIndex(c => c.id == caballoJugadorParticipante.id);
        if (indice < 0) return;
        bool campoEnEdicion = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject?.GetComponent<TMP_InputField>()?.isFocused == true;
        pista.Actualizar(dt, caballosUI[indice].anchoredPosition.x,
            clasificacion?.Resuelto(caballoJugadorParticipante.id) == true, teclaA && !campoEnEdicion && carreraActiva);
    }

    void OnDestroy() => pista?.Liberar();

    void Terminar()
    {
        if (!carreraActiva || !clasificacion.Completa) return;
        carreraPreparada.clasificacionFinal = new List<LlegadaCarrera>(clasificacion.Resultados);
        if (!ClasificacionCarrera.Validar(carreraPreparada, carreraPreparada.clasificacionFinal))
        {
            Debug.LogError("Clasificación incompleta o inválida. No se liquidó la carrera.");
            return;
        }
        carreraActiva = false;
        pista.Finalizar();
        foreach (var visual in visualAnimators) visual.StopRun();
        var primero = carreraPreparada.clasificacionFinal[0];
        Caballo ganador = primero.Finalizo ? caballosData.Find(c => c.id == primero.idCaballo) : null;
        bool ganoJugador = EsCaballoJugadorParticipante(ganador);
        string resultado = GameManager.Instancia.FinalizarCarreraProgramada(carreraPreparada, ganador, null);
        textoEstado.text = ganador == null ? "Sin ganador · Carrera anulada por incidencias"
            : "Ganó: " + ganador.nombre + (ganoJugador ? $" · Premio de carrera +{Moneda.Texto(carreraPreparada.PremioGanador)}"
                : carreraPreparada.versionReglas > 0 ? " · Sin premio de carrera" : " · Balance de carrera −$200");
        if (carreraPreparada.versionReglas > 0) textoEstado.text += $" · Inscripción abonada {Moneda.Texto(carreraPreparada.inscripcion)}";
        if (!string.IsNullOrEmpty(resultado)) textoEstado.text += " · " + resultado;
        textoEstado.enableAutoSizing = true;
        textoEstado.fontSizeMax = 36;
        botonIniciar.SetActive(false);
        botonVolver.SetActive(false);
        resultadosView ??= new ResultadosCarreraView(this);
        resultadosView.Mostrar(GameManager.Instancia.UltimoResultadoCarrera, textoEstado.text);
    }

    bool EsCaballoJugadorParticipante(Caballo caballo)
    {
        if (caballo == null || caballoJugadorParticipante == null)
            return false;

        if (ReferenceEquals(caballo, caballoJugadorParticipante))
            return true;

        return !string.IsNullOrEmpty(caballo.id) &&
               caballo.id == caballoJugadorParticipante.id;
    }

    public void Volver()
    {
        if (carreraActiva || !GameManager.Instancia.ResultadoCarreraGuardado) return;
        GameManager.Instancia.CerrarResultadosDerrota();
        if (GameManager.Instancia.Derrotado) { GameManager.Instancia.CerrarResultadosDerrota(); return; }
        GameManager.Instancia.VolverAlHub();
    }
}
