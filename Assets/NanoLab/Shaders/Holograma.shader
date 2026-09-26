// Holograma para Quest (Built-in, compatible con single-pass instanced).
// Mezcla los colores originales con un tinte, añade un borde brillante (fresnel)
// y permite cortar el modelo con un plano (vista en corte).
Shader "NanoLab/Holograma"
{
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Tint ("Tinte", Color) = (0.3,0.7,1,1)
        _TintAmount ("Cantidad de tinte", Range(0,1)) = 0
        _RimColor ("Color del borde", Color) = (0.4,0.9,1,1)
        _RimPower ("Potencia del borde", Range(0.5,8)) = 3
        _RimIntensity ("Intensidad del borde", Range(0,4)) = 1.5
        _ClipOn ("Corte activo", Float) = 0
        _ClipPlane ("Plano de corte (mundo)", Vector) = (0,0,1,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color, _Tint, _RimColor;
            float _TintAmount, _RimPower, _RimIntensity, _ClipOn;
            float4 _ClipPlane;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wn : TEXCOORD1; float3 wp : TEXCOORD2; UNITY_VERTEX_OUTPUT_STEREO };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.wn = UnityObjectToWorldNormal(v.normal);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                if (_ClipOn > 0.5) clip(dot(i.wp, _ClipPlane.xyz) + _ClipPlane.w);
                float3 n = normalize(i.wn) * (facing > 0 ? 1 : -1);
                float3 v = normalize(_WorldSpaceCameraPos - i.wp);
                float3 l = normalize(float3(0.3, 0.8, 0.5));
                float diff = saturate(dot(n, l)) * 0.6 + 0.4;
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                float lum = dot(c.rgb, float3(0.3, 0.59, 0.11));
                float3 col = lerp(c.rgb * diff, _Tint.rgb * (0.35 + lum * 1.2) * diff, _TintAmount);
                float rim = pow(1 - saturate(dot(n, v)), _RimPower) * _RimIntensity;
                col += _RimColor.rgb * rim;
                if (facing < 0) col = _RimColor.rgb * 0.55;   // interior visible en el corte
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
