using UnityEngine;
using UnityEngine.UI;

// Solid interior only; the empty outline remains baked into the unchanged art.
public sealed class MenuDiamondGlow : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var rect = rectTransform.rect;
        Vector2[] points = {Vector2.zero,Vector2.up,Vector2.right,Vector2.down,Vector2.left};
        foreach (var point in points)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = rect.center + Vector2.Scale(point, rect.size * .5f);
            vertex.color = color; mesh.AddVert(vertex);
        }
        for (int side=0;side<4;side++) mesh.AddTriangle(0,side+1,(side+1)%4+1);
    }
}
