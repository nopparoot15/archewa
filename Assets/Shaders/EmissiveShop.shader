Shader "MyArcade/EmissiveShop"
{
    Properties {
        _MainTex ("Artwork", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _EmissionColor ("Source strength", Color) = (1,1,1,1)
        _EmissionMap ("Source artwork", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = .05
        _Metallic ("Metallic", Range(0,1)) = 0
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color;
        half4 _EmissionColor;
        half _Glossiness, _Metallic;
        struct Input { float2 uv_MainTex; };
        void surf(Input IN, inout SurfaceOutputStandard o) {
            fixed3 c = tex2D(_MainTex, IN.uv_MainTex).rgb;
            half warm = smoothstep(.06,.24,c.r-c.b) * smoothstep(.035,.15,c.g-c.b) * smoothstep(1.15,1.45,c.r/max(.01,c.g));
            half cool = smoothstep(.03,.14,min(c.g,c.b)-c.r);
            half bright = smoothstep(.22,.54,dot(c,float3(.299,.587,.114)));
            half source = saturate(max(warm,cool)*bright);
            o.Albedo = c * _Color.rgb;
            o.Emission = c * _EmissionColor.rgb * source * 1.3;
            o.Smoothness = _Glossiness;
            o.Metallic = _Metallic;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
