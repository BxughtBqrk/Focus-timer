using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace FocusTimer.Services
{
    public class BrownNoiseProvider : ISampleProvider
    {
        private readonly Random _random = new Random();
        private float _lastOut = 0f;

        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

        public int Read(float[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i += 2)
            {
                float white = (float)(_random.NextDouble() * 2.0 - 1.0);
                
                // Leaky integrator to filter white noise into brown noise
                _lastOut = (_lastOut + (0.02f * white)) / 1.02f;
                
                // Scale up slightly and clamp to prevent distortion
                float output = _lastOut * 3.5f; 
                if (output > 1.0f) output = 1.0f;
                if (output < -1.0f) output = -1.0f;

                buffer[offset + i] = output; // Left channel
                if (i + 1 < count)
                {
                    buffer[offset + i + 1] = output; // Right channel
                }
            }
            return count;
        }
    }

    public static class AudioEngine
    {
        private static WaveOutEvent? _waveOut;
        private static VolumeSampleProvider? _volumeProvider;

        public static void Initialize()
        {
            if (_waveOut != null) return;

            try
            {
                var noise = new BrownNoiseProvider();
                _volumeProvider = new VolumeSampleProvider(noise) { Volume = 0.5f };
                
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_volumeProvider);
            }
            catch
            {
                // Ignore audio init failures (e.g. no audio device)
                _waveOut = null;
            }
        }

        public static void Play()
        {
            try
            {
                if (_waveOut == null) Initialize();
                if (_waveOut != null && _waveOut.PlaybackState != PlaybackState.Playing)
                {
                    _waveOut.Play();
                }
            }
            catch { }
        }

        public static void Pause()
        {
            try
            {
                if (_waveOut != null && _waveOut.PlaybackState == PlaybackState.Playing)
                {
                    _waveOut.Pause();
                }
            }
            catch { }
        }

        public static void SetVolume(float volume)
        {
            if (_volumeProvider != null)
            {
                // Clamp volume between 0.0 and 1.0
                _volumeProvider.Volume = Math.Max(0f, Math.Min(1f, volume));
            }
        }
    }
}
