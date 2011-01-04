//--------------------------------------------------------------------------------------
// Constant Buffer Variables
//--------------------------------------------------------------------------------------

cbuffer cbPerFrame {
	float3	gEyePosW;
	float4	gFillColor;
	matrix	gViewProj;
	matrix  gWorld;
};

cbuffer cb0 {
	matrix gDepthToRgb;
	float gFocalLengthDepth = 580.0;
	float gFocalLengthImage = 525.0;
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

	float u = vIn.pos.x;
	float v = vIn.pos.y;

	// depth in mm
	uint depth = gDepthMap.Load(gRes.x * v + u);

	float pixelSize = depth / gFocalLengthDepth;
	float4 pos;
	pos.x = (u - gRes.x / 2.0) * pixelSize;
	pos.y = (v - gRes.y / 2.0) * pixelSize;
	pos.z = depth;
	pos.w = 1.0;
	
	// transform to world 
	vOut.centerW = mul(pos, gWorld).xyz; 
	vOut.sizeW.x = length(mul(float4(pixelSize * 1.2, 0, 0, 0), gWorld));
	vOut.sizeW.y = length(mul(float4(0, pixelSize * 1.2, 0, 0), gWorld));

	//float4 uvd1 = float4(pos.xyz, 1);
	//float3 uvw = mul(uvd1, gDepthToRgb).xyz;
	//vOut.texC = float2((uvw.x/uvw.z + 0.5) / gRes.x, (uvw.y/uvw.z + 0.5) / gRes.y);

	// transform to RGB camera space
	//pos.x = pos.x + 35.0f;
	//pos.y = pos.y + 15.0f;
	pos = mul(pos, gDepthToRgb);

	// transform to image frame
	u = pos.x * (gFocalLengthImage / pos.z) + (gRes.x / 2.0);
	v = pos.y * (gFocalLengthImage / pos.z) + (gRes.y / 2.0);

	// map to texture coord. [0..1]
	vOut.texC = float2(u / gRes.x, v / gRes.y);

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
		gOut.posW = mul(v[i], W).xyz; // implicit truncation
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
	Filter = MIN_MAG_MIP_POINT;
	AddressU = Border;
	AddressV = Border;
};

float4 PS( GS_OUT pIn) : SV_Target
{
	// sample texture
	float4 sample = gImageMap.Sample( gTriLinearSam, pIn.texC );

	// mix with fill color based on alpha
	float4 diffuse = float4(lerp(sample.xyz, gFillColor.xyz, gFillColor.w), 1.0f);

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


