Shader "Custom/SelectionRing"
{
    Properties
    {
        _Color ("Color", Color) = (0, 1, 0, 1)
        _InnerRadius ("Inner Radius", Range(0, 1)) = 0.7
        _OuterRadius ("Outer Radius", Range(0, 1)) = 0.85
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
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

            fixed4 _Color;
            float _InnerRadius;
            float _OuterRadius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center) * 2; // normaliza 0-1
                
                // Só desenha entre Inner e Outer radius
                float ring = step(_InnerRadius, dist) * step(dist, _OuterRadius);
                
                return fixed4(_Color.rgb, _Color.a * ring);
            }
            ENDCG
        }
    }
}