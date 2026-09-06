using System;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Sussudio.Services.Flashback;

/// <summary>
/// Owns the temporary-file lifetime for one Flashback export. A reserved path is
/// identity-checked before cleanup or publication so a replacement is never deleted
/// or promoted by this transaction.
/// </summary>
internal sealed class FlashbackExportOutputTransaction : IDisposable
{
    private delegate bool CompletedOutputValidator(string outputPath, out long outputBytes, out string failureMessage);

    private const int TempOutputCreationAttempts = 16;
    private const int ErrorFileNotFound = 2;
    private const int ErrorPathNotFound = 3;
    private const int ErrorSharingViolation = 32;
    private const int ErrorFileExists = 80;
    private const int ErrorAlreadyExists = 183;
    private const uint NativeFileReadAttributes = 0x00000080;
    private const uint NativeDeleteAccess = 0x00010000;
    private const uint NativeFileShareRead = 0x00000001;
    private const uint NativeFileShareWrite = 0x00000002;
    private const uint NativeOpenExisting = 3;
    private const uint NativeFileAttributeNormal = 0x00000080;
    private static readonly TimeSpan OrphanTempFileMinimumAge = TimeSpan.FromMinutes(15);

    private FileStream? _reservationStream;

    private FlashbackExportOutputTransaction(
        string temporaryPath,
        TempFileIdentity identity,
        FileStream reservationStream)
    {
        TemporaryPath = temporaryPath;
        _identity = identity;
        _reservationStream = reservationStream;
    }

    private readonly TempFileIdentity _identity;

    internal string TemporaryPath { get; }

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle file,
        out ByHandleFileInformation fileInformation);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetFileInformationByHandle(
        SafeFileHandle file,
        FileInformationClass fileInformationClass,
        IntPtr fileInformation,
        uint bufferSize);

    internal static bool TryReserve(
        string outputPath,
        [NotNullWhen(true)] out FlashbackExportOutputTransaction? transaction,
        out string failureMessage,
        out string failureCode)
    {
        transaction = null;
        failureCode = FlashbackExportFailureCodes.OutputWriteFailed;
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            failureMessage = $"Flashback export failed: output directory does not exist for '{outputPath}'.";
            failureCode = FlashbackExportFailureCodes.InvalidOutputPath;
            return false;
        }

        var outputBaseName = Path.GetFileNameWithoutExtension(outputPath);
        if (string.IsNullOrWhiteSpace(outputBaseName))
        {
            outputBaseName = "flashback_export";
        }

        if (outputBaseName.Length > 80)
        {
            outputBaseName = outputBaseName[..80];
        }

        for (var attempt = 0; attempt < TempOutputCreationAttempts; attempt++)
        {
            var candidate = Path.Combine(
                outputDirectory,
                $"{outputBaseName}.{Guid.NewGuid():N}.mp4.tmp");
            if (Directory.Exists(candidate))
            {
                continue;
            }

            try
            {
                var reservationStream = new FileStream(
                    candidate,
                    FileMode.CreateNew,
                    FileAccess.ReadWrite,
                    FileShare.ReadWrite);

                if (!TryGetFileIdentity(reservationStream.SafeFileHandle, out var identity, out var identityFailure, out var identityError))
                {
                    reservationStream.Dispose();
                    failureMessage = $"Flashback export failed: could not identify temporary output file before writing '{outputPath}'.";
                    Logger.Log($"FLASHBACK_EXPORT_TMP_IDENTITY_WARN path='{candidate}' win32={identityError} msg='{identityFailure}'");
                    return false;
                }

                transaction = new FlashbackExportOutputTransaction(candidate, identity, reservationStream);
                failureMessage = string.Empty;
                failureCode = string.Empty;
                return true;
            }
            catch (IOException ex)
            {
                Logger.Log($"FLASHBACK_EXPORT_TMP_CREATE_RETRY path='{candidate}' attempt={attempt + 1} type={ex.GetType().Name} msg='{ex.Message}'");
            }
            catch (Exception ex)
            {
                failureMessage = $"Flashback export failed: could not create temporary output file before writing '{outputPath}'.";
                Logger.Log($"FLASHBACK_EXPORT_TMP_CREATE_WARN path='{candidate}' type={ex.GetType().Name} msg='{ex.Message}'");
                return false;
            }
        }

        failureMessage = $"Flashback export failed: could not create a unique temporary output file before writing '{outputPath}'.";
        return false;
    }

    internal bool TryPublish(string outputPath, out long outputBytes, out string failureMessage, out string failureCode)
        => TryPublishCore(outputPath, out outputBytes, out failureMessage, out failureCode, TryValidateCompletedOutputFile);

    private bool TryPublishCore(
        string outputPath,
        out long outputBytes,
        out string failureMessage,
        out string failureCode,
        CompletedOutputValidator validateOutput)
    {
        failureCode = FlashbackExportFailureCodes.OutputWriteFailed;
        if (!validateOutput(TemporaryPath, out outputBytes, out _))
        {
            failureMessage = outputBytes == 0
                ? $"Flashback export failed: temporary output file is empty before replacing '{outputPath}'."
                : $"Flashback export failed: temporary output file length unavailable before replacing '{outputPath}'.";
            failureCode = outputBytes == 0
                ? FlashbackExportFailureCodes.NoMediaWritten
                : FlashbackExportFailureCodes.OutputWriteFailed;
            Abandon();
            return false;
        }

        try
        {
            if (!TryMoveTempFileToOutputPath(outputPath, out failureMessage, out failureCode))
            {
                Abandon();
                return false;
            }
        }
        catch (IOException ex)
        {
            failureMessage = ex.Message;
            failureCode = FlashbackExportFailureCodes.OutputWriteFailed;
            return false;
        }

        if (!validateOutput(outputPath, out outputBytes, out failureMessage))
        {
            failureCode = outputBytes == 0
                ? FlashbackExportFailureCodes.NoMediaWritten
                : FlashbackExportFailureCodes.OutputWriteFailed;
            Logger.Log($"FLASHBACK_EXPORT_FINAL_OUTPUT_VALIDATE_WARN path='{outputPath}' reason='{failureMessage}'");
            return false;
        }

        failureCode = string.Empty;
        return true;
    }

    internal static void CleanupOrphanedTempFiles(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return;

        try
        {
            var nowUtc = DateTime.UtcNow;
            foreach (var tmpFile in Directory.EnumerateFiles(directory, "*.mp4.tmp"))
            {
                try
                {
                    if (!CanDeleteOrphanedTempFile(tmpFile, nowUtc))
                    {
                        Logger.Log($"FLASHBACK_EXPORT_ORPHAN_CLEANUP_SKIP file='{Path.GetFileName(tmpFile)}' reason=active_or_recent");
                        continue;
                    }

                    File.Delete(tmpFile);
                    Logger.Log($"FLASHBACK_EXPORT_ORPHAN_CLEANUP deleted='{Path.GetFileName(tmpFile)}'");
                }
                catch (Exception ex)
                {
                    Logger.Log($"FLASHBACK_EXPORT_ORPHAN_CLEANUP_FAIL path='{Path.GetFileName(tmpFile)}' type={ex.GetType().Name} msg='{ex.Message}'");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_ORPHAN_SCAN_FAIL dir='{directory}' type={ex.GetType().Name} msg='{ex.Message}'");
        }
    }

    private void Abandon()
    {
        const int MaxRetries = 3;
        const int RetryDelayMs = 200;

        for (var attempt = 0; attempt <= MaxRetries; attempt++)
        {
            if (!File.Exists(TemporaryPath))
            {
                ReleaseReservation();
                return;
            }

            if (TryDeleteTempFile(out var failureMessage, out var lastError))
            {
                Logger.Log($"FLASHBACK_EXPORT_TMP_DELETE path='{TemporaryPath}'");
                return;
            }

            if (lastError == ErrorSharingViolation && attempt < MaxRetries)
            {
                Thread.Sleep(RetryDelayMs);
                continue;
            }

            Logger.Log(
                $"FLASHBACK_EXPORT_WARN reason='delete_tmp_failed' path='{TemporaryPath}' " +
                $"win32={lastError} msg='{failureMessage}'");
            return;
        }

        Logger.Log($"FLASHBACK_EXPORT_WARN reason='delete_tmp_failed_sharing_violation' path='{TemporaryPath}'");
    }

    public void Dispose()
        => Abandon();

    private void ReleaseReservation()
    {
        var stream = _reservationStream;
        if (stream == null)
        {
            return;
        }

        _reservationStream = null;
        stream.Dispose();
    }

    private bool TryOpenVerifiedHandle(
        out SafeFileHandle? handle,
        out string failureMessage,
        out int lastError)
    {
        ReleaseReservation();
        handle = CreateFile(
            TemporaryPath,
            NativeDeleteAccess | NativeFileReadAttributes,
            NativeFileShareRead | NativeFileShareWrite,
            IntPtr.Zero,
            NativeOpenExisting,
            NativeFileAttributeNormal,
            IntPtr.Zero);
        if (handle.IsInvalid)
        {
            lastError = Marshal.GetLastWin32Error();
            handle.Dispose();
            handle = null;
            failureMessage = IsPathMissingError(lastError)
                ? string.Empty
                : $"Flashback export failed: could not lock temporary output file '{TemporaryPath}' safely (Win32 error {lastError}).";
            return false;
        }

        if (!TryGetFileIdentity(handle, out var currentIdentity, out var identityFailure, out var identityError))
        {
            lastError = identityError;
            handle.Dispose();
            handle = null;
            failureMessage = $"Flashback export failed: could not verify temporary output file identity for '{TemporaryPath}' ({identityFailure}).";
            return false;
        }

        if (currentIdentity != _identity)
        {
            lastError = 0;
            handle.Dispose();
            handle = null;
            failureMessage = $"Flashback export failed: temporary output path was replaced before finalizing '{TemporaryPath}'.";
            return false;
        }

        lastError = 0;
        failureMessage = string.Empty;
        return true;
    }

    private bool TryDeleteTempFile(out string failureMessage, out int lastError)
    {
        if (!TryOpenVerifiedHandle(out var handle, out failureMessage, out lastError))
        {
            return IsPathMissingError(lastError);
        }

        using (handle)
        {
            var disposition = Marshal.AllocHGlobal(1);
            try
            {
                Marshal.WriteByte(disposition, 1);
                if (SetFileInformationByHandle(handle!, FileInformationClass.FileDispositionInfo, disposition, 1))
                {
                    failureMessage = string.Empty;
                    lastError = 0;
                    return true;
                }

                lastError = Marshal.GetLastWin32Error();
                failureMessage = $"SetFileInformationByHandle(FileDispositionInfo) failed with Win32 error {lastError}";
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(disposition);
            }
        }
    }

    private bool TryMoveTempFileToOutputPath(string outputPath, out string failureMessage, out string failureCode)
    {
        failureCode = FlashbackExportFailureCodes.OutputWriteFailed;
        if (!File.Exists(TemporaryPath))
        {
            failureCode = FlashbackExportFailureCodes.OutputWriteFailed;
            failureMessage = $"Temporary export file was not created: '{TemporaryPath}'.";
            return false;
        }

        if (!TryOpenVerifiedHandle(out var handle, out var ownershipFailure, out var lastError))
        {
            failureCode = string.IsNullOrWhiteSpace(ownershipFailure)
                ? FlashbackExportFailureCodes.OutputWriteFailed
                : FlashbackExportFailureCodes.Failed;
            failureMessage = string.IsNullOrWhiteSpace(ownershipFailure)
                ? $"Temporary export file was not created: '{TemporaryPath}'."
                : ownershipFailure;
            return false;
        }

        using (handle)
        {
            if (File.Exists(outputPath) || Directory.Exists(outputPath))
            {
                failureCode = FlashbackExportFailureCodes.InvalidOutputPath;
                failureMessage = CreateDestinationExistsMessage(outputPath);
                return false;
            }

            if (TryRenameTempHandle(handle!, outputPath, out var renameFailure, out lastError))
            {
                failureMessage = string.Empty;
                failureCode = string.Empty;
                return true;
            }

            var destinationExists = IsDestinationExistsError(lastError) || File.Exists(outputPath) || Directory.Exists(outputPath);
            failureMessage =
                destinationExists
                    ? CreateDestinationExistsMessage(outputPath)
                    : $"Flashback export failed: could not move temporary output file to '{outputPath}' safely ({renameFailure}).";
            failureCode = destinationExists
                ? FlashbackExportFailureCodes.InvalidOutputPath
                : FlashbackExportFailureCodes.OutputWriteFailed;
            return false;
        }
    }

    private static bool TryRenameTempHandle(
        SafeFileHandle handle,
        string outputPath,
        out string failureMessage,
        out int lastError)
    {
        var renameInfo = CreateFileRenameInfo(outputPath, replaceIfExists: false, out var renameInfoSize);
        try
        {
            if (SetFileInformationByHandle(handle, FileInformationClass.FileRenameInfo, renameInfo, (uint)renameInfoSize))
            {
                failureMessage = string.Empty;
                lastError = 0;
                return true;
            }

            lastError = Marshal.GetLastWin32Error();
            failureMessage = $"SetFileInformationByHandle(FileRenameInfo) failed with Win32 error {lastError}";
            return false;
        }
        finally
        {
            Marshal.FreeHGlobal(renameInfo);
        }
    }

    private static IntPtr CreateFileRenameInfo(string outputPath, bool replaceIfExists, out int bufferSize)
    {
        var fullOutputPath = Path.GetFullPath(outputPath);
        var outputPathBytes = Encoding.Unicode.GetBytes(fullOutputPath);
        var rootDirectoryOffset = IntPtr.Size == 8 ? 8 : 4;
        var fileNameLengthOffset = rootDirectoryOffset + IntPtr.Size;
        var fileNameOffset = fileNameLengthOffset + sizeof(int);
        bufferSize = fileNameOffset + outputPathBytes.Length + sizeof(char);

        var buffer = Marshal.AllocHGlobal(bufferSize);
        for (var i = 0; i < bufferSize; i++)
        {
            Marshal.WriteByte(buffer, i, 0);
        }

        Marshal.WriteInt32(buffer, 0, replaceIfExists ? 1 : 0);
        Marshal.WriteIntPtr(buffer, rootDirectoryOffset, IntPtr.Zero);
        Marshal.WriteInt32(buffer, fileNameLengthOffset, outputPathBytes.Length);
        Marshal.Copy(outputPathBytes, 0, IntPtr.Add(buffer, fileNameOffset), outputPathBytes.Length);
        return buffer;
    }

    private static bool TryGetFileIdentity(
        SafeFileHandle handle,
        out TempFileIdentity identity,
        out string failureMessage,
        out int lastError)
    {
        identity = default;
        if (handle.IsInvalid || handle.IsClosed)
        {
            lastError = 0;
            failureMessage = "invalid file handle";
            return false;
        }

        if (!GetFileInformationByHandle(handle, out var info))
        {
            lastError = Marshal.GetLastWin32Error();
            failureMessage = $"GetFileInformationByHandle failed with Win32 error {lastError}";
            return false;
        }

        identity = TempFileIdentity.From(in info);
        lastError = 0;
        failureMessage = string.Empty;
        return true;
    }

    private static bool TryValidateCompletedOutputFile(string outputPath, out long outputBytes, out string failureMessage)
    {
        outputBytes = GetFileLengthBestEffort(outputPath);
        if (outputBytes > 0)
        {
            failureMessage = string.Empty;
            return true;
        }

        failureMessage = outputBytes == 0
            ? $"Flashback export failed: output file is empty '{outputPath}'."
            : $"Flashback export failed: output file length unavailable '{outputPath}'.";
        return false;
    }

    private static long GetFileLengthBestEffort(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (Exception ex)
        {
            Logger.Log($"FLASHBACK_EXPORT_WARN reason='output_length_unavailable' path='{path}' type={ex.GetType().Name} msg='{ex.Message}'");
            return -1;
        }
    }

    private static bool CanDeleteOrphanedTempFile(string tmpFile, DateTime nowUtc)
    {
        var lastWriteUtc = File.GetLastWriteTimeUtc(tmpFile);
        if (lastWriteUtc == DateTime.MinValue || nowUtc - lastWriteUtc < OrphanTempFileMinimumAge)
        {
            return false;
        }

        try
        {
            using var stream = new FileStream(tmpFile, FileMode.Open, FileAccess.Read, FileShare.None);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static bool IsPathMissingError(int lastError)
        => lastError == ErrorFileNotFound || lastError == ErrorPathNotFound;

    private static bool IsDestinationExistsError(int lastError)
        => lastError == ErrorFileExists || lastError == ErrorAlreadyExists;

    internal static string CreateDestinationExistsMessage(string outputPath)
        => $"Flashback export failed: destination file already exists at '{outputPath}'. Choose a path that does not exist; Flashback export does not overwrite existing files.";

    private enum FileInformationClass
    {
        FileRenameInfo = 3,
        FileDispositionInfo = 4
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public long CreationTime;
        public long LastAccessTime;
        public long LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    private readonly record struct TempFileIdentity(
        uint VolumeSerialNumber,
        uint FileIndexHigh,
        uint FileIndexLow)
    {
        public static TempFileIdentity From(in ByHandleFileInformation info)
            => new(info.VolumeSerialNumber, info.FileIndexHigh, info.FileIndexLow);
    }
}
