using UnityEngine;

/// <summary>
/// Gestor central de audio para Candle Fury.
/// Singleton — persiste entre escenas.
/// Maneja música de fondo y efectos de sonido (SFX).
///
/// USO desde cualquier script:
///   AudioManager.Instance?.PlaySFX(AudioManager.Instance.sfxJump);
///   AudioManager.Instance?.PlayMusic(AudioManager.Instance.musicLevel);
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Fuentes de audio")]
    [SerializeField] AudioSource musicSource;   // para la música (loop)
    [SerializeField] AudioSource sfxSource;     // para efectos puntuales

    [Header("Música")]
    public AudioClip musicLevel;
    public AudioClip musicGameOver;

    [Header("SFX del jugador")]
    public AudioClip sfxJump;
    public AudioClip sfxSword;
    public AudioClip sfxFire;
    public AudioClip sfxPlayerHurt;
    public AudioClip sfxDash;

    [Header("SFX de enemigos")]
    public AudioClip sfxHitEnemy;
    public AudioClip sfxEnemyDeath;

    [Header("SFX de items / UI")]
    public AudioClip sfxPickup;
    public AudioClip sfxGameOver;

    [Header("Volúmenes (0 a 1)")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume   = 0.8f;

    void Awake()
    {
        // Singleton persistente
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Auto-crear las fuentes si no se asignaron
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
    }

    void Start()
    {
        // Empieza la música del nivel automáticamente
        if (musicLevel != null)
            PlayMusic(musicLevel);
    }

    // ── Música ────────────────────────────────────────────────────────────────
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return; // ya suena

        musicSource.clip   = clip;
        musicSource.volume = musicVolume;
        musicSource.loop   = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null) musicSource.Stop();
    }

    // ── SFX ───────────────────────────────────────────────────────────────────
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, sfxVolume);
    }

    /// <summary>Reproduce un SFX con volumen personalizado</summary>
    public void PlaySFX(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    // ── Control de volumen (para menús de opciones, opcional) ─────────────────
    public void SetMusicVolume(float v)
    {
        musicVolume = Mathf.Clamp01(v);
        if (musicSource != null) musicSource.volume = musicVolume;
    }

    public void SetSFXVolume(float v)
    {
        sfxVolume = Mathf.Clamp01(v);
    }
}
