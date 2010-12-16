#pragma once

#include "XnMMapMetaData.h"

namespace ManagedNiteEx
{
	public ref class XnMDepthMetaData
		: public XnMMapMetaData 
	{
	public:
		// public constructor for using in C# code
		XnMDepthMetaData(void);

		property UInt16 ZRes { 
			UInt16 get() { return MetaData->ZRes(); } 
		}

	internal:
		XnMDepthMetaData(xn::DepthMetaData*);

		property xn::DepthMetaData* MetaData { 
			xn::DepthMetaData* get() { return (xn::DepthMetaData*)GetNativeObject(); }
		}

	};
}

