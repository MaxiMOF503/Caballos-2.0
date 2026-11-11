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
        GameManager.Instancia.SetState(tipo);
    }
}
