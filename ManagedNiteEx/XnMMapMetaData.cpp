#include "StdAfx.h"
#include "XnMMapMetaData.h"

namespace ManagedNiteEx
{
	XnMMapMetaData::XnMMapMetaData(xn::MapMetaData* pMeta, BOOL bShouldDelete)
		: XnMOutputMetaData(pMeta, bShouldDelete)
	{
		m_pMapMeta = pMeta;
	}
}
