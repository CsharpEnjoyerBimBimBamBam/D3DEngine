
cbuffer ConstantBuffer : register(b0)
{
    float4x4 LightViewProjection;
    float FarClipPlane;
    int TransformZ;
};

struct VS_IN
{
    float3 position : POSITION;
    float3 normal : NORMAL;
};

struct PS_IN
{
    float4 position : SV_POSITION;
};

PS_IN VS(VS_IN input)
{
    PS_IN output;
    
    float4 inputPosition = float4(input.position.xyz, 1); 
    output.position = mul(LightViewProjection, inputPosition);
    
    if (TransformZ == 0)
        return output;
    
    float w = output.position.w;
    output.position.z = (w / FarClipPlane) * w;
    
    return output;
}
