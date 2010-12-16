#include "StdAfx.h"
#include "XnMHelper.h"
#include "XnMException.h"

namespace ManagedNiteEx {

	void XnMHelper::ThrowErrorException(System::String^ message, UInt32 status)
	{
		String^ errorStr = CreateErrorStr(message, status);

		System::Diagnostics::Trace::WriteLine(errorStr);
		throw gcnew XnMException(errorStr, status);
	}

	String^ XnMHelper::CreateErrorStr(String^ message, UInt32 status)
	{
	    System::Text::StringBuilder^ builder = gcnew System::Text::StringBuilder(message);
		builder->Append("\nError: ");
		builder->Append(XnMHelper::CreateString(xnGetStatusString(status)));
		builder->Append("\n");
		return builder->ToString();
	}

	String^ XnMHelper::CreateString(const XnChar* str) {
		return gcnew String((char*)str);
		//return Marshal::PtrToStringAuto((IntPtr)(void*)str);
	}
}
