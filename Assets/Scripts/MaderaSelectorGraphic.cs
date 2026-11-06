using UnityEngine;
using UnityEngine.UI;

// Cover the available area with horizontal grain while retaining the texture's aspect.
internal sealed class MaderaSelectorGraphic : RawImage
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        base.OnPopulateMesh(mesh);
        if (texture == null || rectTransform.rect.height <= 0) return;

        float aspecto = rectTransform.rect.width / rectTransform.rect.height;
        float fuente = (float)texture.width / texture.height;
        float ancho = Mathf.Min(1, aspecto / fuente);
        float alto = Mathf.Min(1, fuente / aspecto);
        for (int i = 0; i < mesh.currentVertCount; i++)
        {
            var vertice = UIVertex.simpleVert;
            mesh.PopulateUIVertex(ref vertice, i);
            vertice.uv0 = new Vector2(.5f + (vertice.uv0.x - .5f) * ancho,
                .5f + (vertice.uv0.y - .5f) * alto);
            mesh.SetUIVertex(vertice, i);
        }
    }
}
