using System.Linq;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Mathematics;

namespace Redpoint.SceneViewer;

public class RedpointScene : Scene
{
    public override void RegisterManagers()
    {
        base.RegisterManagers();
            
        this.Managers.AddManager(new global::Evergine.Bullet.BulletPhysicManager3D());
    }
}