using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace MinorShift.Emuera.Content
{
    /// <summary>
    /// Manages audio playback for Emuera games.
    /// Supports PLAYSOUND, STOPSOUND, PLAYBGM, STOPBGM, and EXISTSOUND commands from Emuera EM/EE.
    /// Supports WAV (synchronous), OGG and MP3 (asynchronous via UnityWebRequest).
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
        
        // Tracks files currently being loaded to prevent duplicate requests
        private HashSet<string> _pendingLoads = new HashSet<string>();
        
        // Queue for pending playback requests (for async-loaded formats)
        private class PendingPlayback
        {
            public string Key;
            public bool IsSound; // true = sound effect, false = BGM
        }
        private List<PendingPlayback> _pendingPlaybacks = new List<PendingPlayback>();
        
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
        /// Checks multiple case variants: sound/, Sound/, SOUND/
        /// </summary>
        private void InitializeSoundDirectory()
        {
            string exeDir = Program.ExeDir;
            string[] variants = { "sound/", "Sound/", "SOUND/" };
            
            _soundDir = exeDir + "sound/"; // Default fallback
            foreach (var variant in variants)
            {
                string testPath = exeDir + variant;
                if (Directory.Exists(testPath))
                {
                    _soundDir = testPath;
                    break;
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
            // Use case-insensitive check - FileExistsInsensitive already checks exact path first
            return uEmuera.Utils.FileExistsInsensitive(fullPath) ? 1 : 0;
        }

        /// <summary>
        /// Plays a sound effect.
        /// For WAV files: plays immediately (synchronous).
        /// For OGG/MP3: starts async load and will play when ready.
        /// </summary>
        /// <param name="filename">The filename of the sound file.</param>
        /// <returns>1 if successful or loading started, 0 otherwise.</returns>
        public int PlaySound(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return 0;

            string key = filename.ToUpperInvariant();
            
            // Check cache first
            if (_audioCache.TryGetValue(key, out AudioClip cachedClip))
            {
                if (cachedClip != null)
                {
                    _soundSource.clip = cachedClip;
                    _soundSource.volume = _soundVolume / 100.0f;
                    _soundSource.Play();
                    return 1;
                }
            }

            // Try to load the audio clip
            AudioClip clip = LoadAudioClip(filename, true);
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
        /// For WAV files: plays immediately (synchronous).
        /// For OGG/MP3: starts async load and will play when ready.
        /// </summary>
        /// <param name="filename">The filename of the BGM file.</param>
        /// <returns>1 if successful or loading started, 0 otherwise.</returns>
        public int PlayBGM(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return 0;

            string key = filename.ToUpperInvariant();
            
            // Check cache first
            if (_audioCache.TryGetValue(key, out AudioClip cachedClip))
            {
                if (cachedClip != null)
                {
                    _bgmSource.clip = cachedClip;
                    _bgmSource.volume = _bgmVolume / 100.0f;
                    _bgmSource.loop = true;
                    _bgmSource.Play();
                    return 1;
                }
            }

            // Try to load the audio clip
            AudioClip clip = LoadAudioClip(filename, false);
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
            return _soundDir + filename;
        }

        /// <summary>
        /// Loads an audio clip from the sound directory.
        /// </summary>
        /// <param name="filename">The filename to load.</param>
        /// <param name="isSound">True if this is for a sound effect, false for BGM.</param>
        /// <returns>AudioClip if loaded synchronously (WAV), null if loading asynchronously (OGG/MP3) or failed.</returns>
        private AudioClip LoadAudioClip(string filename, bool isSound)
        {
            string key = filename.ToUpperInvariant();
            
            // Already in cache
            if (_audioCache.TryGetValue(key, out AudioClip cachedClip))
            {
                return cachedClip;
            }

            string fullPath = GetFullSoundPath(filename);
            
            if (!File.Exists(fullPath))
            {
                // Try case-insensitive resolution first
                string resolved = uEmuera.Utils.ResolvePathInsensitive(fullPath, expectDirectory: false);
                if (!string.IsNullOrEmpty(resolved))
                {
                    fullPath = resolved;
                }
                else
                {
                    // Try with common audio extensions
                    string[] extensions = { ".wav", ".ogg", ".mp3" };
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
                        // Try case-insensitive resolution for each extension
                        resolved = uEmuera.Utils.ResolvePathInsensitive(testPath, expectDirectory: false);
                        if (!string.IsNullOrEmpty(resolved))
                        {
                            fullPath = resolved;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        return null;
                }
            }

            // Load audio based on format
            AudioClip clip = LoadAudioFromFile(fullPath, key, isSound);
            return clip;
        }

        /// <summary>
        /// Loads an audio file. WAV loads synchronously, OGG/MP3 load asynchronously.
        /// </summary>
        /// <param name="filePath">Full path to the audio file.</param>
        /// <param name="cacheKey">Key for caching the loaded clip.</param>
        /// <param name="isSound">True if this is for a sound effect, false for BGM.</param>
        /// <returns>AudioClip if loaded synchronously (WAV), null if loading asynchronously or failed.</returns>
        private AudioClip LoadAudioFromFile(string filePath, string cacheKey, bool isSound)
        {
            try
            {
                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                
                switch (extension)
                {
                    case ".wav":
                        // WAV can be loaded synchronously
                        AudioClip wavClip = LoadWavFile(filePath);
                        if (wavClip != null)
                        {
                            _audioCache[cacheKey] = wavClip;
                        }
                        return wavClip;
                    
                    case ".ogg":
                    case ".mp3":
                        // OGG and MP3 require async loading
                        if (!_pendingLoads.Contains(cacheKey))
                        {
                            _pendingLoads.Add(cacheKey);
                            StartCoroutine(LoadAudioAsync(filePath, cacheKey, extension == ".ogg" ? AudioType.OGGVORBIS : AudioType.MPEG, isSound));
                        }
                        return null;
                    
                    default:
                        Debug.LogWarning($"AudioManager: Unsupported audio format: {extension}");
                        return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"AudioManager: Failed to load audio file: {filePath}, Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Asynchronously loads an audio file using UnityWebRequest.
        /// </summary>
        /// <param name="filePath">Full path to the audio file.</param>
        /// <param name="cacheKey">Key for caching the loaded clip.</param>
        /// <param name="audioType">Audio type (OGGVORBIS or MPEG).</param>
        /// <param name="isSound">True if this is for a sound effect, false for BGM.</param>
        private IEnumerator LoadAudioAsync(string filePath, string cacheKey, AudioType audioType, bool isSound)
        {
            // Convert file path to URI format
            string uri = "file:///" + filePath.Replace("\\", "/");
            
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(uri, audioType))
            {
                // Set download handler to not store in memory until needed
                ((DownloadHandlerAudioClip)www.downloadHandler).streamAudio = true;
                
                yield return www.SendWebRequest();

                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    if (clip != null)
                    {
                        clip.name = Path.GetFileNameWithoutExtension(filePath);
                        _audioCache[cacheKey] = clip;
                        
                        // Play immediately if this was the requested file
                        if (isSound && _soundSource != null)
                        {
                            _soundSource.clip = clip;
                            _soundSource.volume = _soundVolume / 100.0f;
                            _soundSource.Play();
                        }
                        else if (!isSound && _bgmSource != null)
                        {
                            _bgmSource.clip = clip;
                            _bgmSource.volume = _bgmVolume / 100.0f;
                            _bgmSource.loop = true;
                            _bgmSource.Play();
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"AudioManager: Failed to extract AudioClip from: {filePath}");
                    }
                }
                else
                {
                    Debug.LogWarning($"AudioManager: Failed to load audio file: {filePath}, Error: {www.error}");
                }
            }
            
            _pendingLoads.Remove(cacheKey);
        }

        /// <summary>
        /// Loads a WAV file directly (synchronous).
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
            _pendingLoads.Clear();
            _pendingPlaybacks.Clear();
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
