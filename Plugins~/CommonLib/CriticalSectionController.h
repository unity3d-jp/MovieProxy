#pragma once

#include "Types.h"

namespace StreamingImageSequencePlugin {

class CriticalSectionObject;

class COMMONLIB_API CriticalSectionController
{
	CriticalSectionObject* m_cs;
	CriticalSectionController(){};
public:
	CriticalSectionController(CriticalSectionObject* cs);
	virtual ~CriticalSectionController();
};

} // end namespace
