using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public void RestartGame()
    {
        // Перезапустить текущую сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void BackToMainMenu()
    {
        // Загрузить главное меню (индекс 0)
        SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
        // Выйти из игры (работает только в собранной игре)
        Application.Quit();
        Debug.Log("Игра закрыта");
    }
}