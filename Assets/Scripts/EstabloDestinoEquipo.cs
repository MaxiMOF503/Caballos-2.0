using UnityEngine;
using UnityEngine.EventSystems;

internal sealed class EstabloDestinoEquipo : MonoBehaviour, IDropHandler
{
    private System.Action<ItemCaballo> recibir;

    internal void Configurar(System.Action<ItemCaballo> alSoltar) => recibir = alSoltar;

    public void OnDrop(PointerEventData evento)
    {
        if (evento.button == PointerEventData.InputButton.Left &&
            evento.pointerDrag != null &&
            evento.pointerDrag.TryGetComponent<EstabloArrastre>(out var arrastre))
            recibir(arrastre.Item);
    }
}
