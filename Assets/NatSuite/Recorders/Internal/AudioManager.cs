using UnityEngine;
using System;
using NatSuite.Recorders.Clocks;

namespace NatSuite.Recorders.Inputs
{
    public class AudioManager : MonoBehaviour
    {
		private IMediaRecorder _recorder;
		private IClock _clock;

		private float[] mixed;

        private void OnAudioFilterRead(float[] data, int channels)
        {
			if (mixed != null && _recorder != null)
			{
				AndroidJNI.AttachCurrentThread();
				_recorder.CommitSamples(mixed, _clock?.timestamp ?? 0L);
				mixed = null;
			}
        }

		private float ClampToValidRange(float value)
		{
			float min = -1.0f;
			float max = 1.0f;
			return (value < min) ? min : (value > max) ? max : value;
		}

		private float[] MixAndClampFloatBuffers(float[] bufferA, float[] bufferB)
		{
			int maxLength = Mathf.Min(bufferA.Length, bufferB.Length);
			float[] mixedFloatArray = new float[maxLength];

			for (int i = 0; i < maxLength; i++)
			{
				mixedFloatArray[i] = ClampToValidRange((bufferA[i] + bufferB[i]) / 2);
			}
			return mixedFloatArray;
		}

		public void Init(IMediaRecorder recorder, IClock clock)
        {
			_recorder = recorder;
			_clock = clock;
        }

		public void MixSamples(float[] data, bool mute)
        {
			if (mixed == null)
            {
				mixed = new float[data.Length];
				Array.Copy(data, mixed, data.Length);
            }
            else
            {
				mixed = MixAndClampFloatBuffers(mixed, data);
            }

			if (mute)
            {
				Array.Clear(data, 0, data.Length);
            }
        }
	}
}