using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// ReSharper disable MemberCanBePrivate.Global

namespace Redpoint.Infrastructure.Utilities;

/// <summary>
/// A helper class for tracking the memory usage of .NET over time to help diagnose memory leaks.
/// </summary>
public class MemoryUsageTracker : IDisposable
{
    private readonly bool triggerGcOnDispose;

    public static MemoryUsageTracker Track([CallerMemberName] string context = "",
        [CallerFilePath] string filePath = "",
        string eventId = "",
        bool triggerGcOnDispose = false)
    {
        var tag = Path.GetFileNameWithoutExtension(filePath);
        var loggingContext = tag + "." + context;

        return new MemoryUsageTracker(loggingContext, eventId, triggerGcOnDispose);
    }

    public MemoryUsageTracker(string eventName, string eventId = "", bool triggerGcOnDispose = false)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(eventName));

        this.triggerGcOnDispose = triggerGcOnDispose;
        EventName = eventName;
        EventId = string.IsNullOrWhiteSpace(eventId) ? Guid.NewGuid().ToString() : eventId;

#if IOS

        InitialMemoryUsage = (long)IosMemoryHelper.GetPhysFootprint();
        // #elif ANDROID
#else
        InitialMemoryUsage = GC.GetTotalMemory(false);
#endif

        TrackingStartAtUtc = DateTime.UtcNow;

        var formattedMemoryUsage = SizeHelper.GetFormattedSize(InitialMemoryUsage);
        Console.WriteLine($"Start.{EventName}|{EventId}: {formattedMemoryUsage}");
    }

    public DateTime TrackingStartAtUtc { get; set; }

    public bool IsCancelled { get; private set; }

    public string EventId { get; }
    public string EventName { get; }
    public long InitialMemoryUsage { get; }

    public long MemoryChange => GC.GetTotalMemory(false) - InitialMemoryUsage;

    public void Dispose()
    {
        if (IsCancelled)
        {
            return;
        }

#if IOS

        var memoryUsage = (long)IosMemoryHelper.GetPhysFootprint();
        // #elif ANDROID
#else
        var memoryUsage = GC.GetTotalMemory(forceFullCollection: triggerGcOnDispose);
#endif

        var memoryChange = memoryUsage - InitialMemoryUsage;
        var sign = memoryChange < 0 ? "-" : "+";

        var absMemoryChange = Math.Abs(memoryChange);
        var formattedMemoryUsage = SizeHelper.GetFormattedSize(memoryUsage);
        var formattedMemoryChange = SizeHelper.GetFormattedSize(absMemoryChange);

        Console.WriteLine($"End.{EventName}|{EventId}: {formattedMemoryUsage} ({sign}{formattedMemoryChange})");
    }

    public void Cancel()
    {
        IsCancelled = true;
    }

#if IOS
    public static class IosMemoryHelper
    {
        // --- Public API -----------------------------------------------------

        /// <summary>
        /// Bytes currently available to THIS process before Jetsam might terminate it.
        /// iOS 13+ only; returns 0 if unavailable.
        /// </summary>
        public static ulong GetAvailableHeadroom()
        {
            try
            {
                return os_proc_available_memory();
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Current physical footprint (bytes) of THIS process. This is the metric Jetsam uses.
        /// </summary>
        public static ulong GetPhysFootprint()
        {
            var info = new task_vm_info_data_t();
            if (!TryGetTaskVmInfo(ref info))
                return 0;

            return info.phys_footprint;
        }

        /// <summary>
        /// Rough estimate of your current per-process limit (bytes) on this device/state.
        /// </summary>
        public static ulong GetEstimatedPerProcessLimit()
        {
            var footprint = GetPhysFootprint();
            var headroom = GetAvailableHeadroom();
            return footprint + headroom;
        }

        // --- P/Invoke + helpers --------------------------------------------

        // os_proc_available_memory() lives in libSystem
        [DllImport("/usr/lib/libSystem.dylib")]
        static extern ulong os_proc_available_memory();

        // mach_task_self() & task_info() live in libSystem as well
        [DllImport("/usr/lib/libSystem.dylib")]
        static extern IntPtr mach_task_self();

        [DllImport("/usr/lib/libSystem.dylib")]
        static extern int task_info(IntPtr target_task, int flavor, IntPtr task_info_out, ref int task_info_outCnt);

        // Constants from <mach/task_info.h>
        const int TASK_VM_INFO = 22;

        // natural_t is 32-bit on iOS; task_info_outCnt is in units of natural_t
        static bool TryGetTaskVmInfo(ref task_vm_info_data_t info)
        {
            IntPtr self = mach_task_self();
            int sizeBytes = Marshal.SizeOf<task_vm_info_data_t>();
            int count = sizeBytes / sizeof(uint); // natural_t == 32-bit

            IntPtr buffer = Marshal.AllocHGlobal(sizeBytes);
            try
            {
                Marshal.StructureToPtr(info, buffer, false);
                int kr = task_info(self, TASK_VM_INFO, buffer, ref count);
                if (kr != 0 || count * sizeof(uint) < sizeBytes)
                    return false;

                info = Marshal.PtrToStructure<task_vm_info_data_t>(buffer);
                return true;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // Partial definition of task_vm_info_data_t up to phys_footprint.
        // Field order MUST match <mach/task_info.h>. Types:
        //   mach_vm_size_t -> UInt64
        //   integer_t      -> Int32
        //   natural_t      -> UInt32 (not used here)
        [StructLayout(LayoutKind.Sequential)]
        struct task_vm_info_data_t
        {
            public ulong virtual_size;
            public int region_count;
            public int page_size;

            public ulong resident_size;
            public ulong resident_size_peak;

            public ulong device;
            public ulong device_peak;

            public ulong internal_bytes;
            public ulong internal_bytes_peak;

            public ulong external_bytes;
            public ulong external_bytes_peak;

            public ulong reusable_bytes;
            public ulong reusable_bytes_peak;

            public ulong purgeable_volatile_pmap;
            public ulong purgeable_volatile_resident;
            public ulong purgeable_volatile_virtual;

            public ulong compressed;
            public ulong compressed_peak;
            public ulong compressed_lifetime;

            public ulong phys_footprint;
            // The struct contains more fields after this, but we don't need them.
        }
    }
#endif

}