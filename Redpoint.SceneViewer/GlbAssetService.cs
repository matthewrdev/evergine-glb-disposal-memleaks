using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Graphics.Materials;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Runtimes;
using Evergine.Framework.Services;
using Redpoint.SceneViewer.Components;
using Redpoint.SceneViewer.Models;
using Redpoint.SceneViewer.Utilities;

namespace Redpoint.SceneViewer.Services;

public class GlbAssetService  : Service
{
    private class OwnedResourcesComponent : Component
    {
        public string GlbAssetPath {get; set;}
        
        public StandardMaterial Material {get; set;}
    }
    
    public void RemoveAll()
    {
        var scene = RedpointApplication.Scene;
        if (scene is null)
        {
            return;
        }
        
        using var memoryAudit = EvergineDiagnostics.AuditMemory();
        
        var glbAssets = scene.Managers.EntityManager.FindAllByTag(EntityTags.GlbAsset).ToList();
 
        foreach (var asset in glbAssets)
        {
            Unload(asset, scene);
        }
    }

    private void Unload(Entity asset, RedpointScene scene)
    {
        asset.RemoveComponent<OwnedResourcesComponent>();
        scene.Managers.EntityManager.Remove(asset);
    }


    public async Task<SceneLoadResult> LoadGlbAsset(string glbFilePath,
                                                    string glbAssetName,
                                                    bool replaceExisting = false)
    {
        if (glbFilePath == null) throw new ArgumentNullException(nameof(glbFilePath));

        var scene = RedpointApplication.Scene;
        if (scene is null)
        {
            Console.WriteLine("Unable to locate the scene to load the requested GLB asset into.");
            return SceneLoadResult.Failed("Unable to locate 3D scene.");
        }

        return await LoadGlbAsset(glbFilePath, glbAssetName, scene,  replaceExisting);
    }
    
    private async Task<SceneLoadResult> LoadGlbAsset(string glbFilePath,
                                                     string glbAssetName,
                                                        RedpointScene scene,
                                                        bool replaceExisting = false)
    {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        if (string.IsNullOrWhiteSpace(glbFilePath)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(glbFilePath));
        if (string.IsNullOrWhiteSpace(glbAssetName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(glbAssetName));

        if (!File.Exists(glbFilePath))
        {
            return SceneLoadResult.Failed("The provided glb file does not exist.");
        }

        try
        {
            using var memoryAudit = EvergineDiagnostics.AuditMemory(eventId:glbAssetName);
            
            Console.WriteLine($"Loading the requested GLB asset ~{glbAssetName}~: '{glbFilePath}'");

            var glbAssets = new List<Entity>();

            if (replaceExisting)
            {
                glbAssets = scene.Managers.EntityManager.FindAllByTag(EntityTags.GlbAsset).ToList();
            }
            
            var assetsService = Application.Current.Container.Resolve<AssetsService>();

            var resources = new OwnedResourcesComponent()
            {
                GlbAssetPath = glbFilePath
            };
                
            Model model = null;
            using (var fileStream = File.OpenRead(glbFilePath))
            {
                async Task<Material> loadAndAssignMaterialsProxyMethod(MaterialData materialData)
                {
                    var material = await CustomMaterialAssigner(materialData);

                    resources.Material = material;
                    
                    return material.Material;
                }
                model = await Evergine.Runtimes.GLB.GLBRuntime.Instance.Read(fileStream, loadAndAssignMaterialsProxyMethod);
            }
            
            var entity = model.InstantiateModelHierarchy(assetsService);
                
            entity.Name = glbAssetName;
            entity.Tag = EntityTags.GlbAsset;
            entity.AddComponent(resources);
            
            model.Dispose();

            var didAttachCollider = TryAttachMeshCollider(entity);
            if (!didAttachCollider)
            {
                Console.WriteLine($"Unable to attach the mesh collider for {Path.GetFileName(glbFilePath)}. Camera controls and mesh collision may not work as expected.");
            }
                
            // Add to scene
            scene.Managers.EntityManager.Add(entity);
            
            if (replaceExisting)
            {
                foreach (var asset in glbAssets)
                {
                    Unload(asset, scene);
                }                
            }
            
            Console.WriteLine($"The glb asset '{glbAssetName}- {Path.GetFileName(glbFilePath)}' has been loaded. Evergine entity: {entity.Id}");
            
            DetectTouchMovement.Reset();
            
            return SceneLoadResult.Success();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            
            return SceneLoadResult.Failed("An unexpected error occured.\n\n" + e.Message);
        }
    }
    

    private bool TryAttachMeshCollider(Entity entity)
    {
        var meshComponent = entity.FindComponent(typeof(MeshComponent));
        if (meshComponent != null)
        {
            var collider = new MeshCollider3D()
            {
                IsConvex = false,
                
            };
                
            entity.AddComponent(new StaticBody3D()
            {
                CollisionCategories = CollisionCategory3D.All
                
            });
            entity.AddComponent(collider);
            
            Console.WriteLine("Attached mesh collider: " + entity.Id);

            return true;
        }

        foreach (var child in entity.ChildEntities)
        {
            var didAttach = TryAttachMeshCollider(child);
            if (didAttach)
            {
                return true;
            }
        }

        return false;
    }

    // Only Diffuse channel is needed
    private async Task<StandardMaterial> CustomMaterialAssigner(MaterialData data)
    {
        var assetsService = Application.Current.Container.Resolve<AssetsService>();

        // Get textures            
        var baseColor = await data.GetBaseColorTextureAndSampler();

        // Get Layer
        RenderLayerDescription layer;
        float alpha = data.BaseColor.A / 255.0f;
        switch (data.AlphaMode)
        {
            default:
            case AlphaMode.Mask:
            case AlphaMode.Opaque:
                layer = assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.OpaqueRenderLayerID);
                break;
            case AlphaMode.Blend:
                layer = assetsService.Load<RenderLayerDescription>(DefaultResourcesIDs.AlphaRenderLayerID);
                break;
        }

        // Create standard material            
        var effect = assetsService.Load<Effect>(DefaultResourcesIDs.StandardEffectID);
        StandardMaterial standard = new StandardMaterial(effect)
        {
            BaseColor = data.BaseColor,
            Alpha = alpha,
            BaseColorTexture = baseColor.Texture,
            BaseColorSampler = baseColor.Sampler,
            Metallic = data.MetallicFactor,
            Roughness = data.RoughnessFactor,
            EmissiveColor = data.EmissiveColor.ToColor(),                
            LayerDescription = layer,
            LightingEnabled = false
        };
        
        // Alpha test
        if (data.AlphaMode == AlphaMode.Mask)
        {
            standard.AlphaCutout = data.AlphaCutoff;
        }          
        
        Console.WriteLine("Material data bind: " + data.Name + " " + data.AlphaMode  + " => ID: "+ standard.Material.Id);

        return standard;
    }
}