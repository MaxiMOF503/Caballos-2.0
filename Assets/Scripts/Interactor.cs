using UnityEngine;

public class Interactor : MonoBehaviour
{
    public Camera cam;

    void Update()
    {
        // Solo permitir interacción cuando estamos en el HUB
        if (MenuPausa.BloqueaEntrada || GameManager.Instancia.estadoActual != GameState.Hub)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Interactable interactable = BuscarInteractuable(Input.mousePosition);
            if (interactable != null) interactable.Interact();
        }
    }

    // La flecha y el clic consultan el mismo collider, con las mismas reglas.
    internal Interactable BuscarInteractuable(Vector2 posicion)
    {
        if (cam == null) return null;
        Ray ray = cam.ScreenPointToRay(posicion);
        return Physics.Raycast(ray, out RaycastHit hit)
            ? hit.collider.GetComponent<Interactable>()
            : null;
    }
}
