using UnityEngine;
using UnityEngine.Rendering;

public static class RuntimePerformanceSettings
{
    private const int MinimumTargetFrameRate = 60;
    private const int MaximumTargetFrameRate = 120;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        QualitySettings.vSyncCount = 0;
        OnDemandRendering.renderFrameInterval = 1;
        Application.targetFrameRate = GetTargetFrameRate();
    }

    private static int GetTargetFrameRate()
    {
        int refreshRate = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
        if (refreshRate <= 0)
            refreshRate = MinimumTargetFrameRate;

        return Mathf.Clamp(refreshRate, MinimumTargetFrameRate, MaximumTargetFrameRate);
    }
}
