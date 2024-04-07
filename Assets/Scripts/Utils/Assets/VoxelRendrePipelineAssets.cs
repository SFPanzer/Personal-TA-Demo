using UnityEngine;
using UnityEngine.Rendering;
using Utils.Render;

namespace Editor.CustomAssets
{
    [CreateAssetMenu(menuName = "Rendering/VoxelRenderPipelineAssets", fileName = "VoxelRenderPipelineAssets", order = 0)]
    public class VoxelRenderPipelineAssets : RenderPipelineAsset
    {
        protected override RenderPipeline CreatePipeline()
        {
            return new VoxelRenderPipeline(this);
        }
    }
}
