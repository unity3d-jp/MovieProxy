#include "stdafx.h"

#include "CriticalSectionObject.h"

namespace StreamingImageSequencePlugin {

CriticalSectionObject::CriticalSectionObject()
{
#ifdef _WIN32
	InitializeCriticalSection(&m_cs);
#endif
}

CriticalSectionObject::~CriticalSectionObject()
{
#ifdef _WIN32
	DeleteCriticalSection(&m_cs);
#endif
}

void CriticalSectionObject::Enter()
{
#ifdef _WIN32
	EnterCriticalSection(&m_cs);
#else
    m_cs.lock();
#endif
}

void CriticalSectionObject::Leave()
{
#ifdef _WIN32
	LeaveCriticalSection(&m_cs);
#else
    m_cs.unlock();
#endif
}


} //end namespace
