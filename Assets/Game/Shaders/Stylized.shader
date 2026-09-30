// Juice King stylized lit shader (URP 17, mobile friendly).
// One main light + ambient probe, no normal maps. Soft wrapped diffuse with tinted shadows, a gentle rim light,
// contact darkening near the ground, faint painterly variation, fake reflections for metals, optional vertex colours
// and wind sway. Property names match URP Lit (_BaseColor, _BaseMap, _Smoothness, _Metallic, _EmissionColor, _Cull),
// so materials can switch between the two without code changes. SRP Batcher compatible.
Shader "JuiceKing/Stylized"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0,1)) = 0.15
        _Metallic("Metallic", Range(0,1)) = 0
        [HDR] _EmissionColor("Emission", Color) = (0,0,0,0)

        [Header(Stylized light)]
        _ShadowTint("Shadow Tint", Color) = (0.78, 0.74, 0.96, 1)
        _Wrap("Light Wrap", Range(0,1)) = 0.5
        _RimColor("Rim Color", Color) = (1, 0.96, 0.88, 1)
        _RimStrength("Rim Strength", Range(0,1)) = 0.16

        [Header(Contact darkening)]
        _AOHeight("AO Height (m)", Float) = 0.8
        _AOStrength("AO Strength", Range(0,1)) = 0.3

        [Header(Painterly detail)]
        _DetailMap("Detail Noise", 2D) = "white" {}
        _DetailScale("Detail Tile (m)", Float) = 3
        _DetailStrength("Detail Strength", Range(0,1)) = 0.1

        [Header(Vertex colour and wind)]
        [Toggle] _VertexColor("Use Vertex Color", Float) = 0
        _Wind("Wind", Range(0,2)) = 0

        [Header(Player see through)]
        [Toggle(_SEE_THROUGH)] _SeeThrough("Dither Out In Front Of Player", Float) = 0

        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 200

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Smoothness;
            half _Metallic;
            half4 _EmissionColor;
            half4 _ShadowTint;
            half _Wrap;
            half4 _RimColor;
            half _RimStrength;
            float _AOHeight;
            half _AOStrength;
            float4 _DetailMap_ST;
            float _DetailScale;
            half _DetailStrength;
            half _VertexColor;
            half _Wind;
            half _SeeThrough;
            half _Cull;
        CBUFFER_END

        // Player see-through: tall decor standing between the camera and the player dithers away around them.
        // _JKOccluder: xyz = the player's chest in world space, w = 1 while active (set by CameraFollow).
        float4 _JKOccluder;

        void SeeThroughClip(float3 positionWS, float2 pixel)
        {
        #if defined(_SEE_THROUGH)
            if (_JKOccluder.w < 0.5) return;
            float3 Q = _JKOccluder.xyz;
            float3 D = normalize(Q - _WorldSpaceCameraPos);
            float3 R = positionWS - Q;
            float2 toCam = -normalize(D.xz + float2(1e-5, 0));
            float ahead = dot(R.xz, toCam);            // metres from the player towards the camera
            float perp = length(R - dot(R, D) * D);   // distance from the camera-to-player sight line
            float k = saturate((ahead - 0.6) * 2.0) * saturate((1.5 - perp) * 2.0) * saturate((positionWS.y - 0.4) * 4.0);
            if (k <= 0.0) return;
            float dither = frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
            clip(1.0 - k * 0.72 - dither);
        #endif
        }

        // Wind: sway grows with the vertex colour alpha (0 at a leaf's base, 1 at its tip) when vertex colours are on,
        // otherwise with height above the ground.
        float3 ApplyWind(float3 positionWS, half4 vcolor)
        {
            half weight = _VertexColor > 0.5h ? vcolor.a : saturate((positionWS.y - 0.4) * 0.3);
            half w = _Wind * weight;
            if (w <= 0.0001h) return positionWS;
            float t = _Time.y;
            float phase = dot(positionWS.xz, float2(0.37, 0.23));
            float gust = 0.65 + 0.35 * sin(t * 0.45 + phase * 0.2);
            positionWS.x += sin(t * 1.9 + phase) * 0.07 * w * gust;
            positionWS.z += cos(t * 1.5 + phase * 1.3) * 0.05 * w * gust;
            positionWS.y += sin(t * 2.6 + phase * 0.7) * 0.02 * w;
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]
            ZWrite On

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _SEE_THROUGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_DetailMap); SAMPLER(sampler_DetailMap);

            // Per-pixel fog. Big low-poly surfaces (the ground slabs, water) have vertices far past the fog end; a
            // per-vertex fog factor smeared across their triangles fogged the whole surface, even right under the
            // camera, which washed the grass out in the Game view.
            half PixelFog(float3 positionWS)
            {
                float viewZ = -TransformWorldToView(positionWS).z;
                return ComputeFogFactorZ0ToFar(max(viewZ - _ProjectionParams.y, 0.0));
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 color : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS = ApplyWind(positionWS, input.color);
                o.positionWS = positionWS;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.color = input.color;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                SeeThroughClip(i.positionWS, i.positionCS.xy);
                half3 N = normalize(i.normalWS);
                half3 V = normalize(GetWorldSpaceViewDir(i.positionWS));

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half3 albedo = tex.rgb * _BaseColor.rgb;
                albedo *= lerp(half3(1, 1, 1), i.color.rgb, _VertexColor);

                // Painterly variation: soft world-space noise (tops use XZ, sides use the horizontal/vertical plane).
                float s = 1.0 / max(_DetailScale, 0.01);
                half nTop = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, i.positionWS.xz * s).r;
                half nSide = SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, float2(i.positionWS.x + i.positionWS.z, i.positionWS.y) * s).r;
                half detail = lerp(nSide, nTop, saturate(abs(N.y) * 1.5));
                albedo *= 1.0h + (detail - 0.5h) * 2.0h * _DetailStrength;

                // Main light: wrapped diffuse softened into a gentle ramp, then shadowed.
                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light light = GetMainLight(shadowCoord);
                half ndl = dot(N, light.direction);
                half wrap = saturate((ndl + _Wrap) / (1.0h + _Wrap));
                wrap = smoothstep(0.0h, 1.0h, wrap);
                half shadow = light.shadowAttenuation;
                half lit = wrap * shadow;

                half3 ambient = SampleSH(N);
                half3 direct = light.color * lit;
                half3 col = albedo * (ambient + direct);

                // Tinted shade: parts facing away or in shadow drift toward a cool lilac instead of grey.
                col *= lerp(_ShadowTint.rgb, half3(1, 1, 1), saturate(lit * 1.4h + 0.2h));

                // Soft specular + fake reflection of the sky / ground for shiny and metallic surfaces.
                half3 H = normalize(light.direction + V);
                half specPow = exp2(_Smoothness * 9.0h + 1.0h);
                half spec = pow(saturate(dot(N, H)), specPow) * _Smoothness * shadow;
                half3 specCol = lerp(light.color, light.color * albedo * 1.6h, _Metallic);
                col += specCol * spec * (0.35h + _Metallic * 0.9h);
                half3 R = reflect(-V, N);
                half3 env = SampleSH(R);
                col = lerp(col, env * albedo * 1.5h + albedo * direct * 0.4h, _Metallic * 0.55h);

                // Rim light (brighter on the lit side) gives shapes a clean edge from the high camera.
                half rim = pow(1.0h - saturate(dot(N, V)), 3.0h) * _RimStrength * (0.45h + 0.55h * lit);
                col += _RimColor.rgb * rim;

                // Contact darkening: sides of objects near the ground (upward faces stay untouched).
                half side = saturate(1.0h - N.y);
                half aoH = saturate(i.positionWS.y / max(_AOHeight, 0.01));
                half ao = lerp(1.0h - _AOStrength, 1.0h, aoH * aoH);
                col *= lerp(1.0h, ao, side);

                col += _EmissionColor.rgb;
                col = MixFog(col, PixelFog(i.positionWS));
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 ShadowVert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = ApplyWind(TransformObjectToWorld(input.positionOS.xyz), input.color);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
            #if UNITY_REVERSED_Z
                positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #else
                positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
            #endif
                return positionCS;
            }

            half4 ShadowFrag() : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _SEE_THROUGH

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct DepthVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            DepthVaryings DepthVert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);
                DepthVaryings o;
                o.positionWS = ApplyWind(TransformObjectToWorld(input.positionOS.xyz), input.color);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            half DepthFrag(DepthVaryings i) : SV_Target
            {
                SeeThroughClip(i.positionWS, i.positionCS.xy);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
