using System;
using UnityEngine;
using UnityEngine.Rendering;
using Utils.Assets.RIFF_File;
using Random = System.Random;

namespace Utils.Assets
{
    public struct Palette
    {
        public byte AlbedoR, AlbedoG, AlbedoB;
        public byte Roughness;
        public byte Metallic;
        public byte Emission;
    }

    public class VoxelAsset
    {
        public Texture3D VoxelData { get; private set; }
        public Mesh CoverMesh { get; private set; }
        public Palette[] Palettes { get; private set; }

        public void LoadVoxelMagicFile(string path)
        {
            var magicVoxelFile = new MagicVoxelFile();
            magicVoxelFile.Load($"{Application.dataPath}/StreamingAssets/Voxel/{path}.vox");
            var tempBytes = magicVoxelFile.Chunks["SIZE"][0];
            var modelSize = new Vector3Int(
                BitConverter.ToInt32(tempBytes, 0),
                BitConverter.ToInt32(tempBytes, 4),
                BitConverter.ToInt32(tempBytes, 8)
            );
            // Init cover mesh.
            CoverMesh = CreateVoxelCoverMesh(modelSize);
            // Init voxel data.
            tempBytes = magicVoxelFile.Chunks["XYZI"][0];
            var voxelDataArray = new byte[modelSize.x * modelSize.y * modelSize.z];
            for (var i = 0; i < modelSize.x * modelSize.y * modelSize.z; i++)
            {
                voxelDataArray[i] = 0;
            }
            var numVoxels = BitConverter.ToInt32(tempBytes, 0);
            for (var i = 0; i < numVoxels; i++)
            {
                var offset = 4 + 4 * i;
                var x = tempBytes[offset + 0];
                var y = tempBytes[offset + 1];
                var z = tempBytes[offset + 2];
                var index = z * modelSize.y * modelSize.x + y * modelSize.x + x;
                var paletteIndex = (byte)(tempBytes[offset + 3] + 1); // index 0 is for empty.
                voxelDataArray[index] = paletteIndex;
                // Debug.Log($"({x}, {y}, {z}): {paletteIndex}");
            }
            VoxelData = new Texture3D(
                modelSize.x,
                modelSize.y,
                modelSize.z,
                TextureFormat.R8,
                true
            );
            VoxelData.SetPixelData(voxelDataArray, 0);
            VoxelData.Apply();
            // Init palette.
            Palettes = new Palette[255];
            if (magicVoxelFile.Chunks.TryGetValue("RGBA", out var chunk))
            {
                tempBytes = chunk[0];
                for (var i = 0; i < 255; i++)
                {
                    var offset = i * 4;
                    Palettes[i].AlbedoR = tempBytes[offset + 0];
                    Palettes[i].AlbedoG = tempBytes[offset + 1];
                    Palettes[i].AlbedoB = tempBytes[offset + 2];
                    Palettes[i].Roughness = tempBytes[offset + 3];
                    // Debug.Log($"Palettes{i}: R {Palettes[i].AlbedoR}, G {Palettes[i].AlbedoG}, B {Palettes[i].AlbedoB}");
                }
            }
            else
            {
                var random = new Random();
                for (var i = 0; i < 255; i++)
                {
                    var offset = i * 4;
                    Palettes[i].AlbedoR = (byte)random.Next(0, 255);
                    Palettes[i].AlbedoG = (byte)random.Next(0, 255);
                    Palettes[i].AlbedoB = (byte)random.Next(0, 255);
                    Palettes[i].Roughness = (byte)random.Next(0, 255);
                }
            }
            
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct VoxelCoverMeshVertex
        {
            public Vector3 Pos;
            public Vector3 Texcoord;
        }

        private static Mesh CreateVoxelCoverMesh(Vector3Int modelSize)
        {
            const float vertexPerMeter = 16;
            var mesh = new Mesh
            {
                name = "VoxelCoverMesh"
            };
            var layout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0)
            };
            // Set vertex data.
            const int vertexCount = 8;
            mesh.SetVertexBufferParams(vertexCount, layout);
            var vertexes = new VoxelCoverMeshVertex[vertexCount];
            for (var z = 0; z < 2; z++)
            {
                for (var i = 0; i < 4; i++)
                {
                    var index = i + z * 4;
                    vertexes[index].Texcoord.x = (i % 2 * modelSize.x);
                    vertexes[index].Texcoord.y = (i / 2 * modelSize.y);
                    vertexes[index].Texcoord.z = (z * modelSize.y);
                    vertexes[index].Pos = new Vector3(
                        vertexes[index].Texcoord.x / vertexPerMeter,
                        vertexes[index].Texcoord.y / vertexPerMeter,
                        vertexes[index].Texcoord.z / vertexPerMeter
                    );
                }
            }
            mesh.SetVertexBufferData(vertexes, 0, 0, vertexCount);
            // Set index buffer.
            var triangles = new ushort[]
            {
                0, 2, 3,
                3, 1, 0,
                1, 3, 7,
                7, 5, 1,
                5, 7, 6,
                6, 4, 5,
                4, 6, 2,
                2, 0, 4,
                2, 6, 7,
                7, 3, 2,
                4, 0, 1,
                1, 5, 4
            };
            var indexCount = triangles.Length;
            mesh.SetIndexBufferParams(indexCount, IndexFormat.UInt16);
            mesh.SetIndexBufferData(triangles, 0, 0, indexCount);
            // Set sub mesh.
            mesh.subMeshCount = 1;
            var subMeshDescriptor = new SubMeshDescriptor(0, indexCount);
            mesh.SetSubMesh(0, subMeshDescriptor);
            return mesh;
        }
    }
}