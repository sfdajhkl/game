Shader "Arvito/Field Metal"
{
    Properties
    {
        [NoScaleOffset] _Palette ("Metal palette", 2D) = "white" {}
        _Row ("Paint row", Float) = 0
        _Solid ("Use solid colour", Float) = 0
        _Tint ("Solid colour", Color) = (1,1,1,1)
        _Glow ("Emissive", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Field"
            Tags { "LightMode"="UniversalForwardOnly" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_Palette); SAMPLER(sampler_Palette);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                float _Row, _Solid, _Glow;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normal : TEXCOORD0; float3 world : TEXCOORD1; float2 uv : TEXCOORD2; };
            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.world = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.world);
                o.normal = TransformObjectToWorldNormal(v.normalOS);
                o.uv = v.uv;
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                half3 normal = normalize(i.normal);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(i.world));
                #else
                    float4 shadowCoord = TransformWorldToShadowCoord(i.world);
                #endif
                Light sun = GetMainLight(shadowCoord);
                half diffuse = saturate(dot(normal, sun.direction)) * sun.shadowAttenuation;
                half steps = floor(diffuse * 3.99h) / 3.0h;
                half3 base = SAMPLE_TEXTURE2D(_Palette, sampler_Palette, float2(i.uv.x, (_Row + .5) / 4.0)).rgb;
                float index = floor(i.uv.x * 16.0);
                half lamp = (abs(index - 9) < .1 || abs(index - 15) < .1) ? 1.0h : 0.0h;
                base = lerp(base, _Tint.rgb, _Solid);
                half3 ambient = half3(.34h,.42h,.49h);
                half3 key = half3(.86h,.81h,.70h);
                half3 colour = base * (ambient + key * steps);
                colour = lerp(colour, base * 1.6h, lerp(lamp, _Glow, _Solid));
                return half4(colour, 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct Attributes { float4 pos : POSITION; float3 norm : NORMAL; };
            float4 ShadowVert(Attributes input) : SV_POSITION
            {
                float3 pos = TransformObjectToWorld(input.pos.xyz);
                float3 normal = TransformObjectToWorldNormal(input.norm);
                float4 clip = TransformWorldToHClip(ApplyShadowBias(pos, normal, _LightDirection));
                return ApplyShadowClamping(clip);
            }
            half4 ShadowFrag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
