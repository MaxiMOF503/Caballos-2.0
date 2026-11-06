using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class BotonBannerCarrera : Button
{
    static BotonBannerCarrera bajoPuntero;
    Image brillo;

    internal void Configurar(Image imagen)
    {
        brillo = imagen;
        brillo.enabled = false;
    }

    public override void OnPointerEnter(PointerEventData evento)
    {
        base.OnPointerEnter(evento);
        if (bajoPuntero != null && bajoPuntero != this)
            bajoPuntero.brillo.enabled = false;
        bajoPuntero = this;
        brillo.enabled = IsInteractable();
    }

    public override void OnPointerExit(PointerEventData evento)
    {
        base.OnPointerExit(evento);
        brillo.enabled = false;
        if (bajoPuntero == this) bajoPuntero = null;
    }

    protected override void OnDisable()
    {
        if (brillo != null) brillo.enabled = false;
        if (bajoPuntero == this) bajoPuntero = null;
        base.OnDisable();
    }
}
