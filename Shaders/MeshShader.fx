
struct VS_IN
{
    float3 position : POSITION;
    float3 normal : NORMAL;
    float2 uvs : UV;
};

struct PS_IN
{
    float4 position : SV_POSITION;
    float4 worldPosition : TEXCOORD0;
    float3 normal : TEXCOORD1;
    float3 normalWorldDirection : TEXCOORD2;
    float2 uvs : TEXCOORD3;
    float3 color : COLOR;
};

struct BaseLightInput
{
    float4 color;
    int castSwadows;
};

struct FaceMatrix
{
    float4x4 viewProjection;
    float3 direction;
};

struct PointLight : BaseLightInput
{
    float3 worldPosition;
    float range;
    float diffusion;
    float intensity;
    int startTextureIndex;
    FaceMatrix faceMatrices[6];
};

struct DirectionalLight : BaseLightInput
{
    float3 direction;
    float farClipPlane;
    int startTextureIndex;
    int texturesCount;
    float maxShadowCastDistance;
    float frustumLength;
    float4x4 viewProjectionMatrices[10];
};

struct Spotlight : BaseLightInput
{
    float3 position;
    float3 direction;
    float angle;
    float range;
    float4x4 viewProjection;
};

cbuffer Material : register(b1)
{
    float4 Color;
    float Metallic;
}

cbuffer ConstantBuffer : register(b0)
{
    float4x4 ModelViewProjection;
    float4x4 ModelLocalToWorldDirection;
    float4x4 ModelLocalToWorld;
    float3 CameraPosition;
    float3 CameraForward;
    float GlobalIllumination;
    int IsHaveTexture;
};

StructuredBuffer<PointLight> PointLights : register(t0);
StructuredBuffer<DirectionalLight> DirectionalLights : register(t1);
StructuredBuffer<Spotlight> Spotlights : register(t2);
Texture2DArray<float> PointLigtsShadowMap : register(t3);
Texture2DArray<float> DirectionalLigtsShadowMap : register(t4);
Texture2DArray<float> SpotlightsShadowMap : register(t5);
Texture2D Texture : register(t6);
SamplerState Sampler : register(s0);
SamplerState TextureSampler : register(s1);
static const float cos45Degrees = 0.70710678118;
static const float radians45Degrees = 0.785398;
static const float cosMaxFaceDegrees = 0.3; //0.5773504162;

bool IntToBool(int value)
{
    return value == 1;
}

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
    
    return dot(vector1, vector2) / lengthProduct;
}

float3 Reflect(float3 vec, float3 normal)
{
    float3 normalizedNormal = normalize(normal);
    float dotProduct = dot(vec, normalizedNormal);
    float3 projection = normalizedNormal * (dotProduct * 2);
    return vec - projection;
}

bool IsPixelPositionValid(float2 pixelPosition)
{
    float2 absPixelPosition = abs(pixelPosition);
    return absPixelPosition.x <= 1 && absPixelPosition.y <= 1;
}

float3 CalculateUV(float4x4 viewProjection, float4 worldPosition, bool transformZ = false, float range = 0)
{
    float4 prjectionPosition = mul(viewProjection, worldPosition);
    float w = prjectionPosition.w;
    float2 uv = prjectionPosition.xy / w;
    uv.x = (uv.x + 1) / 2;
    uv.y = (-uv.y + 1) / 2;
    
    float z = 0;

    if (transformZ)
        z = w / range;
    else
        z = prjectionPosition.z / w;
    
    return float3(uv, z);
}

float CalculateShadowPercent(Texture2DArray<float> shadowMap, uint index, float2 pixelPosition, float pixelDepth, float2 squareSize, uint2 squarePixelsSize, 
    bool skipValidation = false)
{
    if (!skipValidation && !IsPixelPositionValid(pixelPosition))
        return 0;
    
    uint width;
    uint height;
    uint elements;
    
    shadowMap.GetDimensions(width, height, elements);
    
    if (index >= elements)
        return 0;
    
    float pixelDistanceX = squareSize.x / (float)squarePixelsSize.x;
    float pixelDistanceY = squareSize.y / (float)squarePixelsSize.y;
    
    float halfWidth = squareSize.x / 2;
    float halfHeight = squareSize.y / 2;
    
    float2 currentPixelPosition = pixelPosition - float2(halfWidth, halfHeight);
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
            
            float lightDepth = shadowMap.SampleLevel(Sampler, float3(currentPixelPosition, index), 0);
            lightDepthSum += lightDepth;
            
            if (pixelDepth > lightDepth && lightDepth != 1)
                inShadowPixelsCount++;
            
            currentPixelPosition.x += pixelDistanceX;
            samplesCount++;
        }
        
        currentPixelPosition.x = startX;
        currentPixelPosition.y += pixelDistanceY;
    }
    
    lightDepthSum /= samplesCount;
    return inShadowPixelsCount / samplesCount;
}

float CalculateLightBrightness(PS_IN input, float3 direction)
{
    float3 normal = input.normalWorldDirection;
    float diffuseBrightness = saturate(-CosAngle(direction, normal));
    
    float3 reflectedDirection = Reflect(direction, normal);
    float3 cameraLocal = CameraPosition - input.worldPosition.xyz;
    float specularBrightness = saturate(CosAngle(reflectedDirection, cameraLocal));
    
    float specular = Metallic;
    float diffuse = 1 - specular;
    
    return (diffuseBrightness * diffuse) + (specularBrightness * specular);
}

float3 MixColors(float3 color1, float3 color2)
{
    return color1 * color2;
}

float3 CalulateAverageColor(float3 color1, float3 color2)
{
    return (color1 + color2) / 2;
}

float3 AddColor(float3 referenceColor, float3 colorToAdd, float brightness)
{
    return lerp(referenceColor, colorToAdd, brightness);
}

float SamplePointLight(PointLight light, float4 pixelWorldPosition)
{
    float3 pixelLocal = pixelWorldPosition.xyz - light.worldPosition;
    
    [unroll(6)]
    for (int i = 0; i < 6; i++)
    {
        FaceMatrix faceMatrix = light.faceMatrices[i];
        
        float3 UV =  CalculateUV(faceMatrix.viewProjection, pixelWorldPosition, true, light.range);    
        
        uint index = light.startTextureIndex + i;
        float shadowPercent = CalculateShadowPercent(PointLigtsShadowMap, index, UV.xy, UV.z, float2(0.001, 0.001), uint2(2, 2));
        
        if (shadowPercent > 0)
            return shadowPercent;
    }
    
    return 0;
}

float3 GetPixelColorForPointLights(PS_IN input)
{
    uint lightsCount = GetPointLightsCount();
    float3 color = float3(0, 0, 0);
    
    float4 pixelWorldPosition = input.worldPosition;
    
    for (uint i = 0; i < lightsCount; i++)
    {
        PointLight light = PointLights[i];
        
        float3 lightLocal = light.worldPosition - pixelWorldPosition.xyz;
        float distanceToLight = length(lightLocal);
        float range = light.range;
        
        if (distanceToLight > range)
            continue;
        
        float diffusion = light.diffusion;
        float intensity = light.intensity;
        
        float3 lightDirection = pixelWorldPosition.xyz - light.worldPosition;
        float currentBrightness = CalculateLightBrightness(input, lightDirection);
        currentBrightness = (currentBrightness + diffusion) / (1 + diffusion);
        
        currentBrightness *= intensity;
        
        float rangeSquared = range * range;
        float distanceSquared = distanceToLight * distanceToLight;
        
        float distanceCoefficient = (rangeSquared - distanceSquared) / rangeSquared;
        
        currentBrightness *= distanceCoefficient;
        
        if (!light.castSwadows)
        {
            color = AddColor(color, light.color.xyz, currentBrightness);
            continue;
        }
        
        float lightPercent = 1 - SamplePointLight(light, pixelWorldPosition);
        
        currentBrightness *= lightPercent;
        color = AddColor(color, light.color.xyz, currentBrightness);
    }
    
    return color;
}

float SampleDirectionalLight(DirectionalLight light, float4 pixelWorldPosition)
{
    [loop]
    for (int i = 0; i < light.texturesCount; i++)
    {
        float3 UV = CalculateUV(light.viewProjectionMatrices[i], pixelWorldPosition);
        
        uint index = light.startTextureIndex + i;
        
        float shadowPercent = CalculateShadowPercent(DirectionalLigtsShadowMap, index, UV.xy, UV.z, float2(0.001, 0.001), uint2(3, 3));
        
        if (shadowPercent > 0)
            return shadowPercent;
    }
    
    return 0;
}

float GetSingleDirectionalLightBrightness(DirectionalLight light, PS_IN input, uint index)
{
    float brightness = CalculateLightBrightness(input, light.direction);
    
    if (brightness <= 0)
        return 0;
    
    if (!IntToBool(light.castSwadows))
        return brightness;
    
    float shadowPercent = SampleDirectionalLight(light, input.worldPosition);
    float lightPercent = 1 - shadowPercent;
    
    return brightness * lightPercent;
}

float3 GetDirectionalLightsColor(PS_IN input)
{
    uint lightsCount = GetDirectionalLightsCount();
    float3 color = float3(0, 0, 0);
    
    for (uint i = 0; i < lightsCount; i++)
    {
        DirectionalLight light = DirectionalLights[i];
        float currentBrightness = GetSingleDirectionalLightBrightness(light, input, i);
        
        color = AddColor(color, light.color.xyz, currentBrightness);
    }

    return color;
}

float GetSingleSpotlightsBrightness(Spotlight light, PS_IN input, uint index)
{
    float3 lightLocal = input.worldPosition.xyz - light.position;
    float cosLightAngle = CosAngle(lightLocal, light.direction);
    float lightAngle = acos(cosLightAngle);
    
    if (lightAngle > light.angle)
        return 0;
    
    float distanceToLight = distance(light.position, input.worldPosition.xyz);
    float maxDistanceToLight = light.range / cosLightAngle;
    
    if (distanceToLight > maxDistanceToLight)
        return 0;
    
    float brightness = -CosAngle(lightLocal, input.normalWorldDirection);
    
    if (brightness < 0 || !IntToBool(light.castSwadows))
        return 0;
    
    float3 UV = CalculateUV(light.viewProjection, input.worldPosition, true, light.range);
    float shadowPercent = CalculateShadowPercent(SpotlightsShadowMap, index, UV.xy, UV.z, float2(0.001, 0.001), uint2(2, 2));
    float lightPercent = 1 - shadowPercent;
    
    brightness *= (90 - lightAngle) / 90;
    brightness *= (maxDistanceToLight - distanceToLight) / maxDistanceToLight;
    
    return brightness * lightPercent;
}

float3 GetSpotlightsColor(PS_IN input)
{
    uint lightsCount = GetSpotightsCount();
    float3 color = float3(0, 0, 0);
    
    for (uint i = 0; i < lightsCount; i++)
    {
        Spotlight light = Spotlights[i];
        color = AddColor(color, light.color.xyz, GetSingleSpotlightsBrightness(light, input, i));
    }
    
    return color;
}

float3 GetPixelColor(PS_IN input)
{
    uint pointLightsCount = GetPointLightsCount();
    uint directionalLightsCount = GetDirectionalLightsCount();
    uint spotlightsCount = GetSpotightsCount();
    uint lightsCount = pointLightsCount + directionalLightsCount + spotlightsCount;
    
    float3 color = float3(0, 0, 0);
    
    if (lightsCount == 0)
        return color;
    
    if (pointLightsCount != 0)
        color += GetPixelColorForPointLights(input);
    if (directionalLightsCount != 0)
        color += GetDirectionalLightsColor(input);
    if (spotlightsCount != 0)
        color += GetSpotlightsColor(input);
    
    return color / lightsCount;
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
    output.uvs = input.uvs;
    output.color = float3(1, 1, 1);
    
    return output;
}

float4 PS(PS_IN input) : SV_Target
{
    float3 lightColor = GetPixelColor(input);
    float4 color = float4(1, 1, 1, 1);
    
    if (IsHaveTexture == 1)
        color = Texture.Sample(TextureSampler, input.uvs);
    
    return float4(color.xyz * lightColor, color.w);
}