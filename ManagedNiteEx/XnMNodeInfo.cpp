#include "StdAfx.h"
#include "XnMNodeInfo.h"


namespace ManagedNiteEx
{
	XnMNodeInfo::XnMNodeInfo(xn::NodeInfo nodeInfo)
	{
		m_InstanceName = XnMHelper::CreateString(nodeInfo.GetInstanceName());
		m_CreationInfo = XnMHelper::CreateString(nodeInfo.GetCreationInfo()); 
		
		XnProductionNodeDescription desc = nodeInfo.GetDescription();
		m_Vendor = XnMHelper::CreateString(desc.strVendor);
		m_Name = XnMHelper::CreateString(desc.strName); 
		m_Type = (XnMProductionNodeType)desc.Type;
	}

}
