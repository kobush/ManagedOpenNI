#pragma once

#include "XnMGenerator.h"

namespace ManagedNiteEx
{
	public ref class XnMMapGenerator abstract 
		: public XnMGenerator
	{
	internal:
		XnMMapGenerator(xn::MapGenerator*);
	};
}

