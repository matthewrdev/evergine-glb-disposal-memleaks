using Evergine.Framework.Graphics;
using Evergine.Framework;
using Evergine.Mathematics;
using System;

namespace Redpoint.SceneViewer.Behaviours;


public class FlyToCameraAnimation : Behavior
{
    private readonly Camera3D camera3D;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    
    private Vector3 startLookAt;
    private Vector3 targetLookAt;

    private float duration;
    private float elapsedTime;
    private bool isAnimating = false;
    private Action onComplete;

    public FlyToCameraAnimation(Camera3D camera3D)
    {
        this.camera3D = camera3D;
    }

    public void FlyTo(Vector3 endPosition, 
                         Vector3 fromLookAt,
                         Vector3 toLookAt,
                         float durationSeconds, 
                        Action onComplete = null)
    {
        this.startPosition = camera3D.Transform.Position;
        this.startLookAt = fromLookAt;

        this.targetPosition = endPosition;
        this.targetLookAt = toLookAt;

        this.duration = durationSeconds;
        this.elapsedTime = 0f;
        this.isAnimating = true;
        this.onComplete = onComplete;
    }

    protected override void Update(TimeSpan gameTime)
    {
        if (!this.isAnimating)
        {
            return;
        }

        this.elapsedTime += (float)gameTime.TotalSeconds;
        float t = MathF.Min(this.elapsedTime / this.duration, 1.0f);

        // Optional easing for smooth acceleration/deceleration
        t = SmoothStep(t);
        

        // Interpolate position and rotation
        var newPosition = Vector3.Lerp(this.startPosition, this.targetPosition, t);
        var newLookat = Vector3.Lerp(this.startLookAt, this.targetLookAt, t);
        
        camera3D.Transform.Position = newPosition;
        
        var up = new Vector3(Vector3.Up.X, Vector3.Up.Y, Vector3.Up.Z);
        
        camera3D.Transform.LookAt(newLookat, up);
        
        if (elapsedTime > this.duration)
        {
            this.isAnimating = false;
            this.onComplete?.Invoke();
        }
    }

    private float SmoothStep(float t)
    {
        // Smoothstep easing: smooth in & out
        return t * t * (3f - 2f * t);
    }
}
