using UnityEngine;
using UnityEngine.UI;
using NatSuite.Recorders;
using NatSuite.Recorders.Clocks;
using NatSuite.Recorders.Inputs;
using System;
using System.IO;

public class VideoRecorder : MonoBehaviour
{
	public Action<string> OnSavedToGallery;
	public Action<bool> OnRecordToggled;

	[SerializeField] private AudioManager audioManagerPrefab;

	[SerializeField] private Camera[] _targetCameras;
	[SerializeField] private AudioSource[] _targetAudioSources;

	[SerializeField] private string _album = "Recordings";
	[SerializeField] private int _frameRate = 60;

	[SerializeField]
	private bool _recordMainCamera = true,
								  _recordAudioListener = false,
								  _recordMicrophone = true;

	[SerializeField] private Toggle _recordToggle;

	private MP4Recorder _recorder = null;
	private CameraInput _cameraInput = null;
	private AudioInput[] _audioInputs = null;
	private MicrophoneRecorder _microphoneRecorder = null;
	private AudioManager _manager;

	private void OnDisable()
	{
		if (_recorder != null)
		{
			CancelAndDiscard();
		}
	}

#if UNITY_EDITOR
	//Editor crashes if window is minimised or changed
	private void OnApplicationPause(bool pause)
	{
		if (pause && _recorder != null)
		{
			CancelAndDiscard();
		}
	}
#endif

	public void OnToggledRecord(bool isOn)
	{
		if (isOn)
		{
			StartRecording();
		}
		else
		{
			StopRecordinng();
		}
	}

	private void StartRecording()
	{
		_manager = Instantiate(audioManagerPrefab, transform);

		var sampleRate = AudioSettings.outputSampleRate;
		var channelCount = (int)AudioSettings.speakerMode;

		string newVideoFilePath = CreateNewVideoFilePath();
		Vector2Int videoSize = new Vector2Int(Screen.width, Screen.height);

		//Odd numbers not accepted by Recorders
		videoSize = Even(videoSize);

		var clock = new RealtimeClock();
		_recorder = new MP4Recorder(newVideoFilePath, videoSize.x, videoSize.y, _frameRate, sampleRate, channelCount, audioBitRate: 96_000);

		if (_recordMainCamera)
		{
			Array.Resize(ref _targetCameras, _targetCameras.Length + 1);
			_targetCameras[_targetCameras.Length - 1] = Camera.main;
		}

		_cameraInput = new CameraInput(_recorder, clock, _targetCameras);

		_audioInputs = new AudioInput[_targetAudioSources.Length + (_recordAudioListener ? 1 : 0)];
		for (int i = 0; i < _targetAudioSources.Length - 1; i++)
		{
			_audioInputs[i] = new AudioInput(_recorder, clock, _targetAudioSources[i], _manager, false);
		}

		if (_recordAudioListener)
		{
			_audioInputs[_audioInputs.Length - 1] = new AudioInput(_recorder, FindObjectOfType<AudioListener>(), _manager);
		}

		if (_recordMicrophone)
        {
			_microphoneRecorder = gameObject.AddComponent<MicrophoneRecorder>();
			_microphoneRecorder.StartRecording(_recorder, clock, _manager);
        }
		_manager.Init(_recorder, clock);

		Debug.Log($"VideoRecorder => Recording started with ({videoSize.x}, {videoSize.y})!");

		if (OnRecordToggled != null)
		{
			OnRecordToggled(true);
		}
	}

	//Need this function for making the width of video an even number,
	//NatCorder doesn't accept odd width numbers
	private Vector2Int Even(Vector2Int vec)
	{
		return new Vector2Int(Even(vec.x), Even(vec.y));
	}

	private int Even(float x)
	{
		return Mathf.FloorToInt(x - (x % 2));
	}

	private void CancelAndDiscard()
	{
		_recordToggle.SetIsOnWithoutNotify(false);

		StopRecordinng(true);
	}

	private async void StopRecordinng(bool discard = false)
	{
		if (_recorder != null)
		{
			_cameraInput.Dispose();
			_cameraInput = null;

			for (int i = 0; i < _audioInputs.Length; i++)
			{
				if (_audioInputs[i] != null)
				{
					_audioInputs[i].Dispose();
					_audioInputs[i] = null;
				}
			}

			if (_microphoneRecorder != null)
            {
				_microphoneRecorder.StopRecording();
				_microphoneRecorder = null;
			}

			var recordingMP4FilePath = await _recorder.FinishWriting();
			_recorder = null;

			if (_manager != null)
            {
				Destroy(_manager);
				_manager = null;
            }

			if (OnRecordToggled != null)
			{
				OnRecordToggled(false);
			}

			string videoFileName = Path.GetFileName(recordingMP4FilePath);

			Debug.Log("VideoRecorder => Recording finished!");

#if UNITY_EDITOR
			Debug.Log($"VideoRecorder => Video saved to \n----- {recordingMP4FilePath} -----");
			OnSavedToGallery?.Invoke(videoFileName);
			return;
#endif

			//for saving we copy the file to gallery using
			//NativeGallery in case of mobiles, but
			//ProjectDirectory/gallery for other platforms
			if (!discard)
			{
				NativeGallery.Permission permission = NativeGallery.SaveVideoToGallery(recordingMP4FilePath, _album, videoFileName,
					(success, path) =>
				{
					Debug.Log($"VideoRecorder => Video saved to gallery in {_album}");

					if (OnSavedToGallery != null)
					{
						OnSavedToGallery(videoFileName);
					}

						//delete file frome cache, because we just copied it to Gallery
						File.Delete(recordingMP4FilePath);
				});

				if (permission == NativeGallery.Permission.Denied)
				{
					Debug.Log($"VideoRecorder => Permission Denied!");
				}

				File.Delete(recordingMP4FilePath);
			}
			else
			{
				Debug.Log("VideoRecorder => Recording stopped and discarded!");
				File.Delete(recordingMP4FilePath);
			}
		}
		else
		{
			Debug.Log($"VideoRecorder => Recording not started but StopRecording was called!");
		}
	}

	private string CreateNewVideoFilePath()
	{
#if UNITY_EDITOR || UNITY_EDITOR_OSX || UNITY_STANDALONE || UNITY_STANDALONE_OSX
		string directory = Path.Combine(Directory.GetCurrentDirectory(), "gallery", _album);
#else
			string directory = Application.persistentDataPath;
#endif
		if (!Directory.Exists(directory))
		{
			Directory.CreateDirectory(directory);
		}

		string timestamp = DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss_fff");
		string name = $"recording_{timestamp}.mp4";
		string path = Path.Combine(directory, name);

		return path;
	}
}