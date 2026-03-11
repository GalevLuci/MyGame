using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SettingsMenu : MonoBehaviour
{
    // ══════════════════════════════════════════════════════════════════════
    //  ЧУВСТВИТЕЛЬНОСТЬ МЫШИ
    // ══════════════════════════════════════════════════════════════════════
    [Header("Чувствительность мыши")]
    public Scrollbar sensitivityScrollbar;
    public TextMeshProUGUI sensitivityValueText;
    public float minSensitivity = 0.1f;
    public float maxSensitivity = 10f;

    // ══════════════════════════════════════════════════════════════════════
    //  ЗВУК — МУЗЫКА
    // ══════════════════════════════════════════════════════════════════════
    [Header("Звук — Музыка")]
    [Tooltip("Слайдер громкости музыки (0–1)")]
    public Slider musicVolumeSlider;
    [Tooltip("Текст с текущим значением (опционально)")]
    public TextMeshProUGUI musicVolumeText;

    // ══════════════════════════════════════════════════════════════════════
    //  ЗВУК — SFX
    // ══════════════════════════════════════════════════════════════════════
    [Header("Звук — SFX")]
    [Tooltip("Слайдер громкости звуковых эффектов (0–1)")]
    public Slider sfxVolumeSlider;
    [Tooltip("Текст с текущим значением (опционально)")]
    public TextMeshProUGUI sfxVolumeText;

    // ══════════════════════════════════════════════════════════════════════
    //  ЗВУК — UI
    // ══════════════════════════════════════════════════════════════════════
    [Header("Звук — UI")]
    [Tooltip("Слайдер громкости звуков интерфейса (0–1)")]
    public Slider uiVolumeSlider;
    [Tooltip("Текст с текущим значением (опционально)")]
    public TextMeshProUGUI uiVolumeText;

    // ══════════════════════════════════════════════════════════════════════
    //  VFX
    // ══════════════════════════════════════════════════════════════════════
    [Header("VFX")]
    [Tooltip("Toggle: включить/выключить VFX (частицы)")]
    public Toggle vfxToggle;
    [Tooltip("Toggle: включить/выключить Post Processing")]
    public Toggle postProcessToggle;
    [Tooltip("Dropdown: качество теней  0=Выкл  1=Hard  2=Всё")]
    public TMP_Dropdown shadowQualityDropdown;

    // ══════════════════════════════════════════════════════════════════════
    //  КЛАВИШИ
    // ══════════════════════════════════════════════════════════════════════
    [Header("Кнопка паузы")]
    public TextMeshProUGUI pauseKeyText;
    public Button pauseKeyButton;

    [Header("Кнопки движения")]
    public TextMeshProUGUI moveForwardText;
    public Button moveForwardButton;

    public TextMeshProUGUI moveBackText;
    public Button moveBackButton;

    public TextMeshProUGUI moveLeftText;
    public Button moveLeftButton;

    public TextMeshProUGUI moveRightText;
    public Button moveRightButton;

    [Header("Прыжок")]
    public TextMeshProUGUI jumpKeyText;
    public Button jumpKeyButton;

    [Header("Ссылки")]
    public PlayerController playerController;
    public PauseMenu pauseMenu;

    private const string SENSITIVITY_KEY = "MouseSensitivity";
    private const string PAUSE_KEY       = "PauseKey";
    private const string KEY_FORWARD     = "KeyForward";
    private const string KEY_BACK        = "KeyBack";
    private const string KEY_LEFT        = "KeyLeft";
    private const string KEY_RIGHT       = "KeyRight";
    private const string KEY_JUMP        = "KeyJump";

    private Key keyForward;
    private Key keyBack;
    private Key keyLeft;
    private Key keyRight;
    private Key keyJump;

    private string rebindingTarget = "";

    void Start()
    {
        float savedSens   = PlayerPrefs.GetFloat(SENSITIVITY_KEY, playerController != null ? playerController.mouseSensitivity : 2f);
        keyForward        = (Key)PlayerPrefs.GetInt(KEY_FORWARD, (int)Key.W);
        keyBack           = (Key)PlayerPrefs.GetInt(KEY_BACK,    (int)Key.S);
        keyLeft           = (Key)PlayerPrefs.GetInt(KEY_LEFT,    (int)Key.A);
        keyRight          = (Key)PlayerPrefs.GetInt(KEY_RIGHT,   (int)Key.D);
        keyJump           = (Key)PlayerPrefs.GetInt(KEY_JUMP,    (int)Key.Space);
        Key savedPauseKey = (Key)PlayerPrefs.GetInt(PAUSE_KEY,   pauseMenu != null ? (int)pauseMenu.pauseKey : (int)Key.Escape);

        if (playerController != null)
        {
            playerController.mouseSensitivity = savedSens;
            playerController.keyForward = keyForward;
            playerController.keyBack    = keyBack;
            playerController.keyLeft    = keyLeft;
            playerController.keyRight   = keyRight;
            playerController.keyJump    = keyJump;
        }

        if (pauseMenu != null)
            pauseMenu.pauseKey = savedPauseKey;

        if (sensitivityScrollbar != null)
        {
            sensitivityScrollbar.value = Mathf.InverseLerp(minSensitivity, maxSensitivity, savedSens);
            sensitivityScrollbar.onValueChanged.AddListener(OnSensitivityChanged);
        }

        InitAudioSliders();
        InitVFXControls();
        UpdateAllKeyTexts();
        UpdateSensitivityText(savedSens);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ИНИЦИАЛИЗАЦИЯ АУДИО
    // ══════════════════════════════════════════════════════════════════════

    void InitAudioSliders()
    {
        if (AudioManager.Instance == null) return;

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.value    = AudioManager.Instance.MusicVolume;
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            UpdateVolumeText(musicVolumeText, AudioManager.Instance.MusicVolume);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.value    = AudioManager.Instance.SFXVolume;
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            UpdateVolumeText(sfxVolumeText, AudioManager.Instance.SFXVolume);
        }

        if (uiVolumeSlider != null)
        {
            uiVolumeSlider.minValue = 0f;
            uiVolumeSlider.maxValue = 1f;
            uiVolumeSlider.value    = AudioManager.Instance.UIVolume;
            uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);
            UpdateVolumeText(uiVolumeText, AudioManager.Instance.UIVolume);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ИНИЦИАЛИЗАЦИЯ VFX
    // ══════════════════════════════════════════════════════════════════════

    void InitVFXControls()
    {
        if (VFXManager.Instance == null) return;

        if (vfxToggle != null)
        {
            vfxToggle.isOn = VFXManager.Instance.VFXEnabled;
            vfxToggle.onValueChanged.AddListener(OnVFXToggleChanged);
        }

        if (postProcessToggle != null)
        {
            postProcessToggle.isOn = VFXManager.Instance.PostProcessingEnabled;
            postProcessToggle.onValueChanged.AddListener(OnPostProcessToggleChanged);
        }

        if (shadowQualityDropdown != null)
        {
            shadowQualityDropdown.value = VFXManager.Instance.ShadowQualityLevel;
            shadowQualityDropdown.onValueChanged.AddListener(OnShadowQualityChanged);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CALLBACKS — АУДИО
    // ══════════════════════════════════════════════════════════════════════

    void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.MusicVolume = value;
        UpdateVolumeText(musicVolumeText, value);
    }

    void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SFXVolume = value;
        UpdateVolumeText(sfxVolumeText, value);
    }

    void OnUIVolumeChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.UIVolume = value;
        UpdateVolumeText(uiVolumeText, value);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  CALLBACKS — VFX
    // ══════════════════════════════════════════════════════════════════════

    void OnVFXToggleChanged(bool value)
    {
        if (VFXManager.Instance != null) VFXManager.Instance.VFXEnabled = value;
    }

    void OnPostProcessToggleChanged(bool value)
    {
        if (VFXManager.Instance != null) VFXManager.Instance.PostProcessingEnabled = value;
    }

    void OnShadowQualityChanged(int value)
    {
        if (VFXManager.Instance != null) VFXManager.Instance.ShadowQualityLevel = value;
    }

    static void UpdateVolumeText(TextMeshProUGUI label, float value)
    {
        if (label != null) label.text = Mathf.RoundToInt(value * 100) + "%";
    }

    void Update()
    {
        if (rebindingTarget == "") return;

        foreach (Key key in System.Enum.GetValues(typeof(Key)))
        {
            if (key == Key.None) continue;
            if (Keyboard.current[key].wasPressedThisFrame)
            {
                ApplyRebind(rebindingTarget, key);
                return;
            }
        }
    }

    // ── Чувствительность ─────────────────────────────────────────────

    void OnSensitivityChanged(float value)
    {
        float sens = Mathf.Lerp(minSensitivity, maxSensitivity, value);
        if (playerController != null)
            playerController.mouseSensitivity = sens;
        UpdateSensitivityText(sens);
        PlayerPrefs.SetFloat(SENSITIVITY_KEY, sens);
        PlayerPrefs.Save();
    }

    void UpdateSensitivityText(float value)
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("F1");
    }

    // ── Переназначение кнопок ─────────────────────────────────────────

    public void StartRebind(string target)
    {
        rebindingTarget = target;
        SetAllButtonsInteractable(false);
        EventSystem.current.SetSelectedGameObject(null);

        switch (target)
        {
            case "Pause":   if (pauseKeyText    != null) pauseKeyText.text    = "Нажмите клавишу..."; break;
            case "Forward": if (moveForwardText != null) moveForwardText.text = "Нажмите клавишу..."; break;
            case "Back":    if (moveBackText    != null) moveBackText.text    = "Нажмите клавишу..."; break;
            case "Left":    if (moveLeftText    != null) moveLeftText.text    = "Нажмите клавишу..."; break;
            case "Right":   if (moveRightText   != null) moveRightText.text   = "Нажмите клавишу..."; break;
            case "Jump":    if (jumpKeyText     != null) jumpKeyText.text     = "Нажмите клавишу..."; break;
        }
    }

    void ApplyRebind(string target, Key key)
    {
        switch (target)
        {
            case "Pause":
                if (pauseMenu != null) pauseMenu.pauseKey = key;
                PlayerPrefs.SetInt(PAUSE_KEY, (int)key);
                break;
            case "Forward":
                keyForward = key;
                if (playerController != null) playerController.keyForward = key;
                PlayerPrefs.SetInt(KEY_FORWARD, (int)key);
                break;
            case "Back":
                keyBack = key;
                if (playerController != null) playerController.keyBack = key;
                PlayerPrefs.SetInt(KEY_BACK, (int)key);
                break;
            case "Left":
                keyLeft = key;
                if (playerController != null) playerController.keyLeft = key;
                PlayerPrefs.SetInt(KEY_LEFT, (int)key);
                break;
            case "Right":
                keyRight = key;
                if (playerController != null) playerController.keyRight = key;
                PlayerPrefs.SetInt(KEY_RIGHT, (int)key);
                break;
            case "Jump":
                keyJump = key;
                if (playerController != null) playerController.keyJump = key;
                PlayerPrefs.SetInt(KEY_JUMP, (int)key);
                break;
        }

        PlayerPrefs.Save();
        rebindingTarget = "";
        SetAllButtonsInteractable(true);
        UpdateAllKeyTexts();
    }

    void UpdateAllKeyTexts()
    {
        if (pauseKeyText    != null && pauseMenu != null) pauseKeyText.text    = pauseMenu.pauseKey.ToString();
        if (moveForwardText != null) moveForwardText.text = keyForward.ToString();
        if (moveBackText    != null) moveBackText.text    = keyBack.ToString();
        if (moveLeftText    != null) moveLeftText.text    = keyLeft.ToString();
        if (moveRightText   != null) moveRightText.text   = keyRight.ToString();
        if (jumpKeyText     != null) jumpKeyText.text     = keyJump.ToString();
    }

    void SetAllButtonsInteractable(bool state)
    {
        if (pauseKeyButton    != null) pauseKeyButton.interactable    = state;
        if (moveForwardButton != null) moveForwardButton.interactable = state;
        if (moveBackButton    != null) moveBackButton.interactable    = state;
        if (moveLeftButton    != null) moveLeftButton.interactable    = state;
        if (moveRightButton   != null) moveRightButton.interactable   = state;
        if (jumpKeyButton     != null) jumpKeyButton.interactable     = state;
    }
}
