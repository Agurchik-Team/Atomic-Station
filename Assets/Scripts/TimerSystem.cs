using UnityEngine;
using UnityEngine.UI;
using TMPro;  // Добавьте эту строку

public class TimerSystem : MonoBehaviour
{
    [Header("UI элементы")]
    public TextMeshProUGUI timerText;           // Текст для отображения времени в игре
    
    private float currentTime = 0f;  // Текущее время игры
    private bool isRunning = false;   // Запущен ли таймер
    private float bestTime = 0f;      // Лучшее время (рекорд)
    
    private const string BEST_TIME_KEY = "BestTime"; // Ключ для сохранения рекорда
    
    void Awake()
    {
        // Загружаем сохранённый рекорд
        LoadBestTime();
    }
    
    void Update()
    {
        if (isRunning)
        {
            currentTime += Time.deltaTime;
            UpdateTimerDisplay();
        }
    }
    
    // Запуск таймера (вызывать при старте игры)
    public void StartTimer()
    {
        currentTime = 0f;
        isRunning = true;
        UpdateTimerDisplay();
        Debug.Log("Таймер запущен");
    }
    
    // Остановка таймера и проверка рекорда
   // Остановка таймера и проверка рекорда
public float StopTimer()
{
    isRunning = false;
    Debug.Log($"Таймер остановлен. Время: {GetFormattedTime(currentTime)}");
    
    // Проверяем, побит ли рекорд
    if (bestTime == 0f || currentTime > bestTime)
    {
        bestTime = currentTime;
        SaveBestTime();
        Debug.Log($"НОВЫЙ РЕКОРД! {GetFormattedTime(bestTime)}");
    }
    else
    {
        Debug.Log($"Рекорд не побит. Текущий рекорд: {GetFormattedTime(bestTime)}");
    }
    
    return currentTime;
}
    
    // Обновление отображения времени
    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            timerText.text = GetFormattedTime(currentTime);
        }
    }
    
    // Форматирование времени (минуты:секунды)
    string GetFormattedTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);
        
        return $"{minutes:00}:{seconds:00}:{milliseconds:00}";
    }
    
    // Получить текущее время (для отладки)
    public float GetCurrentTime()
    {
        return currentTime;
    }
    
    // Сохранение рекорда
    void SaveBestTime()
    {
        PlayerPrefs.SetFloat(BEST_TIME_KEY, bestTime);
        PlayerPrefs.Save();
        Debug.Log($"Рекорд сохранён: {GetFormattedTime(bestTime)}");
    }
    
    // Загрузка рекорда
    void LoadBestTime()
    {
        if (PlayerPrefs.HasKey(BEST_TIME_KEY))
        {
            bestTime = PlayerPrefs.GetFloat(BEST_TIME_KEY);
            Debug.Log($"Рекорд загружен: {GetFormattedTime(bestTime)}");
        }
        else
        {
            bestTime = 0f;
            Debug.Log("Рекордов пока нет");
        }
    }
}