using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Vẽ đường thẳng nối các điểm trên Canvas (dùng để hiển thị sợi chỉ khi khâu vá).
/// Gắn vào một GameObject con của Canvas, đặt sau các SewHole để vẽ đè lên.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : Graphic
{
    [Tooltip("Độ dày sợi chỉ (px)")]
    public float lineWidth = 4f;

    private List<Vector2> _points = new List<Vector2>();

    // ------------------------------------------------------------------ //
    //  PUBLIC API
    // ------------------------------------------------------------------ //

    /// <summary>Thêm điểm mới vào cuối đường chỉ.</summary>
    public void AddPoint(Vector2 point)
    {
        _points.Add(point);
        SetVerticesDirty();
    }

    /// <summary>Xoá toàn bộ đường chỉ.</summary>
    public void ClearPoints()
    {
        _points.Clear();
        SetVerticesDirty();
    }

    // ------------------------------------------------------------------ //
    //  OVERRIDE GRAPHIC
    // ------------------------------------------------------------------ //

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (_points.Count < 2) return;

        for (int i = 0; i < _points.Count - 1; i++)
        {
            DrawSegment(vh, _points[i], _points[i + 1], i * 4);
        }
    }

    void DrawSegment(VertexHelper vh, Vector2 p1, Vector2 p2, int vertOffset)
    {
        Vector2 dir    = (p2 - p1).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x) * (lineWidth * 0.5f);

        UIVertex vert = UIVertex.simpleVert;
        vert.color = color;

        vert.position = p1 - normal; vh.AddVert(vert);
        vert.position = p1 + normal; vh.AddVert(vert);
        vert.position = p2 + normal; vh.AddVert(vert);
        vert.position = p2 - normal; vh.AddVert(vert);

        vh.AddTriangle(vertOffset,     vertOffset + 1, vertOffset + 2);
        vh.AddTriangle(vertOffset + 2, vertOffset + 3, vertOffset);
    }
}
