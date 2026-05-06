using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class PressureSystem : MonoBehaviour
{
    [Header("UI элементы")]
    public Slider pressureSlider;      // Слайдер для отображения давления
    public Text pressureText;          // Текст для отображения давления

    [Header("Настройки давления")]
    public float currentPressure = 0f;        // Текущее давление
    public float maxPressure = 100f;          // Максимальное давление (проигрыш)
    public float pressureIncreaseRate = 10f;  // На сколько растёт давление в секунду

    [Header("Кнопки сброса (9 штук)")]
    public Button[] resetButtons = new Button[0];     // Массив кнопок сброса
    public float[] buttonTimers;                      // Таймеры активности каждой кнопки
    public bool[] isButtonActive;                     // Активна ли кнопка сейчас
    public float buttonActiveTimeMin = 10f;           // Минимальное время активности кнопки
    public float buttonActiveTimeMax = 15f;           // Максимальное время активности кнопки
    public float pressureDecreasePerButton = 2f;      // Сила снижения давления от одной активной кнопки

    [Header("Нередактируемые ползунки (3 штуки)")]
    public Slider[] targetSliders = new Slider[0];    // Ползунки-цели (игрок не может их трогать)
    public float[] targetValues;                      // Текущие значения целей
    public float targetChangeInterval = 3f;           // Как часто меняются цели

    [Header("Редактируемые ползунки (3 штуки)")]
    public Slider[] playerSliders = new Slider[0];    // Ползунки игрока
    private bool[] isDragging;                        // Флаг: тянет ли игрок ползунок
    public float sliderTolerance = 10f;               // Допуск попадания (+- от цели)
    public float sliderPressureDecrease = 2f;         // Сколько давления снижает точное попадание

    [Header("Кнопки со значениями (2 штуки)")]
    public Button[] valueButtons = new Button[0];     // Кнопки с накапливаемым значением
    public float[] buttonValues;                      // Текущие значения кнопок
    public float valueIncreasePerClick = 10f;         // На сколько растёт значение при клике
    public float valueDecreasePerSecond = 5f;         // На сколько падает значение за секунду
    public float maxButtonValue = 100f;               // Максимальное значение кнопки
    public float pressureDecreasePerValuePoint = 0.05f; // Снижение давления за 1 единицу значения в секунду

    [Header("Настройки усложнения")]
    public float difficultyIncreaseInterval = 10f;    // Как часто повышается сложность (сек)
    private float difficultyTimer;                    // Таймер до следующего повышения
    private float baseTargetChangeInterval;           // Базовый интервал смены целей
    private float baseButtonActiveTimeMin;            // Базовый минимум времени кнопки
    private float baseButtonActiveTimeMax;            // Базовый максимум времени кнопки

    private bool gameOver = false;                    // Флаг окончания игры

    void Start()
    {
        // Проверка: все ли массивы заполнены в инспекторе
        if (resetButtons.Length == 0 || targetSliders.Length == 0 || playerSliders.Length == 0 || valueButtons.Length == 0)
        {
            Debug.LogError("ОШИБКА: Заполните все массивы в инспекторе Unity!");
            return;
        }

        // Сохраняем базовые значения для усложнения
        baseTargetChangeInterval = targetChangeInterval;
        baseButtonActiveTimeMin = buttonActiveTimeMin;
        baseButtonActiveTimeMax = buttonActiveTimeMax;

        // Начальное давление
        currentPressure = 0f;
        UpdatePressureUI();

        // ===== ИНИЦИАЛИЗАЦИЯ КНОПОК СБРОСА =====
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

        // ===== ИНИЦИАЛИЗАЦИЯ ЦЕЛЕВЫХ ПОЛЗУНКОВ =====
        targetValues = new float[targetSliders.Length];
        for (int i = 0; i < targetSliders.Length; i++)
        {
            if (targetSliders[i] != null)
            {
                targetSliders[i].interactable = false; // Запрещаем игроку их трогать
                ChangeTargetValue(i);
            }
        }

        // ===== ИНИЦИАЛИЗАЦИЯ ПОЛЗУНКОВ ИГРОКА =====
        isDragging = new bool[playerSliders.Length];

        for (int i = 0; i < playerSliders.Length; i++)
        {
            int index = i;
            if (playerSliders[i] != null)
            {
                // Добавляем EventTrigger для отслеживания перетаскивания
                EventTrigger trigger = playerSliders[i].gameObject.GetComponent<EventTrigger>();
                if (trigger == null)
                    trigger = playerSliders[i].gameObject.AddComponent<EventTrigger>();

                trigger.triggers.Clear();

                // Событие: начало перетаскивания
                EventTrigger.Entry beginDragEntry = new EventTrigger.Entry();
                beginDragEntry.eventID = EventTriggerType.BeginDrag;
                beginDragEntry.callback.AddListener((data) => { OnSliderBeginDrag(index); });
                trigger.triggers.Add(beginDragEntry);

                // Событие: конец перетаскивания
                EventTrigger.Entry endDragEntry = new EventTrigger.Entry();
                endDragEntry.eventID = EventTriggerType.EndDrag;
                endDragEntry.callback.AddListener((data) => { OnSliderEndDrag(index); });
                trigger.triggers.Add(endDragEntry);
            }
            isDragging[i] = false;
        }

        // ===== ИНИЦИАЛИЗАЦИЯ КНОПОК СО ЗНАЧЕНИЯМИ =====
        buttonValues = new float[valueButtons.Length];
        for (int i = 0; i < valueButtons.Length; i++)
        {
            int index = i;
            buttonValues[i] = 0f;
            if (valueButtons[i] != null)
            {
                valueButtons[i].onClick.AddListener(() => OnValueButtonClick(index));
                UpdateValueButtonText(index);
            }
        }

        // ===== ЗАПУСК ВСЕХ КОРУТИН =====
        StartCoroutine(PressureIncrease());                 // Постепенный рост давления
        StartCoroutine(ChangeTargetsPeriodically());        // Периодическая смена целей
        StartCoroutine(ProcessButtonDecrease());           // Обработка снижения давления от кнопок
        StartCoroutine(ProcessValueButtonsDecrease());     // Самоуменьшение значений кнопок
        StartCoroutine(ProcessValueButtonsPressureEffect()); // Снижение давления от значений кнопок
        StartCoroutine(IncreaseDifficulty());               // Постепенное усложнение игры

        Debug.Log("Игра запущена!");
    }

    // Постепенный рост давления (вызывается каждую секунду)
    IEnumerator PressureIncrease()
    {
        while (!gameOver)
        {
            yield return new WaitForSeconds(1f);
            currentPressure += pressureIncreaseRate;

            // Проверка на проигрыш
            if (currentPressure >= maxPressure)
            {
                currentPressure = maxPressure;
                GameOver();
            }

            UpdatePressureUI();
        }
    }

    // Обновление UI давления (слайдер и текст)
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
    }

    // Нажатие на кнопку сброса
    void OnResetButtonClick(int index)
    {
        if (gameOver) return;

        // Если кнопка не активна - активируем
        if (index < resetButtons.Length && !isButtonActive[index])
        {
            isButtonActive[index] = true;

            // Случайное время активности от min до max
            float randomTime = Random.Range(buttonActiveTimeMin, buttonActiveTimeMax);
            buttonTimers[index] = randomTime;

            // Меняем цвет кнопки на зелёный (активна)
            if (resetButtons[index] != null)
            {
                resetButtons[index].image.color = Color.green;
            }
            Debug.Log($"Кнопка {index} активирована на {randomTime:F1} сек");
        }
    }

    // Обработка активных кнопок (снижение давления и отсчёт таймера)
    IEnumerator ProcessButtonDecrease()
    {
        while (!gameOver)
        {
            yield return new WaitForSeconds(0.1f); // Проверяем каждые 0.1 секунды

            for (int i = 0; i < resetButtons.Length; i++)
            {
                if (isButtonActive[i])
                {
                    // Уменьшаем таймер активности
                    buttonTimers[i] -= 0.1f;

                    // Если время вышло - отключаем кнопку
                    if (buttonTimers[i] <= 0f)
                    {
                        isButtonActive[i] = false;
                        if (resetButtons[i] != null)
                        {
                            resetButtons[i].image.color = Color.white;
                        }
                        Debug.Log($"Кнопка {i} отключилась");
                    }
                    else
                    {
                        // Пока активна - снижает давление
                        currentPressure -= pressureDecreasePerButton * 0.05f;
                        if (currentPressure < 0) currentPressure = 0;
                        UpdatePressureUI();
                    }
                }
            }
        }
    }

    // Изменение значения целевого ползунка на случайное (0-100)
    void ChangeTargetValue(int index)
    {
        if (index < targetSliders.Length && targetSliders[index] != null)
        {
            targetValues[index] = Random.Range(0f, 100f);
            targetSliders[index].value = targetValues[index];
        }
    }

    // Периодическая смена целей (каждые targetChangeInterval секунд)
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

    // Начало перетаскивания ползунка игроком
    public void OnSliderBeginDrag(int index)
    {
        if (gameOver) return;
        isDragging[index] = true;
        Debug.Log($"Ползунок {index}: начал перетаскивание");
    }

    // Конец перетаскивания (игрок отпустил ползунок)
    public void OnSliderEndDrag(int index)
    {
        if (gameOver) return;

        isDragging[index] = false;
        float currentValue = playerSliders[index].value;

        // Проверка: попал ли игрок в цель с учётом допуска
        if (index < targetValues.Length && Mathf.Abs(currentValue - targetValues[index]) <= sliderTolerance)
        {
            // Попал - снижаем давление
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

    // Самоуменьшение значений кнопок со временем (каждую секунду)
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
                }
            }
        }
    }

    // Постепенное снижение давления от значений кнопок (каждые 0.2 секунды)
    IEnumerator ProcessValueButtonsPressureEffect()
    {
        while (!gameOver)
        {
            yield return new WaitForSeconds(0.2f);

            for (int i = 0; i < valueButtons.Length; i++)
            {
                if (buttonValues[i] > 0f)
                {
                    // Чем выше значение кнопки, тем сильнее снижается давление
                    float decrease = buttonValues[i] * pressureDecreasePerValuePoint * 0.2f;
                    currentPressure -= decrease;
                    if (currentPressure < 0) currentPressure = 0;
                    UpdatePressureUI();
                }
            }
        }
    }

    // Нажатие на кнопку со значением (только увеличивает значение, давление снижается автоматически)
    void OnValueButtonClick(int index)
    {
        if (gameOver) return;

        if (index < buttonValues.Length)
        {
            // Увеличиваем значение кнопки
            buttonValues[index] += valueIncreasePerClick;
            if (buttonValues[index] > maxButtonValue)
                buttonValues[index] = maxButtonValue;

            UpdateValueButtonText(index);

            Debug.Log($"Кнопка {index}: значение увеличено до {buttonValues[index]}");
        }
    }

    // Обновление текста на кнопке (отображает текущее значение)
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

    // Усложнение игры: цели меняются чаще, кнопки работают меньше
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

                // Цели меняются чаще (интервал уменьшается)
                targetChangeInterval = Mathf.Max(0.5f, baseTargetChangeInterval / difficultyLevel);

                // Кнопки сброса работают чуть меньше
                buttonActiveTimeMin = Mathf.Max(0.5f, baseButtonActiveTimeMin - (difficultyLevel - 1) * 0.05f);
                buttonActiveTimeMax = Mathf.Max(1f, baseButtonActiveTimeMax - (difficultyLevel - 1) * 0.05f);

                // Кнопки со значениями быстрее теряют заряд
                valueDecreasePerSecond = Mathf.Min(30f, valueDecreasePerSecond + 1f);

                difficultyTimer = difficultyIncreaseInterval;
                Debug.Log($"Уровень сложности {difficultyLevel}! Цели каждые {targetChangeInterval:F1}сек, значение кнопок падает на {valueDecreasePerSecond:F1}/сек");
            }
        }
    }

    // Конец игры (давление достигло 100)
    void GameOver()
    {
        gameOver = true;
        Debug.Log("ИГРА ОКОНЧЕНА! Давление 100%");
        if (pressureText != null)
        {
            pressureText.text = "ИГРА ОКОНЧЕНА! Давление 100%";
        }
    }
}