using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class PressureSystem : MonoBehaviour
{
    [Header("Таймер")]
    public TimerSystem timerSystem;

    [Header("Звуки")]
    public AudioSource audioSource;          
    public AudioClip highPressureAlarm;       
    public float highPressureThreshold = 80f; 
    private bool isAlarmPlaying = false;      

    [Header("UI элементы")]
    public Slider pressureSlider;
    public Text pressureText;

    [Header("Game Over")]
    public GameObject gameOverCanvas;

    [Header("Настройки давления")]
    public float currentPressure = 0f;
    public float maxPressure = 100f;
    public float pressureIncreaseRate = 10f;

    [Header("Кнопки сброса (9 штук)")]
    public Button[] resetButtons = new Button[0];
    public float[] buttonTimers;
    public bool[] isButtonActive;
    public float buttonActiveTimeMin = 10f;
    public float buttonActiveTimeMax = 15f;
    public float pressureDecreasePerButton = 2f;

    [Header("Текстуры для кнопок сброса")]
    public Sprite buttonActiveSprite;
    public Sprite buttonInactiveSprite;

    [Header("Нередактируемые ползунки (3 штуки)")]
    public Slider[] targetSliders = new Slider[0];
    public float[] targetValues;
    public float targetChangeInterval = 3f;

    [Header("Редактируемые ползунки (3 штуки)")]
    public Slider[] playerSliders = new Slider[0];
    private bool[] isDragging;
    public float sliderTolerance = 10f;
    public float sliderPressureDecrease = 2f;

    [Header("Кнопки со значениями (2 штуки)")]
    public Button[] valueButtons = new Button[0];
    public Slider[] valueDisplaySliders = new Slider[0];
    public float[] buttonValues;
    public float valueIncreasePerClick = 10f;
    public float valueDecreasePerSecond = 5f;
    public float maxButtonValue = 100f;
    public float pressureDecreasePerValuePoint = 0.05f;

    [Header("Настройки усложнения")]
    public float difficultyIncreaseInterval = 10f;
    private float difficultyTimer;
    private float baseTargetChangeInterval;
    private float baseButtonActiveTimeMin;
    private float baseButtonActiveTimeMax;

    private bool gameOver = false;

    void Start()
    {
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(false);
        }

        Time.timeScale = 1f;

        if (timerSystem != null)
        {
            timerSystem.StartTimer();
        }

        if (resetButtons.Length == 0 || targetSliders.Length == 0 || playerSliders.Length == 0 || valueButtons.Length == 0)
        {
            Debug.LogError("ОШИБКА: Заполните все массивы в инспекторе Unity!");
            return;
        }

        baseTargetChangeInterval = targetChangeInterval;
        baseButtonActiveTimeMin = buttonActiveTimeMin;
        baseButtonActiveTimeMax = buttonActiveTimeMax;

        currentPressure = 0f;
        UpdatePressureUI();

        buttonTimers = new float[resetButtons.Length];
        isButtonActive = new bool[resetButtons.Length];

        for (int i = 0; i < resetButtons.Length; i++)
        {
            int index = i;
            if (resetButtons[i] != null)
            {
                resetButtons[i].onClick.AddListener(() => OnResetButtonClick(index));
            }
            buttonTimers[i] = 0f;
            isButtonActive[i] = false;
        }

        // Устанавливаем неактивную текстуру для всех кнопок при старте
        for (int i = 0; i < resetButtons.Length; i++)
        {
            ChangeButtonSprite(i, false);
        }

        targetValues = new float[targetSliders.Length];
        for (int i = 0; i < targetSliders.Length; i++)
        {
            if (targetSliders[i] != null)
            {
                targetSliders[i].interactable = false;
                ChangeTargetValue(i);
            }
        }

        isDragging = new bool[playerSliders.Length];

        for (int i = 0; i < playerSliders.Length; i++)
        {
            int index = i;
            if (playerSliders[i] != null)
            {
                EventTrigger trigger = playerSliders[i].gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                    trigger = playerSliders[i].gameObject.AddComponent<EventTrigger>();

                trigger.triggers.Clear();

                EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
                beginDragEntry.eventID = EventTriggerType.BeginDrag;
                beginDragEntry.callback.AddListener((data) => { OnSliderBeginDrag(index); });
                trigger.triggers.Add(beginDragEntry);

                EventTrigger.Entry endDragEntry = new EventTrigger.Entry();
                endDragEntry.eventID = EventTriggerType.EndDrag;
                endDragEntry.callback.AddListener((data) => { OnSliderEndDrag(index); });
                trigger.triggers.Add(endDragEntry);
            }
            isDragging[i] = false;
        }

        buttonValues = new float[valueButtons.Length];

        for (int i = 0; i < valueDisplaySliders.Length; i++)
        {
            if (valueDisplaySliders[i] != null)
            {
                valueDisplaySliders[i].interactable = false;
                valueDisplaySliders[i].minValue = 0f;
                valueDisplaySliders[i].maxValue = maxButtonValue;
                valueDisplaySliders[i].value = 0f;
            }
        }

        for (int i = 0; i < valueButtons.Length; i++)
        {
            int index = i;
            buttonValues[i] = 0f;
            if (valueButtons[i] != null)
            {
                valueButtons[i].onClick.AddListener(() => OnValueButtonClick(index));
                UpdateValueButtonText(index);
                UpdateValueDisplaySlider(index);
            }
        }

        StartCoroutine(PressureIncrease());
        StartCoroutine(ChangeTargetsPeriodically());
        StartCoroutine(ProcessButtonDecrease());
        StartCoroutine(ProcessValueButtonsDecrease());
        StartCoroutine(ProcessValueButtonsPressureEffect());
        StartCoroutine(IncreaseDifficulty());

        Debug.Log("Игра запущена!");
    }

    // Проверка давления и воспроизведение звука
    void CheckHighPressureSound()
    {
        if (audioSource == null || highPressureAlarm == null) return;

        if (currentPressure >= highPressureThreshold && !gameOver)
        {
            if (!isAlarmPlaying)
            {
                // Включаем звук
                audioSource.clip = highPressureAlarm;
                audioSource.loop = true;
                audioSource.Play();
                isAlarmPlaying = true;
                Debug.Log("Высокое давление! Включен сигнал тревоги");
            }
        }
        else
        {
            if (isAlarmPlaying)
            {
                // Выключаем звук
                audioSource.Stop();
                audioSource.loop = false;
                isAlarmPlaying = false;
                Debug.Log("Давление нормализовалось. Сигнал выключен");
            }
        }
    }

    // ===== МЕТОД ДЛЯ СМЕНЫ ТЕКСТУРЫ КНОПКИ =====
    void ChangeButtonSprite(int index, bool isActive)
    {
        if (resetButtons[index] == null) return;
        Image buttonImage = resetButtons[index].GetComponent<Image>();
        if (buttonImage == null) return;

        if (isActive)
        {
            if (buttonActiveSprite != null)
                buttonImage.sprite = buttonActiveSprite;
        }
        else
        {
            if (buttonInactiveSprite != null)
                buttonImage.sprite = buttonInactiveSprite;
        }
    }

    IEnumerator PressureIncrease()
    {
        while (!gameOver)
        {
            yield return new WaitForSeconds(1f);
            currentPressure += pressureIncreaseRate;

            if (currentPressure >= maxPressure)
            {
                currentPressure = maxPressure;
                GameOver();
            }

            UpdatePressureUI();
        }
    }

    void UpdatePressureUI()
    {
        if (pressureSlider != null)
        {
            pressureSlider.value = currentPressure / maxPressure;
        }
        if (pressureText != null)
        {
            pressureText.text = $"Давление: {Mathf.Round(currentPressure)}/{maxPressure}";
        }

        CheckHighPressureSound();
    }

    void OnResetButtonClick(int index)
    {
        if (gameOver) return;

        if (index < resetButtons.Length && !isButtonActive[index])
        {
            isButtonActive[index] = true;

            float randomTime = Random.Range(buttonActiveTimeMin, buttonActiveTimeMax);
            buttonTimers[index] = randomTime;

            ChangeButtonSprite(index, true);

            Debug.Log($"Кнопка {index} активирована на {randomTime:F1} сек");
        }
    }

    IEnumerator ProcessButtonDecrease()
    {
        while (!gameOver)
        {
            yield return new WaitForSeconds(0.1f);

            for (int i = 0; i < resetButtons.Length; i++)
            {
                if (isButtonActive[i])
                {
                    buttonTimers[i] -= 0.1f;
                    if (buttonTimers[i] <= 0f)
                    {
                        isButtonActive[i] = false;

                        ChangeButtonSprite(i, false);

                        Debug.Log($"Кнопка {i} отключилась");
                    }
                    else
                    {
                        currentPressure -= pressureDecreasePerButton * 0.05f;
                        if (currentPressure < 0) currentPressure = 0;
                        UpdatePressureUI();
                    }
                }
            }
        }
    }

    void ChangeTargetValue(int index)
    {
        if (index < targetSliders.Length && targetSliders[index] != null)
        {
            targetValues[index] = Random.Range(0f, 100f);
            targetSliders[index].value = targetValues[index];
        }
    }

    IEnumerator ChangeTargetsPeriodically()
    {
        while (!gameOver)
        {
            yield return new WaitForSeconds(targetChangeInterval);
            for (int i = 0; i < targetSliders.Length; i++)
            {
                ChangeTargetValue(i);
            }
        }
    }

    public void OnSliderBeginDrag(int index)
    {
        if (gameOver) return;
        isDragging[index] = true;
        Debug.Log($"Ползунок {index}: начал перетаскивание");
    }

    public void OnSliderEndDrag(int index)
    {
        if (gameOver) return;

        isDragging[index] = false;
        float currentValue = playerSliders[index].value;

        if (index < targetValues.Length && Mathf.Abs(currentValue - targetValues[index]) <= sliderTolerance)
        {
            currentPressure -= sliderPressureDecrease;
            if (currentPressure < 0) currentPressure = 0;
            UpdatePressureUI();
            Debug.Log($"Ползунок {index} попал в цель! Давление снижено на {sliderPressureDecrease}");
        }
        else
        {
            Debug.Log($"Ползунок {index}: отпущен на {currentValue}, цель {targetValues[index]} (мимо)");
        }
    }

    IEnumerator ProcessValueButtonsDecrease()
    {
        while (!gameOver)
        {
            yield return new WaitForSeconds(1f);

            for (int i = 0; i < valueButtons.Length; i++)
            {
                if (buttonValues[i] > 0f)
                {
                    buttonValues[i] -= valueDecreasePerSecond;
                    if (buttonValues[i] < 0f) buttonValues[i] = 0f;
                    UpdateValueButtonText(i);
                    UpdateValueDisplaySlider(i);
                }
            }
        }
    }

    IEnumerator ProcessValueButtonsPressureEffect()
    {
        while (!gameOver)
        {
            yield return new WaitForSeconds(0.2f);

            for (int i = 0; i < valueButtons.Length; i++)
            {
                if (buttonValues[i] > 0f)
                {
                    float decrease = buttonValues[i] * pressureDecreasePerValuePoint * 0.2f;
                    currentPressure -= decrease;
                    if (currentPressure < 0) currentPressure = 0;
                    UpdatePressureUI();
                }
            }
        }
    }

    void OnValueButtonClick(int index)
    {
        if (gameOver) return;

        if (index < buttonValues.Length)
        {
            buttonValues[index] += valueIncreasePerClick;
            if (buttonValues[index] > maxButtonValue)
                buttonValues[index] = maxButtonValue;

            UpdateValueButtonText(index);
            UpdateValueDisplaySlider(index);

            Debug.Log($"Кнопка {index}: значение увеличено до {buttonValues[index]}");
        }
    }

    void UpdateValueButtonText(int index)
    {
        if (index < valueButtons.Length && valueButtons[index] != null)
        {
            Text buttonText = valueButtons[index].GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = Mathf.Round(buttonValues[index]).ToString();
            }
        }
    }

    void UpdateValueDisplaySlider(int index)
    {
        if (index < valueDisplaySliders.Length && valueDisplaySliders[index] != null)
        {
            valueDisplaySliders[index].value = buttonValues[index];
        }
    }

    IEnumerator IncreaseDifficulty()
    {
        difficultyTimer = difficultyIncreaseInterval;
        int difficultyLevel = 1;

        while (!gameOver)
        {
            yield return new WaitForSeconds(1f);
            difficultyTimer -= 1f;

            if (difficultyTimer <= 0)
            {
                difficultyLevel++;

                targetChangeInterval = Mathf.Max(0.5f, baseTargetChangeInterval / difficultyLevel);
                buttonActiveTimeMin = Mathf.Max(0.5f, baseButtonActiveTimeMin - (difficultyLevel - 1) * 0.05f);
                buttonActiveTimeMax = Mathf.Max(1f, baseButtonActiveTimeMax - (difficultyLevel - 1) * 0.05f);
                valueDecreasePerSecond = Mathf.Min(30f, valueDecreasePerSecond + 1f);

                difficultyTimer = difficultyIncreaseInterval;
                Debug.Log($"Уровень сложности {difficultyLevel}! Цели каждые {targetChangeInterval:F1}сек, значение кнопок падает на {valueDecreasePerSecond:F1}/сек");
            }
        }
    }

    void GameOver()
    {
        gameOver = true;
        Debug.Log("ИГРА ОКОНЧЕНА! Давление 100%");

        if (audioSource != null && isAlarmPlaying)
        {
            audioSource.Stop();
            isAlarmPlaying = false;
        }

        if (timerSystem != null)
        {
            timerSystem.StopTimer();
        }

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
        }

        Time.timeScale = 0f;
    }
}