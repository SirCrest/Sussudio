using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using Sussudio.Tools;

namespace Sussudio.Services.Automation;

public sealed partial class NamedPipeAutomationServer
{
    private const uint PipeAccessDuplex = 0x00000003;
    private const uint FileFlagOverlapped = 0x40000000;
    private const uint PipeTypeByte = 0x00000000;
    private const uint PipeReadModeByte = 0x00000000;
    private const uint PipeWait = 0x00000000;
    private const uint PipeUnlimitedInstances = 255;

    private NamedPipeServerStream CreateServerStream()
    {
        if (_pipeSecurityDescriptor != null && OperatingSystem.IsWindows() && !_explicitSecurityFailed)
        {
            try
            {
                return _secureServerStreamFactory(_pipeSecurityDescriptor);
            }
            catch (Exception ex)
            {
                _explicitSecurityFailed = true;
                if (!_authTokenRequired)
                {
                    throw new AutomationPipeSecurityException(
                        "Explicit Windows pipe security failed and no automation token is configured.",
                        ex);
                }

                Logger.Log($"Automation pipe explicit security fallback to token-required default security: {ex.Message}");
            }
        }

        if (IsDefaultSecurityDisallowed())
        {
            throw new AutomationPipeSecurityException(
                "Default Windows pipe security is disabled unless automation token auth is required.");
        }

        return _defaultServerStreamFactory();
    }

    private NamedPipeServerStream CreateDefaultServerStream()
    {
        return new NamedPipeServerStream(
            _pipeName,
            PipeDirection.InOut,
            NamedPipeServerStream.MaxAllowedServerInstances,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous);
    }

    private bool IsDefaultSecurityDisallowed()
        => AutomationPipeSecurityPolicy.ShouldDisableDefaultSecurityFallback(
            OperatingSystem.IsWindows(),
            _pipeSecurityDescriptor != null,
            _explicitSecurityFailed,
            _authTokenRequired);

    private NamedPipeServerStream CreateServerStreamWithSecurityDescriptor(byte[] securityDescriptor)
    {
        IntPtr securityDescriptorPtr = IntPtr.Zero;
        try
        {
            securityDescriptorPtr = Marshal.AllocHGlobal(securityDescriptor.Length);
            Marshal.Copy(securityDescriptor, 0, securityDescriptorPtr, securityDescriptor.Length);

            var securityAttributes = new SECURITY_ATTRIBUTES
            {
                nLength = Marshal.SizeOf<SECURITY_ATTRIBUTES>(),
                lpSecurityDescriptor = securityDescriptorPtr,
                bInheritHandle = 0
            };

            var fullPipeName = $@"\\.\pipe\{_pipeName}";
            var pipeHandle = CreateNamedPipe(
                fullPipeName,
                PipeAccessDuplex | FileFlagOverlapped,
                PipeTypeByte | PipeReadModeByte | PipeWait,
                PipeUnlimitedInstances,
                0,
                0,
                0,
                ref securityAttributes);

            if (pipeHandle.IsInvalid)
            {
                var error = Marshal.GetLastWin32Error();
                pipeHandle.Dispose();
                throw new IOException($"CreateNamedPipe failed with Win32 error {error}.");
            }

            return new NamedPipeServerStream(PipeDirection.InOut, isAsync: true, isConnected: false, pipeHandle);
        }
        finally
        {
            if (securityDescriptorPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(securityDescriptorPtr);
            }
        }
    }

    private static (byte[]? SecurityDescriptor, string Mode) CreatePipeSecurityDescriptor()
    {
        if (!OperatingSystem.IsWindows())
        {
            return (null, "default-nonwindows");
        }

        try
        {
            var currentIdentity = WindowsIdentity.GetCurrent();
            var currentUserSid = currentIdentity.User;
            if (currentUserSid == null)
            {
                return (null, "default-security-no-user-sid");
            }

            var security = new PipeSecurity();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

            // Restrict access to this user plus trusted local administrators/system.
            security.AddAccessRule(new PipeAccessRule(
                currentUserSid,
                PipeAccessRights.FullControl,
                AccessControlType.Allow));
            security.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                PipeAccessRights.FullControl,
                AccessControlType.Allow));
            security.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                PipeAccessRights.FullControl,
                AccessControlType.Allow));

            // Keep the automation pipe on explicit Windows security, but avoid adding a
            // mandatory integrity SACL. Creating named objects with a SACL requires
            // SeSecurityPrivilege on some systems; without it CreateNamedPipe fails
            // with ERROR_PRIVILEGE_NOT_HELD and disables MCP/ssctl entirely.
            return (security.GetSecurityDescriptorBinaryForm(), $"explicit-security-user+admins+system ({currentUserSid.Value})");
        }
        catch (Exception ex)
        {
            Logger.Log($"Automation pipe security setup fallback: {ex.Message}");
            return (null, $"default-security-fallback ({ex.GetType().Name})");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNamedPipeClientProcessId(IntPtr hPipe, out uint ClientProcessId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafePipeHandle CreateNamedPipe(
        string lpName,
        uint dwOpenMode,
        uint dwPipeMode,
        uint nMaxInstances,
        uint nOutBufferSize,
        uint nInBufferSize,
        uint nDefaultTimeOut,
        ref SECURITY_ATTRIBUTES lpSecurityAttributes);

    private sealed class AutomationPipeSecurityException : Exception
    {
        public AutomationPipeSecurityException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
