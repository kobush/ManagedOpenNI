#pragma once

namespace ManagedNiteEx {

	ref class XnMHelper
	{
	public:
		static void ThrowErrorException(System::String^, UInt32);
		static String^ CreateString(const XnChar*);
		static String^ CreateErrorStr(System::String^, UInt32);
	};
}