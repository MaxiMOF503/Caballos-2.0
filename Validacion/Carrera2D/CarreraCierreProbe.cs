using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public static class CarreraCierreProbe
{
    public static void Run()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Sólo validación batch aislada");
        var checks = new List<string>();
        var banda = Resources.Load<CatalogoCarreras>("Carreras/Catalogo").bandasRivales;
        foreach (int day in new[] { 1, 8, 15, 28 })
            foreach (int meters in new[] { 1200, 1600, 2100, 2400 })
                foreach (int stat in new[] { 1, 5, 8, 16 })
                    for (int seed = 0; seed < 16; seed++)
                    {
                        var active = new CaballoData { velocidad = stat, stamina = stat, suerte = stat, inteligencia = stat };
                        var rival = new Caballo(); banda.Aplicar(rival, active, meters, day, new System.Random(seed));
                        int phase = day >= banda.diaEtapaAvanzada ? banda.ajusteAvanzado : banda.ajusteInicial;
                        int distance = Mathf.RoundToInt(Mathf.Lerp(-banda.especialidadDistancia, banda.especialidadDistancia, Mathf.InverseLerp(1200, 2400, meters)));
                        if (!Dentro(rival.velocidad, stat + phase - distance, banda.dispersion) || !Dentro(rival.stamina, stat + phase + distance, banda.dispersion) ||
                            !Dentro(rival.inteligencia, stat + phase, banda.dispersion) || !Dentro(rival.suerte, stat, banda.dispersion)) throw new InvalidOperationException("Banda fuera del rango configurado");
                        checks.Add($"Banda acotada: día {day}, {meters}m, atributo {stat}, semilla {seed}");
                    }
        for (int seed = 0; seed < 32; seed++)
        {
            var active = new CaballoData { velocidad = 8, stamina = 8, suerte = 8, inteligencia = 8 };
            var shortRace = new Caballo(); var longRace = new Caballo(); var advanced = new Caballo();
            banda.Aplicar(shortRace, active, 1200, 1, new System.Random(seed));
            banda.Aplicar(longRace, active, 2400, 1, new System.Random(seed));
            banda.Aplicar(advanced, active, 1200, 15, new System.Random(seed));
            if (shortRace.velocidad - longRace.velocidad != 2 || longRace.stamina - shortRace.stamina != 2 || advanced.velocidad - shortRace.velocidad != 3)
                throw new InvalidOperationException("No se aplicaron distancia y etapa");
            checks.Add("Distancia y progresión cambian la especialidad sin ampliar la dispersión: " + seed);
        }
        File.WriteAllLines("C:/Caballos/Validation/Carrera2D/bandas-pruebas.txt", checks);
        Debug.Log("CARRERA BANDAS PASS: " + checks.Count + " comprobaciones");
        CatalogosWindowsProbe.Run();
        CatalogosAssetProbe.Run();
    }
    static bool Dentro(int value, int center, int band) => value >= Caballo.NormalizarEstadistica(center - band) && value <= Caballo.NormalizarEstadistica(center + band);
}
