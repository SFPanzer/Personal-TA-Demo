Shader "VoxRP/LightPass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite On ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _gDepth;
            sampler2D _GT0;
            sampler2D _GT1;
            sampler2D _GT2;
            sampler2D _GT3;

            fixed4 frag(const v2f i, out float depth_out : SV_Depth) : SV_Target
            {
                // Set Depth.
                const float d = UNITY_SAMPLE_DEPTH(tex2D(_gDepth, i.uv));
                depth_out = Linear01Depth(d);

                fixed4 gt0 = tex2D(_GT0, i.uv);

                fixed3 albedo = gt0.rgb;
                fixed metallic = gt0.a;

                fixed3 col = fixed3(0, 0, 0);

                col = albedo;

                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}