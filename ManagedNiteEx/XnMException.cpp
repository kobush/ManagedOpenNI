#include "StdAfx.h"
#include "XnMException.h"


namespace ManagedNiteEx {

	XnMException::XnMException(System::String^ message, UInt32 status)
		: System::Exception(message)
	{
		this->m_status = status;
	}
}
