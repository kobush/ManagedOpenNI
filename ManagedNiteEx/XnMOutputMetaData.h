#pragma once

namespace ManagedNiteEx
{
	public ref class XnMOutputMetaData abstract
	{
	protected:
		XnMOutputMetaData(xn::OutputMetaData*, BOOL b_shouldDelete);
	
	internal:
		xn::OutputMetaData* GetNativeObject() { return m_pOutputMeta; }

	public:
		property DateTime^ Timestamp { 
			DateTime^ get() { return gcnew DateTime(m_pOutputMeta->Timestamp()); } 
		};
		
		property UInt32 FrameID { 
			UInt32 get() { return m_pOutputMeta->FrameID(); } 
		};
		
		property UInt32 DataSize { 
			UInt32 get() { return m_pOutputMeta->DataSize(); } 
		};

		property bool IsDataNew { 
			bool get() { return m_pOutputMeta->IsDataNew(); } 
		};
		
		property IntPtr Data { 
			IntPtr get() { return IntPtr((void*)m_pOutputMeta->Data()); } 
		};
		
		//Data
		//WritableData
		//AllocateData
		//Free
		//MakeDataWritable

	private:
		~XnMOutputMetaData();

	protected:
		BOOL m_bShouldDelete;
		xn::OutputMetaData* m_pOutputMeta;
	};
}
