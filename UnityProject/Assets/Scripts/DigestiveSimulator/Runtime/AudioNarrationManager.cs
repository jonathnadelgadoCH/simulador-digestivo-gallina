using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace DigestiveSimulator.Runtime
{
    public enum NarrationState { Idle, Loading, Playing, Paused, Completed, Missing, Error }

    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioNarrationManager : MonoBehaviour
    {
        private AudioSource source;
        private Coroutine loadRoutine;
        private bool paused;

        public NarrationState State { get; private set; } = NarrationState.Idle;
        public string CurrentFile { get; private set; }
        public string LastError { get; private set; }
        public bool IsMuted => source != null && source.mute;

        public event Action<NarrationState> StateChanged;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
        }

        private void Update()
        {
            if (State == NarrationState.Playing && !source.isPlaying && !paused) SetState(NarrationState.Completed);
        }

        public void PlayNarration(string relativePath)
        {
            StopNarration();
            if (string.IsNullOrWhiteSpace(relativePath)) { SetState(NarrationState.Missing); return; }
            CurrentFile = relativePath;
            loadRoutine = StartCoroutine(LoadAndPlay(relativePath));
        }

        public void PauseNarration()
        {
            if (State != NarrationState.Playing) return;
            source.Pause();
            paused = true;
            SetState(NarrationState.Paused);
        }

        public void ResumeNarration()
        {
            if (State != NarrationState.Paused) return;
            source.UnPause();
            paused = false;
            SetState(NarrationState.Playing);
        }

        public void ReplayNarration()
        {
            var file = CurrentFile;
            if (string.IsNullOrWhiteSpace(file)) { SetState(NarrationState.Missing); return; }
            PlayNarration(file);
        }

        public void SetMuted(bool muted)
        {
            source.mute = muted;
        }

        public void StopNarration()
        {
            if (loadRoutine != null) StopCoroutine(loadRoutine);
            loadRoutine = null;
            paused = false;
            if (source != null)
            {
                source.Stop();
                if (source.clip != null) Destroy(source.clip);
                source.clip = null;
            }
            CurrentFile = null;
            LastError = null;
            SetState(NarrationState.Idle);
        }

        private IEnumerator LoadAndPlay(string relativePath)
        {
            SetState(NarrationState.Loading);
            var root = StreamingAssetClient.Join(Application.streamingAssetsPath, "DigestiveSimulator");
            var url = StreamingAssetClient.Join(root, relativePath);
            using (var request = UnityWebRequestMultimedia.GetAudioClip(url, ResolveAudioType(relativePath)))
            {
                yield return request.SendWebRequest();
                loadRoutine = null;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    LastError = $"Could not load narration '{relativePath}': {request.error}";
                    SetState(NarrationState.Error);
                    yield break;
                }
                source.clip = DownloadHandlerAudioClip.GetContent(request);
                if (source.clip == null)
                {
                    LastError = $"El archivo de narración '{relativePath}' no produjo un clip de audio válido.";
                    SetState(NarrationState.Error);
                    yield break;
                }
                source.Play();
                SetState(NarrationState.Playing);
            }
        }

        private static AudioType ResolveAudioType(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".wav": return AudioType.WAV;
                case ".mp3": return AudioType.MPEG;
                case ".ogg": return AudioType.OGGVORBIS;
                default: return AudioType.UNKNOWN;
            }
        }

        private void SetState(NarrationState state)
        {
            if (State == state) return;
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
