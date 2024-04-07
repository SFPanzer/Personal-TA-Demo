using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Utils.Assets;
using UnityEngine;

namespace Game
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class VoxelGameObject : MonoBehaviour
    {
        public string voxResourcesName;

        private static Material _material;
        
        private VoxelAsset _voxelModel;
        [SerializeField]
        private Texture3D _voxelData;
        private ComputeBuffer _computeBuffer;
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _materialPropertyBlock;

        private void OnEnable()
        {
            _voxelModel = new VoxelAsset();
            _voxelModel.LoadVoxelMagicFile(voxResourcesName);
            _voxelData = _voxelModel.VoxelData;
            _computeBuffer = new ComputeBuffer(255, Marshal.SizeOf(typeof(Palette)));
            _computeBuffer.SetData(_voxelModel.Palettes);
            
            gameObject.GetComponent<MeshFilter>().mesh = _voxelModel.CoverMesh;
            
            _material ??= new Material(Shader.Find("VoxRP/VoxelShader"));
            _meshRenderer = gameObject.GetComponent<MeshRenderer>();
            _meshRenderer.SetSharedMaterials(new List<Material>{_material});

            _materialPropertyBlock ??= new MaterialPropertyBlock();
            _materialPropertyBlock.SetTexture("voxel_data", _voxelModel.VoxelData);
            _materialPropertyBlock.SetBuffer("palettes", _computeBuffer);
            
            _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
        }

        private void OnDisable()
        {
            _computeBuffer.Release();
        }
    }
}