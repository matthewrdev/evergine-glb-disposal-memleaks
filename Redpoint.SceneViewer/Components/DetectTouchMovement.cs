using System;
using Evergine.Common.Input.Pointer;
using Evergine.Mathematics;

namespace Redpoint.SceneViewer.Components;

public class DetectTouchMovement
{
    public static Vector2 PanDelta;
    public static Vector2 OrbitDelta;
    public static float ZoomDelta;
    
    const float pinchTurnRatio = (float)(Math.PI / 2);
    const float minTurnAngle = 0;

    const float pinchRatio = 1;
    public const float MinPinchDistance = 0.038f;

    const float panRatio = 1;
    const float minPanDistance = 0;
    
    private static Vector2? lastSingleTouch;
    private static Vector2? lastTwoFingerMidpoint;
    private static float lastTwoFingerDistance;

    public static bool HasValues
    {
        get
        {
            const float epsilon = 1e-6f;
            
            return Math.Abs(PanDelta.Length() ) > epsilon
                   || Math.Abs(OrbitDelta.Length() ) > epsilon
                    || Math.Abs(ZoomDelta) > epsilon;
        }
    }

    // Used to discard any changes detected by the touch movement.
    public static void Reset()
    {
        lastSingleTouch = null;
        lastTwoFingerMidpoint = null;
        lastTwoFingerMidpoint = null;
        lastTwoFingerDistance = 0;
        lastSingleTouch = null;
    }
    

    /// <summary>
    ///   Calculates Pinch and Turn - This should be used inside LateUpdate
    /// </summary>
    public static bool TryCalculate(PointerDispatcher dispatcher)
    {
        PanDelta = Vector2.Zero;
        OrbitDelta = Vector2.Zero;
        ZoomDelta = 0;
            
        if (dispatcher == null)
        {
            return false;
        }
        
        var touches = dispatcher.Points;

        if (touches.Count == 1)
        {
            var touch = touches[0];
            if (lastSingleTouch.HasValue)
            {
                
                OrbitDelta = touch.Position.ToVector2() - lastSingleTouch.Value;
            }
            lastSingleTouch = touch.Position.ToVector2();
            lastTwoFingerMidpoint = null;
        }
        else if (touches.Count == 2)
        {
            var p1 = touches[0].Position.ToVector2();
            var p2 = touches[1].Position.ToVector2();

            var midpoint = (p1 + p2) * 0.5f;
            var dist = Vector2.Distance(p1, p2);

            if (lastTwoFingerMidpoint.HasValue)
            {
                PanDelta = midpoint - lastTwoFingerMidpoint.Value;
                    
                ZoomDelta = dist - lastTwoFingerDistance;
            }

            lastTwoFingerMidpoint = midpoint;
            lastTwoFingerDistance = dist;
            lastSingleTouch = null;
        }
        else
        {
            lastSingleTouch = null;
            lastTwoFingerMidpoint = null;
        }
        

        return false;
    }


    private static float DeltaAngle(float current, float target)
    {
        float num = Repeat(target - current, 360f);
        if ((double) num > 180.0)
            num -= 360f;
        return num;
    }

    private static float Repeat(float t, float length)
    {
        return (float)Math.Clamp(t - Math.Floor(t / length) * length, 0.0f, length);
    }

    private static float Angle(Evergine.Mathematics.Vector2 pos1, Evergine.Mathematics.Vector2 pos2)
    {
        var from = pos2 - pos1;
        var to = new Vector2(1, 0);

        float result = Vector2.Angle(from, to);
        Vector3 cross = Vector3.Cross(from.ToVector3(0.0f), to.ToVector3(0.0f));

        if (cross.Z > 0)
        {
            result = 360f - result;
        }

        return result;
    }
}