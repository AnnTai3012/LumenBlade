using UnityEngine;

public enum HudShape { BevelBar, Seal, SelectionMarker }

[AddComponentMenu("BTL/UI/HUD Shape")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class HudShapeGraphic : UnityEngine.UI.MaskableGraphic
{
    [SerializeField] private HudShape shape;
    public void SetShape(HudShape value) { shape = value; SetVerticesDirty(); }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (shape == HudShape.SelectionMarker)
        {
            Color32 dark = new Color(0.23f, 0.15f, 0.07f, 1f);
            Color32 gold = new Color(1f, 0.82f, 0.44f, 1f);
            Diamond(vh, r.center, r.width * 0.48f, r.height * 0.42f, dark);
            Diamond(vh, r.center, r.width * 0.37f, r.height * 0.32f, gold);
            Diamond(vh, r.center, r.width * 0.23f, r.height * 0.20f, dark);
            Diamond(vh, r.center, r.width * 0.10f, r.height * 0.09f, gold);
            Quad(vh, new Rect(r.center.x - 2f, r.yMin, 4f, r.height), gold);
            Quad(vh, new Rect(r.xMin, r.center.y - 2f, r.width, 4f), gold);
            return;
        }
        float inset = shape == HudShape.Seal ? r.width * 0.23f : Mathf.Min(14f, r.height * 0.5f);
        Vector2[] points = { new Vector2(r.xMin + inset,r.yMin),new Vector2(r.xMin,r.center.y),
            new Vector2(r.xMin + inset,r.yMax),new Vector2(r.xMax - inset,r.yMax),
            new Vector2(r.xMax,r.center.y),new Vector2(r.xMax - inset,r.yMin) };
        vh.AddVert(r.center, color, Vector2.zero);
        foreach (Vector2 point in points) vh.AddVert(point, color, Vector2.zero);
        for (int i = 0; i < points.Length; i++) vh.AddTriangle(0, i + 1, (i + 1) % points.Length + 1);
    }

    private static void Diamond(UnityEngine.UI.VertexHelper vh, Vector2 c, float w, float h, Color32 tint)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector2(c.x,c.y + h),tint,Vector2.zero);
        vh.AddVert(new Vector2(c.x + w,c.y),tint,Vector2.zero);
        vh.AddVert(new Vector2(c.x,c.y - h),tint,Vector2.zero);
        vh.AddVert(new Vector2(c.x - w,c.y),tint,Vector2.zero);
        vh.AddTriangle(start,start + 1,start + 2); vh.AddTriangle(start,start + 2,start + 3);
    }
    private static void Quad(UnityEngine.UI.VertexHelper vh, Rect r, Color32 tint)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector2(r.xMin,r.yMin),tint,Vector2.zero); vh.AddVert(new Vector2(r.xMin,r.yMax),tint,Vector2.zero);
        vh.AddVert(new Vector2(r.xMax,r.yMax),tint,Vector2.zero); vh.AddVert(new Vector2(r.xMax,r.yMin),tint,Vector2.zero);
        vh.AddTriangle(start,start + 1,start + 2); vh.AddTriangle(start,start + 2,start + 3);
    }
}
