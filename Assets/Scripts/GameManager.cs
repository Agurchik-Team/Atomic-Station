using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Рекорд")]
    public TextMeshProUGUI bestTimeText;

    void Start()
    {
        // Показываем рекорд при загрузке главного меню
        ShowBestTime();
    }

    // Запуск игры
    public void Play()
    {
        SceneManager.LoadScene("Play");
    }

    // Метод для выхода из игры
    public void QuitGame()
    {
        Debug.Log("Выход из игры");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // Отображение рекорда
    void ShowBestTime()
    {
        if (bestTimeText != null)
        {
            if (PlayerPrefs.HasKey("BestTime"))
            {
                float bestTime = PlayerPrefs.GetFloat("BestTime");
                bestTimeText.text = FormatTime(bestTime);
            }
            else
            {
                bestTimeText.text = "00:00:00";
            }
        }
    }

    // Форматирование времени
    string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);

        return $"{minutes:00}:{seconds:00}:{milliseconds:00}";
    }
}