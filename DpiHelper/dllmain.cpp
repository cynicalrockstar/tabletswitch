// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include "DpiHelper.h"
#include "stdio.h"
#include <string>
#include <iostream>
using namespace std;

struct DisplayData {
    LUID m_adapterId;
    int m_targetID;
    int m_sourceID;

    DisplayData()
    {
        m_adapterId = {};
        m_targetID = m_sourceID = -1;
    }
};

std::vector<DisplayData> GetDisplayData()
{
    std::vector<DisplayData> displayDataCache;
    std::vector<DISPLAYCONFIG_PATH_INFO> pathsV;
    std::vector<DISPLAYCONFIG_MODE_INFO> modesV;
    int flags = QDC_ONLY_ACTIVE_PATHS;
    if (false == DpiHelper::GetPathsAndModes(pathsV, modesV, flags))
    {
        cout << "DpiHelper::GetPathsAndModes() failed\n";
    }
    displayDataCache.resize(pathsV.size());
    int idx = 0;
    for (const auto& path : pathsV)
    {
        //get display name
        auto adapterLUID = path.targetInfo.adapterId;
        auto targetID = path.targetInfo.id;
        auto sourceID = path.sourceInfo.id;

        DISPLAYCONFIG_TARGET_DEVICE_NAME deviceName;
        deviceName.header.size = sizeof(deviceName);
        deviceName.header.type = DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME;
        deviceName.header.adapterId = adapterLUID;
        deviceName.header.id = targetID;
        if (ERROR_SUCCESS != DisplayConfigGetDeviceInfo(&deviceName.header))
        {
            cout << "DisplayConfigGetDeviceInfo() failed";
        }
        else
        {
            std::wstring nameString = std::to_wstring(idx) + std::wstring(L". ") + deviceName.monitorFriendlyDeviceName;
            if (DISPLAYCONFIG_OUTPUT_TECHNOLOGY_INTERNAL == deviceName.outputTechnology)
            {
                nameString += L"(internal display)";
            }
            DisplayData dd = {};
            dd.m_adapterId = adapterLUID;
            dd.m_sourceID = sourceID;
            dd.m_targetID = targetID;

            displayDataCache[idx] = dd;
        }
        idx++;
    }
    return displayDataCache;
}

BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved
                     )
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
    case DLL_THREAD_ATTACH:
    case DLL_THREAD_DETACH:
    case DLL_PROCESS_DETACH:
        break;
    }
    return TRUE;
}

extern "C" __declspec(dllexport) 
bool SetDisplayDpi(int dpi)
{
    auto displayDataCache = GetDisplayData();
    auto result = DpiHelper::SetDPIScaling(displayDataCache[0].m_adapterId, displayDataCache[0].m_sourceID, dpi);

    return result;
}