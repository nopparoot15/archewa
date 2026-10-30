Shader "MyArcade/LitPixelSprite"
{
    Properties {
        [PerRendererData] _MainTex ("Sprite",2D) = "white" {}
        _Color ("Tint",Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("Renderer",Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip",Vector) = (1,1,1,1)
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        CGPROGRAM
        #pragma surface surf Lambert alpha:fade vertex:vert
        #pragma target 3.0
        sampler2D _MainTex;
        fixed4 _Color, _RendererColor;
        float4 _Flip;
        struct Input { float2 uv_MainTex; fixed4 color : COLOR; };
        void vert(inout appdata_full v) { v.vertex.xy *= _Flip.xy; }
        void surf(Input IN,inout SurfaceOutput o) {
            fixed4 c = tex2D(_MainTex,IN.uv_MainTex) * IN.color * _Color * _RendererColor;
            clip(c.a-.015);
            o.Albedo = c.rgb;
            o.Emission = c.rgb * .5;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Sprites/Default"
}
