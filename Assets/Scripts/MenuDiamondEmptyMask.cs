using UnityEngine;
using UnityEngine.UI;

// Repairs only the baked-in selected mark inside the first diamond. Colors are
// interpolated from the adjacent original background, without changing the PNG.
public sealed class MenuDiamondEmptyMask : MaskableGraphic
{
    public Color[] leftRows, rightRows;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if(leftRows==null||rightRows==null||leftRows.Length<2||leftRows.Length!=rightRows.Length)return;
        var rect=rectTransform.rect;
        for(int row=0;row<leftRows.Length;row++)
        {
            float y=-1f+2f*row/(leftRows.Length-1), extent=1f-Mathf.Abs(y);
            for(int side=0;side<2;side++)
            {
                float x=(side==0?-extent:extent);
                var vertex=UIVertex.simpleVert;
                vertex.position=rect.center+Vector2.Scale(new Vector2(x,y),rect.size*.5f);
                vertex.color=Color.Lerp(leftRows[row],rightRows[row],(x*13+20)/40f);
                mesh.AddVert(vertex);
            }
            if(row==0)continue;
            int a=(row-1)*2,b=row*2;
            mesh.AddTriangle(a,b,a+1);mesh.AddTriangle(a+1,b,b+1);
        }
    }
}
