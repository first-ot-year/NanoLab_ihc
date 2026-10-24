// Marco de reposo para caminar sin marearse (RNF-01): una esfera de rejilla tenue alrededor de la
// cabeza que se mueve CON el jugador (quieta respecto a su cuerpo, como un habitáculo). Solo se ve en
// la periferia y solo mientras hay desplazamiento artificial; el centro de la vista queda libre.
// Built-in, single-pass instanced (Quest). Se dibuja encima de todo.
Shader "NanoLab/JaulaConfort"
{
    Properties
    {
        _Color ("Rejilla", Color) = (0.55,0.85,1,1)
        _Oscuro ("Fondo periférico", Color) = (0.02,0.04,0.07,1)
        _Intensidad ("Intensidad", Range(0,1)) = 0
        _CosLibre ("Cos. del campo libre (semiángulo)", Float) = 0.82
        _CosLleno ("Cos. donde la rejilla es plena", Float) = 0.55
        _Lineas ("Grados entre líneas", Float) = 12
        _Grosor ("Grosor de línea", Range(0.01,0.2)) = 0.05
        _Oscurecer ("Oscurecimiento periférico", Range(0,1)) = 0.55
    }
    SubShader
    {
        Tags { "Queue"="Overlay+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Front
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            fixed4 _Color, _Oscuro;
            float _Intensidad, _CosLibre, _CosLleno, _Lineas, _Grosor, _Oscurecer;

            struct appdata { float4 vertex : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 pos : SV_POSITION; float3 wp : TEXCOORD0; float3 op : TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wp = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.op = normalize(v.vertex.xyz);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);   // cámara y matriz de vista de este ojo
                if (_Intensidad <= 0.001) discard;
                float3 dir = normalize(i.wp - _WorldSpaceCameraPos);
                float3 frente = normalize(-UNITY_MATRIX_V[2].xyz);          // hacia donde mira este ojo
                float c = dot(dir, frente);
                float periferia = 1 - smoothstep(_CosLleno, _CosLibre, c);  // 0 en el centro, 1 en el borde
                if (periferia <= 0.001) discard;

                // Rejilla de meridianos y paralelos (en el espacio de la esfera, que va con el jugador).
                float lon = degrees(atan2(i.op.z, i.op.x));
                float lat = degrees(asin(clamp(i.op.y, -1, 1)));
                float2 g = abs(frac(float2(lon, lat) / _Lineas + 0.5) - 0.5) * 2;   // 0 en la línea
                float2 aa = fwidth(float2(lon, lat) / _Lineas) * 1.5;
                float linea = 1 - min(smoothstep(_Grosor, _Grosor + aa.x, g.x), smoothstep(_Grosor, _Grosor + aa.y, g.y));

                float a = _Intensidad * periferia;
                fixed3 col = lerp(_Oscuro.rgb, _Color.rgb, linea);
                return fixed4(col, a * lerp(_Oscurecer, 0.9, linea));
            }
            ENDCG
        }
    }
}
