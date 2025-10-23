using System;
using System.Collections.Generic;
using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Components.Primitives;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Graphics.Effects;
using Evergine.Framework.Graphics.Materials;
using Evergine.Framework.Physics3D;
using Evergine.Framework.Services;
using Evergine.Mathematics;

namespace Redpoint.SceneViewer.Utilities;

public class Debug3dHelper
{
    public static void DrawDebugCircle(Scene scene, Vector3 position)
    {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        
        var assetsService = Application.Current.Container.Resolve<AssetsService>();

        RenderLayerDescription layer = assetsService.Load<RenderLayerDescription>(EvergineContent.RenderLayers.Opaque);


        // Create standard material            
        var effect = assetsService.Load<Effect>(DefaultResourcesIDs.StandardEffectID);
        StandardMaterial standard = new StandardMaterial(effect)
        {
            BaseColor = Color.Red,
            Alpha = 1f,           
            LayerDescription = layer,
        };

        
        // Create the sphere entity
        var sphereEntity = new Entity("Sphere")
            .AddComponent(new Transform3D()
            {
                Position = position,
                Scale = new Vector3(1f),
            })
            .AddComponent(new SphereMesh()
            {
                Diameter = 1f,
            })
            .AddComponent(new MaterialComponent() { Material = standard.Material })
            .AddComponent(new MeshRenderer());
        
        scene.Managers.EntityManager.Add(sphereEntity);
    }

    public static void DrawDebugLine(Scene scene, Vector3 start, Vector3 end, Evergine.Common.Graphics.Color? color = null)
    {
        if (scene == null) throw new ArgumentNullException(nameof(scene));
        
        // Create the LineMesh
        var lineEntity = new Entity("DebugLine")
            .AddComponent(new Transform3D())
            .AddComponent(new MeshRenderer())
            .AddComponent(new LineMesh());
        
        
        var lineMesh = new LineMesh
        {
            LineType = LineType.LineStrip,
            IsLoop = false
        };

        List<LinePointInfo> linePoints = new List<LinePointInfo>()
        {
            new LinePointInfo()
            {
                Position = start,
                Color = color ?? Color.Green,
                Thickness = 0.5f
            },

            new LinePointInfo()
            {
                Position = end,
                Color = color ?? Color.Green,
                Thickness = 0.5f
            }
        };

        lineMesh.IsCameraAligned = true;
        lineMesh.LinePoints = linePoints;

        var line = new Entity("DebugLine")
            .AddComponent(new Transform3D())
            .AddComponent(lineMesh)
            .AddComponent(new LineMeshRenderer3D()
            {
            });
        
        scene.Managers.EntityManager.Add(line);
    }
}