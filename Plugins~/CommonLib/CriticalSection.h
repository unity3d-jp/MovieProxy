#pragma once

#include "CriticalSectionObject.h"
#include "CriticalSectionType.h"

namespace StreamingImageSequencePlugin {

class COMMONLIB_API CriticalSection {
    public:
        static CriticalSection& GetInstance( );
        ~CriticalSection( );
    
    CriticalSectionObject* GetObject(const CriticalSectionType csType);
        
    private:
    
        CriticalSectionObject  m_objects[CRITICAL_SECTION_TYPE_MAX];
    
        CriticalSection();
        CriticalSection(CriticalSection const&) = delete;
        void operator=(CriticalSection const&) = delete;
};


} //end namespace
