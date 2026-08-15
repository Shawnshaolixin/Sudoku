using System.Collections.Generic;
using UnityEngine;

namespace Sudoku.Gameplay
{
    /// <summary>
    /// 音频服务:管理背景音乐(BGM,循环)和音效(SFX,一次性)。
    /// 音频文件按约定目录存放,缺失时静默跳过(每个缺失文件只警告一次):
    ///   Assets/Resources/Audio/Bgm/bgm.ogg          —— 背景音乐(1 首,循环)
    ///   Assets/Resources/Audio/Sfx/click.ogg        —— 按钮点击
    ///   Assets/Resources/Audio/Sfx/place.ogg        —— 落子
    ///   Assets/Resources/Audio/Sfx/erase.ogg        —— 擦除
    ///   Assets/Resources/Audio/Sfx/hint.ogg         —— 提示
    ///   Assets/Resources/Audio/Sfx/win.ogg          —— 胜利
    /// </summary>
    public static class AudioService
    {
        private static AudioSource _bgmSource;
        private static AudioSource _sfxSource;
        private static bool _initialized;
        private static readonly HashSet<string> _warned = new HashSet<string>();

        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            var go = new GameObject("AudioService");
            Object.DontDestroyOnLoad(go); // 跨场景常驻

            _bgmSource = go.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            _sfxSource = go.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
        }

        /// <summary>播放背景音乐(循环),受「背景音乐」开关控制。</summary>
        public static void PlayBgm(string name)
        {
            Initialize();
            if (!SettingsService.Music) return;

            var clip = Resources.Load<AudioClip>($"Audio/Bgm/{name}");
            if (clip == null) { Warn($"Audio/Bgm/{name}"); return; }

            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return; // 已在播同一首
            _bgmSource.clip = clip;
            _bgmSource.volume = 0.5f;
            _bgmSource.Play();
        }

        /// <summary>停止背景音乐。</summary>
        public static void StopBgm()
        {
            Initialize();
            _bgmSource.Stop();
        }

        /// <summary>播放一次性音效,受「音效」开关控制。</summary>
        public static void PlaySfx(string name)
        {
            Initialize();
            if (!SettingsService.Sound) return;

            var clip = Resources.Load<AudioClip>($"Audio/Sfx/{name}");
            if (clip == null) { Warn($"Audio/Sfx/{name}"); return; }

            _sfxSource.PlayOneShot(clip);
        }

        private static void Warn(string path)
        {
            if (!_warned.Add(path)) return; // 每个缺失文件只警告一次,避免刷屏
            Debug.LogWarning($"[AudioService] 未找到音频:{path}(可先忽略,放入文件后自动生效)");
        }
    }
}
