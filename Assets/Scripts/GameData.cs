using System;
using System.Collections.Generic;

[Serializable]
public class GameData
{
    public int saveVersion = 1;
    public int dinero;
    public int dia;
    public List<CaballoData> caballosJugador = new List<CaballoData>();
    public CaballoData rivalPersistente;
}

[Serializable]
public class CaballoData
{
    public string id;
    public string nombre;
    public int edad;
    public int nivel;
    public int experiencia;
    public int velocidad;
    public int stamina;
    public int suerte;
    public int inteligencia;
    public int potencial;
    public int relacion;
    public int estres;
    public bool esDelJugador;
    public int spriteIndex = -1;

    public static CaballoData DesdeCaballo(Caballo caballo, int indiceSprite)
    {
        return new CaballoData
        {
            id = caballo.id,
            nombre = caballo.nombre,
            edad = caballo.edad,
            nivel = caballo.nivel,
            experiencia = caballo.experiencia,
            velocidad = caballo.velocidad,
            stamina = caballo.stamina,
            suerte = caballo.suerte,
            inteligencia = caballo.inteligencia,
            potencial = caballo.potencial,
            relacion = caballo.relacion,
            estres = caballo.estres,
            esDelJugador = caballo.esDelJugador,
            spriteIndex = indiceSprite
        };
    }

    public Caballo CrearCaballo()
    {
        Caballo caballo = new Caballo
        {
            id = id,
            nombre = nombre,
            edad = edad,
            nivel = nivel,
            experiencia = experiencia,
            velocidad = velocidad,
            stamina = stamina,
            suerte = suerte,
            inteligencia = inteligencia,
            potencial = potencial,
            relacion = relacion,
            estres = estres,
            esDelJugador = esDelJugador
        };

        caballo.ValidarDatos();
        return caballo;
    }
}
