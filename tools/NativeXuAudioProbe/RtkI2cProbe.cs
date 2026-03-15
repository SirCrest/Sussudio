using System.Runtime.InteropServices;

/// <summary>
/// Direct P/Invoke probe for RTK_IO_x64.dll's rtk_sendI2CATCommand.
/// Uses the real DLL to test I2C AT audio switching without RTICE_SDK.
/// </summary>
static class RtkI2cProbe
{
    // RTK_IO DLL path — resolved relative to the probe's output dir
    private const string RtkIoDll = "RTK_IO_x64.dll";

    // P/Invoke declarations for RTK_IO_x64.dll
    // Signatures inferred from shim log analysis.
    // On x64 Windows, all calling conventions collapse to Microsoft x64 ABI.

    [DllImport(RtkIoDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern long rtk_initialize(long a1, long a2, long a3, long a4);

    [DllImport(RtkIoDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern long rtk_uninitialize();

    [DllImport(RtkIoDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern long rtk_setUVCExtension(long a1, long a2, long a3, long a4);

    [DllImport(RtkIoDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern long rtk_setCurrentDevice([MarshalAs(UnmanagedType.LPStr)] string deviceName, IntPtr a2);

    [DllImport(RtkIoDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern long rtk_openPort(
        long a1, long a2, long a3, long a4);

    [DllImport(RtkIoDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern long rtk_closePort();

    [DllImport(RtkIoDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern long rtk_isOpen(long a1, long a2, long a3, long a4);

    // rtk_sendI2CATCommand — key function for I2C AT commands
    // From shim log:
    //   GET: rtk_sendI2CATCommand(0x1C, context, i2cFrame, frameLen=6, 1, outBuf?, 0, 0)
    //   SET: rtk_sendI2CATCommand(0x1B, context, i2cFrame, frameLen=7, 1, 0, 1, callback?)
    // Return: response length (4)
    [DllImport(RtkIoDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern long rtk_sendI2CATCommand(
        long type,
        IntPtr context,
        byte[] i2cFrame,
        long frameLen,
        long a5,
        long a6,
        long a7,
        long a8);

    // Alternative signature with output buffer as separate parameter
    [DllImport(RtkIoDll, CallingConvention = CallingConvention.Cdecl, EntryPoint = "rtk_sendI2CATCommand")]
    private static extern long rtk_sendI2CATCommand_v2(
        long type,
        IntPtr context,
        byte[] i2cFrame,
        long frameLen,
        long a5,
        byte[] outBuffer,
        long a7,
        long a8);

    // rtk_sendATCommand for regular AT commands
    [DllImport(RtkIoDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern long rtk_sendATCommand(
        long opcode,
        long a2,
        byte[] dataBuffer,
        long a4,
        long a5,
        long a6,
        long a7,
        long a8);

    // getCurrentDeviceName returns a string pointer
    [DllImport(RtkIoDll, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr rtk_getCurrentDeviceName();

    public static int Run(string[] args)
    {
        var subCmd = args.Length > 0 ? args[0] : "init";

        Console.WriteLine("=== RTK_IO Direct P/Invoke Probe ===");

        try
        {
            // Step 1: Initialize (from shim: rtk_initialize(FFFFFFFFFFFFFFFF, 0, stackPtr, 0))
            Console.WriteLine("\n--- rtk_initialize ---");
            var initResult = rtk_initialize(-1, 0, 0, 0);
            Console.WriteLine($"  Result: {initResult}");

            // Step 2: setUVCExtension (from shim: rtk_setUVCExtension(2, ptr, ptr, 0))
            // This seems to set some mode. Let's try with minimal args.
            Console.WriteLine("\n--- rtk_setUVCExtension ---");
            var uvcResult = rtk_setUVCExtension(2, 0, 0, 0);
            Console.WriteLine($"  Result: {uvcResult}");

            // Step 3: setCurrentDevice (from shim: "A7SNB346101346")
            // We need the actual serial. Let's try without it first.
            // Actually, let's find the device serial from our DeviceService
            Console.WriteLine("\n--- rtk_setCurrentDevice ---");
            // Try with a generic name first
            var setDevResult = rtk_setCurrentDevice("Elgato 4K X", IntPtr.Zero);
            Console.WriteLine($"  Result: {setDevResult}");

            // Check current device name
            var namePtr = rtk_getCurrentDeviceName();
            if (namePtr != IntPtr.Zero)
            {
                var name = Marshal.PtrToStringAnsi(namePtr);
                Console.WriteLine($"  Current device: {name}");
            }

            // Step 4: openPort
            Console.WriteLine("\n--- rtk_openPort ---");
            var openResult = rtk_openPort(0, 0, -4, 0);
            Console.WriteLine($"  Result: {openResult}");

            // Step 5: isOpen
            var isOpenResult = rtk_isOpen(0, 0, 0, 0);
            Console.WriteLine($"  isOpen: {isOpenResult}");

            if (subCmd == "get" || subCmd == "init")
            {
                // Step 6: sendI2CATCommand — I2C GET
                Console.WriteLine("\n--- rtk_sendI2CATCommand (I2C GET) ---");

                // Test with I2C GET opcode 0x09, param 0x42
                var i2cFrame = new byte[64]; // allocate larger buffer for safety
                i2cFrame[0] = 0x00;
                i2cFrame[1] = 0x4A;
                i2cFrame[2] = 0x02; // GET
                i2cFrame[3] = 0x00;
                i2cFrame[4] = 0x09;
                i2cFrame[5] = 0x42;

                var outBuffer = new byte[64];

                // Try variant 1: outBuffer as a6
                Console.WriteLine("  Trying v2 (outBuffer as a6):");
                try
                {
                    var resp = rtk_sendI2CATCommand_v2(0x1C, IntPtr.Zero, i2cFrame, 6, 1, outBuffer, 0, 0);
                    Console.WriteLine($"    Return: {resp}");
                    Console.WriteLine($"    outBuffer: {BitConverter.ToString(outBuffer, 0, 16)}");
                    Console.WriteLine($"    i2cFrame after: {BitConverter.ToString(i2cFrame, 0, 16)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    Exception: {ex.GetType().Name}: {ex.Message}");
                }

                // Try variant 2: all long params
                Console.WriteLine("  Trying v1 (all long params):");
                var i2cFrame2 = new byte[64];
                Array.Copy(new byte[] { 0x00, 0x4A, 0x02, 0x00, 0x09, 0x42 }, i2cFrame2, 6);
                try
                {
                    var resp2 = rtk_sendI2CATCommand(0x1C, IntPtr.Zero, i2cFrame2, 6, 1, 0, 0, 0);
                    Console.WriteLine($"    Return: {resp2}");
                    Console.WriteLine($"    i2cFrame after: {BitConverter.ToString(i2cFrame2, 0, 16)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"    Exception: {ex.GetType().Name}: {ex.Message}");
                }
            }

            if (subCmd == "switch")
            {
                Console.WriteLine("\n--- Audio switching via I2C SET ---");
                var target = args.Length > 1 ? args[1] : "analog";
                byte audioVal = (byte)(target == "analog" ? 0x01 : 0x00);
                Console.WriteLine($"  Target: {target} (value={audioVal})");

                // I2C SET opcode 0x04 = audioVal
                var setFrame = new byte[64];
                setFrame[0] = 0x00;
                setFrame[1] = 0x4A;
                setFrame[2] = 0x01; // SET
                setFrame[3] = 0x00;
                setFrame[4] = 0x04;
                setFrame[5] = audioVal;

                var setOut = new byte[64];
                try
                {
                    var setResp = rtk_sendI2CATCommand_v2(0x1B, IntPtr.Zero, setFrame, 6, 1, setOut, 1, 0);
                    Console.WriteLine($"  SET 0x04={audioVal}: return={setResp}");
                    Console.WriteLine($"  setOut: {BitConverter.ToString(setOut, 0, 16)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Exception: {ex.GetType().Name}: {ex.Message}");
                }
            }

            // Cleanup
            Console.WriteLine("\n--- Cleanup ---");
            rtk_closePort();
            rtk_uninitialize();
            Console.WriteLine("  Done");
        }
        catch (DllNotFoundException ex)
        {
            Console.Error.WriteLine($"RTK_IO_x64.dll not found: {ex.Message}");
            Console.Error.WriteLine("Copy RTK_IO_x64.dll to the probe's output directory.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }

        return 0;
    }
}
