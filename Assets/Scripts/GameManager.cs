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
    public static GameManager Instancia;

    [Header("Estado")]
    public GameState estadoActual;

    [Header("Economía")]
    public int dinero = 1000;
    public int dia = 1;

    [Header("Caballos Jugador")]
    public List<Caballo> caballosJugador = new List<Caballo>();

    [Header("Caballos del Día")]
    public List<Caballo> caballosDelDia = new List<Caballo>();

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
        CrearCaballoInicial();
        GenerarCaballosDelDia();
        ActualizarHUD();
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

    Sprite ObtenerSprite()
    {
        if (spritesCaballos == null || spritesCaballos.Length == 0)
            return null;

        if (spritesDisponibles.Count == 0)
            ResetSprites();

        if (spritesDisponibles.Count == 0)
            return spritesCaballos[0];

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
        r.velocidad = 5;
        r.stamina = 4;
        r.suerte = 3;
        r.inteligencia = 5;
        r.potencial = 12;
        r.esDelJugador = true;
        candidatos.Add(r);

        Caballo l = new Caballo();
        l.nombre = "Luna";
        l.velocidad = 4;
        l.stamina = 5;
        l.suerte = 6;
        l.inteligencia = 4;
        l.potencial = 10;
        l.esDelJugador = true;
        candidatos.Add(l);

        Caballo rl = new Caballo();
        rl.nombre = "Relámpago";
        rl.velocidad = 8;
        rl.stamina = 7;
        rl.suerte = 5;
        rl.inteligencia = 9;
        rl.potencial = 6;
        rl.esDelJugador = true;
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

        for (int i = 0; i < cantidad; i++)
        {
            Caballo c = new Caballo();

            c.nombre = ObtenerNombreUnico();
            c.velocidad = Random.Range(0, 5);
            c.stamina = Random.Range(0, 5);
            c.suerte = Random.Range(0, 5);
            c.inteligencia = Random.Range(0, 5);
            c.potencial = Random.Range(0, 5);

            c.esDelJugador = false;

            c.sprite = ObtenerSprite();

            caballosDelDia.Add(c);
        }

        Debug.Log("Caballos del día: " + caballosDelDia.Count);
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
    }

    public void RestarDinero(int c)
    {
        dinero -= c;
        ActualizarHUD();
    }

    public void SiguienteDia()
    {
        dia++;
        GenerarCaballosDelDia();
        ActualizarHUD();
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