using UnityEngine;
using UnityEngine.UI;

internal sealed class BannerCarreraGraphic : RawImage
{
    Rect region;

    internal void Configurar(Texture2D atlas, int indice, Sprite alternativo)
    {
        texture = alternativo == null ? atlas : alternativo.texture;
        if (alternativo != null)
        {
            var rect = alternativo.rect;
            region = new Rect(rect.x / texture.width, rect.y / texture.height,
                rect.width / texture.width, rect.height / texture.height);
        }
        else
        {
            float margenX = 2f / texture.width;
            float margenY = 2f / texture.height;
            region = new Rect(indice % 2 * .5f + margenX, (7 - indice / 2) * .125f + margenY,
                .5f - 2 * margenX, .125f - 2 * margenY);
        }
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (texture == null || rectTransform.rect.height <= 0) return;

        Rect rect = GetPixelAdjustedRect();
        Rect uv = region;
        float aspecto = rect.width / rect.height / (region.width * texture.width / (region.height * texture.height));
        if (aspecto > 1f)
        {
            uv.height = region.height / aspecto;
            uv.y += (region.height - uv.height) * .28f;
        }
        else
        {
            uv.width = region.width * aspecto;
            uv.x += (region.width - uv.width) * .5f;
        }

        AgregarColumna(mesh, rect, uv, 0, 0);
        AgregarColumna(mesh, rect, uv, .34f, 255);
        AgregarColumna(mesh, rect, uv, 1, 255);
        for (int i = 0; i <= 2; i += 2)
        {
            mesh.AddTriangle(i, i + 1, i + 3);
            mesh.AddTriangle(i, i + 3, i + 2);
        }
    }

    static void AgregarColumna(VertexHelper mesh, Rect rect, Rect uv, float t, byte alpha)
    {
        float x = Mathf.Lerp(rect.xMin, rect.xMax, t);
        float u = Mathf.Lerp(uv.xMin, uv.xMax, t);
        var color = new Color32(255, 247, 230, alpha);
        mesh.AddVert(new Vector3(x, rect.yMin), color, new Vector2(u, uv.yMin));
        mesh.AddVert(new Vector3(x, rect.yMax), color, new Vector2(u, uv.yMax));
    }
}
