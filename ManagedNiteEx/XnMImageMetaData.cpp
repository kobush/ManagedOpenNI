#include "StdAfx.h"
#include "XnMImageMetaData.h"

namespace ManagedNiteEx
{
	XnMImageMetaData::XnMImageMetaData(xn::ImageMetaData* pImageMeta)
		: XnMMapMetaData(pImageMeta, false)
	{ }

	XnMImageMetaData::XnMImageMetaData()
		: XnMMapMetaData(new xn::ImageMetaData(), true)
	{ }
}
