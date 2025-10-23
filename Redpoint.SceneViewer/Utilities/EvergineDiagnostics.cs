using System;
using System.Runtime.CompilerServices;

namespace Redpoint.SceneViewer.Utilities;

public delegate IDisposable AuditMemoryDelegate(string context = "", string filePath = "", string eventId = "", bool triggerGcOnDispose = false);

public static class EvergineDiagnostics
{
    public static IDisposable AuditMemory([CallerMemberName] string context = "", 
                                    [CallerFilePath] string filePath = "", 
                                    string eventId = "",
                                    bool triggerGcOnDispose = false)
    {
        return AuditMemoryDelegate(context, filePath, eventId, triggerGcOnDispose);
    }


    private static readonly object _lock = new object();
    private static AuditMemoryDelegate auditMemoryDelegate = null;


    public static AuditMemoryDelegate AuditMemoryDelegate
    {
        get
        {
            lock (_lock)
            {
                if (auditMemoryDelegate is null)
                {
                    throw new InvalidOperationException("No audit memory delegates available.");
                }

                return auditMemoryDelegate;
            }
        }

        set
        {
            lock (_lock)
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(auditMemoryDelegate));
                }

                if (auditMemoryDelegate != null)
                {
                    throw new InvalidOperationException("Cannot set audit memory delegate twice.");
                }

                auditMemoryDelegate = value;
            }
        }
    }
}