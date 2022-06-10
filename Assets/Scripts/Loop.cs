using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Loop : MonoBehaviour
{
    public Geometry geometry = new Geometry();
    private Mesh mesh;
    private float alpha = 3.0f/16.0f;

    // Start is called before the first frame update
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        
    }


    private void NewEdgePoint(Geometry.Edge e)
    {
        Vector3 vleft = Vector3.zero;
        Vector3 vright = Vector3.zero;

        if (e.f1.a.pos != e.v1.pos && e.f1.a.pos != e.v2.pos) vleft = e.f1.a.pos;
        else if (e.f1.b.pos != e.v1.pos && e.f1.b.pos != e.v2.pos) vleft = e.f1.b.pos;
        else vleft = e.f1.c.pos;

        if (e.f2.a.pos != e.v1.pos && e.f2.a.pos != e.v2.pos) vright = e.f2.a.pos;
        else if (e.f2.b.pos != e.v1.pos && e.f2.b.pos != e.v2.pos) vright = e.f2.b.pos;
        else vright = e.f2.c.pos;

        Vector3 newPoint = (3.0f/8.0f) * (e.v1.pos + e.v2.pos) + (1.0f/8.0f) * (vleft + vright);

        e.edgePoint = newPoint;
    }

    private float AlphaValue(float n)
    {
        float a = 0;
        if(n == 3)
        {
            a = 3.0f / 16.0f;
        }
        if(n > 3)
        {
            a = (1.0f / n) * (5.0f/8.0f - Mathf.Pow(3.0f/8.0f + (1.0f/4.0f) * Mathf.Cos((2.0f * Mathf.PI) / n), 2.0f));
            a = 3.0f / (8.0f * n);
        }

        return a;
    }

    private void NewVertexPoint(Geometry.Vertex vertex)
    {
        List<Geometry.Edge> neightboursEdges = 
            geometry.edges.Where(edge => edge.v1.pos == vertex.pos || edge.v2.pos == vertex.pos).ToList();

        List<Geometry.Vertex> neightboursVertex = new List<Geometry.Vertex>();

        foreach(var edge in neightboursEdges)
        {
            neightboursVertex.Add((edge.v1.pos != vertex.pos) ? edge.v1 : edge.v2);
        }

        alpha = AlphaValue(neightboursVertex.Count);

        Vector3 sumVertex = Vector3.zero;

        foreach(var v in neightboursVertex)
        {
            sumVertex += v.pos;
        }

        Vector3 newVertex = (1 - neightboursVertex.Count * alpha) * vertex.pos + alpha * sumVertex;
        vertex.vertexPoint = newVertex;
    }

    public void RebuildGeometry()
    {
        geometry.setupMesh(mesh);
        List<Vector3> newVertices = new List<Vector3>();
        foreach (var f in geometry.faces)
        {
            Geometry.Edge eab = geometry.edges.Where(edge => (edge.v1.pos == f.a.pos && edge.v2.pos == f.b.pos) || (edge.v2.pos == f.a.pos && edge.v1.pos == f.b.pos)).First();
            Geometry.Edge eac = geometry.edges.Where(edge => (edge.v1.pos == f.a.pos && edge.v2.pos == f.c.pos) || (edge.v2.pos == f.a.pos && edge.v1.pos == f.c.pos)).First();
            Geometry.Edge ebc = geometry.edges.Where(edge => (edge.v1.pos == f.b.pos && edge.v2.pos == f.c.pos) || (edge.v2.pos == f.b.pos && edge.v1.pos == f.c.pos)).First();

            if (f.a.vertexPoint == Vector3.zero) NewVertexPoint(f.a);
            if (f.b.vertexPoint == Vector3.zero) NewVertexPoint(f.b);
            if (f.c.vertexPoint == Vector3.zero) NewVertexPoint(f.c);

            if (eab.edgePoint == Vector3.zero) NewEdgePoint(eab);
            if (eac.edgePoint == Vector3.zero) NewEdgePoint(eac);
            if (ebc.edgePoint == Vector3.zero) NewEdgePoint(ebc);

            Geometry.Face f1 = new Geometry.Face(new Geometry.Vertex(f.a.vertexPoint,0), new Geometry.Vertex(eab.edgePoint, 0), new Geometry.Vertex(eac.edgePoint, 0));
            Geometry.Face f2 = new Geometry.Face(new Geometry.Vertex(f.b.vertexPoint,0), new Geometry.Vertex(eab.edgePoint, 0), new Geometry.Vertex(ebc.edgePoint, 0));
            Geometry.Face f3 = new Geometry.Face(new Geometry.Vertex(f.c.vertexPoint,0), new Geometry.Vertex(eac.edgePoint, 0), new Geometry.Vertex(ebc.edgePoint, 0));
            Geometry.Face f4 = new Geometry.Face(new Geometry.Vertex(eab.edgePoint,0), new Geometry.Vertex(eac.edgePoint, 0), new Geometry.Vertex(ebc.edgePoint, 0));

            List<Geometry.Face> newFaces = new List<Geometry.Face>
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
        int[] t = new int[newVertices.Count];
        for (int i = 0; i < newVertices.Count; i++)
        {
            t[i] = i;
        }

        UpdateMeshVertices(newVertices.ToArray(), t);


    }

    void UpdateMeshVertices(Vector3[] vert, int[] t)
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
            Gizmos.DrawSphere(edge.edgePoint, 0.05f);
        }
        Gizmos.color = Color.green;
        foreach (Geometry.Vertex vert in geometry.vertices)
        {
            Gizmos.DrawSphere(vert.vertexPoint, 0.05f);
        }

    }
}
