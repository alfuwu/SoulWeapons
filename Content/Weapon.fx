texture uTex;
texture uImage0;
texture uImage1;
texture uImage2;
sampler2D sampler0 = sampler_state
{
    Texture = uTex;
    AddressU = WRAP;
    AddressV = WRAP;
    MagFilter = POINT;
    MinFilter = POINT;
    MipFilter = POINT;
};

sampler2D sampler1 = sampler_state
{
    Texture = uImage0;
    AddressU = WRAP;
    AddressV = WRAP;
    MagFilter = POINT;
    MinFilter = POINT;
    MipFilter = POINT;
}; // primary material
sampler sampler2 = sampler_state
{
    Texture = uImage1;
    AddressU = WRAP;
    AddressV = WRAP;
    MagFilter = POINT;
    MinFilter = POINT;
    MipFilter = POINT;
};; // secondary material
sampler sampler3 = sampler_state
{
    Texture = uImage2;
    AddressU = WRAP;
    AddressV = WRAP;
    MagFilter = POINT;
    MinFilter = POINT;
    MipFilter = POINT;
};; // tertiary material

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(sampler0, coords);
    float2 snappedUV = floor(coords.xy * 0.5) * 2.0 + 1.0; // snap UV to the center of a 2x2 block

    float isRedPrimary = step(0.001, color.r) * step(abs(color.g - color.b), 0.001);
    float isGreenPrimary = step(0.001, color.g) * step(abs(color.r - color.b), 0.001);
    float isBluePrimary = step(0.001, color.b) * step(abs(color.r - color.g), 0.001);
	
    float4 mat1 = tex2D(sampler1, snappedUV) * color.r * 1.4 * isRedPrimary;
    float4 mat2 = tex2D(sampler2, snappedUV) * color.g * 1.4 * isGreenPrimary;
    float4 mat3 = tex2D(sampler3, snappedUV) * color.b * isBluePrimary;
	
    float4 mat = mat1 + mat2 + mat3;
	
    float allEq = step(0.001, color.r) * step(0.001, color.g) * step(0.001, color.b) * step(abs(color.r - color.g), 0.001) * step(abs(color.g - color.b), 0.001);
    float boostCondition = step(0.001, color.r) * step(0.001, color.g) * step(0.001, color.b) *
	                       (step(abs(color.g - color.b), 0.001) +
	                        step(abs(color.r - color.b), 0.001) +
	                        step(abs(color.r - color.g), 0.001));
	
    mat *= 1.0 + boostCondition * 0.3;
    return lerp(color, float4(mat.rgb, step(0.001, color.a)), step(0.001, (isRedPrimary + isGreenPrimary + isBluePrimary) - allEq));
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
