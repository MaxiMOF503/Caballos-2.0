using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MenuDiamondOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerMoveHandler, IPointerDownHandler, ISelectHandler, IDeselectHandler, IMoveHandler
{
    public Button button;
    public MenuDiamondSelection selection;
    public MenuDiamondGlow indicator;
    public bool CanHighlight => isActiveAndEnabled && button != null && button.IsActive() && button.IsInteractable();

    public void OnPointerEnter(PointerEventData e) => selection.PointerEnter(this);
    public void OnPointerMove(PointerEventData e) => selection.PointerEnter(this);
    public void OnPointerDown(PointerEventData e) => selection.PointerDown(this);
    public void OnPointerExit(PointerEventData e) => selection.PointerExit(this);
    public void OnSelect(BaseEventData e) => selection.Selected(this);
    public void OnDeselect(BaseEventData e) => selection.Deselected(this);
    // Button still handles movement/submission/clicks; this component only observes.
    public void OnMove(AxisEventData e) => selection.KeyboardMove();
    void OnDisable() { if (selection != null) selection.Release(this); }
}
