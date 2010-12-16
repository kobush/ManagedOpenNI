#include "StdAfx.h"
#include "XnMSceneAnalyzer.h"

namespace ManagedNiteEx 
{
	XnMSceneAnalyzer::XnMSceneAnalyzer(xn::SceneAnalyzer* pSceneAnalyzer)
		: XnMMapGenerator(pSceneAnalyzer)
	{
		this->m_pSceneAnalyzer = pSceneAnalyzer;
	}
	
	XnMSceneAnalyzer::~XnMSceneAnalyzer()
	{
		this->m_pSceneAnalyzer = NULL;
	}

	void XnMSceneAnalyzer::GetMetaData(XnMSceneMetaData^ sceneMetaData)
	{
		xn::SceneMetaData* nativeMeta = (xn::SceneMetaData*)sceneMetaData->GetNativeObject();
		m_pSceneAnalyzer->GetMetaData(*nativeMeta);
	}
}
