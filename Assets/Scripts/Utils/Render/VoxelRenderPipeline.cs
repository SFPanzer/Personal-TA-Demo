using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Utils.Render
{
    public class VoxelRenderPipeline : RenderPipeline
    {
        private static readonly int GDepthShaderID = Shader.PropertyToID("_gDepth");
        private static readonly int[] GBufferShaderID = new int[4];
        private RenderTexture _gDepth;
        private RenderTexture[] _gBuffer = new RenderTexture[4];
        private RenderTargetIdentifier[] _gBufferID = new RenderTargetIdentifier[4];

        public VoxelRenderPipeline(RenderPipelineAsset renderPipelineAsset)
        {
            _gDepth = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth)
            {
                name = "G-Depth"
            };
            _gBuffer[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32)
            {
                name = "GT0"
            };
            _gBuffer[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32)
            {
                name = "GT1"
            };
            _gBuffer[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32)
            {
                name = "GT2"
            };
            _gBuffer[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32)
            {
                name = "GT3"
            };
            
            for (var i = 0; i < 4; i++)
            {
                _gBufferID[i] = _gBuffer[i];
                GBufferShaderID[i] = Shader.PropertyToID($"_GT{i}");
            }
        }


        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (var camera in cameras)
            {
                context.SetupCameraProperties(camera);

                SetUpGlobalVariables();

                // Passes.
                GBufferPass(context, camera); 
                LightPass(context, camera);

                // Skybox and Gizmos.
                if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
                {
                    context.DrawSkybox(camera);
                }

                if (Handles.ShouldRenderGizmos())
                {
                    context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                    context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
                }

                context.Submit();
            }
        }

        private void GBufferPass(ScriptableRenderContext context, Camera camera)
        {
            Profiler.BeginSample("G-Buffer Pass");

            var cmd = new CommandBuffer();
            cmd.name = "G-Buffer pass";

            // Setup and clear render target.
            cmd.SetRenderTarget(_gBufferID, _gDepth);
            cmd.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // Culling.
            camera.TryGetCullingParameters(out var cullingParameters);
            var cullingResults = context.Cull(ref cullingParameters);

            // Config settings.
            var shaderTagId = new ShaderTagId("GBuffer"); // 使用 LightMode 为 gbuffer 的 shader
            var sortingSettings = new SortingSettings(camera);
            var drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
            var filteringSettings = FilteringSettings.defaultValue;

            // Draw
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
            context.Submit();

            Profiler.EndSample();
        }

        private static Material _lightPassMat;

        private void LightPass(ScriptableRenderContext context, Camera camera)
        {
            Profiler.BeginSample("Light Pass");

            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "Light pass";

            _lightPassMat ??= new Material(Shader.Find("VoxRP/LightPass"));
            cmd.Blit(_gBuffer[0], BuiltinRenderTextureType.CameraTarget, _lightPassMat);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            Profiler.EndSample();
        }

        private void SetUpGlobalVariables()
        {
            // Set G-buffer
            Shader.SetGlobalTexture(GDepthShaderID, _gDepth);
            for (var i = 0; i < 4; i++)
                Shader.SetGlobalTexture(GBufferShaderID[i], _gBuffer[i]);
        }
    }
}