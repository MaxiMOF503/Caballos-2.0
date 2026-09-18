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
    private const int DiasPorSemana = 7;
    private const int TotalSemanasCampana = 6;
    private const int UltimoDiaCampana = DiasPorSemana * TotalSemanasCampana;

    private static readonly int[] CuotasSemanales =
    {
        1500,
        2000,
        2750,
        3500,
        4500,
        6000
    };

    private enum ResultadoCierreDia
    {
        Continuar,
        CampanaPerdida,
        CampanaCompletada
    }

    private struct TransicionCampana
    {
        public ResultadoCierreDia resultado;
        public int diaSiguiente;
        public int dineroRestante;
        public int semanaFinalizada;
        public int cuotaAplicada;
    }

    public static GameManager Instancia;

    [Header("Estado")]
    public GameState estadoActual;

    [Header("Economía")]
    public int dinero = DineroInicial;
    public int dia = DiaInicial;
    public bool campanaCompletada;

    [Header("Desarrollo")]
    [SerializeField]
    private bool reiniciarPartidaAlIniciarEnEditor = true;

    [Header("Caballos Jugador")]
    public List<Caballo> caballosJugador = new List<Caballo>();

    [Header("Items Jugador")]
    public List<ItemCaballo> itemsJugador = new List<ItemCaballo>();

    [Header("Tienda")]
    [Min(1)] public int ofertasVisiblesTienda = SistemaTienda.OfertasIniciales;
    [Min(1)] public int intervaloRotacionTienda = SistemaTienda.DiasPorRotacion;
    public EstadoTiendaData estadoTienda;

    [Header("Caballos del Día")]
    public List<Caballo> caballosDelDia = new List<Caballo>();

    [Header("Rival Persistente")]
    public Caballo rivalPersistente;

    [Header("Sprites Caballos (IA + jugador)")]
    public Sprite[] spritesCaballos;

    [Header("Sprites Items")]
    public Sprite[] spritesItems;

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

#if UNITY_EDITOR
        if (reiniciarPartidaAlIniciarEnEditor)
        {
            Debug.Log("Reinicio de desarrollo activo: se iniciará una campaña nueva.");
            IniciarNuevaPartida();
            return;
        }
#endif

        bool partidaCargada = CargarPartida();

        if (!partidaCargada)
        {
            IniciarNuevaPartida();
            return;
        }

        if (!campanaCompletada)
            GenerarCaballosDelDia();
        else
            caballosDelDia.Clear();

        GuardarPartida();
        ActualizarHUD();
    }

    public void IniciarNuevaPartida()
    {
        dinero = DineroInicial;
        dia = DiaInicial;
        campanaCompletada = false;
        rivalPersistente = null;

        caballosJugador.Clear();
        caballosDelDia.Clear();

        if (itemsJugador == null)
            itemsJugador = new List<ItemCaballo>();
        else
            itemsJugador.Clear();

        estadoTienda = SistemaTienda.CrearEstado(CatalogoTienda(), OfertasTienda, dia, IntervaloTienda);

        ResetSprites();
        CrearCaballoInicial();
        GenerarCaballosDelDia();
        ActualizarHUD();
        GuardarPartida();
        RefrescarTiendaAbierta();
    }

    // =========================
    // PERSISTENCIA
    // =========================

    public bool GuardarPartida()
    {
        GameData datos = new GameData
        {
            saveVersion = SaveSystem.SaveVersionActual,
            dinero = dinero,
            dia = dia,
            campanaCompletada = campanaCompletada,
            estadoTienda = estadoTienda
        };

        HashSet<string> idsCaballosJugador = new HashSet<string>();

        foreach (Caballo caballo in caballosJugador)
        {
            if (caballo == null)
            {
                Debug.LogWarning("Se omitió un caballo nulo al guardar la partida.");
                continue;
            }

            caballo.ValidarDatos();
            idsCaballosJugador.Add(caballo.id);
            int indiceSprite = ObtenerIndiceSprite(caballo.sprite);
            datos.caballosJugador.Add(CaballoData.DesdeCaballo(caballo, indiceSprite));
        }

        HashSet<string> idsItems = new HashSet<string>();
        HashSet<string> slotsOcupados = new HashSet<string>();
        HashSet<ItemCaballo> referenciasItems = new HashSet<ItemCaballo>();

        if (itemsJugador != null)
        {
            foreach (ItemCaballo item in itemsJugador)
            {
                if (item == null)
                {
                    Debug.LogWarning("Se omitió un item nulo al guardar la partida.");
                    continue;
                }

                if (!referenciasItems.Add(item))
                {
                    Debug.LogWarning("Se omitió una referencia de item repetida al guardar la partida.");
                    continue;
                }

                item.ValidarDatos();

                if (!idsItems.Add(item.id))
                {
                    Debug.LogWarning("Se encontró un ID de item duplicado. Se generará uno nuevo.");
                    item.id = System.Guid.NewGuid().ToString("N");
                    idsItems.Add(item.id);
                }

                ValidarAsignacionItem(item, idsCaballosJugador, slotsOcupados);
                int indiceSprite = ObtenerIndiceSpriteItem(item.sprite);
                datos.itemsJugador.Add(ItemCaballoData.DesdeItem(item, indiceSprite));
            }
        }

        if (rivalPersistente != null && !EsCaballoJugador(rivalPersistente))
        {
            rivalPersistente.esDelJugador = false;
            rivalPersistente.ValidarDatos();
            int indiceSprite = ObtenerIndiceSprite(rivalPersistente.sprite);
            datos.rivalPersistente = CaballoData.DesdeCaballo(rivalPersistente, indiceSprite);
        }

        return SaveSystem.Guardar(datos);
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
        dia = Mathf.Clamp(datos.dia, DiaInicial, UltimoDiaCampana);
        campanaCompletada = datos.campanaCompletada;

        if (campanaCompletada)
            dia = UltimoDiaCampana;

        estadoTienda = datos.estadoTienda;
        SincronizarTienda();

        caballosJugador.Clear();
        caballosJugador.AddRange(caballosCargados);

        if (itemsJugador == null)
            itemsJugador = new List<ItemCaballo>();
        else
            itemsJugador.Clear();
        HashSet<string> idsItemsCargados = new HashSet<string>();
        HashSet<string> slotsOcupados = new HashSet<string>();

        if (datos.itemsJugador != null)
        {
            foreach (ItemCaballoData itemGuardado in datos.itemsJugador)
            {
                if (itemGuardado == null)
                {
                    Debug.LogWarning("La partida contiene un item nulo que será ignorado.");
                    continue;
                }

                string caballoEquipadoGuardado = itemGuardado.caballoEquipadoId;
                ItemCaballo item = itemGuardado.CrearItem();

                if (!string.IsNullOrWhiteSpace(caballoEquipadoGuardado) &&
                    string.IsNullOrEmpty(item.caballoEquipadoId))
                {
                    Debug.LogWarning("Un item tenía un ID de caballo equipado inválido y quedó disponible.");
                }

                if (!idsItemsCargados.Add(item.id))
                {
                    Debug.LogWarning("Se encontró un ID de item duplicado. Se generará uno nuevo.");
                    item.id = System.Guid.NewGuid().ToString("N");
                    idsItemsCargados.Add(item.id);
                }

                ValidarAsignacionItem(item, idsCargados, slotsOcupados);
                item.sprite = ObtenerSpriteItemGuardado(itemGuardado.spriteIndex);
                itemsJugador.Add(item);
            }
        }

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

    int ObtenerIndiceSpriteItem(Sprite sprite)
    {
        if (sprite == null || spritesItems == null)
            return -1;

        for (int i = 0; i < spritesItems.Length; i++)
        {
            if (spritesItems[i] == sprite)
                return i;
        }

        Debug.LogWarning("El sprite de un item no pertenece a spritesItems.");
        return -1;
    }

    Sprite ObtenerSpriteItemGuardado(int indice)
    {
        if (indice < 0)
            return null;

        if (spritesItems != null &&
            indice < spritesItems.Length &&
            spritesItems[indice] != null)
        {
            return spritesItems[indice];
        }

        Debug.LogWarning("No se pudo restaurar el sprite de item con índice " + indice + ".");
        return null;
    }

    static void ValidarAsignacionItem(
        ItemCaballo item,
        HashSet<string> idsCaballosJugador,
        HashSet<string> slotsOcupados)
    {
        if (!SistemaEquipamiento.ValidarAsignacionPersistida(
                item,
                idsCaballosJugador,
                slotsOcupados,
                out string advertencia))
        {
            Debug.LogWarning(advertencia);
        }
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

        elegido.nivel = 1;
        elegido.experiencia = 0;

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

    private int OfertasTienda => ofertasVisiblesTienda <= 0
        ? SistemaTienda.OfertasIniciales
        : Mathf.Clamp(ofertasVisiblesTienda, 1, 6);
    private int IntervaloTienda => intervaloRotacionTienda <= 0
        ? SistemaTienda.DiasPorRotacion
        : intervaloRotacionTienda;

    public List<DefinicionProductoTienda> CatalogoTienda()
    {
        return SistemaTienda.CrearCatalogo(spritesItems);
    }

    public bool SincronizarTienda()
    {
        List<DefinicionProductoTienda> catalogo = CatalogoTienda();
        if (!SistemaTienda.StockValido(estadoTienda, catalogo, OfertasTienda))
        {
            estadoTienda = SistemaTienda.CrearEstado(catalogo, OfertasTienda, dia, IntervaloTienda);
            return true;
        }

        return SistemaTienda.Sincronizar(estadoTienda, catalogo, OfertasTienda, dia, IntervaloTienda);
    }

    public bool IntentarComprarItem(string idDefinicion)
    {
        DefinicionProductoTienda producto = SistemaTienda.Buscar(CatalogoTienda(), idDefinicion);
        if (producto == null || producto.tipoProducto != TipoProductoTienda.Equipable ||
            !producto.tieneSlotEquipable || !System.Enum.IsDefined(typeof(TipoSlotItem), producto.slotPermitido) ||
            producto.precio < 0 || dinero < producto.precio || itemsJugador == null || producto.sprite == null ||
            estadoTienda == null || estadoTienda.idsProductosStock == null ||
            !estadoTienda.idsProductosStock.Contains(producto.idDefinicion))
            return false;

        ItemCaballo item = new ItemCaballo
        {
            id = System.Guid.NewGuid().ToString("N"),
            idDefinicion = producto.idDefinicion,
            nombre = producto.nombre,
            descripcion = producto.descripcion,
            slotPermitido = producto.slotPermitido,
            sprite = producto.sprite,
            caballoEquipadoId = null
        };

        int dineroAnterior = dinero;
        itemsJugador.Add(item);
        dinero -= producto.precio;
        if (!GuardarPartida())
        {
            itemsJugador.Remove(item);
            dinero = dineroAnterior;
            return false;
        }

        ActualizarHUD();
        return true;
    }

    public bool IntentarRerollTiendaManual()
    {
        if (dinero < SistemaTienda.CostoRerollManual || estadoTienda == null ||
            estadoTienda.idsProductosStock == null)
            return false;

        List<string> anterior = estadoTienda.idsProductosStock;
        List<string> nuevo = SistemaTienda.GenerarStockDistinto(CatalogoTienda(), OfertasTienda, anterior);
        int dineroAnterior = dinero;
        estadoTienda.idsProductosStock = nuevo;
        dinero -= SistemaTienda.CostoRerollManual;
        if (!GuardarPartida())
        {
            estadoTienda.idsProductosStock = anterior;
            dinero = dineroAnterior;
            return false;
        }

        ActualizarHUD();
        return true;
    }

    public bool EquiparItem(Caballo caballo, ItemCaballo item)
    {
        bool equipado = SistemaEquipamiento.EquiparItem(
            caballo,
            item,
            caballosJugador,
            itemsJugador,
            ref dinero,
            out string mensaje);

        if (!equipado)
        {
            Debug.LogWarning(mensaje);
            return false;
        }

        Debug.Log(mensaje + " Coste: $" + SistemaEquipamiento.CostoEquiparItem + ".");
        ActualizarHUD();
        GuardarPartida();
        return true;
    }

    public bool DesequiparItem(Caballo caballo, ItemCaballo item)
    {
        bool desequipado = SistemaEquipamiento.DesequiparItem(
            caballo,
            item,
            caballosJugador,
            itemsJugador,
            ref dinero,
            out string mensaje);

        if (!desequipado)
        {
            Debug.LogWarning(mensaje);
            return false;
        }

        Debug.Log(mensaje + " Coste: $" + SistemaEquipamiento.CostoDesequiparItem + ".");
        ActualizarHUD();
        GuardarPartida();
        return true;
    }

    public ItemCaballo ObtenerItemEquipado(Caballo caballo, TipoSlotItem slot)
    {
        if (!EsCaballoJugador(caballo))
            return null;

        return SistemaEquipamiento.ObtenerItemEquipado(itemsJugador, caballo.id, slot);
    }

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
        if (campanaCompletada)
        {
            Debug.LogWarning("La campaña ya está completada. No se puede avanzar a un día 43.");
            return;
        }

        TransicionCampana transicion = CalcularTransicionCampana(dia, dinero);

        if (transicion.resultado == ResultadoCierreDia.CampanaPerdida)
        {
            Debug.LogWarning(
                "CAMPAÑA PERDIDA. Semana " + transicion.semanaFinalizada +
                ": dinero $" + dinero + ", cuota $" + transicion.cuotaAplicada + ".");
            IniciarNuevaPartida();
            return;
        }

        dinero = transicion.dineroRestante;

        if (transicion.cuotaAplicada > 0)
        {
            Debug.Log(
                "Semana " + transicion.semanaFinalizada + " superada. Cuota pagada: $" +
                transicion.cuotaAplicada + ". Dinero restante: $" + dinero + ".");
        }

        if (transicion.resultado == ResultadoCierreDia.CampanaCompletada)
        {
            dia = UltimoDiaCampana;
            campanaCompletada = true;
            Debug.Log("CAMPAÑA COMPLETADA");
            ActualizarHUD();
            GuardarPartida();
            return;
        }

        dia = transicion.diaSiguiente;
        bool tiendaRotada = SincronizarTienda();
        GenerarCaballosDelDia();
        ActualizarHUD();
        GuardarPartida();
        if (tiendaRotada)
            RefrescarTiendaAbierta();
    }

    private static void RefrescarTiendaAbierta()
    {
        if (UIManager.instance == null || UIManager.instance.viewTienda == null ||
            !UIManager.instance.viewTienda.activeInHierarchy)
            return;

        TiendaView tienda = UIManager.instance.viewTienda.GetComponentInChildren<TiendaView>(true);
        if (tienda != null)
            tienda.RefrescarPorCambioDeStock();
    }

    public static int ObtenerSemana(int diaCampana)
    {
        int diaValido = diaCampana < DiaInicial
            ? DiaInicial
            : diaCampana > UltimoDiaCampana
                ? UltimoDiaCampana
                : diaCampana;

        return ((diaValido - 1) / DiasPorSemana) + 1;
    }

    public static int ObtenerDiaDeSemana(int diaCampana)
    {
        int diaValido = diaCampana < DiaInicial
            ? DiaInicial
            : diaCampana > UltimoDiaCampana
                ? UltimoDiaCampana
                : diaCampana;

        return ((diaValido - 1) % DiasPorSemana) + 1;
    }

    public static int ObtenerCuotaSemana(int semana)
    {
        return semana >= 1 && semana <= CuotasSemanales.Length
            ? CuotasSemanales[semana - 1]
            : 0;
    }

    static TransicionCampana CalcularTransicionCampana(int diaActual, int dineroActual)
    {
        int diaValido = diaActual < DiaInicial
            ? DiaInicial
            : diaActual > UltimoDiaCampana
                ? UltimoDiaCampana
                : diaActual;

        TransicionCampana transicion = new TransicionCampana
        {
            resultado = ResultadoCierreDia.Continuar,
            diaSiguiente = diaValido + 1,
            dineroRestante = dineroActual
        };

        if (diaValido % DiasPorSemana != 0)
            return transicion;

        transicion.semanaFinalizada = ObtenerSemana(diaValido);
        transicion.cuotaAplicada = ObtenerCuotaSemana(transicion.semanaFinalizada);

        if (dineroActual < transicion.cuotaAplicada)
        {
            transicion.resultado = ResultadoCierreDia.CampanaPerdida;
            transicion.diaSiguiente = DiaInicial;
            return transicion;
        }

        transicion.dineroRestante = dineroActual - transicion.cuotaAplicada;

        if (transicion.semanaFinalizada == TotalSemanasCampana)
        {
            transicion.resultado = ResultadoCierreDia.CampanaCompletada;
            transicion.diaSiguiente = UltimoDiaCampana;
        }

        return transicion;
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
