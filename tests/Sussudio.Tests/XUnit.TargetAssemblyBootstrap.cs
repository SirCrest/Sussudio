static partial class Program
{
    private static readonly object XUnitTargetAssemblyLoadLock = new();

    internal static void EnsureTargetAssemblyLoadedForXUnit()
    {
        lock (XUnitTargetAssemblyLoadLock)
        {
            _assembly ??= Sussudio.Tests.SussudioAssembly.Load();
        }
    }
}
