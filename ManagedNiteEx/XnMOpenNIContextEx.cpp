#include "StdAfx.h"
#include "XnMHelper.h"
#include "XnMOpenNIContextEx.h"

namespace ManagedNiteEx
{
	XnMOpenNIContextEx::XnMOpenNIContextEx(void)
	{
		this->m_pniContext = new xn::Context();
	}

	XnMOpenNIContextEx::~XnMOpenNIContextEx()
	{
		this->m_pniContext->Shutdown();
		delete m_pniContext;
	}

	UInt32 XnMOpenNIContextEx::InitFromXmlFile(System::String^ xmlFileName) 
	{
		XnStatus status;
		
		//TODO: add error enumeration
		EnumerationErrors errors;

		//http://support.microsoft.com/kb/311259
		XnChar* path = (char*)(void*)Marshal::StringToHGlobalAnsi(xmlFileName);
		status = m_pniContext->InitFromXmlFile(path, &errors);
		Marshal::FreeHGlobal((IntPtr)path);
		if (status == XN_STATUS_NO_NODE_PRESENT)
		{
			return status;
		}
		if (status != XN_STATUS_OK)
		{
			XnMHelper::ThrowErrorException("Failed to open XML config", status);
			return status;
		}

		xn::NodeInfoList list = xn::NodeInfoList();

		// Make sure all generators are generating data. 
		status = m_pniContext->StartGeneratingAll();
		if (status == XN_STATUS_OK) {
			//return 
		}
	}

	UInt32 XnMOpenNIContextEx::Shutdown() {
		this->m_pniContext->Shutdown();
		return 0;
	}

	UInt32 XnMOpenNIContextEx::WaitAndUpdateAll()
	{
	    XnStatus status = 0;
		status = this->m_pniContext->WaitAndUpdateAll();
		if (status != XN_STATUS_OK)
		{
			XnMHelper::ThrowErrorException("Update failed", status);
		}
		return status;
	}

	XnMProductionNode^ XnMOpenNIContextEx::FindExistingNode(XnMProductionNodeType nodeType)
	{
		XnStatus status;

		xn::ProductionNode* pNode = new xn::ProductionNode();
		status = this->m_pniContext->FindExistingNode((XnProductionNodeType)nodeType, *pNode);
		if (status != XN_STATUS_OK)
		{
			XnMHelper::ThrowErrorException("Failed to get production node", status);
		}
		
		//TODO: wrap in appropriate type
		return WrapProductionNode(pNode);
	}

	XnMProductionNode^ XnMOpenNIContextEx::WrapProductionNode(xn::ProductionNode* pNode)
	{
		xn::NodeInfo info = pNode->GetInfo();
		XnProductionNodeDescription desc = info.GetDescription();
		switch(desc.Type)
		{
		case XN_NODE_TYPE_DEVICE:
			break;
		case XN_NODE_TYPE_DEPTH:
			return gcnew XnMDepthGenerator((xn::DepthGenerator*)pNode);
			break;
		case XN_NODE_TYPE_IMAGE:
			return gcnew XnMImageGenerator((xn::ImageGenerator*)pNode);
		case XN_NODE_TYPE_SCENE:
			return gcnew XnMSceneAnalyzer((xn::SceneAnalyzer*)pNode);
		}
		//TODO: store context reference
		return gcnew XnMProductionNode(pNode);
	}
}
