using System.Runtime.InteropServices;

public static class MicUtils
{
#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern void _StartRecording();

    public static void StartRecording()
    {
        _StartRecording();
    }

    [DllImport("__Internal")]
    private static extern void _StopRecording();


    public static void StopRecording()
    {
        _StopRecording();
    }
#endif
}