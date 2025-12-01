using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MinorShift.Emuera.Content
{
    /// <summary>
    /// Manages audio playback for Emuera games.
    /// Supports PLAYSOUND, STOPSOUND, PLAYBGM, STOPBGM, and EXISTSOUND commands from Emuera EM/EE.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Create a new GameObject for AudioManager
                    GameObject go = new GameObject("AudioManager");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        // Sound directory path (typically "sound/" folder in the game directory)
        private string _soundDir;
        
        // Audio sources for sound effects and BGM
        private AudioSource _soundSource;
        private AudioSource _bgmSource;
        
        // Cache for loaded audio clips
        private Dictionary<string, AudioClip> _audioCache = new Dictionary<string, AudioClip>();
        
        // Volume settings (0-100)
        private int _soundVolume = 100;
        private int _bgmVolume = 100;

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            // Create audio sources
            _soundSource = gameObject.AddComponent<AudioSource>();
            _soundSource.playOnAwake = false;

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;

            // Initialize sound directory
            InitializeSoundDirectory();
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            ClearCache();
        }

        /// <summary>
        /// Initializes the sound directory path.
        /// </summary>
        private void InitializeSoundDirectory()
        {
            string exeDir = Program.ExeDir;
            _soundDir = exeDir + "sound/";
            if (!Directory.Exists(_soundDir))
            {
                _soundDir = exeDir + "Sound/";
                if (!Directory.Exists(_soundDir))
                {
                    _soundDir = exeDir + "SOUND/";
                }
            }
        }

        /// <summary>
        /// Checks if a sound file exists.
        /// </summary>
        /// <param name="filename">The filename of the sound file.</param>
        /// <returns>1 if the file exists, 0 otherwise.</returns>
        public int ExistSound(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return 0;

            string fullPath = GetFullSoundPath(filename);
            return File.Exists(fullPath) ? 1 : 0;
        }

        /// <summary>
        /// Plays a sound effect.
        /// </summary>
        /// <param name="filename">The filename of the sound file.</param>
        /// <returns>1 if successful, 0 otherwise.</returns>
        public int PlaySound(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return 0;

            AudioClip clip = LoadAudioClip(filename);
            if (clip == null)
                return 0;

            _soundSource.clip = clip;
            _soundSource.volume = _soundVolume / 100.0f;
            _soundSource.Play();
            return 1;
        }

        /// <summary>
        /// Stops the currently playing sound effect.
        /// </summary>
        public void StopSound()
        {
            if (_soundSource != null && _soundSource.isPlaying)
            {
                _soundSource.Stop();
            }
        }

        /// <summary>
        /// Plays background music with loop.
        /// </summary>
        /// <param name="filename">The filename of the BGM file.</param>
        /// <returns>1 if successful, 0 otherwise.</returns>
        public int PlayBGM(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return 0;

            AudioClip clip = LoadAudioClip(filename);
            if (clip == null)
                return 0;

            _bgmSource.clip = clip;
            _bgmSource.volume = _bgmVolume / 100.0f;
            _bgmSource.loop = true;
            _bgmSource.Play();
            return 1;
        }

        /// <summary>
        /// Stops the currently playing background music.
        /// </summary>
        public void StopBGM()
        {
            if (_bgmSource != null && _bgmSource.isPlaying)
            {
                _bgmSource.Stop();
            }
        }

        /// <summary>
        /// Sets the sound effect volume.
        /// </summary>
        /// <param name="volume">Volume level (0-100).</param>
        public void SetSoundVolume(int volume)
        {
            _soundVolume = Mathf.Clamp(volume, 0, 100);
            if (_soundSource != null)
            {
                _soundSource.volume = _soundVolume / 100.0f;
            }
        }

        /// <summary>
        /// Sets the BGM volume.
        /// </summary>
        /// <param name="volume">Volume level (0-100).</param>
        public void SetBGMVolume(int volume)
        {
            _bgmVolume = Mathf.Clamp(volume, 0, 100);
            if (_bgmSource != null)
            {
                _bgmSource.volume = _bgmVolume / 100.0f;
            }
        }

        /// <summary>
        /// Gets the full path to a sound file.
        /// </summary>
        private string GetFullSoundPath(string filename)
        {
            // If filename already has path separators, use as-is relative to sound dir
            if (filename.Contains("/") || filename.Contains("\\"))
            {
                return _soundDir + filename;
            }
            return _soundDir + filename;
        }

        /// <summary>
        /// Loads an audio clip from the sound directory.
        /// </summary>
        private AudioClip LoadAudioClip(string filename)
        {
            string key = filename.ToUpperInvariant();
            
            // Check cache first
            if (_audioCache.TryGetValue(key, out AudioClip cachedClip))
            {
                return cachedClip;
            }

            string fullPath = GetFullSoundPath(filename);
            
            if (!File.Exists(fullPath))
            {
                // Try with common audio extensions
                string[] extensions = { ".wav", ".ogg", ".mp3", ".WAV", ".OGG", ".MP3" };
                bool found = false;
                foreach (var ext in extensions)
                {
                    string testPath = fullPath + ext;
                    if (File.Exists(testPath))
                    {
                        fullPath = testPath;
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return null;
            }

            // Load audio using UnityWebRequest (works on all platforms)
            AudioClip clip = LoadAudioFromFile(fullPath);
            if (clip != null)
            {
                _audioCache[key] = clip;
            }
            return clip;
        }

        /// <summary>
        /// Loads an audio file synchronously.
        /// Note: This uses a synchronous approach for compatibility with the Emuera script execution model.
        /// </summary>
        private AudioClip LoadAudioFromFile(string filePath)
        {
            try
            {
                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                AudioType audioType;
                
                switch (extension)
                {
                    case ".wav":
                        audioType = AudioType.WAV;
                        break;
                    case ".ogg":
                        audioType = AudioType.OGGVORBIS;
                        break;
                    case ".mp3":
                        audioType = AudioType.MPEG;
                        break;
                    default:
                        audioType = AudioType.UNKNOWN;
                        break;
                }

                // Use WWW class for synchronous loading (deprecated but works)
                string url = "file://" + filePath;
                
#if UNITY_2018_3_OR_NEWER
                // For newer Unity versions, we need to use UnityWebRequest
                // But since we need synchronous loading, we'll use a coroutine workaround
                // For now, fall back to basic file loading for WAV files
                if (audioType == AudioType.WAV)
                {
                    return LoadWavFile(filePath);
                }
                else
                {
                    // For other formats, we'd need async loading
                    // Just return null for now - this can be enhanced later
                    return null;
                }
#else
                WWW www = new WWW(url);
                while (!www.isDone) { }
                
                if (string.IsNullOrEmpty(www.error))
                {
                    return www.GetAudioClip(false, false, audioType);
                }
                return null;
#endif
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AudioManager: Failed to load audio file: {filePath}, Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads a WAV file directly.
        /// </summary>
        private AudioClip LoadWavFile(string filePath)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(filePath);
                return WavUtility.ToAudioClip(fileBytes, 0, Path.GetFileNameWithoutExtension(filePath));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AudioManager: Failed to load WAV file: {filePath}, Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Clears the audio cache.
        /// </summary>
        public void ClearCache()
        {
            foreach (var clip in _audioCache.Values)
            {
                if (clip != null)
                {
                    Destroy(clip);
                }
            }
            _audioCache.Clear();
        }
    }

    /// <summary>
    /// Utility class for loading WAV files.
    /// </summary>
    internal static class WavUtility
    {
        public static AudioClip ToAudioClip(byte[] fileBytes, int offsetSamples = 0, string name = "wav")
        {
            try
            {
                // Read WAV header
                int channels = BitConverter.ToInt16(fileBytes, 22);
                int sampleRate = BitConverter.ToInt32(fileBytes, 24);
                int bitsPerSample = BitConverter.ToInt16(fileBytes, 34);
                
                // Find data chunk
                int dataIndex = 44; // Standard WAV data offset
                for (int i = 12; i < fileBytes.Length - 4; i++)
                {
                    if (fileBytes[i] == 'd' && fileBytes[i + 1] == 'a' && 
                        fileBytes[i + 2] == 't' && fileBytes[i + 3] == 'a')
                    {
                        dataIndex = i + 8;
                        break;
                    }
                }

                int dataSize = BitConverter.ToInt32(fileBytes, dataIndex - 4);
                int sampleCount = dataSize / (bitsPerSample / 8) / channels;

                float[] samples = new float[sampleCount * channels];
                int byteIndex = dataIndex;

                if (bitsPerSample == 16)
                {
                    for (int i = 0; i < samples.Length; i++)
                    {
                        short sample = BitConverter.ToInt16(fileBytes, byteIndex);
                        samples[i] = sample / 32768.0f;
                        byteIndex += 2;
                    }
                }
                else if (bitsPerSample == 8)
                {
                    for (int i = 0; i < samples.Length; i++)
                    {
                        samples[i] = (fileBytes[byteIndex] - 128) / 128.0f;
                        byteIndex++;
                    }
                }
                else
                {
                    return null;
                }

                AudioClip clip = AudioClip.Create(name, sampleCount, channels, sampleRate, false);
                clip.SetData(samples, offsetSamples);
                return clip;
            }
            catch
            {
                return null;
            }
        }
    }
}
