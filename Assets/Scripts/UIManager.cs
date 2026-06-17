using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    public GameObject viewCarrera;
    public GameObject viewTienda;
    public GameObject viewEstablo;
    public GameObject viewTablon;

    private void Awake()
    {
        instance = this;
    }

    public void CerrarTodo()
    {
        viewCarrera.SetActive(false);
        viewTienda.SetActive(false);
        viewEstablo.SetActive(false);
        viewTablon.SetActive(false);
    }

    public void AbrirCarrera()
    {
        CerrarTodo();
        viewCarrera.SetActive(true);
    }

    public void AbrirTienda()
    {
        CerrarTodo();
        viewTienda.SetActive(true);
    }

    public void AbrirEstablo()
    {
        CerrarTodo();
        viewEstablo.SetActive(true);
    }

    public void AbrirTablon()
    {
        CerrarTodo();
        viewTablon.SetActive(true);
    }
}