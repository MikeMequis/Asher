# Implementation Notes: Bootstrap Initialization Fix

## Problem Summary

The Bootstrap DLL is not initializing because .NET Framework managed DLLs loaded via `LoadLibrary` do not automatically run static constructors. The static constructor only runs when the type is first accessed, which never happens after injection.

## Required Solution: Native Proxy DLL

Create a native C++ DLL (`Asher.Bootstrap.Proxy.dll`) that acts as a bridge between the injection mechanism and the managed Bootstrap code.

### Steps to Implement:

1. **Create C++ Dynamic Library Project**
   - Add new C++ project: `Asher.Bootstrap.Proxy`
   - Project type: Dynamic Library (.dll)
   - Target: x86 (or x64, depending on game architecture)

2. **Implement DllMain**
   ```cpp
   #include <windows.h>
   #include <mscoree.h>
   #include <metahost.h>
   #pragma comment(lib, "mscoree.lib")

   BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
   {
       if (ul_reason_for_call == DLL_PROCESS_ATTACH)
       {
           // Get CLR runtime host
           ICLRMetaHost* pMetaHost = NULL;
           ICLRRuntimeInfo* pRuntimeInfo = NULL;
           ICLRRuntimeHost* pRuntimeHost = NULL;
           
           CLRCreateInstance(CLSID_CLRMetaHost, IID_ICLRMetaHost, (LPVOID*)&pMetaHost);
           // Get runtime version (e.g., "v4.0.30319")
           pMetaHost->GetRuntime(L"v4.0.30319", IID_ICLRRuntimeInfo, (LPVOID*)&pRuntimeInfo);
           pRuntimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_ICLRRuntimeHost, (LPVOID*)&pRuntimeHost);
           
           // Start runtime
           pRuntimeHost->Start();
           
           // Get base directory
           char dllPath[MAX_PATH];
           GetModuleFileNameA(hModule, dllPath, MAX_PATH);
           std::string baseDir = dllPath;
           baseDir = baseDir.substr(0, baseDir.find_last_of("\\/"));
           
           std::string bootstrapPath = baseDir + "\\Asher.Bootstrap.dll";
           
           // Call Bootstrap.EntryPoint()
           DWORD returnValue;
           pRuntimeHost->ExecuteInDefaultAppDomain(
               bootstrapPath.c_str(),
               L"Asher.Bootstrap.AsherBootstrap",
               L"EntryPoint",
               L"",
               &returnValue);
           
           // Cleanup
           pRuntimeHost->Release();
           pRuntimeInfo->Release();
           pMetaHost->Release();
       }
       return TRUE;
   }
   ```

3. **Update Injection Code**
   - Modify `DllInjectorService` to inject `Asher.Bootstrap.Proxy.dll` instead of `Asher.Bootstrap.dll`
   - The proxy DLL will handle loading and calling the managed Bootstrap

4. **Build and Deploy**
   - Build the proxy DLL
   - Copy it to the game folder alongside Bootstrap and Runtime DLLs
   - Update launcher to inject the proxy DLL

### Alternative: Use Existing Solutions

Consider using existing libraries that handle this, such as:
- EasyHook (provides managed DLL injection with proper initialization)
- SharpMonoInjector (for Mono-based games)
- Custom native proxy DLL (as described above)

## References

- [CLR Hosting Interfaces](https://docs.microsoft.com/en-us/dotnet/framework/unmanaged-api/hosting/clr-hosting-interfaces)
- [DllMain Entry Point](https://docs.microsoft.com/en-us/windows/win32/dlls/dllmain)
- Similar implementations: SMAPI, DustAetPatchingPlatform, various game modding frameworks

