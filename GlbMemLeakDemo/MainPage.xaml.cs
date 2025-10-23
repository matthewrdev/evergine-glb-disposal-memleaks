using Redpoint.Mobile.Utilities;
using Redpoint.SceneViewer;

namespace GlbMemLeakDemo;

public partial class MainPage : ContentPage
{
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly RedpointApplication evergineApplication;
    private readonly HttpClient httpClient =  new HttpClient();
    private readonly HttpClientDownloadWithProgress downloadClient = new HttpClientDownloadWithProgress();

    public MainPage()
    {
        InitializeComponent();
        
        this.evergineApplication = new Redpoint.SceneViewer.RedpointApplication();
        this.evergineView.Application = this.evergineApplication;
    }

    public string ApplicationDataPath { get; } = FileSystem.Current.AppDataDirectory;

    private void Button_OnClicked(object? sender, EventArgs e)
    {
        if (sender == this.buttonOne)
        {
            LoadModel(Assets.LosHuasamacosDelSur).ConfigureAwait(false);
        }
        else if (sender == this.buttonTwo)
        {
            LoadModel(Assets.PlaccaNevruz).ConfigureAwait(false);
        }
        else
        {
            LoadModel(Assets.Torbole).ConfigureAwait(false);
        }
    }

    public async Task LoadModel(AssetConfig assetConfig)
    {
        var glbAssetPath = Path.Combine(ApplicationDataPath, assetConfig.Name + ".glb");

        if (!File.Exists(glbAssetPath))
        {
            await DownloadGlb(assetConfig, glbAssetPath);
        }

        this.evergineApplication.GlbAssetService.RemoveAll();

        var result = await this.evergineApplication.GlbAssetService.LoadGlbAsset(glbAssetPath, assetConfig.Name, replaceExisting: true);

        await DisplayAlert(assetConfig.Name, "Loaded successfully!", "OK");
    }

    private async Task DownloadGlb(AssetConfig assetConfig, string glbAssetPath)
    {
        double? lastPercentage = null;
        
        void OnDownloadClientOnProgressChanged(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage)
        {
            if (progressPercentage.HasValue)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    this.progressBar.Progress = progressPercentage.Value;
                });
            }
        }
        
        try
        {
            this.loadingIndicatorContainer.IsVisible = true;
            this.label.Text = "Downloading " + assetConfig.Name + "...";
            this.progressBar.Progress = 0;

            downloadClient.ProgressChanged += OnDownloadClientOnProgressChanged;

            await downloadClient.Download(httpClient, assetConfig.Url, glbAssetPath, CancellationToken.None);
        }
        finally
        {
            loadedModelabel.Text = assetConfig.Name;
            downloadClient.ProgressChanged -= OnDownloadClientOnProgressChanged;
            this.loadingIndicatorContainer.IsVisible = false;
        }
    }
}