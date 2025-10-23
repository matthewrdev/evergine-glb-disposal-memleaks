namespace Redpoint.SceneViewer.Models;

using System.Collections.Generic;
using System.Linq;

public static class CameraSensitivityIncrements
{

    public static CameraSensitivity FindCameraSensitivity(float distance)
    {
        var previousIncrement = Values[0];

        for (var i = 0; i < Values.Count; i++)
        {
            var increment = Values[i];

            if (distance < increment.FocalDistance
                && distance > previousIncrement.FocalDistance)
            {
                return previousIncrement;
            }

            previousIncrement = increment;
        }

        return Values.LastOrDefault();
    }

    public static float GetPanSpeedMultiplierForFocalDistance(float distance)
    {
        var increment = FindCameraSensitivity(distance);
        
        return increment?.PanSpeed ?? 3.0f;
    }
    
    public static readonly IReadOnlyList<CameraSensitivity> Values =
        new List<CameraSensitivity>()
        {
            new CameraSensitivity(0.5f, 0.03f, 0.03f),
            new CameraSensitivity(0.8f, 0.07f, 0.05f),
            new CameraSensitivity(1.2f, 0.1f, 0.07f),
            new CameraSensitivity(2f, 0.3f, 0.17f),
            new CameraSensitivity(2.5f, 0.5f, 0.25f),
            new CameraSensitivity(6f, 1.25f, 0.5f),
            new CameraSensitivity(10f, 1.5f, 0.8f),
            new CameraSensitivity(25f, 1.75f, 1.2f),
            new CameraSensitivity(40f, 2.5f, 1.75f),
            new CameraSensitivity(50f, 2.75f, 2.5f),
            new CameraSensitivity(60f, 4.5f,3f),
            new CameraSensitivity(70f, 6f,4f),
            new CameraSensitivity(100f, 9f,5f),
            new CameraSensitivity(150f, 12f,6f),
            new CameraSensitivity(200f, 16f,8f),
            new CameraSensitivity(280f, 20f,10f),
            new CameraSensitivity(400f, 25f,12.5f),
            new CameraSensitivity(800, 25f,15),
            new CameraSensitivity(1000f, 25f,20f),
            new CameraSensitivity(1300f, 25f,30f),
            new CameraSensitivity(1600, 25f,40f),
            new CameraSensitivity(1900, 25f,50f),
            new CameraSensitivity(2200, 25f,60f),
            new CameraSensitivity(2600, 25f,75),
            new CameraSensitivity(3000, 25f,85),
            new CameraSensitivity(4000, 25f,90),
            new CameraSensitivity(5000, 25f,100),
            new CameraSensitivity(8000, 25f,150f),
            new CameraSensitivity(10_000, 25f,200f),
            new CameraSensitivity(12_000, 25f,300f),
            new CameraSensitivity(15_000, 25f,500f),
            new CameraSensitivity(20_000f, 30f,1000f),
        };
    
}