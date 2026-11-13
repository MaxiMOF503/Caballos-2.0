using System;
using System.Collections.Generic;
using System.Linq;

public enum DatoSubasta { Edad, Nivel, Potencial, Velocidad, Stamina, Suerte, Inteligencia }
public enum ResultadoSubasta { Abierta, Ganada, Perdida, SinOfertas, Cancelada }

[Serializable]
public sealed class OfertaSubasta
{
    public string id;
    public int dia;
    public string postorId;
    public int importe;
}

[Serializable]
public sealed class PostorSubasta
{
    public string id;
    public string nombre;
    public int presupuesto;
    public bool retirado;
}

[Serializable]
public sealed class AvisoSubasta
{
    public string id;
    public string texto;
    public bool leido;
    public int diaLimite;
}

[Serializable]
public sealed class SubastaData
{
    public string id;
    public int diaInicio, diaFin, ultimoDiaProcesado, semilla;
    public CaballoData caballo;
    public int valorEstimado, precioInicial, pujaActual, reserva;
    public string postorId;
    public bool siguiendo;
    public ResultadoSubasta resultado;
    public List<DatoSubasta> revelaciones = new();
    public List<PostorSubasta> postores = new();
    public List<OfertaSubasta> ofertas = new();

    public bool LideraJugador => postorId == SistemaSubastas.Jugador;
    public int DiaDelCiclo(int dia) => Math.Clamp(dia - diaInicio + 1, 1, 4);
    public bool Visible(DatoSubasta dato, int dia)
    {
        int cantidad = DiaDelCiclo(dia) >= 4 ? 2 : DiaDelCiclo(dia) >= 2 ? 1 : 0;
        return revelaciones.Take(cantidad).Contains(dato);
    }
    public string NombrePostor => LideraJugador ? "Vos" : postores.Find(p => p.id == postorId)?.nombre ?? "Sin ofertas";
}

[Serializable]
public sealed class EstadoSubastasData
{
    public SubastaData actual;
    public List<SubastaData> historial = new();
    public List<AvisoSubasta> avisos = new();
}

public static class SistemaSubastas
{
    public const string Jugador = "jugador";
    public const int DiasPorSubasta = 4;
    static readonly string[] Establos = { "Establo Norte", "La Ribera", "Los Álamos" };

    public static int Incremento(int precio) => Math.Max(25, ((precio / 10 + 24) / 25) * 25);
    public static int Minima(SubastaData subasta) => subasta.pujaActual == 0
        ? subasta.precioInicial : (int)Math.Min(int.MaxValue, (long)subasta.pujaActual + Incremento(subasta.pujaActual));

    public static SubastaData Crear(int dia, string[] nombres, HorseVisualCatalog catalogo, SubastaData anterior, int? semilla = null)
    {
        var azar = new Random(semilla ?? Guid.NewGuid().GetHashCode());
        var identidades = CatalogoCaballos.Todos.Where(c => nombres.Contains(c.nombre) &&
            c.nombre != anterior?.caballo.nombre && c.visualProfileId != anterior?.caballo.visualProfileId &&
            catalogo?.Resolve(c.visualProfileId)?.IsValid == true).ToArray();
        if (identidades.Length == 0)
            throw new InvalidOperationException("La subasta necesita perfiles visuales válidos.");
        var identidad = identidades[azar.Next(identidades.Length)];
        int semana = GameManager.ObtenerSemana(dia);
        int maximo = Math.Min(10, 7 + semana / 2);
        var caballo = new Caballo
        {
            id = CrearId(azar),
            edad = azar.Next(identidad.edadMinima, identidad.edadMaxima + 1), nivel = 1,
            velocidad = Math.Clamp(azar.Next(3, maximo + 1) + identidad.ajusteVelocidad, 3, maximo),
            stamina = Math.Clamp(azar.Next(3, maximo + 1) + identidad.ajusteStamina, 3, maximo),
            suerte = Math.Clamp(azar.Next(2, maximo + 1) + identidad.ajusteSuerte, 2, maximo),
            inteligencia = Math.Clamp(azar.Next(3, maximo + 1) + identidad.ajusteInteligencia, 3, maximo), relacion = 25
        };
        CatalogoCaballos.Asignar(caballo, identidad);
        caballo.potencial = Math.Clamp(Math.Max(caballo.velocidad, caballo.stamina) + azar.Next(1, 5),
            identidad.potencialMinimo, identidad.potencialMaximo);
        int inicio = (Math.Max(1, dia) - 1) / DiasPorSubasta * DiasPorSubasta + 1;
        int valor = 250 + (caballo.velocidad + caballo.stamina + caballo.suerte + caballo.inteligencia) * 25 +
            caballo.potencial * 20 + (7 - caballo.edad) * 25;
        var subasta = new SubastaData
        {
            id = CrearId(azar), diaInicio = inicio, diaFin = inicio + 3,
            ultimoDiaProcesado = dia, semilla = azar.Next(), caballo = CaballoData.DesdeCaballo(caballo, -1),
            valorEstimado = valor, precioInicial = Math.Max(250, valor / 2 / 25 * 25)
        };
        var datos = Enum.GetValues(typeof(DatoSubasta)).Cast<DatoSubasta>().ToList();
        for (int i = 0; i < 2; i++)
        {
            int index = azar.Next(datos.Count);
            subasta.revelaciones.Add(datos[index]);
            datos.RemoveAt(index);
        }
        for (int i = 0; i < Establos.Length; i++)
            subasta.postores.Add(new PostorSubasta
            {
                id = "postor_" + i, nombre = Establos[i],
                presupuesto = (int)(valor * (.65 + azar.NextDouble() * .60)) / 25 * 25
            });
        return subasta;
    }

    static string CrearId(Random azar)
    {
        var bytes = new byte[16];
        azar.NextBytes(bytes);
        return new Guid(bytes).ToString("N");
    }

    // Una sola decisión reproducible por oferta o día. Reabrir la vista nunca tira los dados.
    public static bool OfrecerIA(SubastaData subasta, int dia, bool respuesta, EstadoSubastasData estado, ref int saldo)
    {
        if (subasta.resultado != ResultadoSubasta.Abierta) return false;
        var azar = new Random(unchecked(subasta.semilla + dia * 7919 + subasta.ofertas.Count * 104729));
        int minima = Minima(subasta);
        var candidatos = subasta.postores.Where(p => !p.retirado && p.id != subasta.postorId).ToList();
        while (candidatos.Count > 0)
        {
            int index = azar.Next(candidatos.Count);
            var postor = candidatos[index];
            candidatos.RemoveAt(index);
            if (minima > postor.presupuesto) { postor.retirado = true; continue; }
            double proporcion = (double)minima / postor.presupuesto;
            double probabilidad = .55 + subasta.DiaDelCiclo(dia) * .07 - proporcion * .24;
            if (respuesta) probabilidad -= .08;
            if (azar.NextDouble() >= probabilidad) continue;
            if (subasta.LideraJugador)
            {
                saldo += subasta.reserva;
                subasta.reserva = 0;
                if (subasta.siguiendo) Avisar(estado, subasta.id + ":superada:" + subasta.ofertas.Count,
                    postor.nombre + " superó tu puja por " + subasta.caballo.nombre + ". Reserva liberada.", subasta.diaFin);
            }
            Registrar(subasta, dia, postor.id, minima);
            return true;
        }
        return false;
    }

    public static void Registrar(SubastaData subasta, int dia, string postor, int importe)
    {
        subasta.pujaActual = importe;
        subasta.postorId = postor;
        subasta.ofertas.Add(new OfertaSubasta
        {
            id = Guid.NewGuid().ToString("N"), dia = dia, postorId = postor, importe = importe
        });
    }

    public static void Avisar(EstadoSubastasData estado, string id, string texto, int diaLimite = 0)
    {
        if (!estado.avisos.Exists(a => a.id == id))
            estado.avisos.Add(new AvisoSubasta { id = id, texto = texto, diaLimite = diaLimite });
    }
}
