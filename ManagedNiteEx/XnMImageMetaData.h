#pragma once

#include "XnMMapMetaData.h"

namespace ManagedNiteEx
{
	public ref class XnMImageMetaData
		: public XnMMapMetaData 
	{
	public:
		// public constructor for using in C# code
		XnMImageMetaData();
	
	internal:
		XnMImageMetaData(xn::ImageMetaData*);

		//TODO: OpenNI class has functions to return pointers to different types of 
		// ImageMaps
		// RGB24Data
		// YUV422Data
		// Grayscale8Data
		// Grayscale16Data
		
		// RGB24Map
		// Grayscale16Map
		// Grayscale8Map
		// ImageMap


		//TODO: add functions to allocate data or use existing data buffer
		// AllocateData(nXRes, xYRes, pixelformat)
		// WritableData
		// WritableImageMap
	};
}

