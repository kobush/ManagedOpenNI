#pragma once

#include "XnMMapGenerator.h"
#include "XnMDepthMetaData.h"

namespace ManagedNiteEx 
{
	public ref class XnMDepthGenerator
		: public XnMMapGenerator
	{
	internal:
		XnMDepthGenerator(xn::DepthGenerator*);
	private:
		~XnMDepthGenerator();

	public:
		void GetMetaData(XnMDepthMetaData^);

		//TODO:
		//ConvertProjectiveToRealWorld 
		//ConvertRealWorldToProjective 
		//GetDepthMap 
		//GetDeviceMaxDepth 
		//GetFieldOfView 
		//GetUserPositionCap 

		// events:
		//RegisterToFieldOfViewChange 
		//UnregisterFromFieldOfViewChange 

	protected:
		xn::DepthGenerator* m_pDepthGenerator;
	};
}

