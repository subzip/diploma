Shader "Custom/XRayUnlit"
{
    Properties
    {
        _Color("Color", Color) = (0, 1, 1, 1)
        _Emission("Emission", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Transparent+10" }
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            float4 _Color;
            float _Emission;

            Varyings vert (Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                return half4(_Color.rgb * _Emission, _Color.a);
            }
            ENDHLSL
        }
    }
}
