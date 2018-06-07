Shader "CustomRenderTexture/Simple"
{
    Properties
    {
        _Tex ("Base (RGB)", 2D) = "white" {}
     }

     SubShader
     {
        Lighting Off
		ZWrite Off

        Pass
        {
            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

			sampler2D   _Tex;

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                return tex2D(_Tex, IN.localTexcoord.xy);
            }
            ENDCG
		}
    }
}