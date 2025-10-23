# evergine-glb-disposal-memleaks

This codebase demonstrates a memory leak in Evergine where GLB resources don't appear to be released when they are removed from a scene.

## Details

In Evergine, when loading and then unloading GLB assets at runtime, the memory usage of the app constantly grows.

Loading a GLB:

```
var assetsService = Application.Current.Container.Resolve<AssetsService>();

Model model = null;
using (var fileStream = File.OpenRead(glbFilePath))
{
    model = await Evergine.Runtimes.GLB.GLBRuntime.Instance.Read(fileStream, CustomMaterialAssigner);
}

var entity = model.InstantiateModelHierarchy(assetsService);
```

Unloading a GLB:

```
var glbAssets = scene.Managers.EntityManager.FindAllByTag(EntityTags.GlbAsset).ToList();

foreach (var asset in glbAssets)
{
  scene.Managers.EntityManager.Remove(asset);
}
```

AFter several large GLB models, the app will be killed by the operating system.

## Steps To Reproduce

To see the memory leak:

 1. Launch the app with the debugger attached. This can be done on a device or in the iOS simulator
 2. Tap on any of the bottom buttons to download and load a GLB.
 3. Observe the memory logging in the debugger console. The memory logger query's the Jetsam process managers and logs the used process memory.
 4. Tap on another model to download and load a new GLB. The memory logger will show a *significant* increase in memory.
 5. If testing on device, the app will crash after opening 3-5 models.


## Additional Details

Please note that each model:

 * Has a single 8K jpeg texture.
 * Has 500k polys per model.


<details>
<summary>Sample Logs (Evergine v2025.10.21.8)</summary>
```
2025-10-23 12:55:33.940942+1100 GlbMemLeakDemo[83947:4268397] Start.GlbAssetService.RemoveAll|324932d3-aad7-4192-a752-9558987cf208: 177.9MB
2025-10-23 12:55:33.941627+1100 GlbMemLeakDemo[83947:4268397] End.GlbAssetService.RemoveAll|324932d3-aad7-4192-a752-9558987cf208: 178.3MB (+320KB)
2025-10-23 12:55:33.943549+1100 GlbMemLeakDemo[83947:4268397] Start.GlbAssetService.LoadGlbAsset|Los Huasamacos Del Sur: 178.4MB
2025-10-23 12:55:33.943680+1100 GlbMemLeakDemo[83947:4268397] Loading the requested GLB asset ~Los Huasamacos Del Sur~: '/Users/matthewrobbins/Library/Developer/CoreSimulator/Devices/2FFA8EB2-60B9-4C98-8089-8F874161CC69/data/Containers/Data/Application/B18A22F7-DDCA-4AAB-8AEE-BCB4A0652905/Library/Los Huasamacos Del Sur.glb'
2025-10-23 12:55:35.920933+1100 GlbMemLeakDemo[83947:4268397] Material data bind: Material Opaque => ID: 4b682f26-1bf6-461d-8848-96ffb8e67034
2025-10-23 12:55:35.927426+1100 GlbMemLeakDemo[83947:4268397] Attached mesh collider: 2fafa90c-6ec5-4843-8392-0bbf381f0ca9
2025-10-23 12:55:36.188231+1100 GlbMemLeakDemo[83947:4268397] The glb asset 'Los Huasamacos Del Sur- Los Huasamacos Del Sur.glb' has been loaded. Evergine entity: 356472f9-026b-4c80-a117-5fc238b6a07e
2025-10-23 12:55:36.188464+1100 GlbMemLeakDemo[83947:4268397] End.GlbAssetService.LoadGlbAsset|Los Huasamacos Del Sur: 0.9GB (+0.7GB)
2025-10-23 12:56:24.650002+1100 GlbMemLeakDemo[83947:4268397] Start.GlbAssetService.RemoveAll|2d7367a3-51ad-4e04-a8e4-e07b13294a88: 0.9GB
2025-10-23 12:56:24.653566+1100 GlbMemLeakDemo[83947:4268397] End.GlbAssetService.RemoveAll|2d7367a3-51ad-4e04-a8e4-e07b13294a88: 0.9GB (+128KB)
2025-10-23 12:56:24.653733+1100 GlbMemLeakDemo[83947:4268397] Start.GlbAssetService.LoadGlbAsset|Placca Nevruz: 0.9GB
2025-10-23 12:56:24.653777+1100 GlbMemLeakDemo[83947:4268397] Loading the requested GLB asset ~Placca Nevruz~: '/Users/matthewrobbins/Library/Developer/CoreSimulator/Devices/2FFA8EB2-60B9-4C98-8089-8F874161CC69/data/Containers/Data/Application/B18A22F7-DDCA-4AAB-8AEE-BCB4A0652905/Library/Placca Nevruz.glb'
2025-10-23 12:56:26.440050+1100 GlbMemLeakDemo[83947:4268397] Material data bind: AtlasMaterial Opaque => ID: 6ce1ebe5-b906-4bec-9322-ac2efc03cdcd
2025-10-23 12:56:26.440634+1100 GlbMemLeakDemo[83947:4268397] Attached mesh collider: d573c668-f570-470c-afac-88df338796a8
2025-10-23 12:56:26.690926+1100 GlbMemLeakDemo[83947:4268397] The glb asset 'Placca Nevruz- Placca Nevruz.glb' has been loaded. Evergine entity: 6ce4e1f0-27ba-4024-96c4-dc8540417de4
2025-10-23 12:56:26.691078+1100 GlbMemLeakDemo[83947:4268397] End.GlbAssetService.LoadGlbAsset|Placca Nevruz: 1.7GB (+0.7GB)
2025-10-23 12:56:39.450721+1100 GlbMemLeakDemo[83947:4268397] Start.GlbAssetService.RemoveAll|00bebcfc-9967-44ee-9496-a1c5feed7b3e: 1.3GB
2025-10-23 12:56:39.451118+1100 GlbMemLeakDemo[83947:4268397] End.GlbAssetService.RemoveAll|00bebcfc-9967-44ee-9496-a1c5feed7b3e: 1.3GB (+0bytes)
2025-10-23 12:56:39.451368+1100 GlbMemLeakDemo[83947:4268397] Start.GlbAssetService.LoadGlbAsset|Torbole: 1.3GB
2025-10-23 12:56:39.451454+1100 GlbMemLeakDemo[83947:4268397] Loading the requested GLB asset ~Torbole~: '/Users/matthewrobbins/Library/Developer/CoreSimulator/Devices/2FFA8EB2-60B9-4C98-8089-8F874161CC69/data/Containers/Data/Application/B18A22F7-DDCA-4AAB-8AEE-BCB4A0652905/Library/Torbole.glb'
2025-10-23 12:56:41.192198+1100 GlbMemLeakDemo[83947:4268397] Material data bind: AtlasMaterial Opaque => ID: 454149f8-e8e9-4561-8282-20add6893cfa
2025-10-23 12:56:41.192662+1100 GlbMemLeakDemo[83947:4268397] Attached mesh collider: d204c0b6-e8f5-4a5b-af06-6ebc6e5c7594
2025-10-23 12:56:41.445492+1100 GlbMemLeakDemo[83947:4268397] The glb asset 'Torbole- Torbole.glb' has been loaded. Evergine entity: 905dd8d6-5981-4f0e-8143-decb63288654
2025-10-23 12:56:41.445652+1100 GlbMemLeakDemo[83947:4268397] End.GlbAssetService.LoadGlbAsset|Torbole: 2.1GB (+0.7GB)
2025-10-23 12:56:46.791547+1100 GlbMemLeakDemo[83947:4268397] Start.GlbAssetService.RemoveAll|3f71c596-0eb4-4a17-9d0a-22b95c198de5: 1.7GB
2025-10-23 12:56:46.791863+1100 GlbMemLeakDemo[83947:4268397] End.GlbAssetService.RemoveAll|3f71c596-0eb4-4a17-9d0a-22b95c198de5: 1.7GB (+0bytes)
2025-10-23 12:56:46.791951+1100 GlbMemLeakDemo[83947:4268397] Start.GlbAssetService.LoadGlbAsset|Los Huasamacos Del Sur: 1.7GB
2025-10-23 12:56:46.792057+1100 GlbMemLeakDemo[83947:4268397] Loading the requested GLB asset ~Los Huasamacos Del Sur~: '/Users/matthewrobbins/Library/Developer/CoreSimulator/Devices/2FFA8EB2-60B9-4C98-8089-8F874161CC69/data/Containers/Data/Application/B18A22F7-DDCA-4AAB-8AEE-BCB4A0652905/Library/Los Huasamacos Del Sur.glb'
2025-10-23 12:56:48.572768+1100 GlbMemLeakDemo[83947:4268397] Material data bind: Material Opaque => ID: a457da04-9877-4ce5-bfb0-6afa386cfb87
2025-10-23 12:56:48.573415+1100 GlbMemLeakDemo[83947:4268397] Attached mesh collider: fa2c11f1-4680-4945-a962-e099b5a90754
2025-10-23 12:56:48.824090+1100 GlbMemLeakDemo[83947:4268397] The glb asset 'Los Huasamacos Del Sur- Los Huasamacos Del Sur.glb' has been loaded. Evergine entity: 1a2759ed-ef83-4dc4-86a6-177648c80ee9
2025-10-23 12:56:48.824254+1100 GlbMemLeakDemo[83947:4268397] End.GlbAssetService.LoadGlbAsset|Los Huasamacos Del Sur: 2.4GB (+0.7GB)
2025-10-23 12:57:13.924595+1100 GlbMemLeakDemo[83947:4268397] Start.GlbAssetService.RemoveAll|c9fe873c-b402-4e5e-93f9-fc71b6e89a58: 2.1GB
2025-10-23 12:57:13.925049+1100 GlbMemLeakDemo[83947:4268397] End.GlbAssetService.RemoveAll|c9fe873c-b402-4e5e-93f9-fc71b6e89a58: 2.1GB (+0bytes)
2025-10-23 12:57:13.925192+1100 GlbMemLeakDemo[83947:4268397] Start.GlbAssetService.LoadGlbAsset|Placca Nevruz: 2.1GB
2025-10-23 12:57:13.925259+1100 GlbMemLeakDemo[83947:4268397] Loading the requested GLB asset ~Placca Nevruz~: '/Users/matthewrobbins/Library/Developer/CoreSimulator/Devices/2FFA8EB2-60B9-4C98-8089-8F874161CC69/data/Containers/Data/Application/B18A22F7-DDCA-4AAB-8AEE-BCB4A0652905/Library/Placca Nevruz.glb'
2025-10-23 12:57:15.722417+1100 GlbMemLeakDemo[83947:4268397] Material data bind: AtlasMaterial Opaque => ID: ca462304-6cfa-4508-b053-fb9bc6290e76
2025-10-23 12:57:15.723142+1100 GlbMemLeakDemo[83947:4268397] Attached mesh collider: 0cd25007-6d3b-463c-aef5-138bb9b41b86
2025-10-23 12:57:15.976281+1100 GlbMemLeakDemo[83947:4268397] The glb asset 'Placca Nevruz- Placca Nevruz.glb' has been loaded. Evergine entity: 0c9cde6b-473c-4467-b8fa-1de00e7a621b
2025-10-23 12:57:15.976474+1100 GlbMemLeakDemo[83947:4268397] End.GlbAssetService.LoadGlbAsset|Placca Nevruz: 2.8GB (+0.7GB)
2025-10-23 12:57:26.377001+1100 GlbMemLeakDemo[83947:4268397] Double tap gesture detected at: {X:718 Y:1479}
2025-10-23 12:57:26.377904+1100 GlbMemLeakDemo[83947:4268397] Camera fly to animation  requested: {X:39.751614 Y:30.253426 Z:40.605625}, {X:10.383858 Y:15.073164 Z:-6.506485}, 0.5 seconds
2025-10-23 12:57:26.378182+1100 GlbMemLeakDemo[83947:4268397] The camera has been locked.
2025-10-23 12:57:26.857430+1100 GlbMemLeakDemo[83947:4268397] The camera has been un-locked.
2025-10-23 12:57:33.541026+1100 GlbMemLeakDemo[83947:4268397] Double tap gesture detected at: {X:1293 Y:962}
2025-10-23 12:57:33.541214+1100 GlbMemLeakDemo[83947:4268397] Camera fly to animation  requested: {X:27.23641 Y:16.644775 Z:22.65129}, {X:3.5422325 Y:19.266397 Z:0.3584633}, 0.5 seconds
2025-10-23 12:57:33.541273+1100 GlbMemLeakDemo[83947:4268397] The camera has been locked.
2025-10-23 12:57:34.040344+1100 GlbMemLeakDemo[83947:4268397] The camera has been un-locked.
2025-10-23 12:57:36.208032+1100 GlbMemLeakDemo[83947:4268397] Start.GlbAssetService.RemoveAll|1fa2888d-e883-4cce-89ce-8eb1ad28b58e: 2.5GB
2025-10-23 12:57:36.208737+1100 GlbMemLeakDemo[83947:4268397] End.GlbAssetService.RemoveAll|1fa2888d-e883-4cce-89ce-8eb1ad28b58e: 2.5GB (+0bytes)
2025-10-23 12:57:36.209041+1100 GlbMemLeakDemo[83947:4268397] Start.GlbAssetService.LoadGlbAsset|Torbole: 2.5GB
2025-10-23 12:57:36.209187+1100 GlbMemLeakDemo[83947:4268397] Loading the requested GLB asset ~Torbole~: '/Users/matthewrobbins/Library/Developer/CoreSimulator/Devices/2FFA8EB2-60B9-4C98-8089-8F874161CC69/data/Containers/Data/Application/B18A22F7-DDCA-4AAB-8AEE-BCB4A0652905/Library/Torbole.glb'
2025-10-23 12:57:37.906464+1100 GlbMemLeakDemo[83947:4268397] Material data bind: AtlasMaterial Opaque => ID: 0b43153c-2bd4-42c9-a56e-b7aa447b25a8
2025-10-23 12:57:37.906886+1100 GlbMemLeakDemo[83947:4268397] Attached mesh collider: 4b9a10f0-f5f9-410f-bff6-910cb7ad8d9e
2025-10-23 12:57:38.155636+1100 GlbMemLeakDemo[83947:4268397] The glb asset 'Torbole- Torbole.glb' has been loaded. Evergine entity: 545ccc8b-4a8d-4fbe-9b74-bb7b8a039c3a
2025-10-23 12:57:38.155770+1100 GlbMemLeakDemo[83947:4268397] End.GlbAssetService.LoadGlbAsset|Torbole: 3.2GB (+0.7GB)
```
</details>
 

-------------------------------------------------


To test this app on a device:

Create a `Codesign.props` file in the root of the repository with the following contents:
 
```<Project>
  <PropertyGroup>
      <CodesignTeamId>Enter your team ID here</CodesignTeamId>
    <CodesignKey>Enter your code sign key here</CodesignKey>
    <CodesignProvision>Automatic</CodesignProvision>
  </PropertyGroup>
</Project>


```

You will then need to setup a code signing cert and provisiiongin profile for `com.companyname.glbmemleakdemo`. 

I recommend this is done through Xcodes automatic code signing.
