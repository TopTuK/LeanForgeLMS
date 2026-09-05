using System.Runtime.CompilerServices;
using LF.Application;
using Mapster;

namespace LF.ApplicationTests.TestSupport;

// Services under test call the parameterless `.Adapt<T>()`, which reads the process-wide
// `TypeAdapterConfig.GlobalSettings`. Production code only populates it via
// `DependencyInjection.Add*Application()`'s `TypeAdapterConfig.GlobalSettings.Scan(...)` call,
// which most unit tests never invoke directly - so a custom mapping (e.g. Course ->
// CourseDetailDto's CoverImageKey) silently falls back to Mapster's default convention and
// comes back null, unless some other test happened to run first in the same process and
// triggered the scan as a side effect (e.g. DependencyInjectionTests). Scanning once here, at
// assembly load, makes every test's mapping deterministic regardless of run order or parallelization.
internal static class MapsterTestSetup
{
    [ModuleInitializer]
    public static void Initialize() => TypeAdapterConfig.GlobalSettings.Scan(typeof(DependencyInjection).Assembly);
}
