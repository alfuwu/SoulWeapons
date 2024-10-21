texture tex;
sampler2D uTex = sampler_state
{
    Texture = tex;
    AddressU = WRAP;
    AddressV = WRAP;
    MagFilter = POINT;
    MinFilter = POINT;
    MipFilter = POINT;
};
float4 uColor : register(c0);
float2 uShaderSpecificData : register(c1);

float4 PixelShaderFunction(float4 position : SV_POSITION, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uTex, coords);
    float3 light = lerp(uColor.rgb, float3(1, 1, 1), 0.6);
    float3 mid = uColor.rgb;
    float3 dark = uColor.rgb * 0.25;
    
    float brightness = dot(color.rgb, float3(0.299, 0.587, 0.114)) * 2.2;
    float step1 = step(0.0, brightness) - step(0.33, brightness);
    float step2 = step(0.33, brightness) - step(0.66, brightness);
    float step3 = step(0.66, brightness);
    
    float3 outputColor = dark * step1 +
                         mid * step2 +
                         light * step3;
    return float4(outputColor, color.a);
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
