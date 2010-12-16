#include "StdAfx.h"
#include "XnMDepthMetaData.h"

namespace ManagedNiteEx
{
	XnMDepthMetaData::XnMDepthMetaData(xn::DepthMetaData* pDepthMeta)
		: XnMMapMetaData(pDepthMeta, false)
	{ }

	XnMDepthMetaData::XnMDepthMetaData()
		: XnMMapMetaData(new xn::DepthMetaData(), true)
	{ }
}