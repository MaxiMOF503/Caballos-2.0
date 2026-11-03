using UnityEngine;
using UnityEngine.EventSystems;

// One renderer state: pointer takes priority over genuine keyboard focus.
public sealed class MenuDiamondSelection : MonoBehaviour
{
    public MenuDiamondOption[] options;
    public MenuDiamondOption ActiveOption { get; private set; }
    bool keyboard;
    MenuDiamondOption hovered, keyboardOption;
    CanvasGroup menuGroup;

    void Awake() => menuGroup = GetComponent<CanvasGroup>();
    void OnEnable() => ClearAllHighlights();
    void OnDisable() => ClearAllHighlights();
    void LateUpdate()
    {
        if (menuGroup != null && (!menuGroup.interactable || !menuGroup.blocksRaycasts))
        { ClearAllHighlights(); return; }
        ApplyCurrentSelection();
    }
    public void ClearAllHighlights() { keyboard=false; hovered=null; keyboardOption=null; Show(null); }
    public void ApplyCurrentSelection()
    {
        if (keyboardOption != null && (!keyboardOption.CanHighlight || EventSystem.current == null ||
            EventSystem.current.currentSelectedGameObject != keyboardOption.gameObject)) keyboardOption=null;
        Show(hovered != null && hovered.CanHighlight ? hovered : keyboardOption);
    }
    void Show(MenuDiamondOption option)
    {
        if (option != null && !option.CanHighlight) option = null;
        ActiveOption = option;
        if (options == null) return;
        foreach (var entry in options)
            if (entry != null && entry.indicator != null)
                entry.indicator.gameObject.SetActive(false);
        if (option != null && option.indicator != null) option.indicator.gameObject.SetActive(true);
    }
    public void PointerEnter(MenuDiamondOption option) { keyboard = false; hovered=option; ApplyCurrentSelection(); }
    public void PointerDown(MenuDiamondOption option) { keyboardOption=null; PointerEnter(option); }
    public void PointerExit(MenuDiamondOption option)
    {
        if (hovered==option) hovered=null;
        ApplyCurrentSelection();
    }
    public void KeyboardMove()
    {
        keyboard = true;
        hovered=null;
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        MenuDiamondOption match = null;
        if (options != null)
            foreach (var option in options)
                if (option != null && option.gameObject == selected) { match = option; break; }
        keyboardOption=match; ApplyCurrentSelection();
    }
    public void Selected(MenuDiamondOption option)
    { if(keyboard) keyboardOption=option; else if(keyboardOption!=option) keyboardOption=null; ApplyCurrentSelection(); }
    public void Deselected(MenuDiamondOption option) { if(keyboardOption==option) keyboardOption=null; ApplyCurrentSelection(); }
    public void Release(MenuDiamondOption option)
    { if(hovered==option) hovered=null; if(keyboardOption==option) keyboardOption=null; ApplyCurrentSelection(); }
}
