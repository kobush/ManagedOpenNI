#pragma once

#include "XnMProductionNode.h"

namespace ManagedNiteEx
{
	public ref class XnMGenerator abstract
		: public XnMProductionNode
	{
	internal:
		XnMGenerator(xn::Generator*);
	};
}

