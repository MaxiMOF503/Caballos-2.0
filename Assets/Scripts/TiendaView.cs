using TMPro;
using UnityEngine;

public class TiendaView : MonoBehaviour
{
    [Header("Textos")]
    public TMP_Text textoNombre;
    public TMP_Text textoDescripcion;
    public TMP_Text textoPrecio;

    [Header("Botones")]
    public GameObject botonComprar;
    public GameObject botonSi;
    public GameObject botonNo;

    void Start()
    {
        ReiniciarConfirmacion();
    }

    private void ReiniciarConfirmacion()
    {
        botonComprar.SetActive(true);

        botonSi.SetActive(false);
        botonNo.SetActive(false);
    }

    public void SeleccionarHerraduraPlata()
    {
        ReiniciarConfirmacion();

        textoNombre.text = "Herradura de Plata";
        textoDescripcion.text = "Una herradura de buena calidad que mejora ligeramente el rendimiento del caballo.";
        textoPrecio.text = "Precio: $200";
    }

    public void SeleccionarManzanaDorada()
    {
        ReiniciarConfirmacion();

        textoNombre.text = "Manzana Dorada";
        textoDescripcion.text = "Una fruta especial que aumenta considerablemente la energía del caballo.";
        textoPrecio.text = "Precio: $350";
    }

    public void SeleccionarHerraduraOro()
    {
        ReiniciarConfirmacion();

        textoNombre.text = "Herradura de Oro";
        textoDescripcion.text = "Una herradura de lujo que mejora notablemente la velocidad del caballo.";
        textoPrecio.text = "Precio: $500";
    }

    public void SeleccionarZapatillas()
    {
        ReiniciarConfirmacion();

        textoNombre.text = "Zapatillas";
        textoDescripcion.text = "Equipamiento ligero que ayuda a mejorar el desempeño en carrera.";
        textoPrecio.text = "Precio: $150";
    }

    public void SeleccionarSombreroCopa()
    {
        ReiniciarConfirmacion();

        textoNombre.text = "Sombrero de Copa";
        textoDescripcion.text = "Un elegante accesorio para destacar entre los demás propietarios.";
        textoPrecio.text = "Precio: $300";
    }

    public void SeleccionarSombreroNoble()
    {
        ReiniciarConfirmacion();

        textoNombre.text = "Sombrero Noble";
        textoDescripcion.text = "Accesorio exclusivo reservado para los criadores más prestigiosos.";
        textoPrecio.text = "Precio: $450";
    }

    public void MostrarConfirmacion()
    {
        botonComprar.SetActive(false);

        botonSi.SetActive(true);
        botonNo.SetActive(true);
    }

    public void CancelarCompra()
    {
        ReiniciarConfirmacion();
    }

    public void ConfirmarCompra()
    {
        Debug.Log("Compra confirmada");

        ReiniciarConfirmacion();
    }
}