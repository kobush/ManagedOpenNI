#pragma once
namespace ManagedNiteEx {

	public ref class XnMException : public System::Exception
	{
	public:
		XnMException(System::String^, UInt32);

		property UInt32 Status { 
			public: UInt32 get() { return this->m_status; }  
		};

	private:
		UInt32 m_status;
	};
}

