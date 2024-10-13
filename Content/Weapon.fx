sampler uTex : register(s0);
sampler uImage1 : register(s1); // primary texture
sampler uImage2 : register(s2); // secondary texture
sampler uImage3 : register(s3); // tertiary texture

float4 PixelShaderFunction(float4 position : SV_POSITION, float2 coords : TEXCOORD0) : COLOR0
{
    float4 rgba = tex2D(uTex, coords).rgba;
    return rgba;
}

technique Technique1
{
    pass ScreenPass
    {
        AlphaBlendEnable = TRUE;
        BlendOp = ADD;
        SrcBlend = SRCALPHA;
        DestBlend = INVSRCALPHA;
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
