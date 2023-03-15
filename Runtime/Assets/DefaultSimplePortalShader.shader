Shader "Unlit/DefaultSimplePortalShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _AlphaMap ("Alpha Map", 2D) = "white" {}
    }
    SubShader
    {
        // Tags { "RenderType"="Opaque" }
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        // LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag alpha
            // make fog work
            // #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct Meshdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators
            {
                float2 uv : TEXCOORD0;
                float2 maskUv : TEXCOORD1;
                // UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPosition : TEXCOORD2;
            };

            sampler2D _MainTex;
            sampler2D _AlphaMap;
            float4 _MainTex_ST;
            #define PI 3.141592653589793

            Interpolators vert (Meshdata i)
            {
                Interpolators o;
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);

                float3 vertex = UnityObjectToClipPos(i.vertex);
                o.worldPosition = mul(unity_ObjectToWorld, i.vertex).xyz;
                // o.worldPosition = vertex.xyz;


                //copy them so we can change them (demonstration purpos only)
                float4x4 m = UNITY_MATRIX_M;
                float4x4 v = UNITY_MATRIX_V;
                float4x4 p = UNITY_MATRIX_P;

                //break out the axis
                float3 right = normalize(v._m00_m01_m02);
                float3 up = float3(0, 1, 0); // normalize(v._m10_m11_m12);
                float3 forward = normalize(v._m20_m21_m22);
                //get the rotation parts of the matrix
                float4x4 rotationMatrix = float4x4(right, 0,
                    up, 0,
                    forward, 0,
                    0, 0, 0, 1);

                //the inverse of a rotation matrix happens to always be the transpose
                float4x4 rotationMatrixInverse = transpose(rotationMatrix);

                //apply the rotationMatrixInverse, model, view and projection matrix
                float4 pos = i.vertex;
                pos = mul(rotationMatrixInverse, pos);
                pos = mul(m, pos);
                pos = mul(v, pos);
                pos = mul(p, pos);
                o.vertex = pos;

                // o.vertex = UnityObjectToClipPos(i.vertex);


                return o;
            }

            fixed4 frag (Interpolators i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                col.a = tex2D(_AlphaMap, i.uv).a;

                const float RPI2 = 0.15915494;


                // float3 direction = normalize(i.worldPosition - _WorldSpaceCameraPos);
                // float distance = normalize(i.worldPosition - _WorldSpaceCameraPos);
                float3 direction = (_WorldSpaceCameraPos - i.worldPosition);

                float2 sampleUV = float2(
                    atan2(direction.z,  - direction.x) * RPI2,
                    i.uv.y
                );
                // sampleUV *= distance;
                sampleUV = i.uv;

                float2 sampleA = sampleUV + sin(_Time + sampleUV.x * 0.24 + sampleUV.y * 18.0) * 0.004;
                float2 sampleB = sampleUV + sin(_Time + sampleUV.x * 0.24 + sampleUV.y * 18.0) * 0.005;
                float4 panoSampleA = tex2D(_MainTex, sampleA);
                float4 panoSampleB = tex2D(_MainTex, sampleB);
                // float4 alpha = tex2D(_AlphaMap, i.uv).a * lerp(0.7, 0.9, sin(_Time.z + sampleUV.x * 20 + sampleUV.y * 20));

                // float alpha = tex2D(_AlphaMap, i.uv).a - sin(_Time.y) / 2.0 + 0.5;

                // col.rgba = float4(panoSampleB.r, panoSampleA.gb, tex2D(_AlphaMap, i.uv).a);
                col.rgba = float4(panoSampleB.r, panoSampleA.gb, 1);

                // col = tex2D(_MainTex, sampleUV);
                // col = float4(sampleUV.x, sampleUV.x, sampleUV.x, 1);
                // float3 worldPos = mul(unity_ObjectToWorld, i.vertex).xyz;
                // float2 pan = atan2(direction.z, -direction.x);
                // col = tex2D(_MainTex, i.uv);


                // apply fog
                // UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
