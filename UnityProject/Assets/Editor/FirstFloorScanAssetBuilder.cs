#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CommerceDemo.EditorTools
{
    /// <summary>
    /// Converts the private Scaniverse GLB scans supplied for the home (first
    /// floor, garage, and upstairs) into native Unity mesh, texture, and
    /// material assets. Keeping the conversion in the editor avoids adding a
    /// runtime glTF package to the WebGL build.
    /// </summary>
    public static class FirstFloorScanAssetBuilder
    {
        public const string MeshPath = "Assets/Models/HomeScans/FirstFloorScan.asset";
        public const string TexturePath = "Assets/Models/HomeScans/FirstFloorScanTexture.jpg";
        public const string MaterialPath = "Assets/Models/HomeScans/FirstFloorScan.mat";

        public const string GarageMeshPath = "Assets/Models/HomeScans/GarageScan.asset";
        public const string GarageTexturePath = "Assets/Models/HomeScans/GarageScanTexture.jpg";
        public const string GarageMaterialPath = "Assets/Models/HomeScans/GarageScan.mat";

        public const string UpstairsMeshPath = "Assets/Models/HomeScans/UpstairsScan.asset";
        public const string UpstairsTexturePath = "Assets/Models/HomeScans/UpstairsScanTexture.jpg";
        public const string UpstairsMaterialPath = "Assets/Models/HomeScans/UpstairsScan.mat";

        const uint GlbMagic = 0x46546C67;
        const uint JsonChunkType = 0x4E4F534A;
        const uint BinChunkType = 0x004E4942;

        [MenuItem("Tools/House Model/Import First Floor GLB")]
        public static void CreateOrUpdateAssets() =>
            BuildScanAssets("first-floor.glb", MeshPath, TexturePath, MaterialPath, "FirstFloorScan");

        // NOTE: the supplied capture files are cross-labeled — "upstairs.glb" is
        // actually the garage and "garage.glb" is actually the upstairs, so each
        // location's asset is imported from the opposite source file.
        [MenuItem("Tools/House Model/Import Garage GLB")]
        public static void CreateGarageAssets() =>
            BuildScanAssets("upstairs.glb", GarageMeshPath, GarageTexturePath, GarageMaterialPath, "GarageScan");

        [MenuItem("Tools/House Model/Import Upstairs GLB")]
        public static void CreateUpstairsAssets() =>
            BuildScanAssets("garage.glb", UpstairsMeshPath, UpstairsTexturePath, UpstairsMaterialPath, "UpstairsScan");

        [MenuItem("Tools/House Model/Import All Scan GLBs")]
        public static void CreateAllAssets()
        {
            CreateOrUpdateAssets();
            CreateGarageAssets();
            CreateUpstairsAssets();
        }

        static void BuildScanAssets(string glbName, string meshPath, string texturePath, string materialPath, string assetName)
        {
            string sourcePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "HouseSources", glbName));
            if (!File.Exists(sourcePath))
            {
                Debug.LogWarning($"[FirstFloorScanAssetBuilder] Source not found: {sourcePath}. Procedural fallback will be used.");
                return;
            }

            byte[] bytes = File.ReadAllBytes(sourcePath);
            if (bytes.Length < 28 || ReadUInt32(bytes, 0) != GlbMagic || ReadUInt32(bytes, 4) != 2)
            {
                throw new InvalidDataException($"{glbName} is not a supported glTF 2.0 binary file.");
            }

            int jsonLength = checked((int)ReadUInt32(bytes, 12));
            if (ReadUInt32(bytes, 16) != JsonChunkType)
            {
                throw new InvalidDataException($"{glbName} does not start with a JSON chunk.");
            }

            string json = System.Text.Encoding.UTF8.GetString(bytes, 20, jsonLength).TrimEnd('\0', ' ', '\t', '\r', '\n');
            var document = JsonUtility.FromJson<GltfDocument>(json);
            int binHeader = 20 + jsonLength;
            int binLength = checked((int)ReadUInt32(bytes, binHeader));
            if (ReadUInt32(bytes, binHeader + 4) != BinChunkType)
            {
                throw new InvalidDataException($"{glbName} does not contain a binary data chunk.");
            }

            int binStart = binHeader + 8;
            if (binStart + binLength > bytes.Length)
            {
                throw new InvalidDataException($"{glbName} binary chunk is truncated.");
            }

            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Models", "HomeScans"));
            var primitive = document.meshes[0].primitives[0];
            Vector3[] vertices = ReadPositions(bytes, binStart, document, primitive.attributes.POSITION);
            Vector2[] uvs = ReadUvs(bytes, binStart, document, primitive.attributes.TEXCOORD_0);
            int[] triangles = ReadIndices(bytes, binStart, document, primitive.indices);

            var generatedMesh = new Mesh
            {
                name = assetName,
                indexFormat = IndexFormat.UInt32
            };
            generatedMesh.vertices = vertices;
            generatedMesh.uv = uvs;
            generatedMesh.triangles = triangles;
            generatedMesh.RecalculateNormals();
            generatedMesh.RecalculateTangents();
            generatedMesh.RecalculateBounds();

            var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existingMesh == null)
            {
                AssetDatabase.CreateAsset(generatedMesh, meshPath);
            }
            else
            {
                EditorUtility.CopySerialized(generatedMesh, existingMesh);
                UnityEngine.Object.DestroyImmediate(generatedMesh);
                EditorUtility.SetDirty(existingMesh);
            }

            var image = document.images[0];
            var imageView = document.bufferViews[image.bufferView];
            byte[] imageBytes = new byte[imageView.byteLength];
            Buffer.BlockCopy(bytes, binStart + imageView.byteOffset, imageBytes, 0, imageBytes.Length);
            string textureFilePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", texturePath));
            File.WriteAllBytes(textureFilePath, imageBytes);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);

            var textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (textureImporter != null)
            {
                textureImporter.sRGBTexture = true;
                textureImporter.mipmapEnabled = true;
                textureImporter.maxTextureSize = 4096;
                textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
                textureImporter.wrapMode = TextureWrapMode.Repeat;
                textureImporter.SaveAndReimport();
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = assetName };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.mainTexture = texture;
            material.color = Color.white;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0.18f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(material);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FirstFloorScanAssetBuilder] Imported {vertices.Length:N0} vertices and {triangles.Length / 3:N0} triangles from {sourcePath}.");
        }

        static Vector3[] ReadPositions(byte[] bytes, int binStart, GltfDocument document, int accessorIndex)
        {
            var accessor = document.accessors[accessorIndex];
            if (accessor.componentType != 5126 || accessor.type != "VEC3")
            {
                throw new InvalidDataException("Scan positions must be FLOAT VEC3.");
            }

            var view = document.bufferViews[accessor.bufferView];
            int stride = view.byteStride > 0 ? view.byteStride : 12;
            int start = binStart + view.byteOffset + accessor.byteOffset;
            var result = new Vector3[accessor.count];
            for (int i = 0; i < result.Length; i++)
            {
                int offset = start + i * stride;
                // glTF is right-handed; reflecting Z converts it to Unity's
                // left-handed coordinate system.
                result[i] = new Vector3(
                    ReadSingle(bytes, offset),
                    ReadSingle(bytes, offset + 4),
                    -ReadSingle(bytes, offset + 8));
            }
            return result;
        }

        static Vector2[] ReadUvs(byte[] bytes, int binStart, GltfDocument document, int accessorIndex)
        {
            var accessor = document.accessors[accessorIndex];
            if (accessor.componentType != 5126 || accessor.type != "VEC2")
            {
                throw new InvalidDataException("Scan UVs must be FLOAT VEC2.");
            }

            var view = document.bufferViews[accessor.bufferView];
            int stride = view.byteStride > 0 ? view.byteStride : 8;
            int start = binStart + view.byteOffset + accessor.byteOffset;
            var result = new Vector2[accessor.count];
            for (int i = 0; i < result.Length; i++)
            {
                int offset = start + i * stride;
                result[i] = new Vector2(ReadSingle(bytes, offset), 1f - ReadSingle(bytes, offset + 4));
            }
            return result;
        }

        static int[] ReadIndices(byte[] bytes, int binStart, GltfDocument document, int accessorIndex)
        {
            var accessor = document.accessors[accessorIndex];
            if (accessor.componentType != 5125 || accessor.type != "SCALAR" || accessor.count % 3 != 0)
            {
                throw new InvalidDataException("Scan indices must be UNSIGNED_INT triangles.");
            }

            var view = document.bufferViews[accessor.bufferView];
            int start = binStart + view.byteOffset + accessor.byteOffset;
            var result = new int[accessor.count];
            for (int triangle = 0; triangle < accessor.count / 3; triangle++)
            {
                int source = start + triangle * 12;
                int target = triangle * 3;
                // Swap winding after the Z reflection above.
                result[target] = checked((int)ReadUInt32(bytes, source));
                result[target + 1] = checked((int)ReadUInt32(bytes, source + 8));
                result[target + 2] = checked((int)ReadUInt32(bytes, source + 4));
            }
            return result;
        }

        static uint ReadUInt32(byte[] bytes, int offset) => BitConverter.ToUInt32(bytes, offset);
        static float ReadSingle(byte[] bytes, int offset) => BitConverter.ToSingle(bytes, offset);

        [Serializable]
        class GltfDocument
        {
            public GltfBufferView[] bufferViews;
            public GltfAccessor[] accessors;
            public GltfMesh[] meshes;
            public GltfImage[] images;
        }

        [Serializable]
        class GltfBufferView
        {
            public int byteOffset;
            public int byteLength;
            public int byteStride;
        }

        [Serializable]
        class GltfAccessor
        {
            public int bufferView;
            public int byteOffset;
            public int componentType;
            public int count;
            public string type;
        }

        [Serializable]
        class GltfMesh
        {
            public GltfPrimitive[] primitives;
        }

        [Serializable]
        class GltfPrimitive
        {
            public int indices;
            public GltfAttributes attributes;
        }

        [Serializable]
        class GltfAttributes
        {
            public int POSITION;
            public int TEXCOORD_0;
        }

        [Serializable]
        class GltfImage
        {
            public int bufferView;
        }
    }
}
#endif
