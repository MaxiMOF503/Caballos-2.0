using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class TiendaProductoUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    static TiendaProductoUI bajoPuntero;
    internal string ProductoId { get; private set; }
    Image icono, brillo;
    GameObject seleccion;
    Button boton;

    internal void Configurar(Button control, Image imagen, Image hover, GameObject indicador, Action<string> seleccionar)
    {
        boton = control;
        icono = imagen;
        brillo = hover;
        seleccion = indicador;
        boton.onClick = new Button.ButtonClickedEvent();
        boton.onClick.AddListener(() => seleccionar(ProductoId));
    }

    internal void Mostrar(DefinicionProductoTienda producto)
    {
        ProductoId = producto?.idDefinicion;
        icono.sprite = producto?.sprite;
        icono.enabled = icono.sprite != null;
        boton.interactable = producto != null;
        brillo.enabled = false;
        seleccion.SetActive(false);
    }

    internal void Marcar(string id) => seleccion.SetActive(ProductoId != null && ProductoId == id);

    public void OnPointerEnter(PointerEventData evento)
    {
        if (bajoPuntero != null) bajoPuntero.brillo.enabled = false;
        bajoPuntero = this;
        brillo.enabled = ProductoId != null && boton.IsInteractable();
    }

    public void OnPointerExit(PointerEventData evento)
    {
        QuitarHover();
    }

    internal void QuitarHover()
    {
        brillo.enabled = false;
        if (bajoPuntero == this) bajoPuntero = null;
    }

    void OnDisable()
    {
        if (brillo != null) QuitarHover();
    }
}
