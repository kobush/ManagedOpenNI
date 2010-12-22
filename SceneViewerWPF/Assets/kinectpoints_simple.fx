//--------------------------------------------------------------------------------------
// Constant Buffer Variables
//--------------------------------------------------------------------------------------

cbuffer cbPerFrame {
	float3	gEyePosW;
	float4	gFillColor;
	matrix	gViewProj;
};

cbuffer cb0 {
	float gZeroPlanePixelSize;
	float gZeroPlaneDistance;
	float gScale;
	float2 gRes;
}

Texture2D gImageMap;

Buffer<uint> gDepthMap;



//--------------------------------------------------------------------------------------
// Vertex Shader
//--------------------------------------------------------------------------------------

struct VS_IN
{
	int2 pos : POSITION;
};

//--------------------------------------------------------------------------------------

struct VS_OUT
{
    float3 centerW : POSITION;
	float2 sizeW : SIZE;
    float2 texC : TEXCOORD;
};

VS_OUT VS(VS_IN vIn)
{
	VS_OUT vOut;

	uint depth = gDepthMap.Load(gRes.x * vIn.pos.y + vIn.pos.x);
	float pixelSize = depth * gZeroPlanePixelSize * gScale / gZeroPlaneDistance;

	vOut.centerW.x = (vIn.pos.x - gRes.x / 2.0) * pixelSize;
	vOut.centerW.y = (gRes.y / 2.0 - vIn.pos.y) * pixelSize;
	vOut.centerW.z = depth * gScale; 

	vOut.sizeW = float2(pixelSize *1.2, pixelSize*1.2);
	vOut.texC = float2(vIn.pos.x / gRes.x, vIn.pos.y / gRes.y);
    return vOut;
}

//--------------------------------------------------------------------------------------
// Geometry Shader
//--------------------------------------------------------------------------------------

struct GS_OUT
{
	float4 posH : SV_POSITION;
	float3 posW : POSITION;
	float3 normalW : NORMAL;
	float2 texC : TEXCOORD;
	float4 diffuse : DIFFUSE;
};

[maxvertexcount(4)]
void GS(point VS_OUT gIn[1], inout TriangleStream<GS_OUT> triStream)
{
	[branch]
	if (gIn[0].centerW.z == 0) // skip not visible points
	{
		return;
	}

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

	float c = 1.0 - gIn[0].centerW.z / 1000.0;
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
		gOut.texC = gIn[0].texC; //texC[i];
		gOut.diffuse = float4(c, c, 0.0f, 1.0);
		triStream.Append(gOut);
	}
}

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------

SamplerState gTriLinearSam
{
	Filter = MIN_MAG_MIP_LINEAR;
	AddressU = Border;
	AddressV = Border;
};

float4 PS( GS_OUT pIn) : SV_Target
{
	float2 uv = pIn.texC;
	float4 diffuse = gImageMap.Sample( gTriLinearSam, uv );

	// no lighting so just return input color
	return diffuse;
}

//--------------------------------------------------------------------------------------

technique10 Render
{
    pass P0
    {
        SetVertexShader( CompileShader( vs_4_0, VS() ) );
        SetGeometryShader( CompileShader( gs_4_0, GS() ) );
        SetPixelShader( CompileShader( ps_4_0, PS() ) );

		// restore states
		SetBlendState(NULL, float4(0.0f, 0.0f, 0.0f, 0.0f), 0xffffffff);
		SetDepthStencilState( NULL, 0 );
    }
}


