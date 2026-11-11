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
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                Interactable interactable = hit.collider.GetComponent<Interactable>();

                if (interactable != null)
                {
                    interactable.Interact();
                }
            }
        }
    }
}
