using System;
using UnityEngine;

public static class HapticFeedback
{
    private const int SelectionDurationMs = 18;
    private const int SelectionAmplitude = 70;

    private static readonly long[] MatchPattern = { 0, 22, 35, 32, 45, 45 };
    private static readonly int[] MatchAmplitudes = { 0, 80, 0, 135, 0, 210 };

    public static void PlaySelection()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroid(SelectionDurationMs, SelectionAmplitude);
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

    public static void PlayMatch()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        VibrateAndroidPattern(MatchPattern, MatchAmplitudes);
#elif UNITY_IOS && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void VibrateAndroid(long milliseconds, int amplitude)
    {
        try
        {
            AndroidJavaObject vibrator = GetVibrator();
            if (vibrator == null)
                return;

            if (GetAndroidSdkVersion() >= 26)
            {
                using AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                using AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                    "createOneShot",
                    milliseconds,
                    Mathf.Clamp(amplitude, 1, 255));
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", milliseconds);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Haptic selection failed: {exception.Message}");
        }
    }

    private static void VibrateAndroidPattern(long[] timings, int[] amplitudes)
    {
        try
        {
            AndroidJavaObject vibrator = GetVibrator();
            if (vibrator == null)
                return;

            if (GetAndroidSdkVersion() >= 26)
            {
                using AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                using AndroidJavaObject effect = vibrationEffectClass.CallStatic<AndroidJavaObject>(
                    "createWaveform",
                    timings,
                    amplitudes,
                    -1);
                vibrator.Call("vibrate", effect);
            }
            else
            {
                vibrator.Call("vibrate", timings, -1);
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Haptic match pattern failed: {exception.Message}");
        }
    }

    private static AndroidJavaObject GetVibrator()
    {
        using AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        return activity?.Call<AndroidJavaObject>("getSystemService", "vibrator");
    }

    private static int GetAndroidSdkVersion()
    {
        using AndroidJavaClass versionClass = new AndroidJavaClass("android.os.Build$VERSION");
        return versionClass.GetStatic<int>("SDK_INT");
    }
#endif
}
