using System;
using System.Linq;
using System.Threading.Tasks;
using Evergine.Common.IO;
using Evergine.Framework;
using Evergine.Framework.Services;
using Redpoint.SceneViewer.Services;
using Random = Evergine.Framework.Services.Random;

namespace Redpoint.SceneViewer;

public partial class RedpointApplication : Application
{
    public static string EvergineVersion
    {
        get
        {
            var assembly = typeof(Application).Assembly; // Core Evergine assembly
            var version = assembly.GetName().Version?.ToString() ?? "Unknown";

            return version;
        }
    }
    
    RedpointScene scene;

#pragma warning disable CS8603 // Possible null reference return.
    public static RedpointScene Scene => (Application.Current as RedpointApplication)?.scene;
#pragma warning restore CS8603 // Possible null reference return.
        
    public RedpointApplication()
    {
        this.Container.Register<Settings>();
        this.Container.Register<Clock>();
        this.Container.Register<TimerFactory>();
        this.Container.Register<Random>();
        this.Container.Register<ErrorHandler>();
        this.Container.Register<ScreenContextManager>();
        this.Container.Register<GraphicsPresenter>();
        this.Container.Register<AssetsDirectory>();
        this.Container.Register<AssetsService>();
        this.Container.Register<ForegroundTaskSchedulerService>();
        this.Container.Register<WorkActionScheduler>();
        
        // Red-Point Specific Services
        this.Container.Register<GlbAssetService>();
    }
    
    public static RedpointApplication Instance => Application.Current as RedpointApplication;
    
    public GlbAssetService GlbAssetService => this.Container.Resolve<GlbAssetService>();
    
    public override void Initialize()
    {
        base.Initialize();

        // Get ScreenContextManager
        var screenContextManager = this.Container.Resolve<ScreenContextManager>();
        var assetsService = this.Container.Resolve<AssetsService>();

        // Navigate to scene
        scene = assetsService.Load<RedpointScene>(EvergineContent.Scenes.MyScene_wescene);
        ScreenContext screenContext = new ScreenContext(scene);
        screenContextManager.To(screenContext);
    }
}