using UnityEngine;

namespace UnityCommonEx
{

    public class GridMeshController : NodeController
    {

        public float GridSize;
        public float GridMargin;
        public Vector2Int GridCount;
        public Color[] Palette;
        public MeshFilter MeshFilter;

        [HideInInspector]
        public byte[,] GridColors { get; private set; }
        [HideInInspector]
        Color[] vertexColors;


        public void InitGrid()
        {
            if (GridSize <= 0 || GridCount.x < 0 || GridCount.y < 0)
            {
                return;
            }

            Mesh mesh = new Mesh();
            GridColors = new byte[GridCount.x, GridCount.y];

            int gridCount = GridCount.x * GridCount.y;
            Vector3[] vertices = new Vector3[gridCount * 4];
            Vector3[] normals = new Vector3[gridCount * 4];
            vertexColors = new Color[gridCount * 4];
            int[] triangles = new int[gridCount * 6];
            for (int x = 0; x < GridCount.x; x++)
            {
                for (int y = 0; y < GridCount.y; y++)
                {
                    int index = x * GridCount.y + y;
                    int offsetV = index * 4;
                    vertices[offsetV].Set(x * GridSize + GridMargin, y * GridSize + GridMargin, 0);
                    vertices[offsetV + 1].Set(x * GridSize + GridMargin, (y + 1) * GridSize - GridMargin, 0);
                    vertices[offsetV + 2].Set((x + 1) * GridSize - GridMargin, (y + 1) * GridSize - GridMargin, 0);
                    vertices[offsetV + 3].Set((x + 1) * GridSize - GridMargin, y * GridSize + GridMargin, 0);
                    int offsetT = index * 6;
                    triangles[offsetT] = offsetV;
                    triangles[offsetT + 1] = offsetV + 1;
                    triangles[offsetT + 2] = offsetV + 2;
                    triangles[offsetT + 3] = offsetV;
                    triangles[offsetT + 4] = offsetV + 2;
                    triangles[offsetT + 5] = offsetV + 3;

                    GridColors[x, y] = 0;
                    vertexColors[offsetV] = vertexColors[offsetV + 1] = vertexColors[offsetV + 2] = vertexColors[offsetV + 3] = Palette.Length > 0 ? Palette[0] : Color.white;
                }
            }
            for (int i = 0; i < normals.Length; i++)
            {
                normals[i].Set(0, 0, 1);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetNormals(normals);
            mesh.SetColors(vertexColors);
            mesh.Optimize();

            MeshFilter.mesh = mesh;
        }

        public void UpdateGridColor()
        {
            if (MeshFilter.mesh == null || MeshFilter.mesh.vertices.Length != GridCount.x * GridCount.y * 4 || GridColors.GetLength(0) != GridCount.x || GridColors.GetLength(1) != GridCount.y)
            {
                LogUtil.Error("should refresh grid before refresh colors, after grid count is updated");
            }

            for (int x = 0; x < GridCount.x; x++)
            {
                for (int y = 0; y < GridCount.y; y++)
                {
                    int index = x * GridCount.y + y;
                    int offsetV = index * 4;
                    vertexColors[offsetV] = vertexColors[offsetV + 1] = vertexColors[offsetV + 2] = vertexColors[offsetV + 3] = Palette.Length > GridColors[x, y] ? Palette[GridColors[x, y]] : Color.white;
                }
            }
            MeshFilter.mesh?.SetColors(vertexColors);
        }

    }

}