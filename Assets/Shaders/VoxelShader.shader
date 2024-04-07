Shader "VoxRP/VoxelShader"
{
    Properties {}
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            Cull Back
            Tags
            {
                "LightMode" = "GBuffer"
            }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            struct palette
            {
                uint3 albedo;
                uint roughness;
                uint metallic;
                uint emission;
            };

            Texture3D<uint> voxel_data;
            StructuredBuffer<palette> palettes;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            void frag(
                v2f i,
                out float4 gt0 : SV_Target0,
                out float4 gt1 : SV_Target1,
                out float4 gt2 : SV_Target2,
                out float4 gt3 : SV_Target3)
            {
                float3 test = float3(0, 0, 0);

                float3 objectSpaceLookDir = normalize(mul(unity_WorldToObject, i.worldPos - _WorldSpaceCameraPos));
                // Super cover ray marching.
                uint3 index = floor(i.uv);
                uint voxData = 0;
                {
                    float3 lookDirStep = step(0, objectSpaceLookDir);
                    float3 lookDirSign = sign(objectSpaceLookDir);
                    float3 offset = frac(i.uv);
                    uint3 voxelDataSize = uint3(0, 0, 0);
                    voxel_data.GetDimensions(voxelDataSize.x, voxelDataSize.y, voxelDataSize.z);
                    [loop]
                    for (int i = 0; i < 64; ++i) // LOD0
                    {
                        // Check Out of bound
                        if (
                            index.x < 0 || index.x > voxelDataSize.x ||
                            index.y < 0 || index.y > voxelDataSize.y ||
                            index.z < 0 || index.z > voxelDataSize.z
                        )
                        {
                            test = voxelDataSize;
                            break;
                        }

                        // Check Voxel date.
                        voxData = voxel_data[index];
                        if (voxData != 0)
                        {
                            test = float3(1, 0, 0);
                            break;
                        }

                        // Move To next voxel
                        float3 step_length_axis = lookDirStep * (1 - offset) / objectSpaceLookDir;
                        // active when objectSpaceLookDir positive.
                        step_length_axis += (1 - lookDirStep) * offset / -objectSpaceLookDir; // active when negtive.
                        float step_length = min(step_length_axis.x, min(step_length_axis.y, step_length_axis.z));
                        offset += step_length * objectSpaceLookDir;
                        if (step_length == step_length_axis.x)
                        {
                            index.x += lookDirSign.x;
                            offset.x -= lookDirSign.x;
                        }
                        else if (step_length == step_length_axis.y)
                        {
                            index.y += lookDirSign.y;
                            offset.y -= lookDirSign.y;
                        }
                        else
                        {
                            index.z += lookDirSign.z;
                            offset.z -= lookDirSign.z;
                        }
                    }
                }


                gt0 = float4(0, 0, 0, 1);
                gt1 = float4(0, 0, 0, 0);
                gt2 = float4(0, 0, 0, 0);
                gt3 = float4(0, 0, 0, 0);

                gt0.xyz = test;
                //gt0.xyz = palettes[voxData].albedo;
            }
            ENDHLSL
        }
    }
}