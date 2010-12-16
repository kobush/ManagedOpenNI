#include "StdAfx.h"
#include "XnMDepthGenerator.h"

namespace ManagedNiteEx 
{
	XnMDepthGenerator::XnMDepthGenerator(xn::DepthGenerator* pDepthGenerator)
		: XnMMapGenerator(pDepthGenerator)
	{
		this->m_pDepthGenerator = pDepthGenerator;
	}

	XnMDepthGenerator::~XnMDepthGenerator()
	{
		this->m_pDepthGenerator = NULL;
	}

	void XnMDepthGenerator::GetMetaData(XnMDepthMetaData^ depthMetaData)
	{
		xn::DepthMetaData* nativeMeta = (xn::DepthMetaData*)depthMetaData->GetNativeObject();
		m_pDepthGenerator->GetMetaData(*nativeMeta);
	}
}