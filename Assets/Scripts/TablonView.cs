using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Globalization;

public class TablonView : MonoBehaviour
{
    public TMP_Text textoNombre, textoEdad, textoNivel;
    public TMP_Text textoVelocidad, textoStamina, textoSuerte, textoInteligencia, textoPotencial;
    public Transform content;
    public GameObject caballoButtonPrefab;
    public TMP_InputField importe;
    public Button apostar, confirmar, cancelar;
    [SerializeField] Sprite marco, madera;
    internal TMP_Text saldo, feedback;
    Sprite marcoRecortado;

    readonly List<CaballoButtonUI> filas = new List<CaballoButtonUI>();
    CarreraProgramada carrera;
    Caballo elegido;
    int importeConfirmacion;
    string mensaje;

    void Awake() => marcoRecortado = TablonLayout.Construir(this, (RectTransform)transform.parent, marco, madera);

    void OnDestroy()
    {
        if (marcoRecortado != null) Destroy(marcoRecortado);
    }

    void OnEnable()
    {
        var juego = GameManager.Instancia;
        if (juego == null) return;
        var actual = juego.ObtenerCarreraProgramada();
        string seleccion = carrera?.id == actual?.id ? elegido?.id : null;
        carrera = actual;
        mensaje = juego.UltimaApuesta?.Resultado;
        importe.contentType = TMP_InputField.ContentType.IntegerNumber;
        importe.characterValidation = TMP_InputField.CharacterValidation.Digit;
        importe.SetTextWithoutNotify(carrera?.apuesta?.cantidad.ToString() ?? "");
        importe.onValueChanged.AddListener(CambiarImporte);
        apostar.onClick.AddListener(PedirConfirmacion);
        confirmar.onClick.AddListener(Confirmar);
        cancelar.onClick.AddListener(Cancelar);
        filas.Clear();
        foreach (Transform child in content)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
        if (carrera != null)
            foreach (var participante in carrera.participantes)
            {
                var fila = Instantiate(caballoButtonPrefab, content).GetComponent<CaballoButtonUI>();
                TablonLayout.EstiloFila(fila);
                fila.Setup(juego.CrearVistaParticipante(participante), this);
                filas.Add(fila);
            }
        var anterior = carrera?.Buscar(seleccion ?? carrera.apuesta?.idCaballo);
        if (anterior == null && carrera?.participantes.Count > 0) anterior = carrera.participantes[0];
        SeleccionarCaballo(anterior == null ? null : juego.CrearVistaParticipante(anterior));
    }

    void OnDisable()
    {
        importe.onValueChanged.RemoveListener(CambiarImporte);
        apostar.onClick.RemoveListener(PedirConfirmacion);
        confirmar.onClick.RemoveListener(Confirmar);
        cancelar.onClick.RemoveListener(Cancelar);
    }

    public void SeleccionarCaballo(Caballo caballo)
    {
        elegido = caballo;
        importeConfirmacion = 0;
        foreach (var fila in filas) fila.RefrescarEtiqueta(carrera, elegido?.id);
        textoNombre.text = elegido == null ? "Participantes" : elegido.nombre + (elegido.esDelJugador ? " (tuyo)" : "");
        textoEdad.text = elegido == null ? "" : $"Edad: {elegido.edad} · Potencial {RangoCaballo.ObtenerRango(elegido.potencial)}";
        textoNivel.text = elegido == null ? "" : $"Nivel: {elegido.nivel} · Cuota {CuotaTexto(carrera.Buscar(elegido.id).cuotaDecimas)}";
        textoVelocidad.text = elegido == null ? "" : "Velocidad: " + RangoCaballo.ObtenerRango(elegido.velocidad);
        textoStamina.text = elegido == null ? "" : "Resistencia: " + RangoCaballo.ObtenerRango(elegido.stamina);
        textoSuerte.text = elegido == null ? "" : "Suerte: " + RangoCaballo.ObtenerRango(elegido.suerte);
        textoInteligencia.text = elegido == null ? "" : "Inteligencia: " + RangoCaballo.ObtenerRango(elegido.inteligencia);
        RefrescarApuesta();
    }

    void CambiarImporte(string _)
    {
        mensaje = null;
        RefrescarApuesta();
    }

    void PedirConfirmacion()
    {
        if (!apostar.interactable || !int.TryParse(importe.text, out int cantidad)) return;
        importeConfirmacion = cantidad;
        mensaje = null;
        RefrescarApuesta();
    }

    void Cancelar()
    {
        importeConfirmacion = 0;
        RefrescarApuesta();
    }

    void Confirmar()
    {
        if (importeConfirmacion <= 0 || elegido == null || carrera == null) return;
        GameManager.Instancia.Apostar(carrera.id, elegido.id, importeConfirmacion, out mensaje);
        importeConfirmacion = 0;
        RefrescarApuesta();
    }

    void RefrescarApuesta()
    {
        int dinero = GameManager.Instancia.dinero;
        bool abierta = carrera != null && !carrera.iniciada && carrera.apuesta == null;
        var participante = carrera?.Buscar(elegido?.id);
        bool valida = int.TryParse(importe.text, out int cantidad) && cantidad > 0 && cantidad <= dinero;
        importe.interactable = abierta && importeConfirmacion == 0;
        apostar.gameObject.SetActive(importeConfirmacion == 0);
        apostar.interactable = abierta && participante != null && valida;
        confirmar.gameObject.SetActive(importeConfirmacion > 0);
        cancelar.gameObject.SetActive(importeConfirmacion > 0);

        var confirmada = carrera?.apuesta;
        long pago = confirmada?.Pago ?? (abierta && valida && participante != null
            ? (long)cantidad * participante.cuotaDecimas / 10 : 0);
        saldo.text = $"Saldo: ${dinero}";
        textoPotencial.text = $"Pago estimado: ${pago}";

        if (confirmada != null)
        {
            feedback.text = $"Apuesta confirmada · {confirmada.nombreCaballo} · ${confirmada.cantidad} · {CuotaTexto(confirmada.cuotaDecimas)}";
            return;
        }
        if (importeConfirmacion > 0)
            feedback.text = $"Confirmar ${importeConfirmacion} por {elegido.nombre} · {CuotaTexto(participante.cuotaDecimas)}";
        else if (!string.IsNullOrEmpty(mensaje)) feedback.text = mensaje;
        else if (carrera == null && !GameManager.Instancia.campanaCompletada)
            feedback.text = "Elegí una carrera en el televisor para ver sus apuestas.";
        else if (!abierta) feedback.text = "Apuestas cerradas.";
        else if (participante == null) feedback.text = "Elegí un caballo de la lista.";
        else if (importe.text.Length > 0 && !valida) feedback.text = "Ingresá un importe positivo dentro de tu saldo.";
        else feedback.text = "";
    }

    internal static string CuotaTexto(int decimas) =>
        "×" + (decimas / 10m).ToString("0.0", CultureInfo.GetCultureInfo("es-AR"));
}
