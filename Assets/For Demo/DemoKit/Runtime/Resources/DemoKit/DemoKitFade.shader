Shader "DemoKit/Fade"
{
    // Covers everything seen by a camera inside the object. Cameras outside it (mirrors, previews) are not affected.
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        _Radius ("Radius", Float) = 0.5
    }
    SubShader
    {
        Tags { "Queue"="Overlay+900" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Radius;

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 center = float3(unity_ObjectToWorld._m03, unity_ObjectToWorld._m13, unity_ObjectToWorld._m23);
                float inside = step(distance(_WorldSpaceCameraPos.xyz, center), _Radius);
                // Outside: collapse to a point that is clipped.
                o.pos = inside > 0.5 ? UnityObjectToClipPos(v.vertex) : float4(0, 0, 0, -1);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
    }
}
