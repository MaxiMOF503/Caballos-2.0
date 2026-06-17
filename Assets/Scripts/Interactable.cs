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
        // enfocar cámara
        CameraController.instancia.EnfocarObjeto(transform);

        // cambiar estado
        GameManager.Instancia.SetState(tipo);
    }
}