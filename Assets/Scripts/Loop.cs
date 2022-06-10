using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Loop : MonoBehaviour
{
    public Geometry geometry = new Geometry();
    private Mesh mesh;
    private float _alpha = 3.0f / 16.0f;
    [SerializeField] private float debugRadius = 0.025f;

    // Start is called before the first frame update
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;

    }

    private void NewEdgePoint(Geometry.Edge e)
    {
        Vector3 vleft;
        Vector3 vright;
        Vector3 newPoint;

        if (e.f1 != null && e.f2 != null)
        {
            const float a = 3f / 8f;
            const float b = 1f / 8f;
            vleft = GetLeft(e);
            vright = GetRight(e);
            newPoint = a * (e.v1.pos + e.v2.pos) + b * (vleft + vright);
        }
        else
        {
            newPoint = (e.v1.pos + e.v2.pos) / 2;
        }

        e.edgePoint = newPoint;
    }

    private static Vector3 GetRight(Geometry.Edge e)
    {
        return GetOtherVertex(e.f2, e);
    }

    private static Vector3 GetLeft(Geometry.Edge e)
    {
        return GetOtherVertex(e.f1, e);
    }

    private static Vector3 GetOtherVertex(Geometry.Face face, Geometry.Edge e)
    {
        Vector3 value;
        if (face.a.pos != e.v1.pos && face.a.pos != e.v2.pos) value = face.a.pos;
        else if (face.b.pos != e.v1.pos && face.b.pos != e.v2.pos) value = face.b.pos;
        else value = face.c.pos;
        return value;
    }

    private float AlphaValue(int n)
    {
        float a = 0;

        if (n == 3)
        {
            a = 3.0f / 16.0f;
        }
        if (n > 3)
        {
            a = (1.0f / n) * (5.0f / 8.0f - Mathf.Pow(3.0f / 8.0f + (1.0f / 4.0f) * Mathf.Cos((2.0f * Mathf.PI) / n), 2.0f));
        }

        return a;
    }

    private void NewVertexPoint(Geometry.Vertex vertex)
    {
        var edges = geometry.edges.Where(edge => edge.v1.pos == vertex.pos || edge.v2.pos == vertex.pos).ToList();
        var neighbours = edges.Select(edge => GetOtherVertexFromEdge(vertex, edge)).ToList();
        var n = neighbours.Count;
        Vector3 newVertex;

        if (n < 3)
        {
            var v0 = GetOtherVertexFromEdge(vertex, edges[0]);
            var v1 = GetOtherVertexFromEdge(vertex, edges[1]);
            const float a = 3f / 4f;
            const float b = 1f / 8f;
            newVertex = a * vertex.pos + b * (v0.pos + v1.pos);
        }
        else
        {
            var alpha = AlphaValue(n);

            var sumVertex = Vector3.zero;

            foreach (var v in neighbours)
            {
                sumVertex += v.pos;
            }

            newVertex = (1 - n * alpha) * vertex.pos + alpha * sumVertex;
        }
        
        vertex.vertexPoint = newVertex;

        /*
        var neighboursEdges =
            geometry.edges.Where(edge => edge.v1.pos == vertex.pos || edge.v2.pos == vertex.pos).ToList();

        var neighboursVertex = new List<Geometry.Vertex>();

        foreach (var edge in neighboursEdges)
        {
            neighboursVertex.Add((edge.v1.pos != vertex.pos) ? edge.v1 : edge.v2);
        }

        _alpha = AlphaValue(neighboursVertex.Count);

        var sumVertex = Vector3.zero;

        foreach (var v in neighboursVertex)
        {
            sumVertex += v.pos;
        }

        var newVertex = (1 - neighboursVertex.Count * _alpha) * vertex.pos + _alpha * sumVertex;
        vertex.vertexPoint = newVertex;*/
    }

    private static Geometry.Vertex GetOtherVertexFromEdge(Geometry.Vertex vertex, Geometry.Edge edge)
    {
        return (edge.v1.pos != vertex.pos) ? edge.v1 : edge.v2;
    }

    public void RebuildGeometry()
    {
        geometry.setupMesh(mesh);
        var newVertices = new List<Vector3>();
        foreach (var f in geometry.faces)
        {
            var eab = geometry.edges.First(edge => (edge.v1.pos == f.a.pos && edge.v2.pos == f.b.pos) || (edge.v2.pos == f.a.pos && edge.v1.pos == f.b.pos));
            var eac = geometry.edges.First(edge => (edge.v1.pos == f.a.pos && edge.v2.pos == f.c.pos) || (edge.v2.pos == f.a.pos && edge.v1.pos == f.c.pos));
            var ebc = geometry.edges.First(edge => (edge.v1.pos == f.b.pos && edge.v2.pos == f.c.pos) || (edge.v2.pos == f.b.pos && edge.v1.pos == f.c.pos));

            if (f.a.vertexPoint == Vector3.zero) NewVertexPoint(f.a);
            if (f.b.vertexPoint == Vector3.zero) NewVertexPoint(f.b);
            if (f.c.vertexPoint == Vector3.zero) NewVertexPoint(f.c);

            if (eab.edgePoint == Vector3.zero) NewEdgePoint(eab);
            if (eac.edgePoint == Vector3.zero) NewEdgePoint(eac);
            if (ebc.edgePoint == Vector3.zero) NewEdgePoint(ebc);

            var f1 = new Geometry.Face(new Geometry.Vertex(f.a.vertexPoint, 0), new Geometry.Vertex(eab.edgePoint, 0), new Geometry.Vertex(eac.edgePoint, 0));
            var f2 = new Geometry.Face(new Geometry.Vertex(f.b.vertexPoint, 0), new Geometry.Vertex(eab.edgePoint, 0), new Geometry.Vertex(ebc.edgePoint, 0));
            var f3 = new Geometry.Face(new Geometry.Vertex(f.c.vertexPoint, 0), new Geometry.Vertex(eac.edgePoint, 0), new Geometry.Vertex(ebc.edgePoint, 0));
            var f4 = new Geometry.Face(new Geometry.Vertex(eab.edgePoint, 0), new Geometry.Vertex(eac.edgePoint, 0), new Geometry.Vertex(ebc.edgePoint, 0));

            var newFaces = new List<Geometry.Face>
            {
                f1,
                f2,
                f3,
                f4
            };

            foreach (var face in newFaces)
            {
                newVertices.Add(face.a.pos);
                newVertices.Add(face.b.pos);
                newVertices.Add(face.c.pos);
            }

        }
        var t = new int[newVertices.Count];
        for (var i = 0; i < newVertices.Count; i++)
        {
            t[i] = i;
        }

        UpdateMeshVertices(newVertices.ToArray(), t);


    }

    private void UpdateMeshVertices(Vector3[] vert, int[] t)
    {
        mesh.SetVertices(vert);
        mesh.triangles = t;
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        Geometry.FixNormals(mesh);


    }

    private void OnDrawGizmos()
    {

        Gizmos.color = Color.red;
        foreach (Geometry.Edge edge in geometry.edges)
        {
            Gizmos.DrawSphere(edge.edgePoint, debugRadius);
        }
        Gizmos.color = Color.green;
        foreach (Geometry.Vertex vert in geometry.vertices)
        {
            Gizmos.DrawSphere(vert.vertexPoint, debugRadius);
        }

    }
}