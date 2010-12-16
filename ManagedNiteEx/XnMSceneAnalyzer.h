#pragma once

#include "XnMMapGenerator.h"
#include "XnMSceneMetaData.h"

namespace ManagedNiteEx 
{
	public ref class XnMSceneAnalyzer
		: public XnMMapGenerator
	{
	internal:
		XnMSceneAnalyzer(xn::SceneAnalyzer*);
	private: 
		~XnMSceneAnalyzer();
	public:
		void GetMetaData(XnMSceneMetaData^);
		//XnMLabel GetLabelMap();
		//GetFloor(XnMPlane3D);
	protected:
		xn::SceneAnalyzer* m_pSceneAnalyzer;
	};
}