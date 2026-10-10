using UnityEngine;

// Draws existing pixel frames in nine pieces without changing their Sprite importer.
[AddComponentMenu("BTL/UI/Sprite Frame")]
[RequireComponent(typeof(CanvasRenderer))]
public sealed class UiSpriteFrame : UnityEngine.UI.MaskableGraphic
{
    [SerializeField] private Sprite sprite;
    [SerializeField] private Vector4 borderPixels = new Vector4(8f, 8f, 8f, 8f);
    [SerializeField] private float pixelScale = 3f;
    public override Texture mainTexture => sprite != null ? sprite.texture : Texture2D.whiteTexture;

    public void Configure(Sprite value, Vector4 borders, float scale)
    {
        sprite = value;
        borderPixels = borders;
        pixelScale = scale;
        SetAllDirty();
    }

    public void CopyAppearanceFrom(UiSpriteFrame template)
    {
        if (template != null) Configure(template.sprite, template.borderPixels, template.pixelScale);
    }

    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
    {
        vh.Clear();
        if (sprite == null) return;
        Rect r = GetPixelAdjustedRect();
        Rect source = sprite.rect;
        float horizontalScale = Mathf.Min(pixelScale, r.width / Mathf.Max(1f, borderPixels.x + borderPixels.z));
        float verticalScale = Mathf.Min(pixelScale, r.height / Mathf.Max(1f, borderPixels.y + borderPixels.w));
        float[] xs = { r.xMin, r.xMin + borderPixels.x * horizontalScale, r.xMax - borderPixels.z * horizontalScale, r.xMax };
        float[] ys = { r.yMin, r.yMin + borderPixels.y * verticalScale, r.yMax - borderPixels.w * verticalScale, r.yMax };
        float[] us = { source.xMin, source.xMin + borderPixels.x, source.xMax - borderPixels.z, source.xMax };
        float[] vs = { source.yMin, source.yMin + borderPixels.y, source.yMax - borderPixels.w, source.yMax };
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
                vh.AddVert(new Vector3(xs[x], ys[y]), color, new Vector2(us[x] / sprite.texture.width, vs[y] / sprite.texture.height));
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
            {
                int a = y * 4 + x;
                vh.AddTriangle(a, a + 4, a + 5);
                vh.AddTriangle(a, a + 5, a + 1);
            }
    }
}
