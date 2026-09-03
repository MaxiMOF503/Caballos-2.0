using System;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    public const int SaveVersionActual = 1;
    private const string NombreArchivo = "gameData.json";

    public static string RutaArchivo =>
        Path.Combine(Application.persistentDataPath, NombreArchivo);

    public static bool Guardar(GameData datos)
    {
        return GuardarEnRuta(datos, RutaArchivo);
    }

    public static bool IntentarCargar(out GameData datos)
    {
        return IntentarCargarDesdeRuta(RutaArchivo, out datos);
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
