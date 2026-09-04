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

    public void Actualizar(float deltaTime, float distanciaCarrera)
    {
        if (deltaTime <= 0f || distanciaCarrera <= 0f)
            return;

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

    bool carreraActiva;
    Caballo caballoJugadorParticipante;
    int diaPreparado;

    const float POS_INICIO = -1000f;
    const float META_X = 1000f;

    [Header("Simulación")]
    [SerializeField] float distanciaCarrera = 100f;

    Vector2[] posicionesIniciales =
    {
        new Vector2(-1000, 60),
        new Vector2(-1000, 210),
        new Vector2(-1000, 350),
        new Vector2(-1000, -80),
        new Vector2(-1000, -230),
        new Vector2(-1000, -380)
    };

    void OnEnable()
    {
        PrepararCarrera();
    }

    void PrepararCarrera()
    {
        carreraActiva = false;

        textoEstado.text = "Listo para correr";

        botonIniciar.SetActive(true);
        botonVolver.SetActive(true);

        caballosData.Clear();
        caballoJugadorParticipante = null;

        if (GameManager.Instancia.caballosJugador.Count > 0)
        {
            caballoJugadorParticipante = GameManager.Instancia.caballosJugador[0];

            if (caballoJugadorParticipante != null)
                caballosData.Add(caballoJugadorParticipante);
        }

        caballosData.AddRange(GameManager.Instancia.caballosDelDia);
        diaPreparado = GameManager.Instancia.dia;

        CrearCaballosUI();
    }

    void CrearCaballosUI()
    {
        foreach (Transform child in contenedorCaballos)
            Destroy(child.gameObject);

        caballosUI.Clear();
        caballosImg.Clear();
        estadosCarrera.Clear();

        for (int i = 0; i < caballosData.Count; i++)
        {
            GameObject obj = Instantiate(caballoPrefab, contenedorCaballos);

            RectTransform ui = obj.GetComponent<RectTransform>();

            //  FIX IMPORTANTE: buscar Image en hijos
            Image img = obj.GetComponentInChildren<Image>(true);

            caballosUI.Add(ui);
            caballosImg.Add(img);

            ui.anchoredPosition = posicionesIniciales[Mathf.Min(i, posicionesIniciales.Length - 1)];

            //  ASIGNAR SPRITE SEGURO
            if (img != null)
            {
                if (caballosData[i].sprite != null)
                {
                    img.sprite = caballosData[i].sprite;
                    img.preserveAspect = true;
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
        if (GameManager.Instancia.campanaCompletada)
        {
            Debug.LogWarning("La campaña ya está completada. No se puede iniciar una carrera del día 43.");
            return;
        }

        if (diaPreparado != GameManager.Instancia.dia)
            PrepararCarrera();

        if (caballosData.Count == 0)
            return;

        estadosCarrera.Clear();

        for (int i = 0; i < caballosData.Count; i++)
        {
            estadosCarrera.Add(new EstadoCaballoCarrera(
                caballosData[i],
                Random.Range(1, int.MaxValue)));

            Vector2 posicionInicial = caballosUI[i].anchoredPosition;
            posicionInicial.x = POS_INICIO;
            caballosUI[i].anchoredPosition = posicionInicial;
        }

        carreraActiva = true;

        botonIniciar.SetActive(false);
        botonVolver.SetActive(false);

        textoEstado.text = "¡Carrera iniciada!";
    }

    void Update()
    {
        if (!carreraActiva) return;

        float dt = Time.deltaTime;
        float distanciaValida = Mathf.Max(1f, distanciaCarrera);
        int ganador = -1;
        float menorFraccionDeLlegada = float.PositiveInfinity;

        for (int i = 0; i < estadosCarrera.Count; i++)
        {
            EstadoCaballoCarrera estado = estadosCarrera[i];
            float distanciaAnterior = estado.distanciaRecorrida;

            estado.Actualizar(dt, distanciaValida);

            float t = Mathf.Clamp01(estado.distanciaRecorrida / distanciaValida);
            float x = Mathf.Lerp(POS_INICIO, META_X, t);

            Vector2 pos = caballosUI[i].anchoredPosition;
            pos.x = x;
            caballosUI[i].anchoredPosition = pos;

            if (estado.distanciaRecorrida >= distanciaValida)
            {
                float avanceDelFrame = estado.distanciaRecorrida - distanciaAnterior;
                float fraccionDeLlegada = avanceDelFrame > 0f
                    ? (distanciaValida - distanciaAnterior) / avanceDelFrame
                    : 1f;

                if (fraccionDeLlegada < menorFraccionDeLlegada)
                {
                    ganador = i;
                    menorFraccionDeLlegada = fraccionDeLlegada;
                }
            }
        }

        if (ganador >= 0)
            Terminar(ganador);
    }

    void Terminar(int ganador)
    {
        if (!carreraActiva)
            return;

        carreraActiva = false;

        Caballo caballoGanador = caballosData[ganador];
        bool ganoJugador = EsCaballoJugadorParticipante(caballoGanador);
        Caballo mejorRival = ganoJugador
            ? ObtenerMejorRivalPorProgreso()
            : caballoGanador;

        GameManager.Instancia.EstablecerRivalPersistente(mejorRival);

        textoEstado.text = "Ganó: " + caballoGanador.nombre;

        if (ganoJugador)
            GameManager.Instancia.AgregarDinero(500);
        else
            GameManager.Instancia.RestarDinero(200);

        if (caballoJugadorParticipante != null)
        {
            int experienciaGanada = ganoJugador ? 100 : 25;
            ProgresionCaballo.AgregarExperiencia(caballoJugadorParticipante, experienciaGanada);

            Debug.Log(caballoJugadorParticipante.nombre + " obtuvo " + experienciaGanada +
                      (ganoJugador ? " XP por victoria." : " XP por participar."));
        }

        GameManager.Instancia.SiguienteDia();

        botonIniciar.SetActive(true);
        botonVolver.SetActive(true);
    }

    Caballo ObtenerMejorRivalPorProgreso()
    {
        Caballo mejorRival = null;
        float mayorProgreso = float.NegativeInfinity;

        for (int i = 0; i < caballosData.Count; i++)
        {
            Caballo candidato = caballosData[i];

            if (candidato == null || EsCaballoJugadorParticipante(candidato))
                continue;

            float progresoCandidato = i < estadosCarrera.Count
                ? estadosCarrera[i].distanciaRecorrida
                : float.NegativeInfinity;

            if (mejorRival == null || progresoCandidato > mayorProgreso)
            {
                mejorRival = candidato;
                mayorProgreso = progresoCandidato;
            }
        }

        return mejorRival;
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
        if (carreraActiva) return;
        GameManager.Instancia.VolverAlHub();
    }
}
