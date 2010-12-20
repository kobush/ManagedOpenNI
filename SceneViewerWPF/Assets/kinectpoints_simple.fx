//--------------------------------------------------------------------------------------
// Constant Buffer Variables
//--------------------------------------------------------------------------------------

#include "lighthelper.fx"

cbuffer cbPerFrame {
	Light	gLight;
	float3	gEyePosW;
	float4	gFillColor;
	matrix	gViewProj;
};


//Texture2DArray gDiffuseMapArray;

struct VS_IN
{
/*	
	float3 centerW : POSITION;
	float2 sizeW : SIZE;
	float4 color : COLOR;
	*/
	float4 centerW : POSITION;
	float4 color : COLOR;
};

//--------------------------------------------------------------------------------------
struct VS_OUT
{
    float3 centerW : POSITION;
	float2 sizeW : SIZE;
    float4 color : COLOR;
};

struct GS_OUT
{
	float4 posH : SV_POSITION;
	float3 posW : POSITION;
	float3 normalW : NORMAL;
	float2 texC : TEXCOORD;
	float4 diffuse : DIFFUSE;
	uint primID : SV_PrimitiveID;
};

//--------------------------------------------------------------------------------------
// Vertex Shader
//--------------------------------------------------------------------------------------
VS_OUT VS(VS_IN vIn)
{
	VS_OUT vOut;
	
	// Just pass same data into geometry shader stage. 
    vOut.centerW = vIn.centerW;	// implicit truncation
	vOut.sizeW = float2(vIn.centerW.w, vIn.centerW.w);
	vOut.color = vIn.color;
    return vOut;
}

//--------------------------------------------------------------------------------------
// Geometry Shader
//--------------------------------------------------------------------------------------
[maxvertexcount(4)]
void GS(point VS_OUT gIn[1],
	uint primID : SV_PrimitiveID,
	inout TriangleStream<GS_OUT> triStream)
{
	// Compute 4 triangle strip vertices (quad) in local space.
	// The quad faces down the +z axis in local space.
	//
	float halfWidth = 0.5f*gIn[0].sizeW.x;
	float halfHeight = 0.5f*gIn[0].sizeW.y;
	float4 v[4];
	v[0] = float4(-halfWidth, -halfHeight, 0.0f, 1.0f);
	v[1] = float4(+halfWidth, -halfHeight, 0.0f, 1.0f);
	v[2] = float4(-halfWidth, +halfHeight, 0.0f, 1.0f);
	v[3] = float4(+halfWidth, +halfHeight, 0.0f, 1.0f);
	//
	// Compute texture coordinates to stretch texture over quad.
	//
	float2 texC[4];
	texC[0] = float2(0.0f, 1.0f);
	texC[1] = float2(1.0f, 1.0f);
	texC[2] = float2(0.0f, 0.0f);
	texC[3] = float2(1.0f, 0.0f);
	//
	// Compute world matrix so that billboard is aligned with
	// the y-axis and faces the camera.
	//
	float3 up = float3(0.0f, 1.0f, 0.0f);
	float3 look = gEyePosW - gIn[0].centerW;
//	look.y = 0.0f; // y-axis aligned, so project to xz-plane
	look = normalize(look);
	float3 right = cross(up, look);
	
	float4x4 W;
	W[0] = float4(right, 0.0f);
	W[1] = float4(up, 0.0f);
	W[2] = float4(look, 0.0f);
	W[3] = float4(gIn[0].centerW, 1.0f);
	float4x4 WVP = mul(W, gViewProj);

	//
	// Transform quad vertices to world space and output
	// them as a triangle strip.
	//
	GS_OUT gOut;
	[unroll]
	for(int i = 0; i < 4; ++i)
	{
		gOut.posH = mul(v[i], WVP);
		gOut.posW = mul(v[i], W); // implicit truncation
		gOut.normalW = look;
		gOut.texC = texC[i];
		gOut.diffuse = gIn[0].color;
		gOut.primID = primID;
		triStream.Append(gOut);
	}
}

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------
float4 PS( GS_OUT pIn) : SV_Target
{
	float4 diffuse = pIn.diffuse;
	if (gFillColor.w > 0.5)
		diffuse = gFillColor;
	
	float4 spec = float4(0, 0, 0, 0);

	if (gLight.type == 0)
	{
		// no lighting return input color
		return diffuse;
	}

	float3 litColor;

	// Interpolating normal can make it not be of unit length so
	// normalize it.
	pIn.normalW = normalize(pIn.normalW);

	// set surface info struct
	SurfaceInfo v = {pIn.posW, pIn.normalW, diffuse, spec};

	if( gLight.type == 1 ) // Parallel
	{
		litColor = ParallelLight(v, gLight, gEyePosW);
	}
	else if( gLight.type == 2 ) // Point
	{
		litColor = PointLight(v, gLight, gEyePosW);
	}
	else if( gLight.type == 3 ) // Spot
	{
		litColor = Spotlight(v, gLight, gEyePosW);
	}

	return float4(litColor, diffuse.a);
    
}


//--------------------------------------------------------------------------------------
technique10 Render
{
    pass P0
    {
        SetVertexShader( CompileShader( vs_4_0, VS() ) );
        SetGeometryShader( CompileShader( gs_4_0, GS() ) );
        SetPixelShader( CompileShader( ps_4_0, PS() ) );
    }
}


