namespace Redpoint.SceneViewer.Models;

public class CameraSensitivity
{
    public CameraSensitivity(float focalDistance, float panSpeed, float zoomSpeed)
    {
        FocalDistance = focalDistance;
        PanSpeed = panSpeed;
        ZoomSpeed = zoomSpeed;
    }

    public float FocalDistance { get; }
        
    public float PanSpeed { get; }
        
    public float ZoomSpeed { get; }
}