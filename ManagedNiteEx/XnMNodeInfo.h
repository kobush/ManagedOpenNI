#pragma once

#include "XnMHelper.h"
#include "Enumerations.h"

namespace ManagedNiteEx
{
	public ref class XnMNodeInfo
	{
	internal:
		XnMNodeInfo(xn::NodeInfo);

	public:
		
		// Gets the instance name of the production node.
		property String^ InstanceName { 
			public: String^ get() { return m_InstanceName; } 
		};

		// Gets the vendor of the production node.
		property String^ Vendor { 
			public: String^ get() { return m_Vendor; }
		};

		property String^ Name { 
			public: String^ get() { return m_Name; } 
		};

		// Gets the type of the production node. 
		property XnMProductionNodeType Type { 
			public: XnMProductionNodeType get() { return m_Type; }
		};

		// Gets the creation info of the production node.
		property String^ CreationInfo { 
			public: String^ get() { return m_CreationInfo; }
		}; 

	private:
		String^ m_InstanceName;
		String^ m_CreationInfo;
		String^ m_Vendor;
		String^ m_Name;
		XnMProductionNodeType m_Type;
	};
}