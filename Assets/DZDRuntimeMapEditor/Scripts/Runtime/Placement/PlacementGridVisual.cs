using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace DZDMapEditor
{
    public sealed class PlacementGridVisual : MonoBehaviour
    {
        const string ShaderName = "Hidden/DZDMapEditor/PlacementGrid";

        [LabelText("网格材质")]
        [Tooltip("用顶点色画格子线和当前格填充。可空则运行时按着色器名创建。")]
        [SerializeField, AssetsOnly]
        Material material;

        readonly List<Vector3> vertices = new List<Vector3>(256);
        readonly List<Color> colors = new List<Color>(256);
        readonly List<int> triangles = new List<int>(512);

        Mesh lineMesh;
        Mesh fillMesh;
        Material runtimeMaterial;
        Vector2Int lastCell;
        float lastY;
        float lastSize;
        int lastRadius;
        bool lastOccupied;
        bool hasLast;

        void OnDisable()
        {
            hasLast = false;
        }

        void OnDestroy()
        {
            DestroyMesh(ref lineMesh);
            DestroyMesh(ref fillMesh);
            if (runtimeMaterial != null)
                Destroy(runtimeMaterial);
        }

        public void Hide()
        {
            hasLast = false;
        }

        public void Show(PlacementConfig config, in PlacementHit hit, bool occupied)
        {
            var drawMaterial = ResolveMaterial();
            if (config == null || !hit.HasHit || drawMaterial == null)
                return;

            var cell = PlacementGrid.WorldToCell(hit.Point, config);
            var y = PlacementGrid.BaseY(config) + config.GridYOffset;
            var size = PlacementGrid.CellSize(config);
            var radius = config.GridVisualRadius;
            if (!hasLast ||
                cell != lastCell ||
                occupied != lastOccupied ||
                radius != lastRadius ||
                !Mathf.Approximately(y, lastY) ||
                !Mathf.Approximately(size, lastSize))
            {
                RebuildLines(config, cell, y);
                RebuildFill(config, cell, y, occupied);
                lastCell = cell;
                lastY = y;
                lastSize = size;
                lastRadius = radius;
                lastOccupied = occupied;
                hasLast = true;
            }

            Draw(lineMesh, drawMaterial);
            Draw(fillMesh, drawMaterial);
        }

        void RebuildLines(PlacementConfig config, Vector2Int cell, float y)
        {
            EnsureMesh(ref lineMesh, "PlacementGridLines");
            vertices.Clear();
            colors.Clear();
            triangles.Clear();

            var size = PlacementGrid.CellSize(config);
            var radius = config.GridVisualRadius;
            var min = PlacementGrid.CellMin(new Vector2Int(cell.x - radius, cell.y - radius), y, config);
            var extent = size * (radius * 2 + 1);
            var width = Mathf.Max(0.005f, config.GridLineWidth);
            var color = config.GridLineColor;

            for (var i = 0; i <= radius * 2 + 1; i++)
            {
                var x = min.x + i * size;
                AddLine(new Vector3(x, y, min.z), new Vector3(x, y, min.z + extent), width, color);
                var z = min.z + i * size;
                AddLine(new Vector3(min.x, y, z), new Vector3(min.x + extent, y, z), width, color);
            }

            Upload(lineMesh);
        }

        void RebuildFill(PlacementConfig config, Vector2Int cell, float y, bool occupied)
        {
            EnsureMesh(ref fillMesh, "PlacementGridFill");
            vertices.Clear();
            colors.Clear();
            triangles.Clear();

            var size = PlacementGrid.CellSize(config);
            var inset = Mathf.Min(size * 0.08f, Mathf.Max(0.01f, config.GridLineWidth));
            var min = PlacementGrid.CellMin(cell, y, config);
            var x0 = min.x + inset;
            var x1 = min.x + size - inset;
            var z0 = min.z + inset;
            var z1 = min.z + size - inset;
            var color = occupied ? config.GridOccupiedFillColor : config.GridFreeFillColor;
            AddQuad(
                new Vector3(x0, y, z0),
                new Vector3(x0, y, z1),
                new Vector3(x1, y, z1),
                new Vector3(x1, y, z0),
                color);
            Upload(fillMesh);
        }

        void AddLine(Vector3 a, Vector3 b, float width, Color color)
        {
            var delta = b - a;
            delta.y = 0f;
            if (delta.sqrMagnitude < 1e-8f)
                return;

            var half = Vector3.Cross(Vector3.up, delta).normalized * (width * 0.5f);
            AddQuad(a - half, a + half, b + half, b - half, color);
        }

        void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color)
        {
            var start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        void Upload(Mesh mesh)
        {
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0, true);
            mesh.RecalculateBounds();
        }

        Material ResolveMaterial()
        {
            if (material != null)
                return material;
            if (runtimeMaterial != null)
                return runtimeMaterial;

            var shader = Shader.Find(ShaderName);
            if (shader == null)
                return null;

            runtimeMaterial = new Material(shader)
            {
                name = "PlacementGridRuntime",
                hideFlags = HideFlags.HideAndDontSave,
                color = Color.white
            };
            runtimeMaterial.SetInt("_ZWrite", 0);
            return runtimeMaterial;
        }

        static void Draw(Mesh mesh, Material drawMaterial)
        {
            if (mesh == null || mesh.vertexCount == 0 || drawMaterial == null)
                return;

            Graphics.DrawMesh(
                mesh,
                Matrix4x4.identity,
                drawMaterial,
                0,
                null,
                0,
                null,
                ShadowCastingMode.Off,
                false,
                null,
                LightProbeUsage.Off);
        }

        static void EnsureMesh(ref Mesh mesh, string meshName)
        {
            if (mesh != null)
                return;
            mesh = new Mesh
            {
                name = meshName,
                hideFlags = HideFlags.HideAndDontSave
            };
            mesh.MarkDynamic();
        }

        static void DestroyMesh(ref Mesh mesh)
        {
            if (mesh == null)
                return;
            Destroy(mesh);
            mesh = null;
        }
    }
}
