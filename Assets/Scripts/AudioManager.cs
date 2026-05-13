using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Источники звука")]
    public AudioSource musicSource;      // Для фоновой музыки
    public AudioSource sfxSource;        // Для звуковых эффектов (ДОБАВИТЬ!)

    [Header("Звуки")]
    public AudioClip gameOverSound;      // Звук проигрыша
    public AudioClip pressureSound;      // Звук высокого давления
    public AudioClip buttonClickSound;   // Звук нажатия кнопки (опционально)
    public AudioClip sliderMatchSound;   // Звук попадания ползунка (опционально)

    [Header("Настройки")]
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    [Range(0f, 1f)]
    public float sfxVolume = 0.7f;

    private static AudioManager instance;
    private bool isHighPressurePlaying = false; // Для отслеживания звука давления

    void Awake()
    {
        // Синглтон - чтобы звуки работали между сценами
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Настройка музыки
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
            musicSource.loop = true;
            if (musicSource.clip != null)
                musicSource.Play();
        }

        // Настройка звуковых эффектов
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    // ===== ОБЩИЕ МЕТОДЫ =====

    // Воспроизвести любой звук
    public void PlaySound(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // Звук проигрыша
    public void PlayGameOver()
    {
        if (sfxSource != null && gameOverSound != null)
        {
            sfxSource.PlayOneShot(gameOverSound);
        }
    }

    // Звук высокого давления (зацикленный)
    public void PlayHighPressureSound()
    {
        if (sfxSource != null && pressureSound != null && !isHighPressurePlaying)
        {
            sfxSource.loop = true;
            sfxSource.clip = pressureSound;
            sfxSource.Play();
            isHighPressurePlaying = true;
        }
    }

    // Остановить звук высокого давления
    public void StopHighPressureSound()
    {
        if (sfxSource != null && isHighPressurePlaying)
        {
            sfxSource.loop = false;
            sfxSource.Stop();
            isHighPressurePlaying = false;
        }
    }

    // Звук нажатия кнопки
    public void PlayButtonClick()
    {
        if (sfxSource != null && buttonClickSound != null)
        {
            sfxSource.PlayOneShot(buttonClickSound);
        }
    }

    // Звук попадания ползунка
    public void PlaySliderMatch()
    {
        if (sfxSource != null && sliderMatchSound != null)
        {
            sfxSource.PlayOneShot(sliderMatchSound);
        }
    }

    // Сменить фоновую музыку
    public void ChangeMusic(AudioClip newMusic)
    {
        if (musicSource != null && newMusic != null)
        {
            musicSource.clip = newMusic;
            musicSource.Play();
        }
    }

    // Изменить громкость музыки
    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        if (musicSource != null)
            musicSource.volume = volume;
    }

    // Изменить громкость звуков
    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        if (sfxSource != null)
            sfxSource.volume = volume;
    }
}