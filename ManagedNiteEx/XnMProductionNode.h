#pragma once

#include "XnMNodeInfo.h"

namespace ManagedNiteEx
{
	public ref class XnMProductionNode
	{
	public:
		XnMProductionNode(IntPtr);

	internal:
		XnMProductionNode(xn::ProductionNode*);

	public:
		XnMNodeInfo^ GetNodeInfo();

		System::UInt64 GetIntProperty(String^ name);
		System::Double GetRealProperty(String^ name);
		System::String^ GetStringProperty(String^ name);

	private:
		~XnMProductionNode() {
			if (0 != m_pNode && m_bShouldDelete)
			{
				delete m_pNode;
				this->m_pNode = 0;
			}
		}
		
		bool m_bShouldDelete;
		xn::ProductionNode* m_pNode;
	};
}

