using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using Object=UnityEngine.Object;

[InitializeOnLoad] public static class AvisoBetaV02Probe
{
    const string Key="AvisoBetaV02Probe.Active", Output="C:/Caballos/Validation/BetaV02/";
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static IEnumerator tests;
    static readonly List<string> checks=new();
    static AvisoBetaV02Probe(){EditorApplication.update+=Step;}
    public static void Run()
    {
        if(!Application.isBatchMode || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CABALLOS_VALIDATION_SAVE_PATH"))) throw new Exception("Guardado aislado requerido");
        EditorSceneManager.OpenScene(GameLaunch.HubScenePath);
        EditorApplication.delayCall+=()=>{SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;};
    }
    static void Step()
    {
        if(!SessionState.GetBool(Key,false)||!EditorApplication.isPlaying)return;
        try{tests??=Tests();if(!tests.MoveNext())End(0);}catch(Exception e){Debug.LogException(e);End(1);}
    }
    static IEnumerator Tests()
    {
        while(GameManager.Instancia==null||GameManager.Instancia.caballosJugador.Count==0)yield return null;
        var g=GameManager.Instancia;g.IniciarNuevaPartida();g.SetState(Interactable.Tipo.Tienda);
        var notice=Object.FindFirstObjectByType<AvisosSubasta>();
        var auction=g.SubastaActual;auction.precioInicial=100;
        foreach(var b in auction.postores)b.retirado=true;
        g.dinero=10000;int count=g.caballosJugador.Count;
        Check(g.PujarSubasta(auction.id,1150,out _),"Puja real aceptada");
        Check(g.dinero==8850&&auction.reserva==1150,"Pago reservado una vez");
        Call(g,"CerrarSubasta");Call(g,"CerrarSubasta");
        Check(g.dinero==8850&&auction.reserva==0&&g.caballosJugador.Count==count+1,"Cierre repetido no duplica pago ni caballo");
        string id=auction.id+":resultado", horse=auction.caballo.id;
        Check(auction.resultado==ResultadoSubasta.Ganada&&g.GuardarPartida(),"Resultado ganado guardado");
        yield return null;yield return null;
        Check(!g.subastas.avisos.Single(a=>a.id==id).leido&&!Visible(notice),"Resuelto fuera del Hub queda pendiente");
        Check((bool)Call(g,"CargarPartida"),"Carga pendiente aislada");yield return null;
        Check(!g.subastas.avisos.Single(a=>a.id==id).leido,"Pendiente sobrevive carga fuera del Hub");
        g.VolverAlHub();
        while(!Visible(notice))yield return null;
        double until=(double)Get(notice,"hasta"), shown=until-5;
        Check(Text(notice)==g.subastas.avisos.Single(a=>a.id==id).texto,"Diseño y mensaje dinámico conservados");
        Check(SaveSystem.IntentarCargar(out var saved)&&saved.subastas.avisos.Single(a=>a.id==id).leido,"Consumo persistido al mostrar, antes del cierre");
        Time.timeScale=0;int day=g.dia;
        while(Time.realtimeSinceStartupAsDouble<shown+4.5)yield return null;
        Check(Visible(notice)&&g.dia==day&&Time.timeScale==0,"Visible antes de cinco segundos con timeScale cero");
        while(Time.realtimeSinceStartupAsDouble<until+.1)yield return null;
        yield return null;
        Check(!Visible(notice)&&g.dia==day,"Se oculta a los cinco segundos reales sin avanzar de día");
        Time.timeScale=1;
        g.SetState(Interactable.Tipo.Tienda);yield return null;g.VolverAlHub();yield return null;
        Check(!Visible(notice),"Salir y volver no repite aviso consumido");
        Check((bool)Call(g,"CargarPartida"),"Carga después de mostrar");yield return null;
        Check(!Visible(notice)&&g.caballosJugador.Count(c=>c.id==horse)==1&&g.dinero==8850,"Carga conserva caballo y pago único sin repetir cartel");
        Check(g.subastas.historial.Single(a=>a.id==auction.id).resultado==ResultadoSubasta.Ganada,"Ocultar no borra resultado de subasta");
        g.SiguienteDia();yield return null;Check(!Visible(notice),"Avanzar día no reactiva el aviso");

        // Deterministic boundary tests use the exact same update method with an injected clock.
        notice.enabled=false;
        SistemaSubastas.Avisar(g.subastas,"cola-1","Aviso uno");
        Call(notice,"Actualizar",1000d);
        SistemaSubastas.Avisar(g.subastas,"cola-2","Aviso dos");
        Call(notice,"Actualizar",1004.999d);
        Check(Visible(notice)&&Text(notice)=="Aviso uno"&&!g.subastas.avisos.Single(a=>a.id=="cola-2").leido,"Otro aviso queda pendiente durante el primero");
        Call(notice,"Actualizar",1005d);
        Check(Visible(notice)&&Text(notice)=="Aviso dos"&&(double)Get(notice,"hasta")==1010d,"Segundo aviso recibe cinco segundos propios");
        Call(notice,"Actualizar",1005.001d);Call(notice,"Actualizar",1009.999d);
        Check(Visible(notice)&&Text(notice)=="Aviso dos","Temporizador anterior no oculta al nuevo");
        Call(notice,"Actualizar",1010d);Check(!Visible(notice),"Segundo aviso expira en su propio límite");
        SistemaSubastas.Avisar(g.subastas,"cola-3","Aviso interrumpido");Call(notice,"Actualizar",2000d);
        g.SetState(Interactable.Tipo.Tienda);Call(notice,"Actualizar",2001d);
        g.VolverAlHub();Call(notice,"Actualizar",2002d);
        Check(!Visible(notice)&&g.subastas.avisos.Single(a=>a.id=="cola-3").leido,"Abrir y cerrar menú antes de vencer no repite aviso");
        SistemaSubastas.Avisar(g.subastas,"cola-4","Aviso con fallo de guardado");
        string blocker=SaveSystem.RutaArchivo+".tmp";Directory.CreateDirectory(blocker);
        try{Call(notice,"Actualizar",3000d);Check(!Visible(notice)&&!g.subastas.avisos.Single(a=>a.id=="cola-4").leido,"Fallo de escritura conserva aviso pendiente sin mostrar");}
        finally{Directory.Delete(blocker);}
        Call(notice,"Actualizar",3001d);Check(Visible(notice)&&g.subastas.avisos.Single(a=>a.id=="cola-4").leido,"Escritura recuperada permite mostrar una vez");
        Check((bool)Call(g,"CargarPartida"),"Carga durante aviso activo");Call(notice,"Actualizar",3002d);
        Check(!Visible(notice),"Carga durante temporizador activo no repite cartel");
        Check(g.dinero==8850&&g.caballosJugador.Count(c=>c.id==horse)==1,"Avisos y guardado no cambian economía ni entrega");
        SistemaSubastas.Avisar(g.subastas,"cola-5","Partida anterior");Call(notice,"Actualizar",4000d);
        g.IniciarNuevaPartida();Call(notice,"Actualizar",4001d);Check(!Visible(notice),"Nueva partida no hereda panel activo");
        Debug.Log("AVISO BETA V02 PASS: "+checks.Count);
    }
    static bool Visible(AvisosSubasta n)=>Get(n,"panel") is GameObject p&&p.activeInHierarchy;
    static string Text(AvisosSubasta n)=>((TMP_Text)Get(n,"texto")).text;
    static object Get(object o,string n)=>o.GetType().GetField(n,Flags).GetValue(o);
    static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n,Flags).Invoke(o,a);
    static void Check(bool v,string s){if(!v)throw new Exception(s);checks.Add(s);}
    static void End(int code){SessionState.SetBool(Key,false);Time.timeScale=1;File.WriteAllLines(Output+"aviso-pruebas.txt",checks);EditorApplication.Exit(code);}
}
