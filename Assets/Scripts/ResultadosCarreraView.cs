using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Created only inside the race view; the other canvases and scene assets stay intact.
internal sealed class ResultadosCarreraView
{
    static readonly Vector2 Lienzo = new Vector2(1672, 941);
    static readonly Color Bronce = new Color32(166, 113, 57, 255);
    static readonly Color Claro = new Color32(250, 237, 210, 255);
    static readonly Color Tinta = new Color32(49, 29, 19, 255);
    readonly CarreraUI owner;
    RectTransform root;
    TMP_FontAsset fuente;
    TMP_Text estadoGuardado;
    Button volver, reintentar;

    internal ResultadosCarreraView(CarreraUI owner) { this.owner = owner; }
    internal void Ocultar() { if (root != null) UnityEngine.Object.Destroy(root.gameObject); root = null; }

    internal void Mostrar(ResultadoCarrera resultado, string balance)
    {
        Ocultar();
        if (resultado == null || !resultado.Valido) return;
        fuente = owner.textoEstado.font;
        root = TablonLayout.Rect("ResultadosCarrera", (RectTransform)UIManager.instance.viewCarrera.transform,
            new Rect(0, 0, Lienzo.x, Lienzo.y), Lienzo);
        root.SetAsLastSibling();
        var madera = UIManager.instance.viewEstablo.GetComponentInChildren<EstabloView>(true).TexturaMadera;
        TablonLayout.Wood("Madera", root, new Rect(0, 0, Lienzo.x, Lienzo.y), Lienzo, madera).GetComponent<Graphic>().raycastTarget = true;
        Marco(root, new Rect(14, 14, 1644, 913), Lienzo, new Color(0, 0, 0, .22f));
        Texto("Titulo", root, new Rect(70, 26, 1532, 63), Lienzo, "RESULTADOS DE CARRERA", 43);
        Texto("Carrera", root, new Rect(70, 92, 1532, 40), Lienzo,
            resultado.carrera.NombreVisible + " · " + resultado.carrera.distanciaMetros + " m", 26);

        var lista = resultado.carrera.clasificacionFinal;
        for (int i = 0; i < Math.Min(3, lista.Count); i++)
        {
            var llegada = lista[i];
            if (!llegada.Finalizo) continue;
            var participante = resultado.carrera.Buscar(llegada.idCaballo).caballo;
            Rect area = i == 0 ? new Rect(590, 146, 492, 310)
                : i == 1 ? new Rect(87, 184, 451, 272) : new Rect(1134, 204, 451, 252);
            var tarjeta = Marco(root, area, Lienzo, new Color(.12f, .065f, .035f, .78f));
            string titulo = i == 0 ? "1.º · GANADOR" : i == 1 ? "2.º · SUBCAMPEÓN" : "3.º · TERCER LUGAR";
            Texto("Puesto", tarjeta, new Rect(10, 10, area.width - 20, 38), area.size, titulo, i == 0 ? 28 : 23);
            var identidad = CatalogoCaballos.Buscar(participante.catalogIdentityId);
            // No legacy fallback or an unrelated example horse in the podium.
            var splash = identidad == null || identidad.visualProfileId != participante.visualProfileId
                ? null : RecursosCatalogo.Cargar(identidad.splashRecurso);
            if (splash != null)
            {
                var retrato = TablonLayout.Rect("Splash_" + llegada.idCaballo, tarjeta,
                    new Rect(35, 52, area.width - 70, area.height - 130), area.size).gameObject.AddComponent<Image>();
                retrato.sprite = HorseVisualCatalog.Default.Splash(participante.CrearCaballo());
                retrato.preserveAspect = true;
                retrato.raycastTarget = false;
            }
            Texto("Nombre", tarjeta, new Rect(10, area.height - 76, area.width - 20, 38), area.size, participante.nombre, i == 0 ? 31 : 27);
            Texto("Tiempo", tarjeta, new Rect(10, area.height - 36, area.width - 20, 29), area.size, FormatoTiempo(llegada), 21);
        }
        if (!lista[0].Finalizo)
            Texto("SinGanador", root, new Rect(180, 210, 1312, 130), Lienzo,
                "Carrera sin llegadas válidas\nSin ganador · Consultá las incidencias", 34);

        Texto("ClasificacionTitulo", root, new Rect(78, 475, 1516, 38), Lienzo, "CLASIFICACIÓN COMPLETA", 27);
        var panel = Marco(root, new Rect(78, 520, 1516, 237), Lienzo, new Color(.10f, .05f, .025f, .75f));
        panel.GetComponent<Image>().raycastTarget = true;
        var viewport = TablonLayout.Rect("Viewport", panel, new Rect(12, 8, 1492, 221), new Vector2(1516, 237));
        viewport.gameObject.AddComponent<RectMask2D>();
        var contenido = TablonLayout.Rect("Filas", viewport, new Rect(0, 0, 1492, 221), new Vector2(1492, 221));
        contenido.anchorMin = new Vector2(0, 1); contenido.anchorMax = Vector2.one; contenido.pivot = new Vector2(.5f, 1);
        contenido.sizeDelta = new Vector2(0, Math.Max(221, lista.Count * 35));
        float alto = contenido.sizeDelta.y;
        for (int i = 0; i < lista.Count; i++)
        {
            var llegada = lista[i];
            var caballo = resultado.carrera.Buscar(llegada.idCaballo).caballo;
            bool propio = llegada.idCaballo == resultado.idCaballoJugador;
            var fila = TablonLayout.Surface("Fila_" + llegada.idCaballo, contenido,
                new Rect(0, i * 35, 1492, 34), new Vector2(1492, alto), propio ? new Color(.44f, .28f, .12f, .6f) : Color.clear);
            Texto("Posicion", fila, new Rect(12, 0, 90, 34), new Vector2(1492, 34), llegada.Finalizo ? llegada.posicion + ".º" : "N/C", 23);
            Texto("Nombre", fila, new Rect(117, 0, 1010, 34), new Vector2(1492, 34), caballo.nombre + (propio ? "  ·  Tu caballo" : ""), 23, TextAlignmentOptions.MidlineLeft);
            Texto("Tiempo", fila, new Rect(1135, 0, 345, 34), new Vector2(1492, 34), FormatoTiempo(llegada), 22, TextAlignmentOptions.MidlineRight);
        }
        var scroll = panel.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.content = contenido; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        LlegadaCarrera propioResultado = null;
        foreach (var llegada in lista) if (llegada.idCaballo == resultado.idCaballoJugador) propioResultado = llegada;
        string personal = propioResultado == null ? "" : propioResultado.Finalizo
            ? $"Tu caballo: {propioResultado.posicion}.º de {lista.Count} participantes"
            : $"Tu caballo: No finalizó · {lista.Count} participantes";
        Texto("ResultadoPersonal", root, new Rect(78, 773, 1120, 38), Lienzo, personal, 28, TextAlignmentOptions.MidlineLeft);
        Texto("Balance", root, new Rect(78, 817, 1120, 50), Lienzo, balance, 22, TextAlignmentOptions.MidlineLeft);
        estadoGuardado = Texto("EstadoGuardado", root, new Rect(78, 873, 1120, 32), Lienzo, "", 18, TextAlignmentOptions.MidlineLeft);
        volver = Boton("Volver", new Rect(1232, 788, 345, 100), "Volver al Hub", owner.Volver);
        reintentar = Boton("ReintentarGuardado", new Rect(1232, 788, 345, 100), "Reintentar guardado", () => { GameManager.Instancia.GuardarPartida(); ActualizarGuardado(); });
        ActualizarGuardado();
    }

    internal void ActualizarGuardado()
    {
        if (root == null) return;
        bool ok = GameManager.Instancia.ResultadoCarreraGuardado;
        volver.gameObject.SetActive(ok); reintentar.gameObject.SetActive(!ok);
        estadoGuardado.text = ok ? "Resultado guardado" : "No se pudo guardar. Reintentá antes de salir. La copia anterior en disco se conserva.";
    }

    Button Boton(string nombre, Rect area, string etiqueta, UnityEngine.Events.UnityAction accion)
    {
        var rect = Marco(root, area, Lienzo, Claro);
        rect.name = nombre;
        var imagen = rect.GetComponent<Image>(); imagen.raycastTarget = true;
        var boton = rect.gameObject.AddComponent<Button>();
        boton.targetGraphic = imagen; boton.transition = Selectable.Transition.None;
        boton.onClick.AddListener(accion);
        Texto("Etiqueta", rect, new Rect(8, 8, area.width - 16, area.height - 16), area.size, etiqueta, 26).color = Tinta;
        return boton;
    }

    static RectTransform Marco(RectTransform padre, Rect area, Vector2 medida, Color relleno)
    {
        var rect = TablonLayout.Surface("Marco", padre, area, medida, relleno);
        // Four thin bars preserve the wood underneath the translucent panels.
        TablonLayout.Surface("Arriba", rect, new Rect(0, 0, area.width, 2), area.size, Bronce);
        TablonLayout.Surface("Abajo", rect, new Rect(0, area.height - 2, area.width, 2), area.size, Bronce);
        TablonLayout.Surface("Izquierda", rect, new Rect(0, 0, 2, area.height), area.size, Bronce);
        TablonLayout.Surface("Derecha", rect, new Rect(area.width - 2, 0, 2, area.height), area.size, Bronce);
        return rect;
    }

    TMP_Text Texto(string nombre, RectTransform padre, Rect area, Vector2 medida, string valor, float tamano,
        TextAlignmentOptions alineacion = TextAlignmentOptions.Center) =>
        TablonLayout.Label(nombre, padre, area, medida, fuente, valor, tamano, alineacion, Claro);

    internal static string FormatoTiempo(LlegadaCarrera llegada) => !llegada.Finalizo ? "No finalizó"
        : $"{Math.Floor(llegada.tiempo / 60):00}:{llegada.tiempo % 60:00.000}";
}
