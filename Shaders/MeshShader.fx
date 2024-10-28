
struct VS_IN
{
    float3 position : POSITION;
    float3 normal : NORMAL;
};

struct PS_IN
{
    float4 position : SV_POSITION;
    float4 worldPosition : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float3 normalWorldDirection : TEXCOORD2;
    float3 color : COLOR;
};

struct PointLight
{
    float3 worldPosition;
    float4x4 worldToLocal;
    float4x4 projection;
    float range;
    float diffusion;
    float intensity;
};

struct DirectionalLight
{
    float3 direction;
    float4x4 projection;
    float farClipPlane;
};

struct Spotlight
{
    float3 position;
    float3 direction;
    float angle;
    float range;
    float4x4 viewProjection;
};

cbuffer ConstantBuffer : register(b0)
{
    float4x4 ModelViewProjection;
    float4x4 ModelLocalToWorldDirection;
    float4x4 ModelLocalToWorld;
    float4x4 CameraWorldToLocal;
    float4x4 CameraLocalToScreen;   
    float4x4 CameraScreenToWorld;
    float4 CameraPosition;
    float GlobalIllumination;
};

StructuredBuffer<PointLight> PointLights : register(t0);
StructuredBuffer<DirectionalLight> DirectionalLights : register(t1);
StructuredBuffer<Spotlight> Spotlights : register(t2);
Texture2DArray<float> DirectionalLigtsShadowMap : register(t3);
Texture2DArray<float> SpotlightsShadowMap : register(t4);
StructuredBuffer<float2> PixelOffsets : register(t5);
SamplerState Sampler;
static const float pi = 3.14159265f;

uint GetPointLightsCount()
{
    uint count;
    uint stride;
    
    PointLights.GetDimensions(count, stride);
    
    return count;
}

uint GetDirectionalLightsCount()
{
    uint count;
    uint stride;
    
    DirectionalLights.GetDimensions(count, stride);
    
    return count;
}

uint GetSpotightsCount()
{
    uint count;
    uint stride;
    
    Spotlights.GetDimensions(count, stride);
    
    return count;
}

float CosAngle(float3 vector1, float3 vector2)
{
    float length1 = length(vector1);
    float length2 = length(vector2);
    float lengthProduct = length1 * length2;
    
    if (lengthProduct == 0)
        return 1;
    
    float cosAngle = dot(vector1, vector2) / lengthProduct;
    
    return cosAngle;
}

bool IsPixelPositionValid(float2 pixelPosition)
{
    return pixelPosition.x >= 0 && pixelPosition.x <= 1 && pixelPosition.y >= 0 && pixelPosition.y <= 1;
}

float CalculateShadowPercent(Texture2DArray<float> shadowMap, uint index, float2 pixelPosition, float pixelDepth, float2 squareSize, uint2 squarePixelsSize)
{
    if (!IsPixelPositionValid(pixelPosition))
        return 0;
    
    uint width;
    uint height;
    uint elements;
    
    shadowMap.GetDimensions(width, height, elements);
    
    float pixelDistanceX = (1 / (float)width) * squareSize.x;
    float pixelDistanceY = (1 / (float)height) * squareSize.y;
    
    float halfWidth = (squarePixelsSize.x * pixelDistanceX) / 2;
    float halfHeight = (squarePixelsSize.y * pixelDistanceY) / 2;
    
    float2 offset = float2(-halfWidth, halfHeight);
    float2 currentPixelPosition = pixelPosition + offset;
    float startX = currentPixelPosition.x;
    float inShadowPixelsCount = 0;
    float samplesCount = 0;
    float lightDepthSum = 0;
    
    for (uint row = 0; row < squarePixelsSize.y; row++)
    {
        for (uint column = 0; column < squarePixelsSize.x; column++)
        {
            if (!IsPixelPositionValid(currentPixelPosition))
            {
                currentPixelPosition.x += pixelDistanceX;
                continue;
            }
            
            float lightDepth = shadowMap.Sample(Sampler, float3(currentPixelPosition, index));
            lightDepthSum += lightDepth;
            
            if (pixelDepth > lightDepth && lightDepth != 1)
                inShadowPixelsCount++;
            
            currentPixelPosition.x += pixelDistanceX;
            samplesCount++;
        }
        
        currentPixelPosition.x = startX;
        currentPixelPosition.y -= pixelDistanceY;
    }
    
    lightDepthSum /= samplesCount;
    
    if (pixelDepth > lightDepthSum)
        return inShadowPixelsCount / samplesCount;
    return 0;
}

float GetPixelBrightnessForPointLights(PS_IN input)
{
    uint lightsCount = GetPointLightsCount();
    float brightness = 0;
    
    float3 pixelWorldPosition = input.worldPosition;
    
    for (uint i = 0; i < lightsCount; i++)
    {
        PointLight light = PointLights[i];
        
        float distanceToLight = distance(light.worldPosition, pixelWorldPosition);
        float range = light.range;
        float diffusion = light.diffusion;
        float intensity = light.intensity;
        
        if (distanceToLight > range)
            continue;
        
        float3 lightLocal = light.worldPosition - pixelWorldPosition;
        float currentBrightness = CosAngle(lightLocal, input.normalWorldDirection);
        currentBrightness = (currentBrightness + diffusion) / (1 + diffusion);
        
        currentBrightness *= intensity;
        
        float rangeSquared = range * range;
        float distanceSquared = distanceToLight * distanceToLight;
        
        float distanceCoefficient = (rangeSquared - distanceSquared) / rangeSquared;
        
        currentBrightness *= distanceCoefficient;
        
        brightness = max(brightness, currentBrightness);
    }
    
    return brightness;
}

float GetSingleDirectionalLightBrightness(DirectionalLight light, PS_IN input, uint index)
{
    float4 pixelLightPosition = mul(light.projection, input.worldPosition);
    float3 pixelLightUV = pixelLightPosition.xyz / pixelLightPosition.w;
    
    pixelLightUV.x = (pixelLightUV.x + 1) / 2;
    pixelLightUV.y = (-pixelLightUV.y + 1) / 2;
    
    float pixelDepth = pixelLightUV.z;
    
    float brightness = -CosAngle(light.direction, input.normalWorldDirection);
    
    if (brightness <= 0)
        return 0;
    
    float shadowPercent = CalculateShadowPercent(DirectionalLigtsShadowMap, index, pixelLightUV.xy, pixelDepth, float2(1.5, 1.5), uint2(3, 3));
    
    return brightness * (1 - shadowPercent);
}

float GetDirectionalLightsBrightness(PS_IN input)
{
    uint lightsCount = GetDirectionalLightsCount();
    float brightness = 0;
    
    for (uint i = 0; i < lightsCount; i++)
    {
        DirectionalLight light = DirectionalLights[i];
        float currentBrightness = GetSingleDirectionalLightBrightness(light, input, i);
        brightness = max(brightness, currentBrightness);
    }

    return brightness;
}

float GetSingleSpotlightsBrightness(Spotlight light, PS_IN input, uint index)
{
    float3 lightLocal = input.worldPosition.xyz - light.position;
    float lightAngle = acos(CosAngle(lightLocal, light.direction));
    
    if (lightAngle > light.angle)
        return 0;
    
    float distanceToLight = distance(light.position, input.worldPosition.xyz);
    float maxDistanceToLight = light.range / cos(lightAngle);
    
    if (distanceToLight > maxDistanceToLight)
        return 0;
    
    float4 pixelLightPosition = mul(light.viewProjection, input.worldPosition);
    float3 pixelLightUV = float3(pixelLightPosition.xy / pixelLightPosition.w, index);
    
    pixelLightUV.x = (pixelLightUV.x + 1) / 2;
    pixelLightUV.y = (pixelLightUV.y + 1) / 2;
    
    float3 samplerLocation = float3(pixelLightUV.xy, index);
    float lightDepth = SpotlightsShadowMap.Sample(Sampler, pixelLightUV);
    
    float w = pixelLightPosition.w;
    float pixelDepth = w / light.range;
    
    float brightness = -CosAngle(lightLocal, input.normalWorldDirection);
    
    if (pixelDepth > lightDepth)
        return brightness * GlobalIllumination;
    
    brightness *= (90 - lightAngle) / 90;
    brightness *= (maxDistanceToLight - distanceToLight) / maxDistanceToLight;
    
    return brightness;
}

float GetSpotlightsBrightness(PS_IN input)
{
    uint lightsCount = GetSpotightsCount();
    float brightness = 0;
    
    for (uint i = 0; i < lightsCount; i++)
    {
        Spotlight light = Spotlights[i];
        brightness = max(brightness, GetSingleSpotlightsBrightness(light, input, i));
    }
    
    return brightness;
}

float GetPixelBrightness(PS_IN input)
{
    float brightness = GetPixelBrightnessForPointLights(input);
    brightness = max(brightness, GetDirectionalLightsBrightness(input));
    brightness = max(brightness, GetSpotlightsBrightness(input));
    
    return brightness;
}

PS_IN VS(VS_IN input)
{
    PS_IN output;
    
    float4 inputPosition = float4(input.position.xyz, 1);
    float4 inputWorldPosition = mul(ModelLocalToWorld, inputPosition);
    
    output.worldPosition = inputWorldPosition;
    output.position = mul(ModelViewProjection, inputPosition);
    output.normal = input.normal;
    output.normalWorldDirection = mul((float3x3)ModelLocalToWorldDirection, input.normal);
    output.color = float3(1, 1, 1);
    
    return output;
}

float3 PS(PS_IN input) : SV_Target
{
    float brightness = GetPixelBrightness(input);
    return input.color * brightness;
}