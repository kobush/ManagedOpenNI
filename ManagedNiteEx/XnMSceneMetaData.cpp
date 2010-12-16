#include "StdAfx.h"
#include "XnMSceneMetaData.h"

namespace ManagedNiteEx 
{
	XnMSceneMetaData::XnMSceneMetaData(xn::SceneMetaData* pSceneMetaData)
		: XnMMapMetaData(pSceneMetaData, false)
	{ }

	XnMSceneMetaData::XnMSceneMetaData()
		: XnMMapMetaData(new xn::SceneMetaData(), true)
	{ }

	UInt16 XnMSceneMetaData::GetLabel(UInt32 x, UInt32 y) 
	{
		// TODO: I'm not sure if this is proper way to cast this. 
		return (*MetaData)[x,y];
	}
}