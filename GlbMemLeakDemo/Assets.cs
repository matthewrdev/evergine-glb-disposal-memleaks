namespace GlbMemLeakDemo;

public static class Assets
{
    public static AssetConfig LosHuasamacosDelSur = new AssetConfig()
    {
        Name = "Los Huasamacos Del Sur",
        Url = "https://redpointdemo.blob.core.windows.net/data/los-huasamacos-del-sur_hd.glb",
        Size = 11965896
    };
    
    
    public static AssetConfig Torbole = new AssetConfig()
    {
        Name = "Torbole",
        Url = "https://redpointdemo.blob.core.windows.net/data/torbole_hd.glb",
        Size = 6262644,

    };
    
    
    public static AssetConfig PlaccaNevruz = new AssetConfig()
    {
        Name = "Placca Nevruz",
        Url = "https://redpointdemo.blob.core.windows.net/data/placca-nevruz_hd.glb",
        Size = 12324056
    };
}

public class AssetConfig
{
    public required string Name { get; init; }
    
    public required string Url { get; init; }
    
    public required long Size { get; init; }
}