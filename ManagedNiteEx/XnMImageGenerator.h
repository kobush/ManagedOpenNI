#pragma once

#include "Enumerations.h"
#include "XnMProductionNode.h"
#include "XnMMapGenerator.h"
#include "XnMImageMetaData.h"

namespace ManagedNiteEx 
{
	public ref class XnMImageGenerator 
		: public XnMMapGenerator
	{
	public:
		//XnMImageGenerator(IntPtr);
 
	internal:
		XnMImageGenerator(xn::ImageGenerator*);
	private:
		~XnMImageGenerator();

	public:
	//	XnMImageGenerator(XNodeHandle);
		//GetGrayscale16ImageMap
		//GetGrayscale8ImageMap 
		//GetImageMap 
		//GetRGB24ImageMap 
		//GetYUV422ImageMap 
		void GetMetaData(XnMImageMetaData^);

		XnMPixelFormat GetPixelFormat();
		//IsPixelFormatSupported 
		//SetPixelFormat 
		//RegisterToPixelFormatChange 
		//UnregisterFromPixelFormatChange 

	//	EventHandler PixelFormatChanged;

	private:
		xn::ImageGenerator* m_pImageGenerator;

		//void OnPixelFormatChange();
	};

}