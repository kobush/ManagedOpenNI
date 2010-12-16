#pragma once

#include "Enumerations.h"
#include "XnMProductionNode.h"
#include "XnMImageGenerator.h"
#include "XnMDepthGenerator.h"
#include "XnMSceneAnalyzer.h"

namespace ManagedNiteEx
{
	public ref class XnMOpenNIContextEx 
	{
	public:
		XnMOpenNIContextEx();
		UInt32 InitFromXmlFile(String^);
		UInt32 Shutdown();

		UInt32 WaitAndUpdateAll();

		XnMProductionNode^ FindExistingNode(XnMProductionNodeType);

	internal:
		property xn::Context* Context { 
			xn::Context* get() { return this->m_pniContext; }
		}

	private:
		~XnMOpenNIContextEx();
		XnMProductionNode^ WrapProductionNode(xn::ProductionNode*);

		xn::Context* m_pniContext;
	};
}

