using Evergine.Android;
using Evergine.Common.Graphics;
using Evergine.Common.Helpers;
using Evergine.Framework.Services;
using Evergine.Vulkan;
using Microsoft.Maui.Handlers;
using Redpoint.SceneViewer;

namespace Redpoint.Mobile.Evergine;

public partial class EvergineViewHandler : ViewHandler<EvergineView, AndroidSurfaceView>
{
    private static readonly Redpoint.Logging.ILogger log = Redpoint.Logging.Logger.Create();
    
    private AndroidSurface androidSurface;
    private AndroidWindowsSystem windowsSystem;
    private static bool isEvergineInitialized;
    private static VKGraphicsContext graphicsContext;
    private static SwapChain swapChain;
    private static bool surfaceInvalidated;

    public EvergineViewHandler(IPropertyMapper mapper, CommandMapper commandMapper = null)
       : base(mapper, commandMapper)
    {
    }

    public static void MapApplication(EvergineViewHandler handler, EvergineView evergineView)
    {
        handler.UpdateApplication(evergineView, evergineView.DisplayName);
    }

    internal void UpdateApplication(EvergineView view, string displayName)
    {
        if (view.Application is null)
        {
            return;
        }

        if (!isEvergineInitialized)
        {
            // Register Windows system
            view.Application.Container.RegisterInstance(windowsSystem);
        }
        else
        {
            view.Application.Container.Unregister<AndroidWindowsSystem>();
            view.Application.Container.RegisterInstance(windowsSystem);
        }
        

        System.Diagnostics.Stopwatch clockTimer = System.Diagnostics.Stopwatch.StartNew();
        windowsSystem.Run(
        () =>
        {
            if (!isEvergineInitialized)
            {
                this.ConfigureGraphicsContext(view.Application as RedpointApplication, androidSurface);
                view.Application.Initialize();
                isEvergineInitialized = true;
            }
            else
            {
                this.ConfigureGraphicsContext(view.Application as RedpointApplication, androidSurface);
            }
        },
        () =>
        {
            var gameTime = clockTimer.Elapsed;
            clockTimer.Restart();

            if (view.IsPaused)
            {
                return;
            }

            view.Application.UpdateFrame(gameTime);
            view.Application.DrawFrame(gameTime);
        });
    }

    protected override AndroidSurfaceView CreatePlatformView()
    {
        this.windowsSystem = new AndroidWindowsSystem(this.Context);
        this.androidSurface = this.windowsSystem.CreateSurface(0, 0) as AndroidSurface;            

        return this.androidSurface.NativeSurface;
    }

    protected override void ConnectHandler(AndroidSurfaceView platformView)
    {
        base.ConnectHandler(platformView);
        
        Console.WriteLine("EvergineViewHandler.ConnectHandler: " + platformView.GetHashCode());
        
        this.androidSurface.OnSurfaceInfoChanged += this.AndroidSurface_OnSurfaceInfoChanged;
        this.androidSurface.Closing += this.AndroidSurface_OnClosing;
        this.androidSurface.OnScreenSizeChanged += this.AndroidSurface_OnScreenSizeChanged; 
    }

    protected override void DisconnectHandler(AndroidSurfaceView platformView)
    {
        base.DisconnectHandler(platformView);
        
        
        Console.WriteLine("EvergineViewHandler.DisconnectHandler: " + platformView.GetHashCode());
        
        this.androidSurface.OnSurfaceInfoChanged -= this.AndroidSurface_OnSurfaceInfoChanged;
        this.androidSurface.Closing -= this.AndroidSurface_OnClosing;
        this.androidSurface.OnScreenSizeChanged -= this.AndroidSurface_OnScreenSizeChanged;
    }

    private void AndroidSurface_OnSurfaceInfoChanged(object sender, SurfaceInfo surfaceInfo)
    {
        if (androidSurface.NativeSurface.Width <= 0 || androidSurface.NativeSurface.Height <= 0)
        {
            return; // Skip invalid sizes during transient layout
        }
        
        Console.WriteLine("Evergine AndroidSurface_OnSurfaceInfoChanged");

        swapChain?.RefreshSurfaceInfo(surfaceInfo);
        swapChain?.ResizeSwapChain(this.androidSurface.Width, this.androidSurface.Height);
        surfaceInvalidated = false;
    }

    private void AndroidSurface_OnClosing(object sender, EventArgs e)
    {
        Console.WriteLine("Evergine AndroidSurface_OnClosing");
        surfaceInvalidated = true;
    }

    private void AndroidSurface_OnScreenSizeChanged(object sender, SizeEventArgs e)
    {
        if (!surfaceInvalidated)
        {
            Console.WriteLine("Evergine AndroidSurface_OnScreenSizeChanged: " + this.androidSurface.Width + ", " + this.androidSurface.Height);
            swapChain?.ResizeSwapChain(this.androidSurface.Width, this.androidSurface.Height); 
        }
    }

    bool isFirstLayout = true;
    
    public override void PlatformArrange(Rect frame)
    {
        if (isFirstLayout)
        {
            base.PlatformArrange(frame);
            isFirstLayout = false;
        }
        
        // This is a work-around to MAUI from doing underlying resizing of the surface view.
        // If a more than one resize takes place, the 3D players surface view encounters screen tearing issues and will eventually completely stop working.
        // This override resolves that issue.
    }

    private void ConfigureGraphicsContext(RedpointApplication application, Surface surface)
    {
        if (graphicsContext == null)
        {
            Console.WriteLine("ConfigureGraphicsContext: Create Vulkan Graphics Context...");
            graphicsContext = new VKGraphicsContext();
            graphicsContext.CreateDevice();
        }

        
        Console.WriteLine("ConfigureGraphicsContext: Define SwapChainDescription...");
        SwapChainDescription swapChainDescription = new SwapChainDescription()
        {
            SurfaceInfo = surface.SurfaceInfo,
            Width = surface.Width,
            Height = surface.Height,
            ColorTargetFormat = PixelFormat.B8G8R8A8_UNorm_SRgb,
            ColorTargetFlags = TextureFlags.RenderTarget | TextureFlags.ShaderResource,
            DepthStencilTargetFormat = PixelFormat.D32_Float,
            DepthStencilTargetFlags = TextureFlags.DepthStencil,
            SampleCount = TextureSampleCount.None,
            IsWindowed = true,
            RefreshRate = 60,
        };
        
        Console.WriteLine("ConfigureGraphicsContext: Create SwapChainDescription from Vulkan Graphics Context...");
        swapChain = graphicsContext.CreateSwapChain(swapChainDescription);
        swapChain.VerticalSync = true;

        Console.WriteLine("ConfigureGraphicsContext: Create Display...");
        var graphicsPresenter = application.Container.Resolve<GraphicsPresenter>();
        var firstDisplay = new global::Evergine.Framework.Graphics.Display(surface, swapChain);

        if (!isEvergineInitialized)
        {
            Console.WriteLine("ConfigureGraphicsContext: Initializing...");
            graphicsPresenter.AddDisplay("DefaultDisplay", firstDisplay);
            application.Container.RegisterInstance(graphicsContext);
        }
        else
        {
            Console.WriteLine("ConfigureGraphicsContext: Changing display...");
            
            graphicsPresenter.RemoveDisplay("DefaultDisplay");
            graphicsPresenter.AddDisplay("DefaultDisplay", firstDisplay);
        }
    }
}
