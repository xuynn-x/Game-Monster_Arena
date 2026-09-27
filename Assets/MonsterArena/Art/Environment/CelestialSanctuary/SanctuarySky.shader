Shader "MonsterArena/Sanctuary Sky"
{
    Properties
    {
        _MainTex ("Panorama", 2D) = "white" {}
        _Exposure ("Exposure", Range(0,2)) = 1
        _Rotation ("Rotation", Range(0,360)) = 90
        _HorizonOffset ("Scenery horizon offset", Range(-0.4,0.4)) = 0.13
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float _Exposure, _Rotation, _HorizonOffset;
            struct Input { float4 vertex : POSITION; };
            struct Output { float4 position : SV_POSITION; float3 direction : TEXCOORD0; };
            Output vert(Input input)
            {
                Output output;
                output.position = UnityObjectToClipPos(input.vertex);
                output.direction = input.vertex.xyz;
                return output;
            }
            half4 frag(Output input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                float u = atan2(direction.x, direction.z) / (2 * UNITY_PI) + 0.5;
                float v = asin(clamp(direction.y,-1,1)) / UNITY_PI + 0.5;
                // Repeat the distant vista around both teams; bring its horizon into
                // the existing downward battle camera without moving that camera.
                float2 uv = float2(frac(u * 2 + _Rotation / 360), saturate((v - 0.5) * 2 + 0.5 + _HorizonOffset));
                return half4(tex2D(_MainTex, uv).rgb * _Exposure, 1);
            }
            ENDHLSL
        }
    }
}
