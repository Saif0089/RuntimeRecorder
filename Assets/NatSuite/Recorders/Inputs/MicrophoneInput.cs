using System;
using System.Threading.Tasks;
using NatSuite.Recorders.Clocks;
using UnityEngine;

namespace NatSuite.Recorders.Inputs
{
	public class MicrophoneInput : MonoBehaviour
	{
		const float UPDATE_FREQUENCY = 1;
		const int MIC_BUFFER_DURATION = 2;

		private bool _recording = false;
		private string _device = null;
		private AudioClip _audioClip;
		private int _sampleRate => 96_000;
		private float[] _clipBuffer;

		private int _lastPos;
		private double _lastUpdateTime;

        //private async void Update()
        //{
        //	while (_recording)
        //	{
        //		double dtime = AudioSettings.dspTime - _lastUpdateTime;
        //		if (dtime >= UPDATE_FREQUENCY)
        //		{
        //			UpdateBuffer();
        //			_lastUpdateTime = AudioSettings.dspTime;
        //		}

        //		await Task.Yield();
        //	}
        //}

        //private void UpdateBuffer()
        //{
        //	int pos = Microphone.GetPosition(_device);
        //	if (pos == _lastPos) return;

        //	_audioClip.GetData(_clipBuffer, _lastPos);

        //	OnSampleBuffer(_clipBuffer);

        //	_lastPos = pos;
        //}

        private bool Init()
		{
			if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
			{
				Debug.LogWarning("User has not granted microphone authorization");
				return false;
			}

			int minFrequency, maxFrequency;
			Microphone.GetDeviceCaps(_device, out minFrequency, out maxFrequency);
			Debug.Log($"Caps: minFrequency: {minFrequency}; maxFrequency: {maxFrequency}");

			_clipBuffer = new float[MIC_BUFFER_DURATION * _sampleRate];
			_lastPos = 0;

			return true;
		}

		public void StartRecording()
		{
			if (!Init()) return;

			Debug.Log("Microphone devices:");
			foreach (var device in Microphone.devices)
			{
				Debug.Log("Device: " + device);
			}
			_audioClip = Microphone.Start(_device, true, MIC_BUFFER_DURATION, _sampleRate);

			var source = gameObject.AddComponent<AudioSource>();
			source.clip = _audioClip;
			source.volume = 1f * 0.000001f;
			source.loop = true;

			while (Microphone.GetPosition(_device) <= 0) { }

			source.Play();

			Debug.Log("Recording audio at frequency " + _sampleRate);

			_recording = true;
		}

		private void StopRecording()
		{
			Microphone.End(_device);
			Debug.Log("Stop recording");

			_recording = false;
		}

		#region --Operations--

		public IMediaRecorder recorder;
		public IClock clock;

		private void OnSampleBuffer(float[] data)
		{
			AndroidJNI.AttachCurrentThread();
			recorder.CommitSamples(data, clock.timestamp);
		}

        #endregion

        private void OnDestroy()
        {
			Dispose();
        }

        public void Dispose()
		{
			StopRecording();
		}
	}
}
