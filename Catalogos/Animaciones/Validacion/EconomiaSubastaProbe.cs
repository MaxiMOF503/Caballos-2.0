using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class EconomiaSubastaProbe
{
    const string Key="EconomiaSubastaProbe.Active";
    static readonly List<string> checks=new();
    static IEnumerator tests;
    static EconomiaSubastaProbe()=>EditorApplication.update+=Step;
    public static void Run()
    {
        if(!Application.isBatchMode||string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH"))||File.Exists(SaveSystem.RutaArchivo))throw new InvalidOperationException("Guardado aislado requerido");
        EditorSceneManager.OpenScene(GameLaunch.HubScenePath);
        EditorApplication.delayCall+=()=>EditorApplication.delayCall+=()=>{SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;};
    }
    static void Step(){if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;try{tests??=Tests();if(!tests.MoveNext())Finish(0);}catch(Exception e){Debug.LogException(e);Finish(1);}}
    static IEnumerator Tests()
    {
        while(GameManager.Instancia==null||GameManager.Instancia.caballosJugador.Count==0)yield return null;
        double ready=EditorApplication.timeSinceStartup+2;while(EditorApplication.timeSinceStartup<ready)yield return null;
        var game=GameManager.Instancia;var auction=game.SubastaActual;
        game.dinero=1000;auction.precioInicial=100;
        foreach(var bidder in auction.postores)bidder.retirado=true;
        Check(game.PujarSubasta(auction.id,100,out _),"Primera puja aceptada");
        Check(game.dinero==900&&game.DineroReservado==100,"Reserva sale del disponible una sola vez");
        Check(game.PujarSubasta(auction.id,150,out _),"Aumenta puja propia");
        Check(game.dinero==850&&game.DineroReservado==150,"Aumento descuenta solo diferencia de 50");
        Check(!game.PujarSubasta(auction.id,150,out _)&&game.dinero==850,"Doble clic en la misma puja no descuenta");
        Check(!game.PujarSubasta(auction.id,1100,out _)&&game.dinero==850&&game.DineroReservado==150,"Puja impagable conserva ambos saldos");
        var savedId=auction.id;Check(game.GuardarPartida(),"Guarda la reserva");Load(game);
        auction=game.SubastaActual;
        Check(auction.id==savedId&&game.dinero==850&&auction.reserva==150,"Carga no descuenta de nuevo la reserva");
        Call(game,"CerrarSubasta");
        Check(game.dinero==850&&game.DineroReservado==0&&auction.resultado==ResultadoSubasta.Ganada,"Entrega no cobra dos veces la oferta reservada");
        int horses=game.caballosJugador.Count;Call(game,"CerrarSubasta");
        Check(game.dinero==850&&game.caballosJugador.Count==horses,"Cierre repetido no cobra ni duplica caballo");
        game.subastas.actual=SistemaSubastas.Crear(1,new[]{"Rayo","Luna"},HorseVisualCatalog.Default,null,42);
        auction=game.SubastaActual;auction.precioInicial=100;game.dinero=1000;
        foreach(var bidder in auction.postores)bidder.retirado=true;
        Check(game.PujarSubasta(auction.id,150,out _),"Reserva para probar devolución");
        foreach(var bidder in auction.postores){bidder.retirado=false;bidder.presupuesto=10000;}
        bool response=false;
        for(int seed=1;seed<100&&!response;seed++)
        {
            auction.semilla=seed;int cash=game.dinero;
            response=SistemaSubastas.OfrecerIA(auction,game.dia,true,game.subastas,ref cash);game.dinero=cash;
        }
        Check(response&&game.dinero==1000&&auction.reserva==0,"IA supera puja y devuelve exactamente la reserva");
        Call(game,"CerrarSubasta");
        Check(game.dinero==1000&&auction.reserva==0,"Perder la subasta no duplica la devolución");
        game.subastas.actual=SistemaSubastas.Crear(game.dia,new[]{"Rayo","Luna"},HorseVisualCatalog.Default,null,43);
        game.dinero=150;
        Check(game.ElegirCarrera(game.ObtenerOfertaCarreras()[0].id,out _),"Prepara carrera sin apuesta con saldo de 150");
        var race=game.ObtenerCarreraProgramada();
        Check(game.IniciarCarreraProgramada(race),"Inicia carrera guardada");
        var winner=game.CrearVistaParticipante(race.participantes.First(p=>!p.caballo.esDelJugador));
        game.FinalizarCarreraProgramada(race,winner,winner);
        Check(game.dinero==-50&&game.DineroReservado==0,"Reproduce video: 150 menos penalización existente de 200 resulta en -50, sin reservas");
        Check(game.GuardarPartida(),"Guarda el resultado de la carrera");Load(game);
        Check(game.dinero==-50&&game.DineroReservado==0,"El saldo negativo ya está en el JSON y no se calcula en la vista de subasta");
        Check(!game.PujarSubasta(game.SubastaActual.id,1000,out _),"Saldo negativo no permite nuevas pujas");
        Check(game.dinero==-50,"Intento rechazado no agrava el saldo");
        Debug.Log("ECONOMIA SUBASTA PASS: "+checks.Count+" comprobaciones; negativo explicado por penalización de carrera, no por cobro duplicado de reserva.");
    }
    static object Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
    static void Load(GameManager game)=>Check((bool)Call(game,"CargarPartida"),"Carga completa");
    static void Check(bool value,string text){if(!value)throw new InvalidOperationException(text);checks.Add(text);}
    static void Finish(int code){SessionState.SetBool(Key,false);File.WriteAllLines("C:/Caballos/Validation/AnimacionCamara/economia-pruebas.txt",checks);EditorApplication.Exit(code);}
}
