using Microsoft.Extensions.Logging;
using Redpoint.Infrastructure.Utilities;
using Redpoint.Mobile.Evergine;
using Redpoint.SceneViewer.Utilities;

namespace GlbMemLeakDemo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        EvergineDiagnostics.AuditMemoryDelegate = MemoryUsageTracker.Track;
        
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers((handlers) =>
            {
                handlers.AddHandler<EvergineView, EvergineViewHandler>();
            });;

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}