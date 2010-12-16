#include "StdAfx.h"
#include "XnMImageGenerator.h"

namespace ManagedNiteEx 
{

	XnMImageGenerator::XnMImageGenerator(xn::ImageGenerator* pImageGenerator)
		: XnMMapGenerator(pImageGenerator)
	{
		this->m_pImageGenerator = pImageGenerator;
	}

	XnMImageGenerator::~XnMImageGenerator()
	{
		this->m_pImageGenerator = NULL;
	}

	XnMPixelFormat XnMImageGenerator::GetPixelFormat(void)
	{
		return XnMPixelFormat::Rgb24;
	}

	void XnMImageGenerator::GetMetaData(XnMImageMetaData^ imageMeta) 
	{
		xn::ImageMetaData* nativeMeta = (xn::ImageMetaData*)imageMeta->GetNativeObject();
		m_pImageGenerator->GetMetaData(*nativeMeta);
	}
}