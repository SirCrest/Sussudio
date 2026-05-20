using System.Collections.Generic;
using System.Threading.Tasks;

static partial class Program
{
    private static async Task AddToolContractChecksAsync(List<CheckResult> results)
    {
        // --- Tool CommandMap & Formatter Alignment ---
        await AddCheckAsync(results,
            "RTK I2C probe guards unsafe native paths",
            RtkI2cProbe_GuardsUnsafeNativePaths);
    }
}
