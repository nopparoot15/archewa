Shader "MyArcade/WorldAsphalt"
{
    Properties {
        _MainTex ("Asphalt", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _EmissionColor ("Ambient floor", Color) = (.1,.1,.1,1)
        _EmissionMap ("Unused", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = .05
        _Metallic ("Metallic", Range(0,1)) = 0
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half4 _EmissionColor;
        half _Glossiness;
        struct Input { float3 worldPos; };
        void surf(Input IN, inout SurfaceOutputStandard o) {
            fixed3 c = tex2D(_MainTex,IN.worldPos.xz*.06363636).rgb;
            o.Albedo = c * _Color.rgb;
            o.Emission = c * _EmissionColor.rgb;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
