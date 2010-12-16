#pragma once
#include "XnMMapMetaData.h"

namespace ManagedNiteEx 
{
	public ref class XnMSceneMetaData :
		public XnMMapMetaData
	{
	public:
		XnMSceneMetaData(void);
	internal:
		XnMSceneMetaData(xn::SceneMetaData*);

		property xn::SceneMetaData* MetaData { 
			xn::SceneMetaData* get() { return (xn::SceneMetaData*)this->GetNativeObject(); }
		}

	public:
		UInt16 GetLabel(UInt32 x, UInt32 y); 
		//XnMLabel^ Data { get ()
		//LabelMap
		// indexer[x,y]
		// indexer[int]

		//TODO:
		// reAdjust
		//WritableData
		//WritableLabelMap
		//CopyFrom
	};
}

