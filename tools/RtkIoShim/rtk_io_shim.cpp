// RTK_IO_x64.dll shim — intercepts all calls by forwarding through GetProcAddress.
// Uses a generic approach: each exported function looks up and calls the real one.
// For key functions (sendATCommand, readRbus, writeRbus), we capture args.
//
// V2: IAT hooks on CreateFileW/ReadFile/WriteFile to capture the USB wire protocol
// that RTK_IO uses internally for I2C AT commands.
//
// On x64 Windows, calling conventions collapse to the Microsoft x64 ABI, but
// argument count, pointer shape, and return type still matter. Only exports
// verified against the local NativeXuAudioProbe P/Invoke surface are forwarded
// by default; generic forwarding requires an explicit unsafe environment opt-in.

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
#include <stdarg.h>
#include <string.h>
#include <wchar.h>

static HMODULE g_realDll = nullptr;
static FILE* g_log = nullptr;
static CRITICAL_SECTION g_cs;

// ============================================================
// IAT Hook infrastructure — patches kernel32 imports inside
// RTK_IO_x64_real.dll to intercept CreateFileW/ReadFile/WriteFile
// ============================================================

// Original function pointers (saved before patching)
static decltype(&CreateFileW) pOrigCreateFileW = nullptr;
static decltype(&ReadFile)    pOrigReadFile = nullptr;
static decltype(&WriteFile)   pOrigWriteFile = nullptr;
static decltype(&CloseHandle) pOrigCloseHandle = nullptr;
static decltype(&DeviceIoControl) pOrigDeviceIoControl = nullptr;
static decltype(&GetProcAddress) pOrigGetProcAddress = nullptr;

// Track handles opened by RTK_IO so we only log relevant I/O
#define MAX_TRACKED_HANDLES 16
static HANDLE g_trackedHandles[MAX_TRACKED_HANDLES];
static wchar_t g_trackedPaths[MAX_TRACKED_HANDLES][512];
static int g_trackedCount = 0;
static CRITICAL_SECTION g_handleCs;

static bool IsTrackedHandle(HANDLE h) {
    for (int i = 0; i < g_trackedCount; i++)
        if (g_trackedHandles[i] == h) return true;
    return false;
}

static void TrackHandle(HANDLE h, LPCWSTR path) {
    if (g_trackedCount >= MAX_TRACKED_HANDLES) return;
    g_trackedHandles[g_trackedCount] = h;
    wcsncpy(g_trackedPaths[g_trackedCount], path ? path : L"(null)", 511);
    g_trackedPaths[g_trackedCount][511] = 0;
    g_trackedCount++;
}

static void UntrackHandle(HANDLE h) {
    for (int i = 0; i < g_trackedCount; i++) {
        if (g_trackedHandles[i] == h) {
            g_trackedHandles[i] = g_trackedHandles[g_trackedCount - 1];
            wcscpy(g_trackedPaths[i], g_trackedPaths[g_trackedCount - 1]);
            g_trackedCount--;
            return;
        }
    }
}

// Forward declaration for Log
static void Log(const char* fmt, ...);
static void LogHex(const char* label, const void* data, int len);

static HANDLE WINAPI HookedCreateFileW(
    LPCWSTR lpFileName, DWORD dwDesiredAccess, DWORD dwShareMode,
    LPSECURITY_ATTRIBUTES lpSecurityAttributes, DWORD dwCreationDisposition,
    DWORD dwFlagsAndAttributes, HANDLE hTemplateFile)
{
    HANDLE h = pOrigCreateFileW(lpFileName, dwDesiredAccess, dwShareMode,
        lpSecurityAttributes, dwCreationDisposition, dwFlagsAndAttributes, hTemplateFile);
    DWORD err = GetLastError();

    // Log all CreateFileW calls from RTK_IO (helps identify device paths)
    EnterCriticalSection(&g_handleCs);
    Log("[IAT:CreateFileW] path=\"%ls\" access=0x%X share=0x%X disp=%u flags=0x%X -> handle=%p err=%u",
        lpFileName ? lpFileName : L"(null)", dwDesiredAccess, dwShareMode,
        dwCreationDisposition, dwFlagsAndAttributes, h, err);

    // Track device-like handles (\\?\ or \\.\) for ReadFile/WriteFile logging
    if (h != INVALID_HANDLE_VALUE && lpFileName) {
        if (wcsncmp(lpFileName, L"\\\\?\\", 4) == 0 ||
            wcsncmp(lpFileName, L"\\\\.\\", 4) == 0 ||
            wcsstr(lpFileName, L"vid_") != nullptr ||
            wcsstr(lpFileName, L"usb#") != nullptr) {
            TrackHandle(h, lpFileName);
            Log("[IAT:CreateFileW]   -> TRACKED as device handle #%d", g_trackedCount);
        }
    }
    LeaveCriticalSection(&g_handleCs);

    SetLastError(err);
    return h;
}

static BOOL WINAPI HookedReadFile(
    HANDLE hFile, LPVOID lpBuffer, DWORD nNumberOfBytesToRead,
    LPDWORD lpNumberOfBytesRead, LPOVERLAPPED lpOverlapped)
{
    BOOL result = pOrigReadFile(hFile, lpBuffer, nNumberOfBytesToRead,
        lpNumberOfBytesRead, lpOverlapped);
    DWORD err = GetLastError();

    if (IsTrackedHandle(hFile)) {
        DWORD bytesRead = (lpNumberOfBytesRead && result) ? *lpNumberOfBytesRead : 0;
        Log("[IAT:ReadFile] handle=%p reqSize=%u -> %s bytesRead=%u err=%u",
            hFile, nNumberOfBytesToRead, result ? "OK" : "FAIL", bytesRead, err);
        if (result && lpBuffer && bytesRead > 0) {
            LogHex("[IAT:ReadFile] data", lpBuffer,
                (int)(bytesRead < 256 ? bytesRead : 256));
        }
    }

    SetLastError(err);
    return result;
}

static BOOL WINAPI HookedWriteFile(
    HANDLE hFile, LPCVOID lpBuffer, DWORD nNumberOfBytesToWrite,
    LPDWORD lpNumberOfBytesWritten, LPOVERLAPPED lpOverlapped)
{
    // Log BEFORE the write so we see the data being sent
    if (IsTrackedHandle(hFile)) {
        Log("[IAT:WriteFile] handle=%p size=%u",
            hFile, nNumberOfBytesToWrite);
        if (lpBuffer && nNumberOfBytesToWrite > 0) {
            LogHex("[IAT:WriteFile] data", lpBuffer,
                (int)(nNumberOfBytesToWrite < 256 ? nNumberOfBytesToWrite : 256));
        }
    }

    BOOL result = pOrigWriteFile(hFile, lpBuffer, nNumberOfBytesToWrite,
        lpNumberOfBytesWritten, lpOverlapped);
    DWORD err = GetLastError();

    if (IsTrackedHandle(hFile)) {
        DWORD bytesWritten = (lpNumberOfBytesWritten && result) ? *lpNumberOfBytesWritten : 0;
        Log("[IAT:WriteFile]   -> %s bytesWritten=%u err=%u",
            result ? "OK" : "FAIL", bytesWritten, err);
    }

    SetLastError(err);
    return result;
}

static BOOL WINAPI HookedCloseHandle(HANDLE hObject)
{
    bool wasTracked = false;
    EnterCriticalSection(&g_handleCs);
    if (IsTrackedHandle(hObject)) {
        Log("[IAT:CloseHandle] handle=%p (tracked device)", hObject);
        UntrackHandle(hObject);
        wasTracked = true;
    }
    LeaveCriticalSection(&g_handleCs);

    return pOrigCloseHandle(hObject);
}

static BOOL WINAPI HookedDeviceIoControl(
    HANDLE hDevice, DWORD dwIoControlCode, LPVOID lpInBuffer, DWORD nInBufferSize,
    LPVOID lpOutBuffer, DWORD nOutBufferSize, LPDWORD lpBytesReturned, LPOVERLAPPED lpOverlapped)
{
    // Log before
    Log("[IAT:DeviceIoControl] handle=%p code=0x%08X inSize=%u outSize=%u",
        hDevice, dwIoControlCode, nInBufferSize, nOutBufferSize);
    if (lpInBuffer && nInBufferSize > 0)
        LogHex("[IAT:IOCTL] input", lpInBuffer,
            (int)(nInBufferSize < 512 ? nInBufferSize : 512));

    BOOL result = pOrigDeviceIoControl(hDevice, dwIoControlCode, lpInBuffer, nInBufferSize,
        lpOutBuffer, nOutBufferSize, lpBytesReturned, lpOverlapped);
    DWORD err = GetLastError();

    DWORD returned = (lpBytesReturned && result) ? *lpBytesReturned : 0;
    Log("[IAT:DeviceIoControl]   -> %s returned=%u err=%u",
        result ? "OK" : "FAIL", returned, err);
    if (result && lpOutBuffer && returned > 0)
        LogHex("[IAT:IOCTL] output", lpOutBuffer,
            (int)(returned < 512 ? returned : 512));

    SetLastError(err);
    return result;
}

// GetProcAddress hook — intercepts dynamic function resolution
static FARPROC WINAPI HookedGetProcAddress(HMODULE hModule, LPCSTR lpProcName)
{
    FARPROC real = pOrigGetProcAddress(hModule, lpProcName);

    // Only intercept if lpProcName is a string (not ordinal)
    if ((ULONG_PTR)lpProcName > 0xFFFF && real) {
        // Check for I/O functions we want to hook
        if (strcmp(lpProcName, "DeviceIoControl") == 0) {
            Log("[IAT:GetProcAddress] intercepted DeviceIoControl -> returning hook");
            if (!pOrigDeviceIoControl)
                pOrigDeviceIoControl = (decltype(&DeviceIoControl))real;
            return (FARPROC)HookedDeviceIoControl;
        }
        if (strcmp(lpProcName, "CreateFileW") == 0) {
            Log("[IAT:GetProcAddress] intercepted CreateFileW -> returning hook");
            return (FARPROC)HookedCreateFileW;
        }
        if (strcmp(lpProcName, "CreateFileA") == 0) {
            Log("[IAT:GetProcAddress] intercepted CreateFileA (returning real — no hook)");
        }
        if (strcmp(lpProcName, "ReadFile") == 0) {
            Log("[IAT:GetProcAddress] intercepted ReadFile -> returning hook");
            return (FARPROC)HookedReadFile;
        }
        if (strcmp(lpProcName, "WriteFile") == 0) {
            Log("[IAT:GetProcAddress] intercepted WriteFile -> returning hook");
            return (FARPROC)HookedWriteFile;
        }
    }
    return real;
}

// IAT patching: walk the import table of a loaded module and replace a function pointer
static bool PatchIAT(HMODULE module, const char* targetDllName, const char* funcName,
                     void* hookFunc, void** origFunc)
{
    if (!module) return false;

    // Get PE headers
    BYTE* base = (BYTE*)module;
    IMAGE_DOS_HEADER* dosHdr = (IMAGE_DOS_HEADER*)base;
    if (dosHdr->e_magic != IMAGE_DOS_SIGNATURE) return false;

    IMAGE_NT_HEADERS* ntHdr = (IMAGE_NT_HEADERS*)(base + dosHdr->e_lfanew);
    if (ntHdr->Signature != IMAGE_NT_SIGNATURE) return false;

    IMAGE_DATA_DIRECTORY* importDir =
        &ntHdr->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    if (importDir->Size == 0 || importDir->VirtualAddress == 0) return false;

    IMAGE_IMPORT_DESCRIPTOR* importDesc =
        (IMAGE_IMPORT_DESCRIPTOR*)(base + importDir->VirtualAddress);

    for (; importDesc->Name; importDesc++) {
        const char* dllName = (const char*)(base + importDesc->Name);
        if (_stricmp(dllName, targetDllName) != 0) continue;

        IMAGE_THUNK_DATA* origThunk =
            (IMAGE_THUNK_DATA*)(base + importDesc->OriginalFirstThunk);
        IMAGE_THUNK_DATA* firstThunk =
            (IMAGE_THUNK_DATA*)(base + importDesc->FirstThunk);

        for (; origThunk->u1.AddressOfData; origThunk++, firstThunk++) {
            if (IMAGE_SNAP_BY_ORDINAL(origThunk->u1.Ordinal)) continue;

            IMAGE_IMPORT_BY_NAME* importByName =
                (IMAGE_IMPORT_BY_NAME*)(base + origThunk->u1.AddressOfData);
            if (strcmp(importByName->Name, funcName) != 0) continue;

            // Save original
            *origFunc = (void*)firstThunk->u1.Function;

            // Make IAT entry writable
            DWORD oldProtect;
            VirtualProtect(&firstThunk->u1.Function, sizeof(void*),
                          PAGE_READWRITE, &oldProtect);
            firstThunk->u1.Function = (ULONG_PTR)hookFunc;
            VirtualProtect(&firstThunk->u1.Function, sizeof(void*),
                          oldProtect, &oldProtect);

            return true;
        }
    }
    return false;
}

// Enumerate all import DLL names from a module for diagnostics
static void LogImportDlls(HMODULE module, const char* moduleName) {
    if (!module) return;
    BYTE* base = (BYTE*)module;
    IMAGE_DOS_HEADER* dosHdr = (IMAGE_DOS_HEADER*)base;
    if (dosHdr->e_magic != IMAGE_DOS_SIGNATURE) return;
    IMAGE_NT_HEADERS* ntHdr = (IMAGE_NT_HEADERS*)(base + dosHdr->e_lfanew);
    if (ntHdr->Signature != IMAGE_NT_SIGNATURE) return;
    IMAGE_DATA_DIRECTORY* importDir =
        &ntHdr->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    if (importDir->Size == 0 || importDir->VirtualAddress == 0) return;
    IMAGE_IMPORT_DESCRIPTOR* importDesc =
        (IMAGE_IMPORT_DESCRIPTOR*)(base + importDir->VirtualAddress);
    Log("[DIAG] Import DLLs for %s:", moduleName);
    for (; importDesc->Name; importDesc++) {
        const char* dllName = (const char*)(base + importDesc->Name);
        Log("[DIAG]   -> %s", dllName);
    }
}

// Try patching a function across ALL import DLLs (handles API set names)
static bool PatchIATAnyDll(HMODULE module, const char* funcName,
                           void* hookFunc, void** origFunc) {
    if (!module) return false;
    BYTE* base = (BYTE*)module;
    IMAGE_DOS_HEADER* dosHdr = (IMAGE_DOS_HEADER*)base;
    if (dosHdr->e_magic != IMAGE_DOS_SIGNATURE) return false;
    IMAGE_NT_HEADERS* ntHdr = (IMAGE_NT_HEADERS*)(base + dosHdr->e_lfanew);
    if (ntHdr->Signature != IMAGE_NT_SIGNATURE) return false;
    IMAGE_DATA_DIRECTORY* importDir =
        &ntHdr->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
    if (importDir->Size == 0 || importDir->VirtualAddress == 0) return false;
    IMAGE_IMPORT_DESCRIPTOR* importDesc =
        (IMAGE_IMPORT_DESCRIPTOR*)(base + importDir->VirtualAddress);

    for (; importDesc->Name; importDesc++) {
        if (!importDesc->OriginalFirstThunk) continue;
        IMAGE_THUNK_DATA* origThunk =
            (IMAGE_THUNK_DATA*)(base + importDesc->OriginalFirstThunk);
        IMAGE_THUNK_DATA* firstThunk =
            (IMAGE_THUNK_DATA*)(base + importDesc->FirstThunk);
        for (; origThunk->u1.AddressOfData; origThunk++, firstThunk++) {
            if (IMAGE_SNAP_BY_ORDINAL(origThunk->u1.Ordinal)) continue;
            IMAGE_IMPORT_BY_NAME* importByName =
                (IMAGE_IMPORT_BY_NAME*)(base + origThunk->u1.AddressOfData);
            if (strcmp(importByName->Name, funcName) != 0) continue;

            const char* dllName = (const char*)(base + importDesc->Name);
            Log("[DIAG] Found %s in %s (IAT addr=%p, current=%p)",
                funcName, dllName, &firstThunk->u1.Function,
                (void*)firstThunk->u1.Function);

            *origFunc = (void*)firstThunk->u1.Function;
            DWORD oldProtect;
            VirtualProtect(&firstThunk->u1.Function, sizeof(void*),
                          PAGE_READWRITE, &oldProtect);
            firstThunk->u1.Function = (ULONG_PTR)hookFunc;
            VirtualProtect(&firstThunk->u1.Function, sizeof(void*),
                          oldProtect, &oldProtect);

            Log("[DIAG] Patched %s: old=%p new=%p", funcName,
                *origFunc, hookFunc);
            return true;
        }
    }
    return false;
}

static void InstallIATHooks(HMODULE realDll) {
    InitializeCriticalSection(&g_handleCs);
    g_trackedCount = 0;

    // Diagnostic: enumerate import DLLs
    LogImportDlls(realDll, "RTK_IO_x64_real");
    HMODULE rtice_diag = GetModuleHandleA("RTICE_SDK_x64.dll");
    if (rtice_diag) LogImportDlls(rtice_diag, "RTICE_SDK_x64");

    // Set up originals from kernel32 before patching any module
    HMODULE k32 = GetModuleHandleA("kernel32.dll");
    pOrigCreateFileW = (decltype(&CreateFileW))GetProcAddress(k32, "CreateFileW");
    pOrigReadFile = (decltype(&ReadFile))GetProcAddress(k32, "ReadFile");
    pOrigWriteFile = (decltype(&WriteFile))GetProcAddress(k32, "WriteFile");
    pOrigCloseHandle = (decltype(&CloseHandle))GetProcAddress(k32, "CloseHandle");

    // Patch RTK_IO_x64_real.dll IAT
    bool ok1 = PatchIAT(realDll, "KERNEL32.dll", "CreateFileW",
                        (void*)HookedCreateFileW, (void**)&pOrigCreateFileW);
    bool ok2 = PatchIAT(realDll, "KERNEL32.dll", "ReadFile",
                        (void*)HookedReadFile, (void**)&pOrigReadFile);
    bool ok3 = PatchIAT(realDll, "KERNEL32.dll", "WriteFile",
                        (void*)HookedWriteFile, (void**)&pOrigWriteFile);
    bool ok4 = PatchIAT(realDll, "KERNEL32.dll", "CloseHandle",
                        (void*)HookedCloseHandle, (void**)&pOrigCloseHandle);

    Log("IAT hooks on RTK_IO_real: CreateFileW=%s ReadFile=%s WriteFile=%s CloseHandle=%s",
        ok1?"YES":"NO", ok2?"YES":"NO", ok3?"YES":"NO", ok4?"YES":"NO");

    // Also patch RTICE_SDK_x64.dll — the actual USB I/O happens there
    HMODULE rtice = GetModuleHandleA("RTICE_SDK_x64.dll");
    if (rtice) {
        void* dummy = nullptr;
        bool r1 = PatchIAT(rtice, "KERNEL32.dll", "CreateFileW",
                           (void*)HookedCreateFileW, &dummy);
        bool r2 = PatchIAT(rtice, "KERNEL32.dll", "ReadFile",
                           (void*)HookedReadFile, &dummy);
        bool r3 = PatchIAT(rtice, "KERNEL32.dll", "WriteFile",
                           (void*)HookedWriteFile, &dummy);
        bool r4 = PatchIAT(rtice, "KERNEL32.dll", "CloseHandle",
                           (void*)HookedCloseHandle, &dummy);
        // Hook GetProcAddress in RTICE_SDK to intercept dynamic resolution
        bool r5 = PatchIAT(rtice, "KERNEL32.dll", "GetProcAddress",
                           (void*)HookedGetProcAddress, (void**)&pOrigGetProcAddress);
        Log("IAT hooks on RTICE_SDK: CreateFileW=%s ReadFile=%s WriteFile=%s CloseHandle=%s GetProcAddress=%s",
            r1?"YES":"NO", r2?"YES":"NO", r3?"YES":"NO", r4?"YES":"NO", r5?"YES":"NO");
    } else {
        Log("RTICE_SDK_x64.dll not found in process — skipping IAT hooks");
    }

    // Also try EGAVDeviceSupport.dll
    HMODULE egavds = GetModuleHandleA("EGAVDeviceSupport.dll");
    if (egavds) {
        void* dummy = nullptr;
        bool e1 = PatchIAT(egavds, "KERNEL32.dll", "CreateFileW",
                           (void*)HookedCreateFileW, &dummy);
        bool e2 = PatchIAT(egavds, "KERNEL32.dll", "ReadFile",
                           (void*)HookedReadFile, &dummy);
        bool e3 = PatchIAT(egavds, "KERNEL32.dll", "WriteFile",
                           (void*)HookedWriteFile, &dummy);
        bool e4 = PatchIAT(egavds, "KERNEL32.dll", "CloseHandle",
                           (void*)HookedCloseHandle, &dummy);
        // Hook GetProcAddress in EGAVDS too
        bool e5 = PatchIAT(egavds, "KERNEL32.dll", "GetProcAddress",
                           (void*)HookedGetProcAddress, &dummy);
        Log("IAT hooks on EGAVDS: CreateFileW=%s ReadFile=%s WriteFile=%s CloseHandle=%s GetProcAddress=%s",
            e1?"YES":"NO", e2?"YES":"NO", e3?"YES":"NO", e4?"YES":"NO", e5?"YES":"NO");
    }

    // Also hook GetProcAddress in RTK_IO_real
    {
        void* dummy = nullptr;
        bool ok5 = PatchIAT(realDll, "KERNEL32.dll", "GetProcAddress",
                            (void*)HookedGetProcAddress, &dummy);
        Log("IAT hooks on RTK_IO_real: GetProcAddress=%s", ok5?"YES":"NO");
    }

    // Set up DeviceIoControl original from kernel32
    pOrigDeviceIoControl = (decltype(&DeviceIoControl))GetProcAddress(k32, "DeviceIoControl");
    if (!pOrigGetProcAddress)
        pOrigGetProcAddress = (decltype(&GetProcAddress))GetProcAddress(k32, "GetProcAddress");
}

static void LogInit() {
    InitializeCriticalSection(&g_cs);
    g_log = fopen("rtk_io_shim.log", "w");
    if (g_log) {
        fprintf(g_log, "=== RTK_IO Shim Log ===\n");
        fflush(g_log);
    }
}

static void Log(const char* fmt, ...) {
    if (!g_log) return;
    EnterCriticalSection(&g_cs);
    LARGE_INTEGER pc, freq;
    QueryPerformanceCounter(&pc);
    QueryPerformanceFrequency(&freq);
    double ms = (double)pc.QuadPart / freq.QuadPart * 1000.0;
    fprintf(g_log, "[%12.3f] ", ms);
    va_list args;
    va_start(args, fmt);
    vfprintf(g_log, fmt, args);
    va_end(args);
    fprintf(g_log, "\n");
    fflush(g_log);
    LeaveCriticalSection(&g_cs);
}

static void LogHex(const char* label, const void* data, int len) {
    if (!g_log || !data || len <= 0) return;
    EnterCriticalSection(&g_cs);
    fprintf(g_log, "  %s (%d bytes): ", label, len);
    const unsigned char* p = (const unsigned char*)data;
    for (int i = 0; i < len && i < 512; i++)
        fprintf(g_log, "%02X ", p[i]);
    if (len > 512) fprintf(g_log, "...");
    fprintf(g_log, "\n");
    fflush(g_log);
    LeaveCriticalSection(&g_cs);
}

static FARPROC GetReal(const char* name) {
    if (!g_realDll) {
        char path[MAX_PATH];
        GetModuleFileNameA(nullptr, path, MAX_PATH);
        char* last = strrchr(path, '\\');
        if (last) strcpy(last + 1, "RTK_IO_x64_real.dll");
        else strcpy(path, "RTK_IO_x64_real.dll");
        g_realDll = LoadLibraryA(path);
        if (!g_realDll) {
            Log("FATAL: Cannot load real DLL from %s, error=%lu", path, GetLastError());
            return nullptr;
        }
        Log("Loaded real DLL: %s", path);
        // Install IAT hooks to capture kernel32 calls from inside RTK_IO
        InstallIATHooks(g_realDll);
    }
    return GetProcAddress(g_realDll, name);
}

// ============================================================
// Generic forwarder: calls real function, returns its result.
// We use a typedef with lots of args; on x64, extra args are harmless
// (caller cleans stack, and register args in rcx/rdx/r8/r9 are always
// passed regardless of actual param count).
// ============================================================

typedef long long (__cdecl *GenericFn)(
    long long a1, long long a2, long long a3, long long a4,
    long long a5, long long a6, long long a7, long long a8,
    long long a9, long long a10, long long a11, long long a12);

typedef long long (__cdecl *RtkNoArgsFn)();
typedef long long (__cdecl *RtkFourArgsFn)(long long a1, long long a2, long long a3, long long a4);
typedef long long (__cdecl *RtkEightArgsFn)(
    long long a1, long long a2, long long a3, long long a4,
    long long a5, long long a6, long long a7, long long a8);
typedef long long (__cdecl *RtkSetCurrentDeviceFn)(const char* deviceName, long long a2);
typedef const char* (__cdecl *RtkGetCurrentDeviceNameFn)();

static bool ReadTruthyEnvironmentFlag(const wchar_t* name) {
    wchar_t value[32] = {};
    DWORD length = GetEnvironmentVariableW(
        name,
        value,
        (DWORD)(sizeof(value) / sizeof(value[0])));
    if (length == 0) {
        return false;
    }

    value[(sizeof(value) / sizeof(value[0])) - 1] = 0;
    return _wcsicmp(value, L"0") != 0 &&
           _wcsicmp(value, L"false") != 0 &&
           _wcsicmp(value, L"no") != 0;
}

static bool AllowUnverifiedAbiForwarding() {
    return ReadTruthyEnvironmentFlag(L"RTK_SHIM_ALLOW_UNVERIFIED_ABI");
}

static bool AllowRepeatedRtkMutations() {
    return ReadTruthyEnvironmentFlag(L"RTK_SHIM_ALLOW_REPEAT_MUTATION");
}

static long long BlockUnverifiedAbiCall(const char* name) {
    Log("blocked unverified ABI call %s; set RTK_SHIM_ALLOW_UNVERIFIED_ABI=1 to forward with the legacy generic ABI", name);
    SetLastError(ERROR_INVALID_FUNCTION);
    return -1;
}

static HANDLE AcquireRtkMutationLock(const char* name) {
    HANDLE lock = CreateMutexW(nullptr, FALSE, L"Local\\Sussudio.RtkIoShim.Mutation.v1");
    if (!lock) {
        Log("%s blocked: mutation mutex create failed, error=%lu", name, GetLastError());
        return nullptr;
    }

    DWORD wait = WaitForSingleObject(lock, 0);
    if (wait != WAIT_OBJECT_0 && wait != WAIT_ABANDONED) {
        Log("%s blocked: RTK mutation already in progress, wait=%lu", name, wait);
        SetLastError(ERROR_BUSY);
        CloseHandle(lock);
        return nullptr;
    }

    return lock;
}

static void ReleaseRtkMutationLock(HANDLE lock) {
    if (!lock) {
        return;
    }

    ReleaseMutex(lock);
    CloseHandle(lock);
}

static bool TryBeginRtkMutationAttempt(const char* name, volatile LONG* attempted) {
    if (AllowRepeatedRtkMutations()) {
        return true;
    }

    if (InterlockedCompareExchange(attempted, 1, 0) == 0) {
        return true;
    }

    Log("%s blocked: repeated RTK mutation call; set RTK_SHIM_ALLOW_REPEAT_MUTATION=1 to allow repeated lab calls", name);
    SetLastError(ERROR_ALREADY_INITIALIZED);
    return false;
}

#define RESOLVE_TYPED_OR_RETURN(name, type, failure) \
    static type real_##name = nullptr; \
    if (!real_##name) real_##name = (type)GetReal(#name); \
    if (!real_##name) { Log("ERROR: " #name " not found"); return failure; }

#define RESOLVE_UNVERIFIED_OR_RETURN(name) \
    static GenericFn real_##name = nullptr; \
    if (!real_##name) real_##name = (GenericFn)GetReal(#name); \
    if (!real_##name) { Log("ERROR: " #name " not found"); return -1; }

#define VERIFIED_FORWARD0(name) \
__declspec(dllexport) long long __cdecl name() \
{ \
    Log(#name "()"); \
    RESOLVE_TYPED_OR_RETURN(name, RtkNoArgsFn, -1); \
    long long r = real_##name(); \
    Log("  -> %lld", r); \
    return r; \
}

#define VERIFIED_FORWARD4(name) \
__declspec(dllexport) long long __cdecl name(long long a1, long long a2, long long a3, long long a4) \
{ \
    Log(#name "(a1=%llX, a2=%llX, a3=%llX, a4=%llX)", a1, a2, a3, a4); \
    RESOLVE_TYPED_OR_RETURN(name, RtkFourArgsFn, -1); \
    long long r = real_##name(a1, a2, a3, a4); \
    Log("  -> %lld", r); \
    return r; \
}

#define UNVERIFIED_FORWARD(name) \
__declspec(dllexport) long long __cdecl name( \
    long long a1, long long a2, long long a3, long long a4, \
    long long a5, long long a6, long long a7, long long a8, \
    long long a9, long long a10, long long a11, long long a12) \
{ \
    if (!AllowUnverifiedAbiForwarding()) return BlockUnverifiedAbiCall(#name); \
    Log(#name "(a1=%llX, a2=%llX, a3=%llX, a4=%llX)", a1, a2, a3, a4); \
    RESOLVE_UNVERIFIED_OR_RETURN(name); \
    long long r = real_##name(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12); \
    Log("  -> %lld", r); \
    return r; \
}

#define MUTATING_UNVERIFIED_FORWARD(name) \
__declspec(dllexport) long long __cdecl name( \
    long long a1, long long a2, long long a3, long long a4, \
    long long a5, long long a6, long long a7, long long a8, \
    long long a9, long long a10, long long a11, long long a12) \
{ \
    static volatile LONG attempted_##name = 0; \
    if (!AllowUnverifiedAbiForwarding()) return BlockUnverifiedAbiCall(#name); \
    HANDLE mutationLock = AcquireRtkMutationLock(#name); \
    if (!mutationLock) return -1; \
    if (!TryBeginRtkMutationAttempt(#name, &attempted_##name)) { ReleaseRtkMutationLock(mutationLock); return -1; } \
    Log(#name "(a1=%llX, a2=%llX, a3=%llX, a4=%llX)", a1, a2, a3, a4); \
    static GenericFn real_##name = nullptr; \
    if (!real_##name) real_##name = (GenericFn)GetReal(#name); \
    if (!real_##name) { Log("ERROR: " #name " not found"); ReleaseRtkMutationLock(mutationLock); return -1; } \
    long long r = real_##name(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12); \
    ReleaseRtkMutationLock(mutationLock); \
    Log("  -> %lld", r); \
    return r; \
}

extern "C" {

// --- Critical interception targets ---

// rtk_sendATCommand — unknown true signature; dump all args and try to find AT frame
__declspec(dllexport) long long __cdecl rtk_sendATCommand(
    long long a1, long long a2, long long a3, long long a4, long long a5,
    long long a6, long long a7, long long a8, long long a9, long long a10, long long a11, long long a12)
{
    if (!AllowUnverifiedAbiForwarding()) return BlockUnverifiedAbiCall("rtk_sendATCommand");

    Log("rtk_sendATCommand(a1=%llX, a2=%llX, a3=%llX, a4=%llX, a5=%llX, a6=%llX, a7=%llX, a8=%llX)",
        a1, a2, a3, a4, a5, a6, a7, a8);

    // Try to dump any arg that looks like a pointer to a buffer
    for (int i = 0; i < 8; i++) {
        long long v = (&a1)[i];
        if (v > 0x10000 && v < 0x7FFFFFFFFFFF) {
            __try {
                char label[32];
                sprintf(label, "a%d as bytes", i+1);
                LogHex(label, (const void*)v, 64);
            } __except(1) {}
        }
    }

    RESOLVE_UNVERIFIED_OR_RETURN(rtk_sendATCommand);
    long long r = real_rtk_sendATCommand(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);

    Log("  -> %lld (0x%llX)", r, r);

    // After call, try to dump response buffers
    for (int i = 0; i < 8; i++) {
        long long v = (&a1)[i];
        if (v > 0x10000 && v < 0x7FFFFFFFFFFF) {
            __try {
                char label[32];
                sprintf(label, "a%d AFTER", i+1);
                LogHex(label, (const void*)v, 64);
            } __except(1) {}
        }
    }

    return r;
}

// rtk_sendI2CATCommand — verified against NativeXuAudioProbe P/Invoke declarations.
__declspec(dllexport) long long __cdecl rtk_sendI2CATCommand(
    long long a1, long long a2, long long a3, long long a4,
    long long a5, long long a6, long long a7, long long a8)
{
    Log("rtk_sendI2CATCommand(a1=%llX, a2=%llX, a3=%llX, a4=%llX, a5=%llX, a6=%llX, a7=%llX, a8=%llX)",
        a1, a2, a3, a4, a5, a6, a7, a8);

    // Dump any pointer-like args
    for (int i = 0; i < 8; i++) {
        long long v = (&a1)[i];
        if (v > 0x10000 && v < 0x7FFFFFFFFFFF) {
            __try {
                char label[32];
                sprintf(label, "a%d as bytes", i+1);
                LogHex(label, (const void*)v, 64);
            } __except(1) {}
        }
    }

    RESOLVE_TYPED_OR_RETURN(rtk_sendI2CATCommand, RtkEightArgsFn, -1);
    long long r = real_rtk_sendI2CATCommand(a1, a2, a3, a4, a5, a6, a7, a8);

    Log("  -> %lld (0x%llX)", r, r);

    // Dump after
    for (int i = 0; i < 8; i++) {
        long long v = (&a1)[i];
        if (v > 0x10000 && v < 0x7FFFFFFFFFFF) {
            __try {
                char label[32];
                sprintf(label, "a%d AFTER", i+1);
                LogHex(label, (const void*)v, 64);
            } __except(1) {}
        }
    }

    return r;
}

// rtk_setUVCExtension(handle, selector, data, len)
__declspec(dllexport) long long __cdecl rtk_setUVCExtension(
    long long a1, long long a2, long long a3, long long a4)
{
    Log("rtk_setUVCExtension(a1=%llX, a2=%llX, a3=%llX, a4=%lld)",
        a1, a2, a3, a4);
    // Try to log data if a3 looks like a pointer and a4 looks like a length
    if (a3 > 0x10000 && a4 > 0 && a4 < 1024)
        LogHex("data", (const void*)a3, (int)a4);

    RESOLVE_TYPED_OR_RETURN(rtk_setUVCExtension, RtkFourArgsFn, -1);
    long long r = real_rtk_setUVCExtension(a1, a2, a3, a4);
    Log("  -> %lld", r);
    return r;
}

// rtk_openPort
__declspec(dllexport) long long __cdecl rtk_openPort(
    long long a1, long long a2, long long a3, long long a4)
{
    Log("rtk_openPort(a1=%llX, a2=%llX, a3=%llX, a4=%llX)", a1, a2, a3, a4);
    // a1 or a2 might be a string (device path)
    if (a1 > 0x10000) {
        __try { Log("  a1 as string: %s", (const char*)a1); } __except(1) {}
    }
    if (a2 > 0x10000) {
        __try { Log("  a2 as string: %s", (const char*)a2); } __except(1) {}
    }

    RESOLVE_TYPED_OR_RETURN(rtk_openPort, RtkFourArgsFn, -1);
    long long r = real_rtk_openPort(a1, a2, a3, a4);
    Log("  -> %lld", r);
    return r;
}

// --- Verified simple forwarders used by NativeXuAudioProbe ---

VERIFIED_FORWARD4(rtk_initialize)
VERIFIED_FORWARD0(rtk_uninitialize)
VERIFIED_FORWARD0(rtk_closePort)
VERIFIED_FORWARD4(rtk_isOpen)

// --- Unverified exports fail closed unless explicitly enabled for lab use ---

UNVERIFIED_FORWARD(rtk_uninitialize_ex)
UNVERIFIED_FORWARD(rtk_setDevice)
UNVERIFIED_FORWARD(rtk_enableLog)
UNVERIFIED_FORWARD(rtk_readRbus)
MUTATING_UNVERIFIED_FORWARD(rtk_writeRbus)
MUTATING_UNVERIFIED_FORWARD(rtk_enterDebugMode)
MUTATING_UNVERIFIED_FORWARD(rtk_exitDebugMode)
UNVERIFIED_FORWARD(rtk_rescueReadRbus)
MUTATING_UNVERIFIED_FORWARD(rtk_rescueWriteRbus)
MUTATING_UNVERIFIED_FORWARD(rtk_burnDPEDID)
MUTATING_UNVERIFIED_FORWARD(rtk_burnEDID)
MUTATING_UNVERIFIED_FORWARD(rtk_burnHDCP)
MUTATING_UNVERIFIED_FORWARD(rtk_burnMultiFiles)
MUTATING_UNVERIFIED_FORWARD(rtk_burnToFlash)
MUTATING_UNVERIFIED_FORWARD(rtk_burnToFlashWithLog)
MUTATING_UNVERIFIED_FORWARD(rtk_burnUSBDesription)
UNVERIFIED_FORWARD(rtk_readMultiFiles)
UNVERIFIED_FORWARD(rtk_get_HDCP_Version_ByFile)
UNVERIFIED_FORWARD(rtk_setBeforeBurnCallBack)

// setCurrentDevice — log the name string
__declspec(dllexport) long long __cdecl rtk_setCurrentDevice(
    const char* deviceName, long long a2)
{
    Log("rtk_setCurrentDevice(deviceName=%p, a2=%llX)", deviceName, a2);
    if (deviceName) {
        __try { Log("  deviceName: %s", deviceName); } __except(1) {}
    }

    RESOLVE_TYPED_OR_RETURN(rtk_setCurrentDevice, RtkSetCurrentDeviceFn, -1);
    long long r = real_rtk_setCurrentDevice(deviceName, a2);
    Log("  -> %lld", r);
    return r;
}

// getCurrentDeviceName — returns a string
__declspec(dllexport) const char* __cdecl rtk_getCurrentDeviceName()
{
    RESOLVE_TYPED_OR_RETURN(rtk_getCurrentDeviceName, RtkGetCurrentDeviceNameFn, nullptr);
    const char* r = real_rtk_getCurrentDeviceName();
    if (r) {
        __try { Log("rtk_getCurrentDeviceName -> %s", r); } __except(1) { Log("rtk_getCurrentDeviceName -> %p", r); }
    } else {
        Log("rtk_getCurrentDeviceName -> null");
    }
    return r;
}

UNVERIFIED_FORWARD(rtk_Get_Customer_version)

} // extern "C"

BOOL APIENTRY DllMain(HMODULE hModule, DWORD reason, LPVOID lpReserved) {
    switch (reason) {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);
        LogInit();
        Log("RTK_IO shim loaded (DLL_PROCESS_ATTACH)");
        break;
    case DLL_PROCESS_DETACH:
        Log("RTK_IO shim unloading");
        if (g_realDll) FreeLibrary(g_realDll);
        if (g_log) fclose(g_log);
        DeleteCriticalSection(&g_cs);
        break;
    }
    return TRUE;
}
