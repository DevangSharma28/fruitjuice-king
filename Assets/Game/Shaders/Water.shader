// Juice King stylized water (URP 17, opaque, mobile friendly).
// Gentle vertex waves, two drifting caustic layers, a shallow/deep colour blend by view angle, moving sun sparkles
// and soft received shadows. No depth texture needed.
Shader "JuiceKing/Water"
{
    Properties
    {
        _ShallowColor("Shallow", Color) = (0.36, 0.86, 0.9, 1)
        _DeepColor("Deep", Color) = (0.1, 0.5, 0.74, 1)
        [MainTexture] _BaseMap("Caustics", 2D) = "white" {}
        _CausticStrength("Caustic Strength", Range(0,1)) = 0.35
        _Speed("Flow Speed", Float) = 0.03
        _WaveHeight("Wave Height", Float) = 0.05
        _WaveScale("Wave Scale", Float) = 0.35
        _Sparkle("Sparkle", Range(0,2)) = 0.8
        _SparkleScale("Sparkle Tile", Float) = 3
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _ShallowColor;
            half4 _DeepColor;
            float4 _BaseMap_ST;
            half _CausticStrength;
            float _Speed;
            float _WaveHeight;
            float _WaveScale;
            half _Sparkle;
            float _SparkleScale;
        CBUFFER_END

        float3 Waves(float3 p)
        {
            float t = _Time.y;
            p.y += (sin(p.x * _WaveScale + t * 1.2) + sin(p.z * _WaveScale * 1.3 + t * 0.9)) * _WaveHeight;
            return p;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            // Per-pixel fog (see Stylized.shader: per-vertex fog smears across big low-poly surfaces).
            half PixelFog(float3 positionWS)
            {
                float viewZ = -TransformWorldToView(positionWS).z;
                return ComputeFogFactorZ0ToFar(max(viewZ - _ProjectionParams.y, 0.0));
            }

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 p = Waves(TransformObjectToWorld(input.positionOS.xyz));
                o.positionWS = p;
                o.positionCS = TransformWorldToHClip(p);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                float2 uv = i.positionWS.xz * _BaseMap_ST.xy;
                half c1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv + float2(t, t * 0.6) * _Speed).r;
                half c2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv * 1.37 + float2(-t * 0.7, t) * _Speed).r;
                half caustic = saturate(min(c1, c2) * 1.8h - 0.55h);

                half3 V = normalize(GetWorldSpaceViewDir(i.positionWS));
                half fres = pow(1.0h - saturate(V.y), 2.0h);
                half3 col = lerp(_DeepColor.rgb, _ShallowColor.rgb, 0.35h + 0.65h * fres);
                col += caustic * _CausticStrength;

                // Sun sparkles: two drifting grids of tiny highlights that only flash where both line up.
                float2 sp = i.positionWS.xz * _SparkleScale;
                half s1 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, sp * 0.13 + float2(t * 0.05, 0)).g;
                half s2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, sp * 0.11 + float2(0, -t * 0.04)).g;
                half sparkle = pow(saturate(s1 * s2 * 1.6h), 12.0h) * _Sparkle;

                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half shade = lerp(0.72h, 1.0h, light.shadowAttenuation);
                col = col * shade * (0.75h + 0.35h * light.color) + sparkle * light.color * light.shadowAttenuation;
                col = MixFog(col, PixelFog(i.positionWS));
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex DV
            #pragma fragment DF
            struct A { float4 positionOS : POSITION; };
            float4 DV(A input) : SV_POSITION { return TransformWorldToHClip(Waves(TransformObjectToWorld(input.positionOS.xyz))); }
            half DF() : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
