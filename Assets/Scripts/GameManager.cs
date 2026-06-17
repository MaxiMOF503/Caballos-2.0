using UnityEngine;
using TMPro;

public enum GameState
{
    Hub,
    TV,
    Tienda,
    Establo,
    Tablon
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instancia;

    [Header("Estado")]
    public GameState estadoActual;

    [Header("Economía")]
    public int dinero = 1000;
    public int dia = 1;

    [Header("HUD")]
    public TextMeshProUGUI textoDinero;
    public TextMeshProUGUI textoDia;

    private void Awake()
    {
        Instancia = this;
    }

    private void Start()
    {
        ActualizarHUD();
    }

   
    // CAMBIO DE ESTADO
  
    public void SetState(Interactable.Tipo tipo)
    {
        switch (tipo)
        {
            case Interactable.Tipo.TV:
                estadoActual = GameState.TV;
                UIManager.instance.AbrirCarrera();
                break;

            case Interactable.Tipo.Tienda:
                estadoActual = GameState.Tienda;
                UIManager.instance.AbrirTienda();
                break;

            case Interactable.Tipo.Establo:
                estadoActual = GameState.Establo;
                UIManager.instance.AbrirEstablo();
                break;

            case Interactable.Tipo.Tablon:
                estadoActual = GameState.Tablon;
                UIManager.instance.AbrirTablon();
                break;
        }
    }

  
    // HUD / DINERO
  
    public void AgregarDinero(int cantidad)
    {
        dinero += cantidad;
        ActualizarHUD();
    }

    public void RestarDinero(int cantidad)
    {
        dinero -= cantidad;
        ActualizarHUD();
    }

    public void SiguienteDia()
    {
        dia++;
        ActualizarHUD();
    }

    void ActualizarHUD()
    {
        if (textoDinero != null)
            textoDinero.text = "$ " + dinero;

        if (textoDia != null)
            textoDia.text = "Día " + dia;
    }

  
    // VOLVER AL HUB
  
    public void VolverAlHub()
    {
        estadoActual = GameState.Hub;

        UIManager.instance.CerrarTodo();

        //  VOLVER A ACTIVAR LA CÁMARA
        CameraController.instancia.ActivarControl();
    }
}