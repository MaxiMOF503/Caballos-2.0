Shader "Caballos/UI/MaderaCalida"
{
    Properties
    {
        [PerRendererData] _MainTex ("Textura", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct Vertex
            {
                float4 position : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct Fragment
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;

            Fragment vert(Vertex input)
            {
                Fragment output;
                output.position = UnityObjectToClipPos(input.position);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            fixed4 frag(Fragment input) : SV_Target
            {
                fixed4 wood = tex2D(_MainTex, input.uv);
                // Lift dark grain in display space and keep the warm brown hue.
                #ifndef UNITY_COLORSPACE_GAMMA
                    wood.rgb = LinearToGammaSpace(wood.rgb);
                #endif
                wood.rgb = saturate(pow(wood.rgb, float3(.72, .82, .91)) *
                    float3(1.08, 1.03, 1) + float3(.045, .027, .016));
                #ifndef UNITY_COLORSPACE_GAMMA
                    wood.rgb = GammaToLinearSpace(wood.rgb);
                #endif
                return wood * input.color;
            }
            ENDCG
        }
    }
}
