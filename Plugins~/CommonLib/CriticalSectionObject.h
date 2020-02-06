#pragma once

#include "Types.h"

namespace StreamingImageSequencePlugin {

class COMMONLIB_API CriticalSectionObject
{
private:
#ifdef _WIN32
	CRITICAL_SECTION m_cs;
#else
    std::recursive_mutex m_cs;
#endif
	void operator =(const CriticalSectionObject& src) {}
public:
	CriticalSectionObject();
	virtual ~CriticalSectionObject();

	void Enter();
	void Leave();
};

} //end namespace
