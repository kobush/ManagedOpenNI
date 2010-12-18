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

	System::Double XnMProductionNode::GetRealProperty(String^ name) 
	{
		XnStatus status;
		XnDouble dValue;
		
		XnChar* strName = (char*)(void*)Marshal::StringToHGlobalAnsi(name);
		status = m_pNode->GetRealProperty(strName, dValue);
		Marshal::FreeHGlobal((IntPtr)strName);
		
		if (status != XN_STATUS_OK)
			XnMHelper::ThrowErrorException("Error reading real property", status);
	
		return dValue;
	}

	System::UInt64 XnMProductionNode::GetIntProperty(String^ name) 
	{
		XnStatus status;
		XnUInt64  dValue;
		
		XnChar* strName = (char*)(void*)Marshal::StringToHGlobalAnsi(name);
		status = m_pNode->GetIntProperty(strName, dValue);
		Marshal::FreeHGlobal((IntPtr)strName);
		
		if (status != XN_STATUS_OK)
			XnMHelper::ThrowErrorException("Error reading int property", status);
	
		return dValue;
	}

	System::String^ XnMProductionNode::GetStringProperty(String^ name) 
	{
		return "";
	}

}