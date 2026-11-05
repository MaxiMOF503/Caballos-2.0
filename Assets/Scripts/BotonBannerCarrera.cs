using UnityEngine.EventSystems;
using UnityEngine.UI;

internal sealed class BotonBannerCarrera : Button
{
    Image brillo;

    internal void Configurar(Image imagen)
    {
        brillo = imagen;
        brillo.enabled = false;
    }

    public override void OnPointerEnter(PointerEventData evento)
    {
        base.OnPointerEnter(evento);
        brillo.enabled = IsInteractable();
    }

    public override void OnPointerExit(PointerEventData evento)
    {
        base.OnPointerExit(evento);
        brillo.enabled = false;
    }

    protected override void OnDisable()
    {
        if (brillo != null) brillo.enabled = false;
        base.OnDisable();
    }
}
