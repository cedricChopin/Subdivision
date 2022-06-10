using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Geometry
{
    public List<Face> faces = new List<Face>();
    public List<Edge> edges = new List<Edge>();
    public List<Vertex> vertices = new List<Vertex>();
    public Mesh mesh;

    public class Face
    {
        public Face(Vertex a, Vertex b, Vertex c)
        {
            this.a = a;
            this.b = b;
            this.c = c;
        }
        public Vertex a, b, c;
        public Vector3 facePoint;
    }

    public class Edge
    {
        public Edge(Vertex v1, Vertex v2)
        {
            this.v1 = v1;
            this.v2 = v2;
        }
        public Vertex v1, v2;
        public Vector3 edgePoint;
        public Face f1, f2;
        public bool isOneVisibleAndInvisible;
    }

    public class Vertex
    {
        public Vertex(Vector3 pos, int index)
        {
            this.pos = pos;
            this.index = index;
        }
        public Vector3 pos;
        public Vector3 vertexPoint;
        public int index;
        public bool isOneVisibleAndInvisible;
        public override string ToString()
        {
            return pos.ToString("f3");
        }
    }
    public void setupMesh(Mesh mesh)
    {
        this.mesh = mesh;
        faces = new List<Face>();
        edges = new List<Edge>();
        vertices = new List<Vertex>();
        addVertices();
        addFaces();
        linkEdgesToFace();
    }
    void addVertices()
    {
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            Vertex v = new Vertex(mesh.vertices[i], i);
            vertices.Add(v);
        }
    }
    void addFaces()
    {
        for (int face = 0; face < mesh.triangles.Length; face += 3)
        {
            Vertex v0 = vertices[mesh.triangles[face]];
            Vertex v1 = vertices[mesh.triangles[face + 1]];
            Vertex v2 = vertices[mesh.triangles[face + 2]];
            Face f = new Face(v0, v1, v2);
            faces.Add(f);
            addEdges(v0, v1, v2, f);
        }
    }
    void addEdges(Vertex v0, Vertex v1, Vertex v2, Face f)
    {
        Edge e1 = new Edge(v0, v1);
        Edge e2 = new Edge(v0, v2);
        Edge e3 = new Edge(v1, v2);

        e1.f1 = f;
        e2.f1 = f;
        e3.f1 = f;



        if (edges.Where(edge => (edge.v1.pos == e1.v1.pos && edge.v2.pos == e1.v2.pos) || (edge.v1.pos == e1.v2.pos && edge.v2.pos == e1.v1.pos)).ToList().Count == 0)
        {
            edges.Add(e1);
        }
        if (edges.Where(edge => (edge.v1.pos == e2.v1.pos && edge.v2.pos == e2.v2.pos) || (edge.v1.pos == e2.v2.pos && edge.v2.pos == e2.v1.pos)).ToList().Count == 0)
        {
            edges.Add(e2);
        }
        if (edges.Where(edge => (edge.v1.pos == e3.v1.pos && edge.v2.pos == e3.v2.pos) || (edge.v1.pos == e3.v2.pos && edge.v2.pos == e3.v1.pos)).ToList().Count == 0)
        {
            edges.Add(e3);
        }
    }

    void linkEdgesToFace()
    {

        foreach (Edge edge in edges)
        {
            foreach (Face face in faces)
            {
                if (edge.f1 != face)
                {
                    if ((face.a.pos == edge.v1.pos && face.b.pos == edge.v2.pos) || (face.b.pos == edge.v1.pos && face.a.pos == edge.v2.pos))
                    {
                        edge.f2 = face;
                    }
                    else if ((face.a.pos == edge.v1.pos && face.c.pos == edge.v2.pos) || (face.c.pos == edge.v1.pos && face.a.pos == edge.v2.pos))
                    {
                        edge.f2 = face;
                    }
                    else if ((face.b.pos == edge.v1.pos && face.c.pos == edge.v2.pos) || (face.c.pos == edge.v1.pos && face.b.pos == edge.v2.pos))
                    {
                        edge.f2 = face;
                    }
                }
            }
        }

    }

    public static void FixNormals(Mesh mesh)
    {
        if (mesh.vertexCount != mesh.normals.Length)
            mesh.RecalculateNormals();

        Vector3[] normals = mesh.normals;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        Vector3 center = CenterPoint(vertices);

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 v1 = vertices[triangles[i]];
            Vector3 v2 = vertices[triangles[i + 1]];
            Vector3 v3 = vertices[triangles[i + 2]];

            Vector3 n1 = normals[triangles[i]];
            Vector3 n2 = normals[triangles[i + 1]];
            Vector3 n3 = normals[triangles[i + 2]];

            Vector3 calcNormal = CalculateNormal(v1, v2, v3);

            Vector3 midPoint = center - ((v1 + v2 + v3) / 3);

            if (!WithinTolerance(n1))
                n1 = calcNormal;
            if (!WithinTolerance(n2))
                n2 = calcNormal;
            if (!WithinTolerance(n3))
                n3 = calcNormal;

            if (IsFacingInwards(calcNormal, midPoint))
                Array.Reverse(triangles, i, 3);
        }

        mesh.normals = normals;
        mesh.triangles = triangles;
    }

    private static Vector3 CenterPoint(Vector3[] vertices)
    {
        Vector3 center = Vector3.zero;

        for (int i = 1; i < vertices.Length; ++i)
            center += vertices[i];

        return center / vertices.Length;
    }

    private static bool WithinTolerance(Vector3 normal) => normal.magnitude > 0.001f;

    private static bool IsFacingInwards(Vector3 normal, Vector3 direction) =>
           Vector3.Dot(direction.normalized, normal.normalized) > 0f;
    private static Vector3 CalculateNormal(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 v1 = b - a;
        Vector3 v2 = c - a;

        return new Vector3
        (
            (v1.y * v2.z) - (v1.z * v2.y),
            (v1.z * v2.x) - (v1.x * v2.z),
            (v1.x * v2.y) - (v1.y * v2.x)
        ).normalized;
    }

}