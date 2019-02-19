﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Numerics;
using System.Text;

namespace Ara3D
{
    public interface IGeometry : IG3D
    {
        int PointsPerFace { get; }
        int NumFaces { get; }
        IArray<Vector3> Vertices { get; } 
        IArray<int> Indices { get; }  
        IArray<int> FaceSizes { get; }
        IArray<int> FaceIndices { get; } 
    }

    // https://www.scratchapixel.com/lessons/advanced-rendering/introduction-acceleration-structure/introduction
    // https://stackoverflow.com/questions/99796/when-to-use-binary-space-partitioning-quadtree-octree
    // http://gamma.cs.unc.edu/RS/paper_rt07.pdf
    public interface IGeometryAccelerations
    {
        Box Box { get; }
        object BVH { get; }
        object Octree { get; }
        IArray<int> VertexIndexLookup { get; }
        object BSP { get; }
        object AABBTree { get; }
        object RayStrips { get; }
    }

    public interface ICommonAttributeData
    {
        IArray<Vector2> Uvs(int n);
        IArray<Vector3> Uvws(int n);
        IArray<Vector3> Vertices { get; }
        IArray<int> Indices { get; }
        IArray<int> FaceSizes { get; }
        IArray<int> FaceIndices { get; }
        IArray<Vector3> MapChannelData(int n);
        IArray<int> MapChannelIndices(int n);
        IArray<Vector3> FaceNormals { get; }
        IArray<Vector3> VertexNormals { get; }
        IArray<Vector3> VertexBinormals { get; }
        IArray<Vector3> VertexTangents { get; }
        IArray<int> MaterialIds { get; }
        IArray<int> PolyGroups { get; }
        IArray<float> PerVertex(int n);
        IArray<Vector3> VertexColors { get; }
        IArray<int> SmoothingGroups { get; }
        IArray<byte> EdgeVisibility { get; }
        IArray<float> FaceSelection { get; }
        IArray<float> EdgeSelection { get; }
        IArray<float> VertexSelection { get; }
    }

    public struct Face : IArray<int>
    {
        public IGeometry Geometry { get; }
        public int Index { get; }
        public int Count => Geometry.FaceSizes[Index];
        public int this[int n] => Geometry.Indices[Geometry.FaceIndices[Index] + n];

        public Face(IGeometry g, int index)
        {
            Geometry = g;
            Index = index;
        }
    }

    public static class Geometry
    {
        // Epsilon is bigger than the real epsilon. 
        public const float EPSILON = float.Epsilon * 100;

        public readonly static IGeometry EmptyTriMesh
            = TriMesh(LinqArray.Empty<Vector3>(), LinqArray.Empty<int>());

        public readonly static IGeometry EmptyQuadMesh
            = QuadMesh(LinqArray.Empty<Vector3>(), LinqArray.Empty<int>());

        /*
        public readonly static IGeometry Box
            = QuadMesh(new Box(Vector3.Zero, Vector3.One).Corners, 
            */

        public static Vector3 MidPoint(this Face self)
            => self.Points().Average();        

        /*
        public static Vector3 MidPoint(this Edge self)
        {
            return MidPoint(self[0], self[1]);
        }
        */

        /*
        public static IArray<Vector3> EdgeMidPoints(this Face self)
        {
            return self.Edges().Select(MidPoint);
        }
        */

        public static IArray<Vector3> Points(this Face self)
            => self.Geometry.Vertices.SelectByIndex(self);        

        public static int FaceCount(this IGeometry self)
            => self.GetFaces().Count;        

        /*
        public static IGeometry ToPolyMesh(this IGeometry self, IEnumerable<IEnumerable<int>> indices)
        {
            var verts = self.Vertices;
            var flatIndices = indices.SelectMany(xs => xs).ToIArray();
            var faceIndices = indices.Where(xs => xs.Any()).Select(xs => xs.First()).ToIArray();
            return new BaseMesh(verts, flatIndices, faceIndices);
        }
        */

        /*
      public static IGeometry MergeCoplanar(this IGeometry self)
      {
          if (self.Elements.ByteCount <= 1) return self;
          var curPoly = new List<int>();
          var polys = new List<List<int>> { curPoly };
          var cur = 0;
          for (var i=1; i < self.Elements.ByteCount; ++i)
          {
              if (!self.CanMergeTris(cur, i))
              {
                  cur = i;
                  polys.Add(curPoly = new List<int>());
              }
              curPoly.Add(self.Elements[i].ToList());
          }
          return self.ToPolyMesh(polys);
      }     
      */

        public static Vector3 Tangent(this Face self)
            => self.Points()[1] - self.Points()[0];
        
        public static Vector3 Binormal(this Face self)
            => self.Points()[2] - self.Points()[0];        

        public static IArray<Triangle> Triangles(this Face self)
        {
            if (self.Count < 3) return Triangle.Zero.Repeat(0);
            var pts = self.Points();
            if (self.Count == 3) return new Triangle(pts[0], pts[1], pts[2]).Repeat(1);
            return (self.Count - 2).Select(i => new Triangle(pts[0], pts[i], pts[i + 1]));
        }

        // https://en.wikipedia.org/wiki/Coplanarity
        public static bool Coplanar(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float epsilon = EPSILON)
            => Math.Abs(Vector3.Dot(v3 - v1, Vector3.Cross(v2 - v1, v4 - v1))) < epsilon;        

        public static Vector3 Normal(this Face self)
            => Vector3.Normalize(Vector3.Cross(self.Binormal(), self.Tangent()));
        
        public static IGeometry Mesh(int sidesPerFace, IArray<Vector3> vertices, IArray<int> indices = null)
            => G3DExtensions.ToG3D(sidesPerFace, vertices, indices).ToIGeometry();

        public static IGeometry QuadMesh(this IArray<Vector3> vertices, IArray<int> indices = null)
            => Mesh(4, vertices, indices);

        public static IGeometry TriMesh(this IArray<Vector3> vertices, IArray<int> indices = null)
            => Mesh(3, vertices, indices);

        /* TODO: finish
        public static IGeometry PolyMesh(this IArray<Vector3> vertices, IArray<Face> faces)
        {
            var vertexAttribute = vertices.ToVertexAttribute();
            var indexBuffer = faces.
        }*/

        public static IGeometry QuadMesh(this Func<Vector2, Vector3> f, int usegs, int vsegs)
        {
            var verts = new List<Vector3>();
            var indices = new List<int>();
            for (var i = 0; i <= usegs; ++i)
            {
                var u = (float)i / usegs;
                for (var j = 0; j <= vsegs; ++j)
                {
                    var v = (float)j / vsegs;
                    verts.Add(f(new Vector2(u, v)));

                    if (i < usegs && j < vsegs)
                    {
                        indices.Add(i * (vsegs + 1) + j);
                        indices.Add(i * (vsegs + 1) + j + 1);
                        indices.Add((i + 1) * (vsegs + 1) + j + 1);
                        indices.Add((i + 1) * (vsegs + 1) + j);
                    }
                }
            }
            return QuadMesh(verts.ToIArray(), indices.ToIArray());
        }

        public static bool CanMergeTris(this IGeometry self, int a, int b)
        {
            var e1 = self.GetFaces()[a];
            var e2 = self.GetFaces()[b];
            if (e1.Count != e2.Count && e1.Count != 3) return false;
            var indices = new[] { e1[0], e1[1], e1[2], e2[0], e2[1], e2[2] }.Distinct().ToList();
            if (indices.Count != 4) return false;
            var verts = self.Vertices.SelectByIndex(indices.ToIArray());
            return Coplanar(verts[0], verts[1], verts[2], verts[3]);
        }

        public static IArray<Vector3> UsedVertices(this IGeometry self) 
            => self.GetFaces().SelectMany(es => es.Points());

        public static IArray<Vector3> FaceMidPoints(this IGeometry self) 
            => self.GetFaces().Select(e => e.MidPoint());

        /*
        public static IGeometry WeldVertices(this IGeometry self)
        {
            var verts = new Dictionary<Vector3, int>();
            var indices = new List<int>();
            for (var i = 0; i < self.Vertices.Count; ++i)
            {
                var v = self.Vertices[i];
                if (verts.ContainsKey(v))
                {
                    indices.Add(verts[v]);
                }
                else
                {
                    var n = verts.Count;
                    indices.Add(n);
                    verts.Add(v, n);
                }
            }
            return new Geometry(self.PointsPerFace, verts.Keys.ToIArray(), indices.ToIArray(), self.Elements);
        }
        */

        public static IGeometry Deform(this IGeometry self, Func<Vector3, Vector3> f)
            => self.ReplaceAttribute(self.Vertices.Select(f).ToVertexAttribute()).ToIGeometry();

        public static IGeometry Transform(this IGeometry self, Matrix4x4 m)
            => self.Deform(v => v.Transform(m));
        
        public static IGeometry Translate(this IGeometry self, Vector3 offset)
            => self.Deform(v => v + offset);        

        public static Box BoundingBox(this IArray<Vector3> vertices)
            => Box.Create(vertices.ToEnumerable());        

        public static Box BoundingBox(this IGeometry self)
            => self.Vertices.BoundingBox();        

        public static string GetStats(this IGeometry self)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Number of vertices {self.Vertices.Count}");
            sb.AppendLine($"Number of indices {self.Indices.Count}");
            sb.AppendLine($"Number of elements {self.GetFaces().Count}");
            sb.AppendLine($"Bounding box {self.BoundingBox()}");
            // TODO: distance from ground plane (box extent)
            // TODO: closest distance to origin (from box extent)
            // TODO: standard deviation 
            // TODO: scene analysis as well 
            // TODO: number of distinct vertices 
            // TODO: volume of bounding box
            // TODO: surface area of bounding box on ground plane
            var tris = self.Triangles();
            sb.AppendLine($"Triangles {tris.Count}");
            // TODO: this did not return actual distinct triangles and it is slow!!!
            //sb.AppendLine($"Distinct triangles {tris.ToEnumerable().Distinct().Count()}");
            var smallArea = 0.00001;
            sb.AppendLine($"Triangles with small area {tris.CountWhere(tri => tri.Area < smallArea)}");
            return sb.ToString();
        }

        public static IArray<Triangle> Triangles(this IGeometry self)
            => self.GetFaces().SelectMany(e => e.Triangles());       

        // This assumes that every polygon is convex, and without holes. Line or point elements are not converted into triangles. 
        // TODO: move all data channels along for the ride. 
        public static IGeometry ToTriMesh(this IGeometry self)
        {
            if (self.PointsPerFace == 3)
                return self;
            var indices = new List<int>();
            for (var i = 0; i < self.GetFaces().Count; ++i)
            {
                var e = self.GetFaces()[i];
                for (var j = 1; j < e.Count - 1; ++j)
                {
                    indices.Add(e[0]);
                    indices.Add(e[j]);
                    indices.Add(e[j + 1]);
                }
            }
            return TriMesh(self.Vertices, indices.ToIArray());
        }

        public static IGeometry Merge(this IArray<IGeometry> geometries)
        {
            throw new Exception("Not implemented");
            /*
            var verts = new Vector3[geometries.Sum(g => g.Vertices.Count)];
            var faces = new List<IArray<int>>();
            var offset = 0;
            foreach (var g in geometries.ToEnumerable())
            {
                g.Vertices.CopyTo(verts, offset);
                g.GetFaces().Select(e => e.Add(offset)).AddTo(faces);
                offset += g.Vertices.Count;
            }
            return PolyMesh(verts.ToIArray(), faces.ToIArray());
            */
        }

        public static bool AreAllIndicesValid(this IGeometry self)
            => self.Indices.All(i => i.Between(0, self.Vertices.Count - 1));
        
        public static bool AreAllVerticesUsed(this IGeometry self)
        {
            var bools = new bool[self.Vertices.Count];
            foreach (var i in self.Indices.ToEnumerable())
                bools[i] = true;
            return bools.All(b => b);
        }

        public static bool IsValid(this IGeometry self)
            => self.AreAllIndicesValid();

        public static IArray<int> FaceIndicesToCornerIndices(this IGeometry g3d, IArray<int> faceIndices)
        {
            if (g3d.PointsPerFace > 0)
                return faceIndices.GroupIndicesToIndices(g3d.PointsPerFace);
            var r = new List<int>();
            for (var i = 0; i < faceIndices.Count; ++i)
            {
                var index = faceIndices[i];
                var faceSize = g3d.FaceSizes[index];
                var faceIndex = g3d.FaceIndices[index];
                for (var j=0; j < faceSize; ++j)
                    r.Add(g3d.Indices[faceIndex + j]);
            }

            return r.ToIArray();
        }

        public static IGeometry RemapFaces(this IGeometry g, IArray<int> faceRemap)
        {
            var cornerRemap = g.FaceIndicesToCornerIndices(faceRemap);
            return g.VertexAttributes()
                .Concat(g.NoneAttributes())
                .Concat(g.FaceAttributes().Select(attr => attr.Remap(faceRemap)))
                .Concat(g.EdgeAttributes().Select(attr => attr.Remap(cornerRemap)))
                .Concat(g.CornerAttributes().Select(attr => attr.Remap(cornerRemap)))
                .ToG3D()
                .ToIGeometry();
        }

        public static IGeometry CopyFaces(this IGeometry g, Func<int, bool> predicate)
            => g.RemapFaces(g.NumFaces.Select(i => i).IndicesWhere(predicate).ToIArray());

        public static IGeometry CopyFaces(this IGeometry g, int from, int count)
            => g.CopyFaces(i => i >= from && i < from + count);

        public static IArray<IGeometry> CopyFaceGroups(this IGeometry g, int size)
            => g.GetFaces().Count.DivideRoundUp(size).Select(i => CopyFaces(g, i * size, size));

        public static IArray<Face> GetFaces(this IGeometry g) 
            => g.NumFaces.Select(i => new Face(g, i));
    }
}