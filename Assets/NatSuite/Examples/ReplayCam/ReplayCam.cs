/* 
*   NatCorder
*   Copyright (c) 2022 NatML Inc. All Rights Reserved.
*/

namespace NatSuite.Examples
{

    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Recorders;
    using Recorders.Clocks;
    using Recorders.Inputs;
    using UnityEngine.UI;
    using System.IO;
    using System;
    public class ReplayCam : MonoBehaviour
    {
        public static ReplayCam Instance;

        [Header(@"Recording")]
        public int videoWidth = 1280;
        public int videoHeight = 720;
        [Space]

        [SerializeField] bool RecordAudio;

        [Space]
        [SerializeField] AudioSource audioSource;

        [Space]
        [Header(@"FilesPath")]
        string sourcePath;
        string destinationPath;

        [Space]

        [Header(@"Debug-Text")]
        [SerializeField] Toggle AudioToogle;
        public Text Status_Text;
        public Text Data_Path;

        private MP4Recorder recorder;
        private CameraInput cameraInput;
        private AudioInput audioInput;
        private void Awake()
        {
            Instance = this;
            sourcePath = Application.persistentDataPath;
            destinationPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Oxygen");
            AudioToogle.onValueChanged.AddListener(OnToggleValueChanged);
        }
        private void Start()
        {
#if UNITY_STANDALONE_WIN
            Data_Path.text = "Video Saved Path: " + destinationPath;
#else
            Data_Path.text="Video Path: " + sourcePath;
#endif
            Status_Text.text = "Press start button to record";

            GameObject audioSourceObject = GameObject.Find("Audio");

            if (audioSourceObject != null)
            {
                audioSource = audioSourceObject.GetComponent<AudioSource>();
            }
            else
            {
                Debug.LogWarning("AudioSource not found in the hierarchy.");
            }
        }
        public void StartRecording()
        {
            var frameRate = 30;
            var sampleRate = RecordAudio ? AudioSettings.outputSampleRate : 0;
            var channelCount = RecordAudio ? (int)AudioSettings.speakerMode : 0;
            recorder = new MP4Recorder(videoWidth, videoHeight, frameRate, sampleRate, channelCount, audioBitRate: 96_000);

            InitialaizeRecorder();

            audioSource.mute = audioInput == null;
            Status_Text.text = "Recording.......";
        }
        public async void StopRecording()
        {
            audioInput?.Dispose();
            cameraInput.Dispose();
            var path = await recorder.FinishWriting();
            Debug.Log($"Saved recording to: {path}");

#if UNITY_STANDALONE_WIN
        MoveFile(sourcePath, destinationPath);
        Status_Text.text = "Video Saved";
#else
            Status_Text.text = "Video Saved";
#endif
        }
        public void MoveFile(string sourceFolderPath, string targetFolderPath)
        {
            if (!Directory.Exists(destinationPath))
            {
                Directory.CreateDirectory(destinationPath);
            }

            string[] files = Directory.GetFiles(sourceFolderPath, "*.mp4");

            if (files.Length == 1)
            {
                string fileName = Path.GetFileName(files[0]);
                string destinationFile = Path.Combine(targetFolderPath, fileName);

                File.Move(files[0], destinationFile);
                Debug.Log("File moved successfully: " + fileName);
            }
            else if (files.Length == 0)
            {
                Debug.LogWarning("No .mp4 files found in the source folder.");
            }
        }
        public void QuitAPP()
        {
            Application.Quit();
        }
        public void OnToggleValueChanged(bool isOn)
        {
            RecordAudio = isOn;
        }

        private void OnApplicationQuit()
        {
            StopRecording();
        }

        public void InitialaizeRecorder()
        {
            var clock = new RealtimeClock();
            cameraInput = new CameraInput(recorder, clock, Camera.main);
            audioInput = RecordAudio ? new AudioInput(recorder, clock, audioSource, false) : null;
        }
    }
}