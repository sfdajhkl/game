using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;
using UnityEngine.Rendering;

namespace Arvito.Editor
{
    // The field kit is authored in metres in game space. Its PLY faces also
    // carry a paint index, so one mesh can show friendly, hostile or wreck paint.
    [ScriptedImporter(1, "ply")]
    public sealed class FieldMeshImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            using (var reader = File.OpenText(context.assetPath))
            {
                if (reader.ReadLine() != "ply" || reader.ReadLine() != "format ascii 1.0")
                    throw new InvalidDataException("Expected an ASCII PLY field mesh.");

                int vertexCount = 0, faceCount = 0;
                string line;
                while ((line = reader.ReadLine()) != "end_header")
                {
                    if (line == null) throw new EndOfStreamException("Missing PLY header terminator.");
                    if (line.StartsWith("element vertex ")) vertexCount = int.Parse(line.Substring(15));
                    if (line.StartsWith("element face ")) faceCount = int.Parse(line.Substring(13));
                }
                var points = new Vector3[vertexCount];
                for (int i = 0; i < points.Length; i++)
                {
                    string[] xyz = Fields(reader);
                    points[i] = new Vector3(Number(xyz[0]), Number(xyz[1]), Number(xyz[2]));
                }

                var vertices = new List<Vector3>(faceCount * 4);
                var normals = new List<Vector3>(faceCount * 4);
                var uvs = new List<Vector2>(faceCount * 4);
                var triangles = new List<int>(faceCount * 6);
                for (int i = 0; i < faceCount; i++)
                {
                    string[] face = Fields(reader);
                    int count = int.Parse(face[0]);
                    int paint = int.Parse(face[count + 1]);
                    Vector2 uv = new Vector2((paint + .5f) / 16, .125f);
                    int start = vertices.Count;
                    Vector3 a = points[int.Parse(face[1])], b = points[int.Parse(face[2])], c = points[int.Parse(face[3])];
                    Vector3 normal = Vector3.Cross(b-a,c-a).normalized;
                    for (int j = 0; j < count; j++)
                    {
                        vertices.Add(points[int.Parse(face[j+1])]);
                        normals.Add(normal);
                        uvs.Add(uv);
                        if (j < 2) continue;
                        triangles.Add(start); triangles.Add(start+j-1); triangles.Add(start+j);
                    }
                }
                var mesh = new Mesh { name = Path.GetFileNameWithoutExtension(context.assetPath) };
                if (vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0,uvs);
                mesh.SetTriangles(triangles,0);
                context.AddObjectToAsset("mesh",mesh);
                context.SetMainObject(mesh);
            }
        }

        private static float Number(string text) => float.Parse(text,CultureInfo.InvariantCulture);
        private static string[] Fields(TextReader reader)
        {
            string line = reader.ReadLine() ?? throw new EndOfStreamException("Incomplete PLY mesh.");
            return line.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
