using UnityEngine;

[System.Serializable]
public class Caballo
{
    public const int EstadisticaMinima = 1;
    public const int EstadisticaMaxima = 16;

    public string id = System.Guid.NewGuid().ToString("N");
    public string nombre;
    public int edad = 3;

    public int nivel = 1;
    public int experiencia = 0;
    public int puntosMejoraPendientes = 0;

    public int velocidad = EstadisticaMinima;
    public int stamina = EstadisticaMinima;
    public int suerte = EstadisticaMinima;
    public int inteligencia = EstadisticaMinima;

    public int potencial = EstadisticaMinima;
    public int relacion;
    public int estres;

    public bool esDelJugador;

    public string visualProfileId;
    public Sprite sprite;

    public void ValidarDatos()
    {
        if (!System.Guid.TryParseExact(id, "N", out _))
            id = System.Guid.NewGuid().ToString("N");

        edad = Mathf.Clamp(edad, 3, 7);
        nivel = Mathf.Max(1, nivel);
        experiencia = Mathf.Max(0, experiencia);
        puntosMejoraPendientes = Mathf.Max(0, puntosMejoraPendientes);

        velocidad = NormalizarEstadistica(velocidad);
        stamina = NormalizarEstadistica(stamina);
        suerte = NormalizarEstadistica(suerte);
        inteligencia = NormalizarEstadistica(inteligencia);
        potencial = NormalizarEstadistica(potencial);
    }

    public static int NormalizarEstadistica(int valor)
    {
        return Mathf.Clamp(valor, EstadisticaMinima, EstadisticaMaxima);
    }
}
