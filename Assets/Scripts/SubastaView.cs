using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TablonLayout;

public sealed class SubastaView : MonoBehaviour
{
    static readonly Vector2 Lienzo = new(1672, 941);
    static readonly Color Crema = new Color32(255, 235, 198, 255);
    static readonly Color Tinta = new Color32(54, 28, 14, 255);
    static readonly CultureInfo Moneda = CultureInfo.GetCultureInfo("es-AR");
    Sprite pergamino, cartel, marco;
    TMP_FontAsset fuente;
    TiendaView tienda;
    TMP_Text nombre, basicos, dias, avisoFinal, precio, minima, saldo, reserva, postor, monto, mensaje;
    readonly TMP_Text[] estadisticas = new TMP_Text[7];
    Image caballo;
    Button pujar, seguir, menos, mas;
    GameObject modal;
    CanvasGroup contenido;
    GameObject focoAnterior;
    GameManager juego;
    int importeElegido;

    internal void Configurar(TiendaView origen, Sprite papel, Sprite madera, Sprite borde, TMP_FontAsset font)
    {
        tienda = origen; pergamino = papel; cartel = madera; marco = borde; fuente = font;
        Construir();
    }

    void OnEnable()
    {
        juego = GameManager.Instancia;
        juego.SubastasActualizadas += Refrescar;
        Refrescar();
    }

    void OnDisable()
    {
        if (juego != null) juego.SubastasActualizadas -= Refrescar;
        CerrarPedigri();
    }

    void Construir()
    {
        var raiz = (RectTransform)transform;
        var fondo = Imagen("Escenario", raiz, new Rect(0,0,1672,941), Resources.Load<Sprite>("Subasta/Fondo"));
        fondo.raycastTarget = true;
        var cuerpo = Rect("Contenido", raiz, new Rect(0,0,1672,941), Lienzo);
        contenido = cuerpo.gameObject.AddComponent<CanvasGroup>();
        var tabTienda = Boton("Tienda", cuerpo, new Rect(88,43,230,60), "Tienda", tienda.MostrarProductos, false);
        var colorTienda = tabTienda.colors;
        colorTienda.normalColor = colorTienda.selectedColor = new Color32(141,129,111,255);
        tabTienda.colors = colorTienda;
        tabTienda.GetComponentInChildren<TMP_Text>().color = new Color32(205,189,165,255);
        var tab = Boton("Subasta", cuerpo, new Rect(329,43,237,60), "Subasta", null, false);
        // La pestaña activa se señala con una línea y texto, separados del hover.
        tab.colors = Colores(Color.white);
        tab.GetComponentInChildren<TMP_Text>().color = new Color32(251,211,147,255);
        Surface("PestanaActual", cuerpo, new Rect(352,98,189,3), Lienzo, new Color32(218,163,85,255));
        Imagen("Cartel", cuerpo, new Rect(587,22,497,102), cartel, true);
        Texto("Titulo", cuerpo, new Rect(610,33,450,75), "Subasta", 61);
        Boton("Volver", cuerpo, new Rect(1461,44,165,57), "‹ Volver", tienda.Volver);

        Imagen("CartelDias", cuerpo, new Rect(418,183,244,80), cartel, true);
        dias = Texto("Dias", cuerpo, new Rect(430,192,220,60), "", 28);
        dias.textWrappingMode = TextWrappingModes.Normal;
        avisoFinal = Texto("UltimoDia", cuerpo, new Rect(30,133,380,43), "¡La subasta termina hoy!", 27);
        Imagen("FichaNombre", cuerpo, new Rect(227,295,422,145), pergamino, true);
        nombre = Texto("Nombre", cuerpo, new Rect(250,308,375,60), "", 52, true);
        basicos = Texto("DatosBasicos", cuerpo, new Rect(246,372,380,51), "", 24, true);
        basicos.textWrappingMode = TextWrappingModes.Normal;
        caballo = Imagen("CaballoDinamico", cuerpo, new Rect(690,136,402,286), null);
        caballo.preserveAspect = true;
        caballo.rectTransform.pivot = new Vector2(.5f,0);
        caballo.rectTransform.offsetMin = caballo.rectTransform.offsetMax = Vector2.zero;

        Panel("Detalles", cuerpo, new Rect(30,484,550,409), "Detalles y estadísticas");
        Panel("Pujas", cuerpo, new Rect(598,484,484,409), "Puja");
        Panel("Cuenta", cuerpo, new Rect(1097,484,544,409), "Tu cuenta y acciones");
        string[] etiquetas = { "Raza", "Edad", "Potencial", "Velocidad", "Stamina", "Suerte", "Inteligencia" };
        int[] iconos = { -1, 0, 7, 3, 4, 5, 6 };
        var atlas = UIManager.instance.viewEstablo.GetComponentInChildren<EstabloView>(true).IconosStats;
        for (int i = 0; i < etiquetas.Length; i++)
        {
            float y = 548 + i * 46;
            if (iconos[i] >= 0 && atlas != null)
            {
                var icono = Rect("Icono" + etiquetas[i], cuerpo, new Rect(88,y,54,38), Lienzo).gameObject.AddComponent<RawImage>();
                icono.texture = atlas.texture;
                icono.uvRect = new Rect(iconos[i] / 10f,0,.1f,1);
                icono.raycastTarget = false;
            }
            else Imagen("IconoRaza", cuerpo, new Rect(91,y,46,37), Resources.Load<Sprite>("Carreras/Caballo"));
            Texto("Etiqueta" + etiquetas[i], cuerpo, new Rect(166,y,165,37), etiquetas[i], 27, false, TextAlignmentOptions.MidlineLeft);
            Surface("Linea", cuerpo, new Rect(94,y+41,416,1), Lienzo, new Color32(132,83,41,255));
            estadisticas[i] = Texto(etiquetas[i], cuerpo, new Rect(344,y,190,37), "", 27, false, TextAlignmentOptions.MidlineLeft);
        }
        Imagen("PapelPuja", cuerpo, new Rect(623,549,434,128), pergamino, true);
        Texto("EtiquetaPuja", cuerpo, new Rect(640,561,400,34), "Puja actual", 25, true);
        precio = Texto("PujaActual", cuerpo, new Rect(645,597,390,66), "", 57, true);
        Texto("EtiquetaMinima", cuerpo, new Rect(635,698,414,35), "Próxima puja mínima", 28);
        minima = Texto("Minima", cuerpo, new Rect(638,735,408,48), "", 41);
        postor = Texto("Postor", cuerpo, new Rect(636,809,410,50), "", 25);
        postor.textWrappingMode = TextWrappingModes.Normal;
        saldo = Texto("Saldo", cuerpo, new Rect(1135,552,470,42), "", 30);
        reserva = Texto("Reserva", cuerpo, new Rect(1145,594,452,32), "", 23);
        Texto("EtiquetaMonto", cuerpo, new Rect(1198,629,336,33), "Monto de puja", 26);
        menos = Boton("Menos", cuerpo, new Rect(1148,668,58,53), "−", () => CambiarImporte(-1));
        Imagen("PapelMonto", cuerpo, new Rect(1219,668,302,53), pergamino, true);
        monto = Texto("Monto", cuerpo, new Rect(1230,670,281,47), "", 36, true);
        mas = Boton("Mas", cuerpo, new Rect(1535,668,58,53), "+", () => CambiarImporte(1));
        pujar = Boton("Pujar", cuerpo, new Rect(1125,738,485,65), "Pujar", Pujar);
        seguir = Boton("Seguir", cuerpo, new Rect(1125,822,233,51), "Seguir", Seguir);
        Boton("Pedigri", cuerpo, new Rect(1377,822,233,51), "Ver pedigrí", MostrarPedigri);
        mensaje = Texto("Mensaje", cuerpo, new Rect(602,900,1030,32), "", 23);

        var velo = Surface("PedigriModal", raiz, new Rect(0,0,1672,941), Lienzo, new Color32(0,0,0,170));
        velo.GetComponent<Image>().raycastTarget = true;
        modal = velo.gameObject;
        Imagen("Papel", velo, new Rect(490,310,692,319), pergamino, true);
        Texto("Titulo", velo, new Rect(525,338,622,58), "Pedigrí", 43, true);
        var informacion = Texto("Informacion", velo, new Rect(545,411,582,106),
            "Datos genealógicos no disponibles.\nNo hay padres ni linajes registrados.", 30, true);
        informacion.textWrappingMode = TextWrappingModes.Normal;
        Boton("Cerrar", velo, new Rect(685,544,302,59), "Cerrar", () => CerrarPedigri());
        modal.SetActive(false);
    }

    internal void Refrescar()
    {
        var actual = juego.SubastaActual;
        var datos = actual.caballo;
        int dia = actual.DiaDelCiclo(juego.dia);
        nombre.text = datos.nombre;
        basicos.text = datos.raza + "\nEdad " + Valor(DatoSubasta.Edad, datos.edad) + " · Nivel " + Valor(DatoSubasta.Nivel, datos.nivel);
        dias.text = "Día " + dia + " de 4\n" + (dia == 4 ? "Último día" : (4-dia) + " días restantes");
        avisoFinal.gameObject.SetActive(dia == 4 && actual.resultado == ResultadoSubasta.Abierta);
        var vista = juego.CrearVistaSubasta();
        caballo.sprite = HorseVisualCatalog.Default.Splash(vista);
        caballo.enabled = caballo.sprite != null;
        estadisticas[0].text = datos.raza;
        estadisticas[1].text = Valor(DatoSubasta.Edad,datos.edad);
        estadisticas[2].text = Valor(DatoSubasta.Potencial,datos.potencial);
        estadisticas[3].text = Valor(DatoSubasta.Velocidad,datos.velocidad);
        estadisticas[4].text = Valor(DatoSubasta.Stamina,datos.stamina);
        estadisticas[5].text = Valor(DatoSubasta.Suerte,datos.suerte);
        estadisticas[6].text = Valor(DatoSubasta.Inteligencia,datos.inteligencia);
        precio.text = Pesos(actual.pujaActual);
        minima.text = Pesos(SistemaSubastas.Minima(actual));
        saldo.text = "Saldo disponible " + Pesos(juego.dinero);
        reserva.text = "Tu reserva " + Pesos(actual.reserva);
        postor.text = "Postor actual · " + actual.NombrePostor;
        importeElegido = SistemaSubastas.Minima(actual);
        ActualizarImporte();
        seguir.GetComponentInChildren<TMP_Text>().text = actual.siguiendo ? "Siguiendo" : "Seguir";
    }

    string Valor(DatoSubasta dato,int valor) => juego.SubastaActual.Visible(dato,juego.dia) ? valor.ToString() : "???";
    static string Pesos(int valor) => "$" + valor.ToString("N0",Moneda);

    void CambiarImporte(int direccion)
    {
        long nuevo = (long)importeElegido + direccion * SistemaSubastas.Incremento(importeElegido);
        int limite = (int)Math.Min(int.MaxValue,(long)juego.dinero+juego.DineroReservado);
        importeElegido = (int)Math.Clamp(nuevo,SistemaSubastas.Minima(juego.SubastaActual),Math.Max(limite,SistemaSubastas.Minima(juego.SubastaActual)));
        ActualizarImporte();
    }

    void ActualizarImporte()
    {
        monto.text = Pesos(importeElegido);
        bool abierta = !juego.campanaCompletada && juego.SubastaActual.resultado == ResultadoSubasta.Abierta;
        pujar.interactable = abierta && importeElegido > juego.SubastaActual.pujaActual &&
            importeElegido <= (long)juego.dinero + juego.DineroReservado;
        menos.interactable = abierta && importeElegido > SistemaSubastas.Minima(juego.SubastaActual);
        mas.interactable = abierta && importeElegido < (long)juego.dinero + juego.DineroReservado;
    }

    void Pujar()
    {
        juego.PujarSubasta(juego.SubastaActual.id,importeElegido,out string resultado);
        mensaje.text = resultado;
        Refrescar();
    }
    void Seguir()
    {
        bool ok = juego.SeguirSubasta(!juego.SubastaActual.siguiendo);
        mensaje.text = ok ? "Seguimiento guardado" : "No se pudo guardar el seguimiento";
        Refrescar();
    }
    void MostrarPedigri()
    {
        focoAnterior = EventSystem.current?.currentSelectedGameObject;
        contenido.interactable = contenido.blocksRaycasts = false;
        modal.SetActive(true);
        modal.GetComponentInChildren<Button>().Select();
    }
    internal bool CerrarPedigri()
    {
        if (modal == null || !modal.activeSelf) return false;
        modal.SetActive(false);
        contenido.interactable = contenido.blocksRaycasts = true;
        if (focoAnterior != null && focoAnterior.activeInHierarchy)
            EventSystem.current?.SetSelectedGameObject(focoAnterior);
        focoAnterior = null;
        return true;
    }

    void Panel(string id, RectTransform parent, Rect area,string titulo)
    {
        var madera = UIManager.instance.viewEstablo.GetComponentInChildren<EstabloView>(true).TexturaMadera;
        var fondo = Wood(id,parent,area,Lienzo,madera).GetComponent<Graphic>();
        fondo.color = new Color32(175,150,125,255);
        var borde = Imagen(id+"Marco",parent,area,marco,true);
        borde.fillCenter = false;
        borde.color = new Color32(195,133,61,255);
        borde.pixelsPerUnitMultiplier = .65f;
        Texto(id+"Titulo",parent,new Rect(area.x+18,area.y+8,area.width-36,45),titulo,32);
        Surface("Separador",parent,new Rect(area.x+20,area.y+54,area.width-40,1),Lienzo,new Color32(168,108,49,255));
    }

    Image Imagen(string id,RectTransform parent,Rect area,Sprite sprite,bool sliced=false)
    {
        var image = Rect(id,parent,area,Lienzo).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = 3;
        return image;
    }
    TMP_Text Texto(string id,RectTransform parent,Rect area,string texto,float tamano,bool oscuro=false,
        TextAlignmentOptions alineacion=TextAlignmentOptions.Center)
    {
        var label = Label(id,parent,area,Lienzo,fuente,texto,tamano,alineacion,oscuro ? Tinta : Crema);
        label.richText = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }
    static ColorBlock Colores(Color normal)
    {
        var colores = ColorBlock.defaultColorBlock;
        colores.normalColor = colores.selectedColor = normal * new Color32(211,195,174,255);
        colores.highlightedColor = new Color32(255,239,208,255);
        colores.pressedColor = new Color32(209,176,117,255);
        colores.disabledColor = new Color32(137,124,108,255);
        colores.fadeDuration = .08f;
        return colores;
    }
    Button Boton(string id,RectTransform parent,Rect area,string texto,UnityAction accion,bool papel=true)
    {
        var imagen = Imagen(id,parent,area,papel ? pergamino : cartel,true);
        imagen.raycastTarget = true;
        var boton = imagen.gameObject.AddComponent<Button>();
        boton.targetGraphic = imagen;
        boton.colors = Colores(Color.white);
        if (accion != null) boton.onClick.AddListener(accion);
        var etiqueta = Label("Etiqueta",imagen.rectTransform,new Rect(9,3,area.width-18,area.height-6),area.size,
            fuente,texto,32,TextAlignmentOptions.Center,papel ? Tinta : Crema);
        etiqueta.richText = false;
        etiqueta.textWrappingMode = TextWrappingModes.NoWrap;
        return boton;
    }
}
