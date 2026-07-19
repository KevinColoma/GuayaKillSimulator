Shader "Guayakill/RedOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1, 0.05, 0.05, 1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.15)) = 0.03
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+1" }

        // Inverted-hull outline: extrude along normals and render only back faces.
        // Se dibuja como material extra sobre el SkinnedMeshRenderer del paciente herido.
        Pass
        {
            Name "RedOutline"
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            float  _OutlineWidth;
            half4  _OutlineColor;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 posOS = IN.positionOS.xyz + normalize(IN.normalOS) * _OutlineWidth;
                OUT.positionHCS = TransformObjectToHClip(posOS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
