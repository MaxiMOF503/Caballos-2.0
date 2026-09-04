using UnityEngine;

public enum TipoEstadisticaCaballo
{
    Velocidad,
    Stamina,
    Suerte,
    Inteligencia
}

public static class ProgresionCaballo
{
    public static int ExperienciaNecesariaParaSiguienteNivel(int nivel)
    {
        int nivelValido = Mathf.Max(1, nivel);
        return 100 + ((nivelValido - 1) * 50);
    }

    public static int AgregarExperiencia(Caballo caballo, int cantidad)
    {
        if (caballo == null)
        {
            Debug.LogWarning("No se puede agregar experiencia a un caballo nulo.");
            return 0;
        }

        if (cantidad <= 0)
            return 0;

        caballo.ValidarDatos();

        long experienciaAcumulada = (long)caballo.experiencia + cantidad;
        int nivelesSubidos = 0;

        while (experienciaAcumulada >= ExperienciaNecesariaParaSiguienteNivel(caballo.nivel))
        {
            experienciaAcumulada -= ExperienciaNecesariaParaSiguienteNivel(caballo.nivel);
            caballo.nivel++;
            nivelesSubidos++;
            caballo.puntosMejoraPendientes += CalcularPuntosPorNivel(caballo.potencial);
        }

        caballo.experiencia = experienciaAcumulada > int.MaxValue
            ? int.MaxValue
            : (int)experienciaAcumulada;

        caballo.ValidarDatos();

        if (nivelesSubidos > 0)
        {
            Debug.Log(caballo.nombre + " subió " + nivelesSubidos +
                      " nivel(es) y alcanzó el nivel " + caballo.nivel + ".");
        }

        if (GameManager.Instancia != null)
            GameManager.Instancia.GuardarPartida();

        return nivelesSubidos;
    }

    public static bool MejorarEstadistica(Caballo caballo, TipoEstadisticaCaballo estadistica)
    {
        if (caballo == null)
        {
            Debug.LogWarning("No se puede mejorar una estadística de un caballo nulo.");
            return false;
        }

        caballo.ValidarDatos();

        if (caballo.puntosMejoraPendientes <= 0)
            return false;

        int valorActual;

        switch (estadistica)
        {
            case TipoEstadisticaCaballo.Velocidad:
                valorActual = caballo.velocidad;
                break;

            case TipoEstadisticaCaballo.Stamina:
                valorActual = caballo.stamina;
                break;

            case TipoEstadisticaCaballo.Suerte:
                valorActual = caballo.suerte;
                break;

            case TipoEstadisticaCaballo.Inteligencia:
                valorActual = caballo.inteligencia;
                break;

            default:
                return false;
        }

        if (valorActual >= Caballo.EstadisticaMaxima)
            return false;

        switch (estadistica)
        {
            case TipoEstadisticaCaballo.Velocidad:
                caballo.velocidad++;
                break;

            case TipoEstadisticaCaballo.Stamina:
                caballo.stamina++;
                break;

            case TipoEstadisticaCaballo.Suerte:
                caballo.suerte++;
                break;

            case TipoEstadisticaCaballo.Inteligencia:
                caballo.inteligencia++;
                break;
        }

        caballo.puntosMejoraPendientes--;
        caballo.ValidarDatos();

        if (GameManager.Instancia != null)
            GameManager.Instancia.GuardarPartida();

        return true;
    }

    public static int CalcularPuntosPorNivel(int potencial)
    {
        int potencialValido = Caballo.NormalizarEstadistica(potencial);

        if (potencialValido == Caballo.EstadisticaMaxima)
            return 2;

        float probabilidadSegundaMejora = 0f;

        if (potencialValido >= 13)
            probabilidadSegundaMejora = 0.75f;
        else if (potencialValido >= 9)
            probabilidadSegundaMejora = 0.50f;
        else if (potencialValido >= 5)
            probabilidadSegundaMejora = 0.25f;

        if (probabilidadSegundaMejora <= 0f)
            return 1;

        return 1 + (Random.value < probabilidadSegundaMejora ? 1 : 0);
    }
}
