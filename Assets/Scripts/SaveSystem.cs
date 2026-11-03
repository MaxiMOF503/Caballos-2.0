using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public const int SaveVersionActual = 1;
    private const string NombreArchivo = "gameData.json";

    public static string RutaArchivo =>
        Path.Combine(DirectorioGuardado, NombreArchivo);

    private static string DirectorioGuardado
    {
        get
        {
#if UNITY_EDITOR
            // Batch validation uses a separate save; never overwrites the player's campaign.
            string validationPath = Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH");
            if (string.IsNullOrEmpty(validationPath))
                validationPath = UnityEditor.SessionState.GetString("HorseVisualRecovery.SavePath", "");
            if (!string.IsNullOrEmpty(validationPath)) return validationPath;
#endif
            return Application.persistentDataPath;
        }
    }

    public static bool Guardar(GameData datos)
    {
        return GuardarEnRuta(datos, RutaArchivo);
    }

    public static bool IntentarCargar(out GameData datos)
    {
        return IntentarCargarDesdeRuta(RutaArchivo, out datos);
    }

    // Menu preflight: read only. In particular, corrupt files are neither
    // replaced nor backed up as a side effect of displaying the menu.
    public static bool PuedeCargar(out string error)
    {
        error = null;
        if (!File.Exists(RutaArchivo))
        {
            error = "No se encontró ninguna partida guardada.";
            return false;
        }
        try
        {
            var datos = JsonUtility.FromJson<GameData>(File.ReadAllText(RutaArchivo));
            if (datos == null || datos.saveVersion != SaveVersionActual ||
                datos.caballosJugador == null || datos.caballosJugador.Count == 0 ||
                datos.caballosJugador.Exists(c => c == null))
            {
                error = "La partida guardada es inválida o tiene una versión incompatible. El archivo se conservó.";
                return false;
            }
            return true;
        }
        catch (Exception)
        {
            error = "No se pudo leer la partida guardada. Puede estar dañada o no ser accesible. El archivo se conservó.";
            return false;
        }
    }

    public static void RespaldarPartidaInvalida()
    {
        RespaldarArchivoInvalido(RutaArchivo);
    }

    internal static bool GuardarEnRuta(GameData datos, string ruta)
    {
        string rutaTemporal = ruta + ".tmp";

        try
        {
            string directorio = Path.GetDirectoryName(ruta);
            if (!string.IsNullOrEmpty(directorio))
                Directory.CreateDirectory(directorio);

            string json = JsonUtility.ToJson(datos, true);
            File.WriteAllText(rutaTemporal, json);

            if (File.Exists(ruta))
                ReemplazarArchivo(rutaTemporal, ruta);
            else
                File.Move(rutaTemporal, ruta);

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("No se pudo guardar la partida: " + ex.Message);
            return false;
        }
        finally
        {
            if (File.Exists(rutaTemporal))
            {
                try
                {
                    File.Delete(rutaTemporal);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("No se pudo eliminar el archivo temporal de guardado: " + ex.Message);
                }
            }
        }
    }

    internal static bool IntentarCargarDesdeRuta(string ruta, out GameData datos)
    {
        datos = null;

        if (!File.Exists(ruta))
            return false;

        try
        {
            string json = File.ReadAllText(ruta);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning("El archivo de guardado está vacío.");
                RespaldarArchivoInvalido(ruta);
                return false;
            }

            datos = JsonUtility.FromJson<GameData>(json);
            if (datos == null)
            {
                Debug.LogWarning("El archivo de guardado no contiene datos válidos.");
                RespaldarArchivoInvalido(ruta);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("No se pudo cargar la partida: " + ex.Message);
            RespaldarArchivoInvalido(ruta);
            datos = null;
            return false;
        }
    }

    private static void ReemplazarArchivo(string rutaTemporal, string rutaDestino)
    {
        try
        {
            File.Replace(rutaTemporal, rutaDestino, null);
        }
        catch (PlatformNotSupportedException)
        {
            File.Copy(rutaTemporal, rutaDestino, true);
            File.Delete(rutaTemporal);
        }
    }

    private static void RespaldarArchivoInvalido(string ruta)
    {
        if (!File.Exists(ruta))
            return;

        try
        {
            string rutaRespaldo = ruta + ".corrupt";
            File.Copy(ruta, rutaRespaldo, true);
            Debug.LogWarning("Se conservó una copia del guardado inválido en: " + rutaRespaldo);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("No se pudo respaldar el archivo de guardado inválido: " + ex.Message);
        }
    }
}
