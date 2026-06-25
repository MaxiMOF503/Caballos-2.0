using UnityEngine;

public static class RangoCaballo
{
    public static string ObtenerRango(int valor)
    {
        switch (valor)
        {
            case 1: return "F";
            case 2: return "F+";

            case 3: return "D";
            case 4: return "D+";

            case 5: return "C";
            case 6: return "C+";

            case 7: return "B";
            case 8: return "B+";

            case 9: return "A";
            case 10: return "A+";

            case 11: return "S";
            case 12: return "S+";

            case 13: return "SS";
            case 14: return "SS+";

            case 15: return "SR";
            case 16: return "EX";

            default: return "?";
        }
    }
}