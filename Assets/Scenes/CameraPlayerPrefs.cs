using UnityEngine;

public static class CameraPlayerPrefs
{
    public static string flipCameraRotation = "flipCameraRotation";
    public static string flipCameraZoom = "flipCameraZoom";

    public static float CameraRotationMultiplier => GetBoolFlipMultiplier(flipCameraRotation);
    public static float CameraZoomMultiplier => GetBoolFlipMultiplier(flipCameraZoom);

    private static float GetBoolFlipMultiplier(string key)
    {
        var raw = PlayerPrefs.GetInt(key, 0);
        return raw == 0 ? 1f : -1f;
    }
}
