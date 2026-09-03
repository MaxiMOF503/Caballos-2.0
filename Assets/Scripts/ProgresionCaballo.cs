using UnityEngine;

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

            AplicarCrecimientoPorNivel(caballo);
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

    private static void AplicarCrecimientoPorNivel(Caballo caballo)
    {
        int oportunidades = ObtenerOportunidadesDeMejora(caballo.potencial);

        for (int i = 0; i < oportunidades; i++)
        {
            if (!MejorarEstadisticaAleatoria(caballo))
                break;
        }

        caballo.ValidarDatos();
    }

    private static int ObtenerOportunidadesDeMejora(int potencial)
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

        return 1 + (Random.value < probabilidadSegundaMejora ? 1 : 0);
    }

    private static bool MejorarEstadisticaAleatoria(Caballo caballo)
    {
        int[] estadisticasDisponibles = new int[4];
        int cantidadDisponible = 0;

        if (caballo.velocidad < Caballo.EstadisticaMaxima)
            estadisticasDisponibles[cantidadDisponible++] = 0;

        if (caballo.stamina < Caballo.EstadisticaMaxima)
            estadisticasDisponibles[cantidadDisponible++] = 1;

        if (caballo.suerte < Caballo.EstadisticaMaxima)
            estadisticasDisponibles[cantidadDisponible++] = 2;

        if (caballo.inteligencia < Caballo.EstadisticaMaxima)
            estadisticasDisponibles[cantidadDisponible++] = 3;

        if (cantidadDisponible == 0)
            return false;

        int estadisticaElegida = estadisticasDisponibles[Random.Range(0, cantidadDisponible)];

        switch (estadisticaElegida)
        {
            case 0:
                caballo.velocidad++;
                break;

            case 1:
                caballo.stamina++;
                break;

            case 2:
                caballo.suerte++;
                break;

            case 3:
                caballo.inteligencia++;
                break;
        }

        return true;
    }
}
