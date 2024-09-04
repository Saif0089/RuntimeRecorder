using NatSuite.Recorders.Inputs;
using NatSuite.Recorders.Clocks;
using UnityEngine;
using System.Threading.Tasks;

namespace NatSuite.Recorders
{
    public class MicrophoneRecorder : MonoBehaviour
    {
        private AudioInput audioInput;
        private AudioSource microphoneSource;

        private void OnDestroy()
        {
            if (microphoneSource != null)
            {
                StopRecording();
            }
        }

        public async void StartRecording(IMediaRecorder recorder, IClock clock, AudioManager manager)
        {
            // Start microphone
            microphoneSource = gameObject.AddComponent<AudioSource>();
            audioInput = new AudioInput(recorder, clock, microphoneSource, manager, true);
            microphoneSource.mute =
            microphoneSource.loop = true;
            microphoneSource.bypassEffects =
            microphoneSource.bypassListenerEffects = false;
            microphoneSource.clip = Microphone.Start(null, true, 1, AudioSettings.outputSampleRate);
            while (!(Microphone.GetPosition(null) > 0))
            {
                await Task.Yield();
            }
            microphoneSource.Play();

            microphoneSource.mute = false;
        }

        public void StopRecording()
        {
            audioInput.Dispose();

            Microphone.End(null);
            Destroy(microphoneSource);
            microphoneSource = null;
            Destroy(this);
        }
    }
}