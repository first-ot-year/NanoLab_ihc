// Luz aditiva sin iluminación (resplandores, plataforma y rejilla holográficas).
// Built-in, compatible con single-pass instanced (Quest). El alfa de la textura es la intensidad.
Shader "NanoLab/Aditivo"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Color", Color) = (0.3,0.7,1,1)
        _Intensidad ("Intensidad", Range(0,4)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color; float _Intensidad;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed a = tex2D(_MainTex, i.uv).a;
                return fixed4(_Color.rgb * a * _Intensidad, 1);
            }
            ENDCG
        }
    }
}
