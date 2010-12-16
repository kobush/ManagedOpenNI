#include "StdAfx.h"
#include "XnMOutputMetaData.h"

namespace ManagedNiteEx
{
	XnMOutputMetaData::XnMOutputMetaData(xn::OutputMetaData* pMeta, BOOL bShouldDelete)
	{
		this->m_pOutputMeta = pMeta;
		this->m_bShouldDelete = bShouldDelete;
	}

	XnMOutputMetaData::~XnMOutputMetaData()
	{		
		if (m_pOutputMeta != NULL && m_bShouldDelete)
		{
			delete m_pOutputMeta;
		}
		m_pOutputMeta = NULL;
	}
}
