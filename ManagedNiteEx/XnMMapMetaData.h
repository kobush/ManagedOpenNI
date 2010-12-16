#pragma once

#include "XnMOutputMetaData.h"
#include "Enumerations.h"

namespace ManagedNiteEx
{
	/// <summary>
	/// Represents a MetaData object for generators producing pixel-map-based outputs 
	/// </summary>
	public ref class XnMMapMetaData abstract
		: public XnMOutputMetaData
	{
	protected:
		XnMMapMetaData(xn::MapMetaData*, BOOL);

	internal:
		property xn::MapMetaData* MetaData { 
			xn::MapMetaData* get() { return m_pMapMeta; }
		}

	public:

		// Gets the number of bytes each pixel occupies. 
		property UInt32 BytesPerPixel { 
			UInt32 get() { return MetaData->BytesPerPixel(); } 
		};

		// Gets the FPS in which frame was generated. 
		property UInt32 FPS { 
			UInt32 get() { return MetaData->FPS(); } 
		};
		
		// Gets the actual number of columns in the frame (after cropping) 
		property UInt32 XRes { 
			UInt32 get() { return MetaData->XRes(); } 
		};

		// Gets the actual number of rows in the frame (after cropping) 
		property UInt32 YRes { 
			UInt32 get() { return MetaData->YRes(); } 
		};

		// Gets the offset, in columns, of the buffer within the field of view (0 if cropping is off). 
		property UInt32 XOffset { 
			UInt32 get() { return MetaData->XOffset(); } };

		// Gets the offset, in rows, of the buffer within the field of view (0 if cropping is off). 
		property UInt32 YOffset { 
			UInt32 get() { return MetaData->YOffset(); } 
		};

		// Gets the number of columns in the full frame (entire field-of-view, ignoring cropping). 
		property UInt32 FullXRes { 
			UInt32 get() { return MetaData->FullXRes(); }
		};

		// Gets the number of rows in the full frame (entire field-of-view, ignoring cropping). 
		property UInt32 FullYRes { 
			UInt32 get() { return MetaData->FullYRes(); } 
		};

		// Gets the pixel format of the pixel-map. 
		property XnMPixelFormat PixelFormat { 
			XnMPixelFormat get() { return (XnMPixelFormat)MetaData->PixelFormat(); } 
		};

		// AllocateData()
		// ReAdjust()
	private:
		~XnMMapMetaData() { 
			m_pMapMeta = NULL; 
		}

		xn::MapMetaData* m_pMapMeta;
	};
}
