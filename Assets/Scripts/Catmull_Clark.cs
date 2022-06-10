using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class Catmull_Clark : MonoBehaviour
{

    Mesh mesh;
    Geometry myCube = new Geometry();

    // Start is called before the first frame update
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;


    }

    public void SubDivision()
    {
        myCube.setupMesh(mesh);
        CreateFacePoint();
        CreateEdgePoint();
        CreateVertexPoint();

        RecreateMesh();
    }

    void CreateFacePoint()
    {
        foreach (Geometry.Face face in myCube.faces)
        {
            Vector3 p0 = face.a.pos;
            Vector3 p1 = face.b.pos;
            Vector3 p2 = face.c.pos;
            Vector3 total = p0 + p1 + p2;
            face.facePoint = total / 3.0f;

        }
    }
    void CreateEdgePoint()
    {
        foreach (Geometry.Edge edge in myCube.edges)
        {
            Vector3 p0 = edge.v1.pos;
            Vector3 p1 = edge.v2.pos;
            Vector3 p2 = edge.f1.facePoint;
            Vector3 p3;
            if (edge.f2 != null)
            {
                p3 = edge.f2.facePoint;
            }
            else
            {
                p3 = Vector3.Lerp(edge.v1.pos, edge.v2.pos, 0.5f) - p2 * (1.0f / 5.0f);

            }
            Vector3 total = p0 + p1 + p2 + p3;
            edge.edgePoint = total / 4.0f;
        }
    }
    void CreateVertexPoint()
    {
        Vector3 Q;
        Vector3 R;
        foreach (Geometry.Vertex vertex in myCube.vertices)
        {
            Q = Vector3.zero;
            R = Vector3.zero;
            List<Geometry.Edge> edges = myCube.edges.Where(edge => edge.v1.pos == vertex.pos || edge.v2.pos == vertex.pos).ToList();

            List<Vector3> allFacePoint = new List<Vector3>();
            foreach (Geometry.Edge edge in edges)
            {
                allFacePoint.Add(edge.f1.facePoint);
                if (edge.f2 != null)
                {
                    allFacePoint.Add(edge.f2.facePoint);
                }
            }
            allFacePoint = allFacePoint.Distinct().ToList();

            for (int i = 0; i < allFacePoint.Count; i++)
            {
                Q += allFacePoint[i];
            }
            Q /= allFacePoint.Count;

            foreach (Geometry.Edge edge in edges)
            {
                Vector3 midPoint = Vector3.Lerp(edge.v1.pos, edge.v2.pos, 0.5f);
                R += midPoint;
            }
            float n = edges.Count;
            R /= n;

            Vector3 newPos = (1.0f / n) * Q + (2.0f / n) * R + ((n - 3.0f) / n) * vertex.pos;
            vertex.vertexPoint = newPos;
        }
    }

    public void RecreateMesh()
    {
        List<Vector3> newVertices = new List<Vector3>();

        foreach (Geometry.Face face in myCube.faces)
        {
            Geometry.Edge eAB = myCube.edges.Where(edge => (edge.v1.pos == face.a.pos && edge.v2.pos == face.b.pos) || (edge.v2.pos == face.a.pos && edge.v1.pos == face.b.pos)).First();
            Geometry.Edge eAC = myCube.edges.Where(edge => (edge.v1.pos == face.a.pos && edge.v2.pos == face.c.pos) || (edge.v2.pos == face.a.pos && edge.v1.pos == face.c.pos)).First();
            Geometry.Edge eBC = myCube.edges.Where(edge => (edge.v1.pos == face.b.pos && edge.v2.pos == face.c.pos) || (edge.v2.pos == face.b.pos && edge.v1.pos == face.c.pos)).First();
            

            //(a, edge_pointab, face_pointabc, edge_pointca)
            Geometry.Face f1 = new Geometry.Face(new Geometry.Vertex(face.a.vertexPoint, 0), new Geometry.Vertex(eAB.edgePoint, 0), new Geometry.Vertex(face.facePoint, 0));
            Geometry.Face f2 = new Geometry.Face(new Geometry.Vertex(face.facePoint, 0), new Geometry.Vertex(eAC.edgePoint, 0), new Geometry.Vertex(face.a.vertexPoint, 0));
            //(b, edge_pointbc, face_pointabc, edge_pointab)
            Geometry.Face f3 = new Geometry.Face(new Geometry.Vertex(face.b.vertexPoint, 0), new Geometry.Vertex(eBC.edgePoint, 0), new Geometry.Vertex(face.facePoint, 0));
            Geometry.Face f4 = new Geometry.Face(new Geometry.Vertex(face.facePoint, 0), new Geometry.Vertex(eAB.edgePoint, 0), new Geometry.Vertex(face.b.vertexPoint, 0));
            //(c, edge_pointca, face_pointabc, edge_pointbc)
            Geometry.Face f5 = new Geometry.Face(new Geometry.Vertex(face.c.vertexPoint, 0), new Geometry.Vertex(eAC.edgePoint, 0), new Geometry.Vertex(face.facePoint, 0));
            Geometry.Face f6 = new Geometry.Face(new Geometry.Vertex(face.facePoint, 0), new Geometry.Vertex(eBC.edgePoint, 0), new Geometry.Vertex(face.c.vertexPoint, 0));

            List<Geometry.Face> newFaces = new List<Geometry.Face>
            {
                f1, f2, f3, f4, f5, f6
            };

            foreach (var f in newFaces)
            {
                newVertices.Add(f.a.pos);
                newVertices.Add(f.b.pos);
                newVertices.Add(f.c.pos);
            }


        }
        int[] t = new int[newVertices.Count];
        for (int i = 0; i < newVertices.Count; i++)
        {
            t[i] = i;
        }

        UpdateMeshVertices(newVertices.ToArray(), t);

    }

    public static IEnumerable<T[]> Combinations<T>(IEnumerable<T> source)
    {
        if (null == source)
            throw new ArgumentNullException(nameof(source));

        T[] data = source.ToArray();

        return Enumerable
          .Range(0, 1 << (data.Length))
          .Select(index => data
             .Where((v, i) => (index & (1 << i)) != 0)
             .ToArray());
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        foreach (Geometry.Face face in myCube.faces)
        {
            Gizmos.DrawSphere(face.facePoint, 0.02f);
        }
        Gizmos.color = Color.red;
        foreach (Geometry.Edge edge in myCube.edges)
        {
            Gizmos.DrawSphere(edge.edgePoint, 0.02f);
        }
        Gizmos.color = Color.green;
        foreach (Geometry.Vertex vert in myCube.vertices)
        {
            Gizmos.DrawSphere(vert.vertexPoint, 0.02f);
        }

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

    
}
