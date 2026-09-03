using UnityEngine;
using TMPro;
using System.Collections.Generic;

public enum GameState
{
    Hub,
    TV,
    Tienda,
    Establo,
    Tablon
}

public class GameManager : MonoBehaviour
{
    private const int DineroInicial = 1000;
    private const int DiaInicial = 1;
    private const int EdadMinima = 3;
    private const int EdadMaximaExclusiva = 8;
    private const int EstadisticaRivalMinima = 1;
    private const int EstadisticaRivalMaximaExclusiva = 7;
    private const int PotencialRivalMinimo = 5;
    private const int PotencialRivalMaximoExclusivo = 15;

    public static GameManager Instancia;

    [Header("Estado")]
    public GameState estadoActual;

    [Header("Economía")]
    public int dinero = DineroInicial;
    public int dia = DiaInicial;

    [Header("Caballos Jugador")]
    public List<Caballo> caballosJugador = new List<Caballo>();

    [Header("Caballos del Día")]
    public List<Caballo> caballosDelDia = new List<Caballo>();

    [Header("Rival Persistente")]
    public Caballo rivalPersistente;

    [Header("Sprites Caballos (IA + jugador)")]
    public Sprite[] spritesCaballos;

    private List<Sprite> spritesDisponibles = new List<Sprite>();

    [Header("HUD")]
    public TextMeshProUGUI textoDinero;
    public TextMeshProUGUI textoDia;

    private string[] nombres = new string[]
    {
        "Relámpago","Tormenta","Centella","Pegaso","Trueno","Sombra","Rayo",
        "Luna Negra","Furia","Neblina","Tornado","Azabache","Cometa","Mistic",
        "Gitano","Alba","Eclipse","Fénix","Bandido","Céfiro","Gala","Lucero",
        "Indio","Apolo","Duque","Marqués","Capitán","Valiente","Aquiles",
        "Hércules","Odín","Thor","Ares","Zafiro","Rubí","Esmeralda","Diamante"
    };

    private List<string> nombresDisponibles = new List<string>();

    private void Awake()
    {
        Instancia = this;
    }

    private void Start()
    {
        ResetSprites();

        bool partidaCargada = CargarPartida();

        if (!partidaCargada)
        {
            dinero = DineroInicial;
            dia = DiaInicial;
            caballosJugador.Clear();
            rivalPersistente = null;
            CrearCaballoInicial();
        }

        GenerarCaballosDelDia();
        GuardarPartida();
        ActualizarHUD();
    }

    // =========================
    // PERSISTENCIA
    // =========================

    public void GuardarPartida()
    {
        GameData datos = new GameData
        {
            saveVersion = SaveSystem.SaveVersionActual,
            dinero = dinero,
            dia = dia
        };

        foreach (Caballo caballo in caballosJugador)
        {
            if (caballo == null)
            {
                Debug.LogWarning("Se omitió un caballo nulo al guardar la partida.");
                continue;
            }

            caballo.ValidarDatos();
            int indiceSprite = ObtenerIndiceSprite(caballo.sprite);
            datos.caballosJugador.Add(CaballoData.DesdeCaballo(caballo, indiceSprite));
        }

        if (rivalPersistente != null && !EsCaballoJugador(rivalPersistente))
        {
            rivalPersistente.esDelJugador = false;
            rivalPersistente.ValidarDatos();
            int indiceSprite = ObtenerIndiceSprite(rivalPersistente.sprite);
            datos.rivalPersistente = CaballoData.DesdeCaballo(rivalPersistente, indiceSprite);
        }

        SaveSystem.Guardar(datos);
    }

    bool CargarPartida()
    {
        if (!SaveSystem.IntentarCargar(out GameData datos))
            return false;

        if (datos.saveVersion != SaveSystem.SaveVersionActual)
        {
            Debug.LogWarning("Versión de guardado no compatible: " + datos.saveVersion);
            SaveSystem.RespaldarPartidaInvalida();
            return false;
        }

        if (datos.caballosJugador == null || datos.caballosJugador.Count == 0)
        {
            Debug.LogWarning("La partida guardada no contiene caballos del jugador.");
            SaveSystem.RespaldarPartidaInvalida();
            return false;
        }

        List<Caballo> caballosCargados = new List<Caballo>();
        HashSet<string> idsCargados = new HashSet<string>();

        foreach (CaballoData caballoGuardado in datos.caballosJugador)
        {
            if (caballoGuardado == null)
            {
                Debug.LogWarning("La partida contiene un caballo inválido.");
                SaveSystem.RespaldarPartidaInvalida();
                return false;
            }

            Caballo caballo = caballoGuardado.CrearCaballo();

            if (!idsCargados.Add(caballo.id))
            {
                Debug.LogWarning("Se encontró un ID de caballo duplicado. Se generará uno nuevo.");
                caballo.id = System.Guid.NewGuid().ToString("N");
                idsCargados.Add(caballo.id);
            }

            caballo.sprite = ObtenerSpriteGuardado(caballoGuardado.spriteIndex);
            caballosCargados.Add(caballo);
        }

        dinero = datos.dinero;
        dia = Mathf.Max(DiaInicial, datos.dia);

        caballosJugador.Clear();
        caballosJugador.AddRange(caballosCargados);

        rivalPersistente = null;

        if (datos.rivalPersistente != null)
        {
            Caballo rivalCargado = datos.rivalPersistente.CrearCaballo();

            if (idsCargados.Contains(rivalCargado.id))
            {
                Debug.LogWarning("El rival persistente coincide con un caballo del jugador y será ignorado.");
            }
            else
            {
                rivalCargado.esDelJugador = false;
                rivalCargado.ValidarDatos();
                rivalCargado.sprite = ObtenerSpriteGuardado(datos.rivalPersistente.spriteIndex);
                rivalPersistente = rivalCargado;
            }
        }

        Debug.Log("Partida cargada: " + caballosJugador.Count + " caballo(s).");
        return true;
    }

    int ObtenerIndiceSprite(Sprite sprite)
    {
        if (sprite == null || spritesCaballos == null)
            return -1;

        for (int i = 0; i < spritesCaballos.Length; i++)
        {
            if (spritesCaballos[i] == sprite)
                return i;
        }

        Debug.LogWarning("El sprite de un caballo no pertenece a spritesCaballos.");
        return -1;
    }

    Sprite ObtenerSpriteGuardado(int indice)
    {
        if (spritesCaballos != null &&
            indice >= 0 &&
            indice < spritesCaballos.Length &&
            spritesCaballos[indice] != null)
        {
            return spritesCaballos[indice];
        }

        Debug.LogWarning("No se pudo restaurar el sprite guardado con índice " + indice + ".");

        if (spritesCaballos != null)
        {
            foreach (Sprite sprite in spritesCaballos)
            {
                if (sprite != null)
                    return sprite;
            }
        }

        return null;
    }

    // =========================
    // SPRITES SYSTEM
    // =========================

    void ResetSprites()
    {
        spritesDisponibles = new List<Sprite>();

        if (spritesCaballos == null) return;

        foreach (var s in spritesCaballos)
        {
            if (s != null)
                spritesDisponibles.Add(s);
        }
    }

    Sprite ObtenerSprite(Sprite spriteExcluido = null)
    {
        if (spritesCaballos == null || spritesCaballos.Length == 0)
            return null;

        if (spritesDisponibles.Count == 0)
        {
            ResetSprites();

            if (spriteExcluido != null)
                spritesDisponibles.Remove(spriteExcluido);
        }

        if (spritesDisponibles.Count == 0)
            return spriteExcluido == null ? spritesCaballos[0] : null;

        int index = Random.Range(0, spritesDisponibles.Count);
        Sprite s = spritesDisponibles[index];
        spritesDisponibles.RemoveAt(index);

        return s;
    }

    // =========================
    // CABALLO JUGADOR
    // =========================

    void CrearCaballoInicial()
    {
        if (caballosJugador.Count > 0)
            return;

        List<Caballo> candidatos = new List<Caballo>();

        Caballo r = new Caballo();
        r.nombre = "Rayo";
        r.edad = Random.Range(EdadMinima, EdadMaximaExclusiva);
        r.velocidad = 5;
        r.stamina = 4;
        r.suerte = 3;
        r.inteligencia = 5;
        r.potencial = 12;
        r.relacion = 50;
        r.estres = 0;
        r.esDelJugador = true;
        r.ValidarDatos();
        candidatos.Add(r);

        Caballo l = new Caballo();
        l.nombre = "Luna";
        l.edad = Random.Range(EdadMinima, EdadMaximaExclusiva);
        l.velocidad = 4;
        l.stamina = 5;
        l.suerte = 6;
        l.inteligencia = 4;
        l.potencial = 10;
        l.relacion = 50;
        l.estres = 0;
        l.esDelJugador = true;
        l.ValidarDatos();
        candidatos.Add(l);

        Caballo rl = new Caballo();
        rl.nombre = "Relámpago";
        rl.edad = Random.Range(EdadMinima, EdadMaximaExclusiva);
        rl.velocidad = 8;
        rl.stamina = 7;
        rl.suerte = 5;
        rl.inteligencia = 9;
        rl.potencial = 6;
        rl.relacion = 50;
        rl.estres = 0;
        rl.esDelJugador = true;
        rl.ValidarDatos();
        candidatos.Add(rl);

        int i = Random.Range(0, candidatos.Count);
        Caballo elegido = candidatos[i];

        ResetSprites();
        elegido.sprite = ObtenerSprite();

        caballosJugador.Add(elegido);

        Debug.Log("Jugador: " + elegido.nombre);
    }

    // =========================
    // CABALLOS DEL DÍA
    // =========================

    public void GenerarCaballosDelDia()
    {
        caballosDelDia.Clear();

        nombresDisponibles = new List<string>(nombres);
        ResetSprites();

        int cantidad = Random.Range(4, 6);

        if (rivalPersistente != null)
        {
            if (EsCaballoJugador(rivalPersistente))
            {
                Debug.LogWarning("Un caballo del jugador no puede utilizarse como rival persistente.");
                rivalPersistente = null;
            }
            else
            {
                rivalPersistente.esDelJugador = false;
                rivalPersistente.ValidarDatos();
                caballosDelDia.Add(rivalPersistente);
                nombresDisponibles.Remove(rivalPersistente.nombre);
                spritesDisponibles.Remove(rivalPersistente.sprite);
            }
        }

        for (int i = caballosDelDia.Count; i < cantidad; i++)
        {
            Caballo c = new Caballo();

            c.nombre = ObtenerNombreUnico();
            c.edad = Random.Range(EdadMinima, EdadMaximaExclusiva);
            c.velocidad = Random.Range(EstadisticaRivalMinima, EstadisticaRivalMaximaExclusiva);
            c.stamina = Random.Range(EstadisticaRivalMinima, EstadisticaRivalMaximaExclusiva);
            c.suerte = Random.Range(EstadisticaRivalMinima, EstadisticaRivalMaximaExclusiva);
            c.inteligencia = Random.Range(EstadisticaRivalMinima, EstadisticaRivalMaximaExclusiva);
            c.potencial = Random.Range(PotencialRivalMinimo, PotencialRivalMaximoExclusivo);

            c.relacion = 0;
            c.estres = 0;
            c.esDelJugador = false;

            c.sprite = ObtenerSprite(rivalPersistente != null ? rivalPersistente.sprite : null);
            c.ValidarDatos();

            caballosDelDia.Add(c);
        }

        Debug.Log("Caballos del día: " + caballosDelDia.Count);
    }

    public void EstablecerRivalPersistente(Caballo rival)
    {
        if (rival == null || EsCaballoJugador(rival))
            return;

        rival.esDelJugador = false;
        rival.ValidarDatos();
        rivalPersistente = rival;
    }

    bool EsCaballoJugador(Caballo caballo)
    {
        if (caballo == null)
            return false;

        foreach (Caballo caballoJugador in caballosJugador)
        {
            if (caballoJugador == null)
                continue;

            if (ReferenceEquals(caballo, caballoJugador))
                return true;

            if (!string.IsNullOrEmpty(caballo.id) && caballo.id == caballoJugador.id)
                return true;
        }

        return false;
    }

    // =========================
    // NOMBRES
    // =========================

    string ObtenerNombreUnico()
    {
        if (nombresDisponibles.Count == 0)
            return "Caballo " + Random.Range(100, 999);

        int i = Random.Range(0, nombresDisponibles.Count);
        string n = nombresDisponibles[i];
        nombresDisponibles.RemoveAt(i);
        return n;
    }

    // =========================
    // ECONOMÍA
    // =========================

    public void AgregarDinero(int c)
    {
        dinero += c;
        ActualizarHUD();
        GuardarPartida();
    }

    public void RestarDinero(int c)
    {
        dinero -= c;
        ActualizarHUD();
        GuardarPartida();
    }

    public void SiguienteDia()
    {
        dia++;
        GenerarCaballosDelDia();
        ActualizarHUD();
        GuardarPartida();
    }

    void ActualizarHUD()
    {
        if (textoDinero != null)
            textoDinero.text = "$ " + dinero;

        if (textoDia != null)
            textoDia.text = "Día " + dia;
    }

    // =========================
    // ESTADOS
    // =========================

    public void SetState(Interactable.Tipo tipo)
    {
        switch (tipo)
        {
            case Interactable.Tipo.TV:
                estadoActual = GameState.TV;
                UIManager.instance.AbrirCarrera();
                break;

            case Interactable.Tipo.Tienda:
                estadoActual = GameState.Tienda;
                UIManager.instance.AbrirTienda();
                break;

            case Interactable.Tipo.Establo:
                estadoActual = GameState.Establo;
                UIManager.instance.AbrirEstablo();
                break;

            case Interactable.Tipo.Tablon:
                estadoActual = GameState.Tablon;
                UIManager.instance.AbrirTablon();
                break;
        }
    }

    // =========================
    // HUB
    // =========================

    public void VolverAlHub()
    {
        estadoActual = GameState.Hub;

        UIManager.instance.CerrarTodo();
        CameraController.instancia.ActivarControl();
    }
}
