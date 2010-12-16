#include "StdAfx.h"
#include "XnMProductionNode.h"

namespace ManagedNiteEx
{
	XnMProductionNode::XnMProductionNode(IntPtr nativeNode)
	{
		this->m_pNode = (xn::ProductionNode*)nativeNode.ToPointer();
		this->m_bShouldDelete = false;
	}

	XnMProductionNode::XnMProductionNode(xn::ProductionNode* nativeNode)
	{
		this->m_pNode = nativeNode;
		this->m_bShouldDelete = false;
	}

	XnMNodeInfo^ XnMProductionNode::GetNodeInfo() 
	{
		xn::NodeInfo nodeInfo = this->m_pNode->GetInfo();
		return gcnew XnMNodeInfo(nodeInfo);
	}
}