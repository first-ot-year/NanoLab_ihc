// Holograma para Quest (Built-in, compatible con single-pass instanced).
// Mezcla los colores originales con un tinte, añade un borde brillante (fresnel)
// y permite cortar el modelo con un plano (vista en corte).
// Color por capas (_Radial = 1): según la distancia al centro del virus (en espacio de objeto, así
// el color va pegado a la superficie al girar) pinta cuerpo, espículas y motas de proteínas de
// membrana; el interior (visible en el corte) va en otro color. Luz de estudio con brillo especular.
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

        _Radial ("Color por capas", Float) = 0
        _CentroObj ("Centro (objeto)", Vector) = (0,0,0,0)
        _RadioObj ("Radio máximo (objeto)", Float) = 1
        _RCuerpo ("Radio del cuerpo (0-1)", Float) = 0.75
        _ColCuerpo ("Cuerpo", Color) = (0.22,0.42,0.9,1)
        _ColEspicula ("Espículas", Color) = (0.95,0.3,0.4,1)
        _ColProt1 ("Proteína 1", Color) = (0.62,0.32,0.95,1)
        _ColProt2 ("Proteína 2", Color) = (1,0.62,0.22,1)
        _ColInterior ("Interior", Color) = (0.75,0.3,0.9,1)
        _Especular ("Brillo especular", Range(0,1)) = 0.35
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
            float _Radial, _RadioObj, _RCuerpo, _Especular;
            float4 _CentroObj;
            fixed4 _ColCuerpo, _ColEspicula, _ColProt1, _ColProt2, _ColInterior;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float3 wn : TEXCOORD1; float3 wp : TEXCOORD2; float3 op : TEXCOORD3; UNITY_VERTEX_OUTPUT_STEREO };

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
                o.op = (v.vertex.xyz - _CentroObj.xyz) / max(_RadioObj, 1e-5);
                return o;
            }

            // Ruido de valor 3D (motas de proteínas en la membrana).
            float hash(float3 p) { p = frac(p * 0.3183099 + 0.1); p *= 17.0; return frac(p.x * p.y * p.z * (p.x + p.y + p.z)); }
            float ruido(float3 x)
            {
                float3 i = floor(x), f = frac(x); f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(hash(i), hash(i + float3(1,0,0)), f.x), lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x), f.y),
                            lerp(lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x), lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x), f.y), f.z);
            }

            fixed4 frag (v2f i, fixed facing : VFACE) : SV_Target
            {
                if (_ClipOn > 0.5) clip(dot(i.wp, _ClipPlane.xyz) + _ClipPlane.w);
                float3 n = normalize(i.wn) * (facing > 0 ? 1 : -1);
                float3 v = normalize(_WorldSpaceCameraPos - i.wp);
                float3 l = normalize(float3(0.3, 0.8, 0.5));
                float3 l2 = normalize(float3(-0.6, -0.2, 0.4));               // luz de relleno
                float diff = saturate(dot(n, l)) * 0.65 + saturate(dot(n, l2)) * 0.2 + 0.25;
                fixed4 c = tex2D(_MainTex, i.uv) * _Color;
                float occl = 1;

                if (_Radial > 0.5)
                {
                    float d = length(i.op);
                    float t = smoothstep(_RCuerpo * 1.02, _RCuerpo * 1.14, d);   // 0 cuerpo, 1 espícula
                    float3 col = lerp(_ColCuerpo.rgb, _ColEspicula.rgb, t);
                    float m = ruido(i.op * 11.0) * 0.7 + ruido(i.op * 23.0) * 0.3;
                    float sobreCuerpo = 1 - t;
                    col = lerp(col, _ColProt1.rgb, smoothstep(0.66, 0.72, m) * sobreCuerpo);
                    col = lerp(col, _ColProt2.rgb, (1 - smoothstep(0.18, 0.24, m)) * sobreCuerpo * 0.85);
                    col *= 0.85 + 0.3 * ruido(i.op * 40.0);                   // textura granulada
                    if (d < _RCuerpo * 0.9) col = _ColInterior.rgb;          // núcleo y genoma
                    c.rgb = col;
                    occl = lerp(0.6, 1.0, saturate((d - _RCuerpo * 0.95) / (_RCuerpo * 0.35)));   // valles entre espículas
                }

                float lum = dot(c.rgb, float3(0.3, 0.59, 0.11));
                float3 col = lerp(c.rgb * diff * occl, _Tint.rgb * (0.35 + lum * 1.2) * diff * occl, _TintAmount);
                float spec = pow(saturate(dot(n, normalize(l + v))), 28) * _Especular;
                col += spec;
                float rim = pow(1 - saturate(dot(n, v)), _RimPower) * _RimIntensity;
                col += _RimColor.rgb * rim;
                if (facing < 0) col = (_Radial > 0.5 ? _ColInterior.rgb * 0.8 : _RimColor.rgb * 0.55);   // interior visible en el corte
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
