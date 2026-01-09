using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace AstroModIntegrator
{
    public static class IntegrityLevelChecker
    {
        private const int TokenIntegrityLevel = 25;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(
            IntPtr ProcessHandle,
            uint DesiredAccess,
            out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(
            IntPtr TokenHandle,
            int TokenInformationClass,
            IntPtr TokenInformation,
            int TokenInformationLength,
            out int ReturnLength);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        private const int SECURITY_MANDATORY_LOW_RID = 0x1000;

        public static bool IsCurrentProcessLowIntegrity()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;

            IntPtr token = IntPtr.Zero;

            if (!OpenProcessToken(
                    Process.GetCurrentProcess().Handle,
                    0x0008 /* TOKEN_QUERY */,
                    out token))
            {
                throw new System.ComponentModel.Win32Exception();
            }

            try
            {
                GetTokenInformation(token, TokenIntegrityLevel, IntPtr.Zero, 0, out int size);

                IntPtr buffer = Marshal.AllocHGlobal(size);
                try
                {
                    if (!GetTokenInformation(token, TokenIntegrityLevel, buffer, size, out _)) throw new System.ComponentModel.Win32Exception();

                    IntPtr sidPtr = Marshal.ReadIntPtr(buffer);
                    var sid = new SecurityIdentifier(sidPtr);
                    int integrityRid = int.Parse(sid.Value.Split('-')[^1]);

                    return integrityRid == SECURITY_MANDATORY_LOW_RID;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            finally
            {
                CloseHandle(token);
            }
        }
    }
}
