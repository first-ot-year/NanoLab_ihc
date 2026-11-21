// Piel de Baymax (manos y cuerpo del avatar). Revisión del playtest: "haz que esas manos mejoren". Con luz horneada
// las mallas que se mueven no reciben sombreado y el blanco quedaba plano. Este shader de una pasada (barato en Quest)
// da volumen sin depender de luces en tiempo real: luz de cielo/suelo (hemisférica) + una luz clave suave, la luz
// ambiente de la escena (sondas de luz si están horneadas), un brillo especular de vinilo y un borde azulado (fresnel).
Shader "NanoLab/PielBaymax"
{
    Properties
    {
        _Color ("Color", Color) = (0.93, 0.92, 0.89, 1)
        _Sombra ("Color en sombra", Color) = (0.56, 0.60, 0.68, 1)
        _Borde ("Color del borde", Color) = (0.70, 0.88, 1, 1)
        _PotenciaBorde ("Potencia del borde", Range(0.5, 8)) = 3
        _IntensidadBorde ("Intensidad del borde", Range(0, 1)) = 0.35
        _Brillo ("Brillo especular", Range(0, 1)) = 0.35
        _Dureza ("Dureza del brillo", Range(4, 128)) = 36
        _Ambiente ("Peso de la luz de la escena", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 150
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _Color, _Sombra, _Borde;
            half _PotenciaBorde, _IntensidadBorde, _Brillo, _Dureza, _Ambiente;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 n : TEXCOORD0;
                float3 v : TEXCOORD1;
                half3 sh : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata i)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(i.vertex);
                o.n = UnityObjectToWorldNormal(i.normal);
                o.v = WorldSpaceViewDir(i.vertex);
                o.sh = ShadeSH9(half4(o.n, 1));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float3 v = normalize(i.v);
                float3 l = normalize(float3(0.35, 0.85, 0.25));
                half cielo = saturate(n.y * 0.5 + 0.5);
                half difusa = saturate(dot(n, l)) * 0.55 + 0.45;
                half3 col = lerp(_Sombra.rgb, _Color.rgb, saturate(cielo * 0.55 + difusa * 0.45));
                col = lerp(col, col * saturate(i.sh * 1.6), _Ambiente);
                float3 h = normalize(l + v);
                col += pow(saturate(dot(n, h)), _Dureza) * _Brillo;
                col += pow(1 - saturate(dot(n, v)), _PotenciaBorde) * _Borde.rgb * _IntensidadBorde;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
    FallBack "Mobile/Diffuse"
}
