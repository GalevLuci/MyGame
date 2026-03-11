using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Кнопка для паузы/возврата в игру")]
    public Key pauseKey = Key.Escape;

    [Header("Сцена меню")]
    [Tooltip("Название сцены главного меню")]
    public string menuSceneName = "MainMenu";

    [Header("UI")]
    [Tooltip("Canvas паузы (с кнопками)")]
    public GameObject pauseCanvas;
    [Tooltip("Canvas настроек (отдельный)")]
    public GameObject settingsCanvas;

    private bool isPaused = false;

    void Awake()
    {
        pauseCanvas.SetActive(false);
        settingsCanvas.SetActive(false);
    }

    void Update()
    {
        if (Keyboard.current[pauseKey].wasPressedThisFrame)
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    public void PauseGame()
    {
        pauseCanvas.SetActive(true);
        settingsCanvas.SetActive(false);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isPaused = true;
    }

    public void ResumeGame()
    {
        pauseCanvas.SetActive(false);
        settingsCanvas.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isPaused = false;
    }

    public void OpenSettings()
    {
        pauseCanvas.SetActive(false);
        settingsCanvas.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsCanvas.SetActive(false);
        pauseCanvas.SetActive(true);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }
}
