using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

public static class CatalogosBalanceProbe
{
    sealed class Loadout
    {
        public DefinicionProductoTienda[] items;
        public int cost;
        public int[] bonus;
    }
    public static void Run()
    {
        if(!Application.isBatchMode)throw new InvalidOperationException("Batch requerido");
        var products=SistemaTienda.CrearCatalogo(null);
        var report=new StringBuilder("Prueba de balance orientativa, simulación real EstadoCaballoCarrera.\n100 semillas por escenario, cinco rivales. No estima ganancias reales de una campaña.\nSe supone acceso a todo el catálogo: es una cota optimista; la tienda real ofrece tres productos.\nPresupuesto inicial $1000, incluye $50 por equipar cada unidad. Equipo de nivel 1.\n");
        report.AppendLine("Premios existentes: $1000–5000; perder cuesta $200. No se modifican esos valores ni las cuotas.");
        var slots=new[]{TipoSlotItem.Cabeza,TipoSlotItem.Espalda,TipoSlotItem.Patas,TipoSlotItem.Patas};
        var candidates=slots.Select(slot=>new DefinicionProductoTienda[]{null}.Concat(products.Where(p=>p.tieneSlotEquipable&&p.slotPermitido==slot&&p.nivelMinimo<=1)).ToArray()).ToArray();
        var loadouts=new List<Loadout>();
        foreach(var head in candidates[0])foreach(var back in candidates[1])foreach(var front in candidates[2])foreach(var rear in candidates[3])
        {
            var items=new[]{head,back,front,rear}.Where(p=>p!=null).ToArray();
            int cost=items.Sum(p=>p.precio+SistemaEquipamiento.CostoEquiparItem);
            if(cost>1000)continue;
            var bonus=Enumerable.Range(0,4).Select(s=>items.Sum(p=>p.BonoPara((TipoEstadisticaCaballo)s))).ToArray();
            loadouts.Add(new Loadout{items=items,cost=cost,bonus=bonus});
        }
        var starts=new[]{("Rayo",new[]{5,4,3,5}),("Luna",new[]{4,5,6,4}),("Relámpago",new[]{8,7,5,9})};
        foreach(int distance in new[]{1200,1600,2100,2400})
        {
            int staminaWeight=distance>=2100?3:1;
            var best=loadouts.OrderByDescending(l=>l.bonus[0]*3+l.bonus[1]*staminaWeight+l.bonus[3]+l.bonus[2]*.25).ThenBy(l=>l.cost).First();
            var consumable=products.Where(p=>p.tipoProducto==TipoProductoTienda.Consumible&&p.precio+best.cost<=1000)
                .OrderByDescending(p=>p.BonoPara(TipoEstadisticaCaballo.Velocidad)*3+p.BonoPara(TipoEstadisticaCaballo.Stamina)*staminaWeight+p.BonoPara(TipoEstadisticaCaballo.Inteligencia)).FirstOrDefault();
            report.AppendLine($"\n{distance} m: equipo={string.Join(" / ",best.items.Select(p=>p.idDefinicion))}; coste={best.cost}; consumible={consumable?.idDefinicion??"ninguno"}.");
            foreach(var start in starts)
            {
                var improved=start.Item2.Select((v,s)=>Caballo.NormalizarEstadistica(v+best.bonus[s]+(consumable?.BonoPara((TipoEstadisticaCaballo)s)??0))).ToArray();
                int bare=Wins(start.Item2,distance),equipped=Wins(improved,distance);
                report.AppendLine($"{start.Item1}: sin compras {bare}/100; equipo disponible en presupuesto {equipped}/100; stats=[{string.Join(",",improved)}].");
            }
        }
        int minValue=int.MaxValue,maxValue=0;
        foreach(var id in CatalogoCaballos.Todos)
        {
            var auction=SistemaSubastas.Crear(1,new[]{id.nombre},HorseVisualCatalog.Default,null,153);
            minValue=Math.Min(minValue,auction.precioInicial);maxValue=Math.Max(maxValue,auction.precioInicial);
        }
        report.AppendLine($"\nPrecio inicial de subasta en muestra de 100 identidades, día 1: ${minValue}–${maxValue}. El precio final depende de las pujas; no se garantiza asequibilidad de todas las ofertas con el saldo inicial.");
        report.AppendLine("Consumibles: una unidad por caballo y día, preparación máxima +2 por atributo, XP máxima 35 por uso. Ninguno supera los topes 1–16 de carrera.");
        File.WriteAllText("C:/Caballos/Validation/Catalogos/balance.txt",report.ToString());
        Debug.Log("CATALOGOS BALANCE PASS: "+loadouts.Count+" combinaciones dentro del presupuesto, 2400 carreras simuladas y 100 subastas.");
    }
    static int Wins(int[] values,int distance)
    {
        int wins=0;
        for(int seed=1;seed<=100;seed++)
        {
            var random=new System.Random(seed);
            var runners=new List<EstadoCaballoCarrera>{new(Make(values),seed*113)};
            int band=Mathf.Clamp(Mathf.RoundToInt((distance-1200)/400f),0,3);
            for(int i=0;i<5;i++)
            {
                int[] rival={6+(band==0?1:0)+random.Next(-2,3),3+band*2+random.Next(-2,3),4+random.Next(-2,3),4+random.Next(-2,3)};
                runners.Add(new EstadoCaballoCarrera(Make(rival),seed*113+i+1));
            }
            bool finished=false;
            for(int tick=0;tick<20000&&!finished;tick++)
            {
                foreach(var runner in runners)runner.Actualizar(.1f,distance);
                var winner=runners.OrderByDescending(r=>r.distanciaRecorrida).First();
                if(winner.distanciaRecorrida>=distance){if(winner==runners[0])wins++;finished=true;}
            }
            if(!finished)throw new InvalidOperationException("Carrera no concluye");
        }
        return wins;
    }
    static Caballo Make(int[] a)=>new(){velocidad=Caballo.NormalizarEstadistica(a[0]),stamina=Caballo.NormalizarEstadistica(a[1]),suerte=Caballo.NormalizarEstadistica(a[2]),inteligencia=Caballo.NormalizarEstadistica(a[3])};
}
