using Asher.Services.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Asher.Services.Implementations
{
    public class DllInjectorService : IDllInjectorService
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);


        private const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint MEM_COMMIT = 0x1000;
        private const uint MEM_RESERVE = 0x2000;
        private const uint PAGE_READWRITE = 0x04;
        private const uint INFINITE = 0xFFFFFFFF;

        public bool InjectDll(Process process, string dllPath)
        {
            if (process == null || process.HasExited)
                return false;

            if (!File.Exists(dllPath))
                return false;

            IntPtr hProcess = IntPtr.Zero;
            IntPtr hThread = IntPtr.Zero;
            IntPtr allocMemAddress = IntPtr.Zero;

            try
            {
                // Ensure we have a fresh process handle
                process.Refresh();
                if (process.HasExited)
                    return false;

                // Open the target process
                hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, process.Id);
                if (hProcess == IntPtr.Zero)
                    return false;

                // Get the address of LoadLibraryW (Unicode version for better .NET Framework compatibility)
                IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
                if (loadLibraryAddr == IntPtr.Zero)
                    return false;

                // Allocate memory in the target process
                // Need to include null terminator for Unicode strings
                byte[] dllPathBytes = Encoding.Unicode.GetBytes(dllPath + "\0");
                uint allocSize = (uint)dllPathBytes.Length;
                allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, allocSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

                if (allocMemAddress == IntPtr.Zero)
                    return false;

                // Write the DLL path to the allocated memory
                if (!WriteProcessMemory(hProcess, allocMemAddress, dllPathBytes, allocSize, out UIntPtr bytesWritten))
                    return false;

                // Create a remote thread to call LoadLibraryW
                hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, out IntPtr threadId);

                if (hThread == IntPtr.Zero)
                    return false;

                // Wait for the thread to complete
                WaitForSingleObject(hThread, INFINITE);

                // For .NET Framework managed DLLs, LoadLibrary loads the assembly but
                // static constructors are only called when the type is first accessed.
                // We need to ensure the Bootstrap type is accessed to trigger initialization.
                // Wait a bit for the DLL to fully load, then try to trigger initialization
                System.Threading.Thread.Sleep(1000);
                TryTriggerManagedInitialization(process, dllPath);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                if (hThread != IntPtr.Zero)
                    CloseHandle(hThread);
                if (allocMemAddress != IntPtr.Zero)
                    VirtualAllocEx(hProcess, allocMemAddress, 0, 0x8000, 0x40); // MEM_RELEASE
                if (hProcess != IntPtr.Zero)
                    CloseHandle(hProcess);
            }
        }

        public bool CopyFilesToGameFolder(string gameFolder, string[] sourceFiles, string[] targetNames)
        {
            if (string.IsNullOrEmpty(gameFolder) || !Directory.Exists(gameFolder))
                return false;

            if (sourceFiles == null || targetNames == null || sourceFiles.Length != targetNames.Length)
                return false;

            try
            {
                for (int i = 0; i < sourceFiles.Length; i++)
                {
                    if (string.IsNullOrEmpty(sourceFiles[i]) || !File.Exists(sourceFiles[i]))
                        continue;

                    string targetPath = Path.Combine(gameFolder, targetNames[i]);
                    File.Copy(sourceFiles[i], targetPath, overwrite: true);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void TryTriggerManagedInitialization(Process process, string dllPath)
        {
            // For .NET Framework managed DLLs, LoadLibrary loads the assembly but static constructors
            // are only called when the type is first accessed.
            //
            // SOLUTION: We need to use CLR hosting to call Bootstrap.EntryPoint() in the target process.
            // However, CLR hosting APIs work within the same process, not across process boundaries.
            //
            // The proper solution is to create a native proxy DLL (C++ DLL with DllMain) that:
            // 1. Gets injected via LoadLibrary (DllMain runs automatically)
            // 2. In DllMain, uses CLR hosting APIs to get ICLRRuntimeHost
            // 3. Calls ExecuteInDefaultAppDomain to call Bootstrap.EntryPoint()
            //
            // For now, we rely on the Bootstrap's static constructor running when the type is accessed.
            // This requires the game to access a type from the Bootstrap assembly, which may not happen.
            //
            // TODO: Implement native proxy DLL solution for guaranteed initialization.
        }
    }
}

