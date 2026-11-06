using UnityEngine;

public class Interactable : MonoBehaviour
{
    public enum Tipo
    {
        TV,
        Tienda,
        Establo,
        Tablon
    }

    public Tipo tipo;

    public void Interact()
    {
        if (tipo == Tipo.TV && GameManager.Instancia.ObtenerCarreraProgramada() == null)
        {
            GameManager.Instancia.SetState(tipo);
            return;
        }
        // enfocar cámara
        CameraController.instancia.EnfocarObjeto(transform);

        // cambiar estado
        GameManager.Instancia.SetState(tipo);
    }
}
