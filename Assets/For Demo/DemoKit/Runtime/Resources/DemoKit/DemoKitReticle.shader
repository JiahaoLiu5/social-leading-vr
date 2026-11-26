Shader "DemoKit/Reticle"
{
    // A dot that opens into a ring, with a white edge so that it can be seen on any background.
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Ring ("Ring", Range(0,1)) = 0
        _Press ("Press", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+60" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Ring;
            float _Press;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 p : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.p = v.uv * 2.0 - 1.0;
                return o;
            }

            float cover (float d, float w)
            {
                return saturate(0.5 - d / w);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float r = length(i.p);
                float w = max(fwidth(r), 0.002);
                float outer = lerp(0.46, 0.80, _Ring) * lerp(1.0, 0.82, _Press);
                float inner = lerp(0.0, outer - 0.26, _Ring) * (1.0 - _Press * 0.55);
                float colored = cover(r - outer, w) * (1.0 - cover(r - inner, w) * step(0.01, inner));
                float edge = cover(r - outer - 0.14, w) * (1.0 - cover(r - inner + 0.10, w) * step(0.12, inner));
                float a = max(colored, edge * 0.85);
                fixed3 c = lerp(fixed3(1, 1, 1), _Color.rgb, colored);
                return fixed4(c, a * _Color.a);
            }
            ENDCG
        }
    }
}
