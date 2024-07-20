using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class DrawTriangle : MonoBehaviour
{
    private Mesh mesh;
    private MeshFilter mf;

    void Start()
    {
        mesh = new Mesh();
        mf = GetComponent<MeshFilter>();
        mf.mesh = mesh;
    }

    public void DrawTriangleUtils(Vector3 point1, Vector3 point2, Vector3 point3)
    {
        Vector3[] vertices = new Vector3[3];
        int[] triangles = new int[3];

        vertices[0] = point1;
        vertices[1] = point2;
        vertices[2] = point3;

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        // Optional: Add UVs for texture mapping
        Vector2[] uvs = new Vector2[vertices.Length];
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        uvs[2] = new Vector2(0, 1);
        mesh.uv = uvs;
    }
}