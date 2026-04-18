using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PressureSystem : MonoBehaviour
{
    [Header("UI элементы")]
    public Slider pressureSlider;
    public Text pressureText;

    [Header("Настройки давления")]
    public float currentPressure = 0f;
    public float maxPressure = 100f;
    public float pressureIncreaseRate = 5f;

    [Header("Кнопки сброса (9 штук)")]
    public Button[] resetButtons = new Button[0]; // Инициализируем пустым массивом
    public float[] buttonTimers;
    public bool[] isButtonActive;
    public float buttonActiveTime = 3f;
    public float pressureDecreasePerButton = 2f;

    [Header("Нередактируемые ползунки (3 штуки)")]
    public Slider[] targetSliders = new Slider[0];
    public float[] targetValues;
    public float targetChangeInterval = 3f;

    [Header("Редактируемые ползунки (3 штуки)")]
    public Slider[] playerSliders = new Slider[0];
    public float[] playerSliderDecrease;

    [Header("Кнопки со значениями (2 штуки)")]
    public Button[] valueButtons = new Button[0];
    public float[] buttonValues;
    public float valueIncreasePerClick = 10f;
    public float maxButtonValue = 100f;
    public float pressureDecreaseFromValue = 0.5f;

    [Header("Настройки усложнения")]
    public float difficultyIncreaseInterval = 10f;
    private float difficultyTimer;

    private bool gameOver = false;

    void Start()
    {
        // ПРОВЕРКА: если массивы пустые, не запускаем игру
        if (resetButtons.Length == 0 || targetSliders.Length == 0 || playerSliders.Length == 0 || valueButtons.Length == 0)
        {
            Debug.LogError("ОШИБКА: Заполните все массивы в инспекторе Unity!");
            Debug.LogError($"ResetButtons: {resetButtons.Length}, TargetSliders: {targetSliders.Length}, PlayerSliders: {playerSliders.Length}, ValueButtons: {valueButtons.Length}");
            return;
        }

        // Инициализация давления
        currentPressure = 0f;
        UpdatePressureUI();

        // Инициализация массивов для кнопок
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

        // Инициализация целевых ползунков
        targetValues = new float[targetSliders.Length];
        for (int i = 0; i < targetSliders.Length; i++)
        {
            if (targetSliders[i] != null)
            {
                targetSliders[i].interactable = false;
                ChangeTargetValue(i);
            }
        }

        // Инициализация редактируемых ползунков
        playerSliderDecrease = new float[playerSliders.Length];
        for (int i = 0; i < playerSliders.Length; i++)
        {
            int index = i;
            if (playerSliders[i] != null)
            {
                playerSliders[i].onValueChanged.AddListener((value) => OnPlayerSliderChanged(index, value));
            }
            playerSliderDecrease[i] = 0f;
        }

        // Инициализация кнопок со значениями
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

        // Запускаем корутины
        StartCoroutine(PressureIncrease());
        StartCoroutine(ChangeTargetsPeriodically());
        StartCoroutine(ProcessButtonDecrease());
        StartCoroutine(ProcessSliderDecrease());
        StartCoroutine(IncreaseDifficulty());

        Debug.Log("Игра запущена! Давление: " + currentPressure);
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

    void OnResetButtonClick(int index)
    {
        if (gameOver) return;

        if (index < resetButtons.Length && !isButtonActive[index])
        {
            isButtonActive[index] = true;
            buttonTimers[index] = buttonActiveTime;
            if (resetButtons[index] != null)
            {
                resetButtons[index].image.color = Color.green;
            }
            Debug.Log($"Кнопка {index} активирована");
        }
    }

    IEnumerator ProcessButtonDecrease()
    {
        while (!gameOver)
        {
            yield return new WaitForSeconds(0.5f);

            for (int i = 0; i < resetButtons.Length; i++)
            {
                if (isButtonActive[i])
                {
                    currentPressure -= pressureDecreasePerButton * 0.5f;
                    if (currentPressure < 0) currentPressure = 0;
                    UpdatePressureUI();
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

    void OnPlayerSliderChanged(int index, float value)
    {
        if (gameOver) return;

        if (index < targetValues.Length && Mathf.Abs(value - targetValues[index]) <= 10f)
        {
            playerSliderDecrease[index] = 5f;
            Debug.Log($"Ползунок {index} попал в цель!");
        }
        else if (index < playerSliderDecrease.Length)
        {
            playerSliderDecrease[index] = 0f;
        }
    }

    IEnumerator ProcessSliderDecrease()
    {
        while (!gameOver)
        {
            yield return new WaitForSeconds(0.5f);

            for (int i = 0; i < playerSliders.Length; i++)
            {
                if (i < playerSliderDecrease.Length && playerSliderDecrease[i] > 0)
                {
                    currentPressure -= playerSliderDecrease[i] * 0.5f;
                    if (currentPressure < 0) currentPressure = 0;
                    UpdatePressureUI();

                    playerSliderDecrease[i] -= 0.5f;
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

            float decrease = buttonValues[index] * pressureDecreaseFromValue * 0.01f;
            currentPressure -= decrease;
            if (currentPressure < 0) currentPressure = 0;
            UpdatePressureUI();

            Debug.Log($"Кнопка {index}: {buttonValues[index]}, снижение {decrease}");
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

    IEnumerator IncreaseDifficulty()
    {
        difficultyTimer = difficultyIncreaseInterval;

        while (!gameOver)
        {
            yield return new WaitForSeconds(1f);
            difficultyTimer -= 1f;

            if (difficultyTimer <= 0)
            {
                pressureIncreaseRate += 1f;
                buttonActiveTime = Mathf.Max(1f, buttonActiveTime - 0.3f);
                targetChangeInterval = Mathf.Max(1f, targetChangeInterval - 0.2f);
                difficultyTimer = difficultyIncreaseInterval;
                Debug.Log($"Сложность повышена! Давление +{pressureIncreaseRate}/сек");
            }
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
    }

    void GameOver()
    {
        gameOver = true;
        Debug.Log("ИГРА ОКОНЧЕНА! Давление 100!");
        if (pressureText != null)
        {
            pressureText.text = "ИГРА ОКОНЧЕНА! Давление 100%";
        }
    }
}