sampler uTex : register(s0);
float4 uColor : register(c0);
float4 uShaderSpecificData : register(c1);

float4 PixelShaderFunction(float4 position : SV_POSITION, float2 coords : TEXCOORD0) : COLOR0
{
    // Sample the current texture pixel
    float4 originalColor = tex2D(uTex, coords);

    // Offsets to check neighboring pixels for edge detection
    float2 offset = 1.0 / uShaderSpecificData.xy;

    // Edge detection - Compare current pixel with its neighbors
    float edgeThreshold = 0.1; // Control the thickness of the outline
    float4 neighborUp = tex2D(uTex, coords + float2(0, offset.y));
    float4 neighborDown = tex2D(uTex, coords - float2(0, offset.y));
    float4 neighborLeft = tex2D(uTex, coords - float2(offset.x, 0));
    float4 neighborRight = tex2D(uTex, coords + float2(offset.x, 0));

    // Check if the neighboring pixels are different from the current pixel
    float edgeFactor = step(edgeThreshold, distance(originalColor, neighborUp) +
                                              distance(originalColor, neighborDown) +
                                              distance(originalColor, neighborLeft) +
                                              distance(originalColor, neighborRight));

    // Dark outline: if on an edge, make it a darker version of uColor
    float4 darkColor = uColor * 0.5; // Darker shade
    if (edgeFactor > 0.0)
    {
        return darkColor;
    }

    // Inner part: Make it the same as uColor
    float4 mainColor = uColor;

    // Add highlights in the middle based on proximity to the center
    float2 center = float2(0.5, 0.5);
    float distanceFromCenter = distance(coords, center);

    // Control how far from the center the highlights will be added
    float highlightThreshold = 0.3;
    float highlightIntensity = smoothstep(highlightThreshold, 0.0, distanceFromCenter);

    // Apply highlight by adding a brighter version of uColor in the center
    float4 highlightColor = uColor + float4(0.3, 0.3, 0.3, 0.0); // Brighter shade
    float4 finalColor = lerp(mainColor, highlightColor, highlightIntensity);

    // Combine the result
    return finalColor;
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
