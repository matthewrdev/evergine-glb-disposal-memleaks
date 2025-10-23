using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Evergine.Common.Input.Pointer;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;
using Redpoint.SceneViewer.Behaviours;
using Redpoint.SceneViewer.Models;

namespace Redpoint.SceneViewer.Components;

public class CameraController : Behavior
{
    [BindComponent(source: BindComponentSource.ChildrenSkipOwner)]
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private Camera3D camera3D = null;
    public Camera3D Camera => camera3D;

    private Transform3D Transform => camera3D.Transform;
    
    [BindService]
    // ReSharper disable once FieldCanBeMadeReadOnly.Local
    private GraphicsPresenter graphicsPresenter = null;

    private PointerDispatcher touchDispatcher;
    private Display display;

    private float orbitSpeed = 5.0f;
    private float panSpeed = 0.5f;

    private Vector3 targetPoint;
    private float distanceToTarget = 25f;
    private float yaw = 0f;
    private float pitch = 20f;
    
    public float pinchPanDampening = 0.8f;
    
    protected override bool OnAttached()
    {
        this.camera3D.FarPlane = 10_000f;
        
        return base.OnAttached();
    }
    
     private Vector3 frameStartPosition;
     private Vector3 frameEndPosition;
     public bool DidMoveThisFrame
     {
         get
         {
             var cameraLocationDiff = (frameEndPosition - frameStartPosition);

             return cameraLocationDiff.Length() > 0.01f;
         }
     }

    public void SetValues(Vector3 location, Quaternion facing)
    {
        distanceToTarget = 25.0f;
        Transform.Position = location;
        targetPoint = location + facing * Vector3.Forward * distanceToTarget;
        var direction = (targetPoint - location);
        direction.Normalize();

        yaw = facing.Y;
        pitch = facing.X;

        var up = new Vector3(Vector3.Up.X, Vector3.Up.Y, Vector3.Up.Z);
        
        Quaternion.CreateFromLookAt(ref direction, ref up, out var rot);

        var eulerANgles = Quaternion.ToEuler(rot);
        
        yaw = eulerANgles.Y;
        pitch = eulerANgles.X;

        UpdateCameraPosition();
    }

    public void InitialiseState(bool shouldUpdateLocation = true)
    {
        // Set initial target point in front of camera
        targetPoint = Transform.Position + Transform.Forward * distanceToTarget;
        Vector3 direction = (Transform.Position - targetPoint);
        direction.Normalize();
        
        var up = new Vector3(Vector3.Up.X, Vector3.Up.Y, Vector3.Up.Z);
        
        Quaternion.CreateFromLookAt(ref direction, ref up, out var rot);

        var eulerANgles = Quaternion.ToEuler(rot);
        
        yaw = eulerANgles.Y;
        pitch = eulerANgles.X;

        if (shouldUpdateLocation)
        {
            UpdateCameraPosition();
        }
    }

    private void HandleTouchZoom(TimeSpan gameTime)
    {
        var touches = touchDispatcher.Points;
        if (touches.Count != 2)
        {
            return;
        }
        
        var isZoom = false;
        var zoomAmount = 0f;
        
        if (Math.Abs(DetectTouchMovement.ZoomDelta) > DetectTouchMovement.MinPinchDistance)
        { // zoom
            isZoom = true;
            zoomAmount = DetectTouchMovement.ZoomDelta;
        }

        if (isZoom)
        {
            ApplyZoom(zoomAmount, gameTime);
        }
    }

    private void HandleTouchPan(TimeSpan gameTime)
    {
        var panDirection = DetectTouchMovement.PanDelta * PanSensitivity;
        
        ApplyPan(panDirection, applyTimeDelta:true, gameTime);
    }

    private void HandleTouchOrbit(TimeSpan gameTime)
    {
        var touches = touchDispatcher.Points;
        if (touches.Count != 1)
        {
            return;
        }

        var orbitDelta = DetectTouchMovement.OrbitDelta;
        
        const float orbitDampening = 0.02f;
        
        var touch = touches[0];
        
        ApplyRotation(orbitDelta.X * orbitDampening * (float)gameTime.TotalSeconds, orbitDelta.Y * orbitDampening * (float)gameTime.TotalSeconds);
    }

    private float GetZoomMultiplierForFocalDistance(float distance)
    {
        var increment = CameraSensitivityIncrements.FindCameraSensitivity(distance);

        return increment?.ZoomSpeed ?? 1.0f;
    }

    private void ApplyZoom(float scroll, TimeSpan gameTime)
    {
        if (Math.Abs(scroll) > 0.001f)
        {
            distanceToTarget -= scroll * GetZoomMultiplierForFocalDistance(distanceToTarget) *  pinchPanDampening * (float)gameTime.TotalSeconds;
            distanceToTarget = Math.Clamp(distanceToTarget, 1f, 25_000f);
        }
    }

    private void ApplyPan(Vector2 delta, bool applyTimeDelta, TimeSpan gameTime)
    {
        var modifier = CameraSensitivityIncrements.FindCameraSensitivity(distanceToTarget);
        
        Vector3 right = Transform.Right;
        Vector3 up = Transform.Up;

        var adjustment = modifier.PanSpeed * pinchPanDampening;
        if (applyTimeDelta)
        {
            adjustment *= panSpeed * (float)gameTime.TotalSeconds;
        }

        Vector3 move = (-right * delta.X + up * delta.Y) * adjustment;
        targetPoint += move;
    }

    private void ApplyRotation(float xDelta, float yDelta)
    {
        // CaptureFocalPoint();

        yaw -= xDelta * orbitSpeed;
        pitch -= yDelta * orbitSpeed;
        
        pitch = Math.Clamp(pitch, -85f * MathF.PI / 180f, 85f * MathF.PI / 180f);
    }
    
    public static Ray GetRayFromScreenPoint(Vector2 screenPoint, Camera3D camera, Vector2 screenSize)
    {
        // Normalize screen point to [-1, 1] range (clip space)
        float x = (2.0f * screenPoint.X) / screenSize.X - 1.0f;
        float y = 1.0f - (2.0f * screenPoint.Y) / screenSize.Y; // Flip Y axis
        Vector3 nearPoint = new Vector3(x, y, 0f);
        Vector3 farPoint = new Vector3(x, y, 1f);

        // Inverse ViewProjection
        var view = camera.View;
        var projection = camera.Projection;
        var viewProjectionInverse = (view * projection);
        viewProjectionInverse.Invert();

        // Unproject points
        Vector3 worldNear = Vector3.TransformCoordinate(nearPoint, viewProjectionInverse);
        Vector3 worldFar = Vector3.TransformCoordinate(farPoint, viewProjectionInverse);

        // Ray direction
        Vector3 direction = Vector3.Normalize(worldFar - worldNear);
    
        return new Ray(worldNear, direction);
    }
    
    public bool CaptureFocalPoint()
    {
        var display = camera3D.Display;
        var screenPos = new Vector2(display.Width / 2f, display.Height / 2f);

        var ray = GetRayFromScreenPoint(screenPos, camera3D, new Vector2(display.Width, display.Height));

        var hitResult = Managers.PhysicManager3D.RayCast(ref ray, 20_000f);

        if (hitResult.Succeeded == false)
        {
            return false;
        }
        
        targetPoint = hitResult.Point;
        distanceToTarget = Vector3.Distance(Transform.Position, targetPoint);
        
        // Recalculate yaw/pitch based on current camera-to-target direction
        var direction = Transform.Position -  targetPoint;
        direction.Normalize();
        
        var up = new Vector3(Vector3.Up.X, Vector3.Up.Y, Vector3.Up.Z);
    
        Quaternion.CreateFromLookAt(ref direction, ref up, out var rot);

        var euler = Quaternion.ToEuler(rot);

        yaw = euler.Y;
        pitch = euler.X;

        return true;
    }

    void UpdateCameraPosition()
    {
        Quaternion rotation = Quaternion.CreateFromEuler(new Vector3(pitch, yaw, 0.0f));
        Vector3 offset = rotation * Vector3.Forward;
        Transform.Position = targetPoint - offset * distanceToTarget;
        Transform.LookAt(targetPoint);
    }
    
    private bool isLocked = false;

    public bool IsLocked => isLocked;

    public bool IsUnlocked => this.IsEnabled;
    public float OrbitSensitivity { get; set; }= 1.0f;
    public float PanSensitivity { get; set; }= 1.0f;
    public float ZoomSensitivity { get; set; } = 1.0f;
    public void Unlock()
    {
        Console.WriteLine("The camera has been un-locked.");
        isLocked = false;
    }

    public void Lock()
    {
        Console.WriteLine("The camera has been locked.");
        isLocked = true;
    }

    
    public async Task SetCameraState(Vector3 requestedPosition, Vector3 requestedLookAt, bool animate, float animationDurationSeconds)
    {
        if (animate)
        {
            CaptureFocalPoint();            
            Console.WriteLine($"Camera fly to animation  requested: {requestedPosition}, {requestedLookAt}, {animationDurationSeconds} seconds");

            this.Lock();

            var animation = GetOrCreateCameraAnimationHandler();
            animation.FlyTo(requestedPosition, this.targetPoint, requestedLookAt, animationDurationSeconds, () =>
            {
                CaptureFocalPoint();
                this.Unlock();
            });
        }
        else
        {
            var lookAtMatrix = Matrix4x4.CreateLookAt(requestedPosition, requestedLookAt, Vector3.Up);

            var rotation = Quaternion.CreateFromRotationMatrix(lookAtMatrix);
            
            SetValues(requestedPosition, rotation);
        }
    }

    private Vector3 CaptureTargetFocalPoint(Vector3 position, Quaternion rotation)
    {
        Vector3 forward = Vector3.Transform(Vector3.Backward, rotation);
        Ray ray = new Ray(position, forward);

        var hitResult = this.Managers.PhysicManager3D.RayCast(ref ray, 20_000f);

        if (hitResult.Succeeded == false)
        {
            return ray.Position + (ray.Direction * 25f);
        }

        return hitResult.Point;
    }

    private FlyToCameraAnimation GetOrCreateCameraAnimationHandler()
    { 
        var existingAnimation = Owner.FindComponent<FlyToCameraAnimation>();
        if (existingAnimation != null)
        {
            return existingAnimation;
        }
        
        var animation = new FlyToCameraAnimation(camera3D);
        Owner.AddComponent(animation);
        
        return animation;
    }

    protected override void Update(TimeSpan gameTime)
    {
        DetectTouchMovement.TryCalculate(touchDispatcher);
        
        if (IsLocked)
        {
            return;
        }
        
        this.graphicsPresenter.TryGetDisplay("DefaultDisplay", out var presenterDisplay);
        if (presenterDisplay != this.display) 
        {
            this.camera3D.DisplayTagDirty = true;
            this.RefreshDisplay();
        };
        
        frameStartPosition = Transform.Position;

        HandleTouchOrbit(gameTime);

        HandleTouchPan(gameTime);

        HandleTouchZoom(gameTime);
        
        UpdateCameraPosition();
        
        frameEndPosition = Transform.Position;
    }
    
    
    private void RefreshDisplay()
    {
        UnbindTouchEvents();
            
        this.display = this.camera3D.Display;
        if (this.display != null)
        {
            this.touchDispatcher = this.display.TouchDispatcher;
        }
            
        BindTouchEvents();
    }

    private class TouchPoint
    {
        public TouchPoint(Point startPoint, long pointerId, DateTime startedAt)
        {
            Start = startPoint;
            PointerId = pointerId;
            StartedAt = startedAt;
        }


        public Point Start { get; }
        
        public Point? End { get; private set; }

        public Point Point
        {
            get
            {
                if (End.HasValue == false)
                {
                    return Point.Zero;
                }
                
                var x = (End.Value.X + Start.X) / 2;
                var y = (End.Value.Y + Start.Y) / 2;
                
                return new Point(x, y);
            }
        }
        
        public long PointerId { get; }

        public bool IsTapEvent
        {
            get
            {
                if (EndedAt.HasValue == false
                    || End.HasValue == false)
                {
                    return false;
                }
                
                var duration = EndedAt.Value - StartedAt;
                var endToStartVector = End.Value - Start;
                var length = endToStartVector.ToVector2().Length();

                if (length > 80)
                {
                    return false;
                }

                if (duration > TimeSpan.FromMilliseconds(100))
                {
                    return false;
                }

                return true;
            }
        }

        public bool IsExpired => (DateTime.UtcNow - StartedAt) > TimeSpan.FromMilliseconds(1000);
        
        public DateTime StartedAt { get; }
        
        public DateTime? EndedAt { get; private set; }

        public void SetEnded(Point end, DateTime endedAt)
        {
            EndedAt = endedAt;
            End = end;
        }
    }
    
    private readonly List<TouchPoint> touchPointQueue = new List<TouchPoint>();

    private void UnbindTouchEvents()
    {
        if (touchDispatcher != null)
        {
            touchDispatcher.PointerDown -= OnPointerPressed;
            touchDispatcher.PointerMove -= OnPointerMoved;
            touchDispatcher.PointerUp -= OnPointerReleased;
        }
    }
    
    DateTime lastDoubleTapInteractionAt = DateTime.MinValue;
    
    private void OnPointerReleased(object sender, PointerEventArgs e)
    {
        var touchPoint = touchPointQueue.LastOrDefault(tp => tp.PointerId == e.Id);

        if (touchPoint != null)
        {
            touchPoint.SetEnded(e.Position, DateTime.UtcNow);
        }

        touchPointQueue.RemoveAll(tp => tp.IsExpired);

        var tapEvent = touchPointQueue.FirstOrDefault(tp => tp.IsTapEvent);

        TryDetectDoubleTap();
    }

    private void TryDetectDoubleTap()
    {
        var matchingTouchPoints = touchPointQueue.Where(tp => tp.IsTapEvent).ToList();

        var timeSinceLastDoubleTap = DateTime.UtcNow - lastDoubleTapInteractionAt;

        if (matchingTouchPoints.Count >= 2 && timeSinceLastDoubleTap.TotalSeconds > 1)
        {
            var startPoint = matchingTouchPoints[0].Point.ToVector2();
            var finalPoint = matchingTouchPoints[1].Point.ToVector2();
            
            var distanceBetweenTaps = finalPoint - startPoint;
            if (distanceBetweenTaps.Length() > 100)
            {
                return;
            }
            
            var ray = GetRayFromScreenPoint(finalPoint, camera3D, new Vector2(display.Width, display.Height));
            
            var hitResult = this.Managers.PhysicManager3D.RayCast(ref ray, 20_000f);

            if (hitResult.Succeeded)
            {
                Console.WriteLine("Double tap gesture detected at: " + finalPoint);
                lastDoubleTapInteractionAt = DateTime.UtcNow;
            
                var newLookAt = hitResult.Point;
                var newPosition = hitResult.Point + (this.Transform.Position  -  hitResult.Point) / 2;

                SetCameraState(newPosition, newLookAt, animate: true, animationDurationSeconds: 0.5f).ConfigureAwait(false);
            }
        }
    }
    
    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
    }

    private void OnPointerPressed(object sender, PointerEventArgs e)
    {
        var touchPoint = new TouchPoint(e.Position, e.Id, DateTime.UtcNow);
        touchPointQueue.Add(touchPoint);
        
        CaptureFocalPoint();
    }
    
    private void BindTouchEvents()
    {
        UnbindTouchEvents();
            
        if (touchDispatcher != null)
        {
            touchDispatcher.PointerDown += OnPointerPressed;
            touchDispatcher.PointerMove += OnPointerMoved;
            touchDispatcher.PointerUp += OnPointerReleased;
        }
    }
}
