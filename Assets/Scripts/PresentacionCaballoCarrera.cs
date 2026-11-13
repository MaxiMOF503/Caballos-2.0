using System;
using UnityEngine;
using UnityEngine.UI;

// Metadata measured from existing PNGs, not edited textures. One union over all
// eight frames preserves the size of the body and the natural suspended poses.
public static class PresentacionCaballoCarrera
{
    public const float AlturaVisible = 148;
    public const float SeparacionCarriles = 174;
    [Serializable] sealed class LimitesMedidos { public float x, y, width, height; }
    [Serializable] sealed class Entrada { public string id; public LimitesMedidos bounds; }
    [Serializable] sealed class Archivo { public Entrada[] entries; }
    static Archivo archivo;
    public static Rect Limites(HorseVisualProfile profile)
    {
        archivo ??= JsonUtility.FromJson<Archivo>(Resources.Load<TextAsset>("LimitesVisualesCarrera").text);
        foreach (var e in archivo.entries)
            if (e.id == profile.catalogIdentityId && e.bounds != null && e.bounds.width > 0 && e.bounds.height > 0)
                return new Rect(e.bounds.x, e.bounds.y, e.bounds.width, e.bounds.height);
        return new Rect(0, 0, 1, 1); // Unrecognized legacy frames retain a stable cell.
    }
    public static void Configurar(Image imagen, HorseVisualProfile profile)
    {
        var b = Limites(profile);
        float escala = AlturaVisible / b.height;
        var sprite = profile.runFrames[0];
        float anchoCelda = escala * sprite.rect.width / sprite.rect.height;
        var padre = imagen.transform.parent;
        var mask = new GameObject("VisualCarrera", typeof(RectTransform), typeof(RectMask2D)).GetComponent<RectTransform>();
        mask.SetParent(padre, false); mask.anchorMin = mask.anchorMax = new Vector2(.5f,0);
        mask.pivot = new Vector2(.5f,0); mask.anchoredPosition = new Vector2(0,-4);
        mask.sizeDelta = new Vector2(anchoCelda * b.width + 8, AlturaVisible + 8);
        var rect = imagen.rectTransform;
        rect.SetParent(mask,false); rect.anchorMin = rect.anchorMax = new Vector2(.5f,0);
        rect.pivot = new Vector2(b.center.x,b.yMin);
        rect.sizeDelta = new Vector2(anchoCelda,escala); rect.anchoredPosition = new Vector2(0,4);
        rect.localScale = Vector3.one; imagen.preserveAspect = false;
    }
}
