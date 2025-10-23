using System;

namespace Redpoint.SceneViewer.Models;

public struct SceneLoadResult
{
    private SceneLoadResult(bool success, string message)
    {
        IsSuccess = success;
        Message = message;

        if (!success)
        {
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(message));
        }
    }

    public bool IsSuccess { get; }
    
    public string Message { get; }


    public static SceneLoadResult Success() => new SceneLoadResult(success:true, string.Empty);
    
    public static SceneLoadResult Failed(string message) => new SceneLoadResult(success:false, message);
}