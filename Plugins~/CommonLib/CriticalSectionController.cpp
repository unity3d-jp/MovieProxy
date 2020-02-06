#include "stdafx.h"

#include "CriticalSectionController.h"
#include "CriticalSectionObject.h"

namespace StreamingImageSequencePlugin {

CriticalSectionController::CriticalSectionController(CriticalSectionObject* cs)
	:m_cs(cs)
{
	m_cs->Enter();
}

CriticalSectionController::~CriticalSectionController()
{
	m_cs->Leave();
}

} //end namespace
