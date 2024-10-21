texture uTex;
texture material1;
texture material2;
texture material3;
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
    Texture = material1;
    AddressU = WRAP;
    AddressV = WRAP;
    MagFilter = POINT;
    MinFilter = POINT;
    MipFilter = POINT;
}; // primary material
sampler2D sampler2 = sampler_state
{
    Texture = material2;
    AddressU = WRAP;
    AddressV = WRAP;
    MagFilter = POINT;
    MinFilter = POINT;
    MipFilter = POINT;
};; // secondary material
sampler2D sampler3 = sampler_state
{
    Texture = material3;
    AddressU = WRAP;
    AddressV = WRAP;
    MagFilter = POINT;
    MinFilter = POINT;
    MipFilter = POINT;
}; // tertiary material
float2 uShaderSpecificData : register(c0);

float4 PixelShaderFunction(float2 coords : TEXCOORD) : COLOR0
{
    float4 color = tex2D(sampler0, coords);

    float isRedPrimary = step(0.001, color.r) * step(abs(color.g - color.b), 0.001);
    float isGreenPrimary = step(0.001, color.g) * step(abs(color.r - color.b), 0.001);
    float isBluePrimary = step(0.001, color.b) * step(abs(color.r - color.g), 0.001);

    float2 pixelCoords = coords * (uShaderSpecificData / float2(100, 100));
    float4 mat1 = tex2D(sampler1, pixelCoords) * color.r * 1.2 * isRedPrimary;
    float4 mat2 = tex2D(sampler2, pixelCoords) * color.g * 1.2 * isGreenPrimary;
    float4 mat3 = tex2D(sampler3, pixelCoords) * color.b * isBluePrimary;
	
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
