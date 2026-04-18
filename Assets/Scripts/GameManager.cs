using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Запуск игры
    public void Play()
    {
        SceneManager.LoadScene("Play");
    }
}
