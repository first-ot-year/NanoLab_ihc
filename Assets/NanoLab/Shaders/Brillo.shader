// Brillo aditivo con borde (fresnel) que pulsa. Se usa para resaltar objetos
// agarrables y para la silueta fantasma de la platina.
Shader "NanoLab/Brillo"
{
    Properties
    {
        _Color ("Color", Color) = (0.3,0.9,1,1)
        _Grosor ("Grosor (m)", Float) = 0.002
        _Pulso ("Velocidad del pulso", Float) = 5
        _Base ("Relleno", Range(0,1)) = 0.25
    }
    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _Color; float _Grosor, _Pulso, _Base;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float3 wn : TEXCOORD0; float3 wp : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float3 wn = UnityObjectToWorldNormal(v.normal);
                float3 wp = mul(unity_ObjectToWorld, v.vertex).xyz + wn * _Grosor;
                o.pos = UnityWorldToClipPos(wp);
                o.wn = wn; o.wp = wp;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 v = normalize(_WorldSpaceCameraPos - i.wp);
                float rim = pow(1 - saturate(dot(normalize(i.wn), v)), 2);
                float pulso = 0.75 + 0.25 * sin(_Time.y * _Pulso);
                return _Color * (_Base + rim * 1.2) * pulso;
            }
            ENDCG
        }
    }
}
