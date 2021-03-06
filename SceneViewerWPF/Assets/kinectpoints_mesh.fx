//--------------------------------------------------------------------------------------
// Constant Buffer Variables
//--------------------------------------------------------------------------------------

#include "lighthelper.fx"

cbuffer cbPerFrame {
	Light	gLight;
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
	float4 posH : SV_POSITION;
    float3 posW : POSITION;
	float3 normalW : NORMAL;
    float2 texC : TEXCOORD;
	float sizeW : SIZE;
};

//--------------------------------------------------------------------------------------

bool InBounds(int x, int y)
{
	return x >= 0 && x < gRes.x && 
		   y >= 0 && y < gRes.y;
}

float4 ComputeVertexPosSizeW(int x, int y)
{
/*	smooth the surface (expensive op)
	float depth = 0; float num = 0;
	[loop] for (int m=x-1; m<=x+1;m++)
		[loop] for (int n=y-1; n<=y+1; n++)
		{
			// check if coordinates are in range
			if (InBounds(m,n))
			{
				depth = gDepthMap.Load(gRes.x * n + m);
				num += 1;
			}
		}
	depth = depth / num;
*/
	// check if coordinates are in range
	uint depth = 0;
	if (InBounds(x,y))
		depth = gDepthMap.Load(gRes.x * y + x);
	
	[branch] if (depth == 0)
	{
		return float4(0,0,0,0);
	}
	else 
	{
		float pixelSize = depth / gFocalLengthDepth;

		float4 pos;
		pos.x = (x - gRes.x / 2.0) * pixelSize;
		pos.y = (y - gRes.y / 2.0) * pixelSize;
		pos.z = depth;
		pos.w = pixelSize;

		return pos;
	}
}

float3 ComputeFaceNormal(float3 p0, float3 p1, float3 p2) 
{
	float3 u = p1 - p0;
	float3 v = p2 - p0;
	return normalize(cross(u,v));
}

VS_OUT VS(VS_IN vIn)
{
	float u = vIn.pos.x;
	float v = vIn.pos.y;
	float4 v0 = ComputeVertexPosSizeW(u, v);

	VS_OUT vOut;
	vOut.posW =  mul(float4(v0.xyz, 1.0), gWorld).xyz;
	vOut.posH = mul(float4(vOut.posW, 1.0f), gViewProj);
	vOut.sizeW = mul(float4(v0.w, 0.0, 0.0, 1.0), gWorld).x;
	
	// transform to RGB camera space
	float4 pos = mul(float4(v0.xyz, 1.0), gDepthToRgb);

	// transform to image frame
	u = pos.x * (gFocalLengthImage / pos.z) + (gRes.x / 2.0);
	v = pos.y * (gFocalLengthImage / pos.z) + (gRes.y / 2.0);

	vOut.texC = float2(u / gRes.x, v / gRes.y);

	// don't bother with normal if lighting is not used
	[branch]
	if (gLight.type == 0) 
	{
		vOut.normalW = float3(0,0,0);
		return vOut;
	}
	else 
	{
		/* compute positions of neighbours

	          v2
		    /  | \
		  /    |   \
		v1----v0----v3
		  \    |   /
		    \  | /
			  v4
		*/

		float3 normal = float3(0,0,0);
	

		int o = 1;
		float4 v1 = ComputeVertexPosSizeW(vIn.pos.x - o, vIn.pos.y);
		float4 v2 = ComputeVertexPosSizeW(vIn.pos.x, vIn.pos.y + o);
		float4 v3 = ComputeVertexPosSizeW(vIn.pos.x + o, vIn.pos.y);
		float4 v4 = ComputeVertexPosSizeW(vIn.pos.x, vIn.pos.y - o);

		// average face normals
		// pixelSize will be 0 for invalid vertices
		if (v1.w > 0 && v2.w > 0)
			normal += ComputeFaceNormal(v0.xyz, v1.xyz, v2.xyz); 
		if (v2.w > 0 && v3.w > 0)
			normal += ComputeFaceNormal(v0.xyz, v2.xyz, v3.xyz); 
		if (v3.w > 0 && v4.w > 0)
			normal += ComputeFaceNormal(v0.xyz, v3.xyz, v4.xyz); 
		if (v4.w > 0 && v1.w > 0)
			normal += ComputeFaceNormal(v0.xyz, v4.xyz, v1.xyz); 

		// transform to world space (??)
		//normal = mul(float4(normal, 1.0), gWorld).xyz;
		
		vOut.normalW = normalize(normal);
	    return vOut;
	}
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
};

[maxvertexcount(3)]
void GS(triangle VS_OUT gIn[3], inout TriangleStream<GS_OUT> triStream)
{
	GS_OUT gOut;

	// reject faces if any vertes has invalid depth
	[branch]
	if (gIn[0].posW.z == 0 || gIn[1].posW.z == 0 || gIn[2].posW.z == 0)
		return;

	// reject faces where distance between points is too large
	float avgSize = gIn[0].sizeW + gIn[1].sizeW + gIn[2].sizeW;
	avgSize = avgSize / 3.0f;
	avgSize = avgSize * 14;

	float d1 = distance(gIn[0].posW, gIn[1].posW);
	float d2 = distance(gIn[1].posW, gIn[2].posW);
	float d3 = distance(gIn[2].posW, gIn[0].posW);
	
	[branch]
	if (d1 > avgSize || d2 > avgSize || d3 > avgSize)
		return;

	// output new face
	[unroll]
	for(int i = 0; i < 3; ++i)
	{
		// copy to output stream
		gOut.posW = gIn[i].posW;
		gOut.posH = gIn[i].posH;
		gOut.texC = gIn[i].texC; 
		gOut.normalW = gIn[i].normalW;
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
	float4 sample = gImageMap.Sample( gTriLinearSam, pIn.texC );

	// mix with fill color based on alpha
	float4 diffuse = float4(lerp(sample.xyz, gFillColor.xyz, gFillColor.w), 1.0f);
	
	[branch]
	if (gLight.type == 0)
	{
		// no lighting so just return input color
		return diffuse;
	}
	else 
	{
		float4 spec = float4(0.2, 0.2, 0.2, 0);

		// Map [0,1] --> [0,256]
		spec.a *= 255.0f;

		float3 litColor;

		// Interpolating normal can make it not be of unit length so
		// normalize it.
		pIn.normalW = normalize(pIn.normalW);

		// set surface info struct
		SurfaceInfo v = {pIn.posW, pIn.normalW, diffuse, spec};

		// use lighting method
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


