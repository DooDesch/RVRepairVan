#if SNITCH
using Snitch.Api;
using RVRepairVan.Persistence;    // RepairStateStore

namespace RVRepairVan.Profiling
{
    /// <summary>
    /// DEBUG-only Snitch instrumentation for RVRepairVan. This is an event/coroutine-driven quest mod with no
    /// per-frame cost, so the profiler value is the quest-stage timeline (a gauge here + a Mark on each stage
    /// advance in Questline). No-op when the Snitch host is absent. Compiled only when SNITCH is defined
    /// (Debug + EnableSnitch); excluded from Release. See Workspace/build/Snitch.props.
    /// </summary>
    internal static class SnitchProbe
    {
        public static void Register()
        {
            Profiler.RegisterCounter("RVRepairVan.Stage", () => RepairStateStore.GetStage(), "stage");
        }
    }
}
#endif
