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
    [Tooltip("Scrollbar громкости музыки (value 0–1)")]
    public Scrollbar musicVolumeScrollbar;
    [Tooltip("Текст с текущим значением (опционально)")]
    public TextMeshProUGUI musicVolumeText;

    // ══════════════════════════════════════════════════════════════════════
    //  ЗВУК — SFX
    // ══════════════════════════════════════════════════════════════════════
    [Header("Звук — SFX")]
    [Tooltip("Scrollbar громкости звуковых эффектов (value 0–1)")]
    public Scrollbar sfxVolumeScrollbar;
    [Tooltip("Текст с текущим значением (опционально)")]
    public TextMeshProUGUI sfxVolumeText;

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

    [Header("Инструменты")]
    public TextMeshProUGUI umbrellaKeyText;
    public Button umbrellaKeyButton;

    [Tooltip("Текст и кнопка для переназначения клавиши действия (Q — одна на все инструменты).")]
    public TextMeshProUGUI actionKeyText;
    public Button actionKeyButton;

    [Header("Взаимодействие")]
    public TextMeshProUGUI interactKeyText;
    public Button interactKeyButton;

    [Header("Ссылки")]
    public PlayerController playerController;
    public PauseMenu pauseMenu;
    public UmbrellaTool umbrellaTool;
    public ToolHolder toolHolder;
    public PlayerInteraction playerInteraction;

    private const string SENSITIVITY_KEY = "MouseSensitivity";
    private const string PAUSE_KEY       = "PauseKey";
    private const string KEY_FORWARD     = "KeyForward";
    private const string KEY_BACK        = "KeyBack";
    private const string KEY_LEFT        = "KeyLeft";
    private const string KEY_RIGHT       = "KeyRight";
    private const string KEY_JUMP        = "KeyJump";
    private const string KEY_UMBRELLA = "KeyUmbrella";
    private const string KEY_ACTION   = "KeyAction";
    private const string KEY_INTERACT = "KeyInteract";

    private Key keyForward;
    private Key keyBack;
    private Key keyLeft;
    private Key keyRight;
    private Key keyJump;
    private Key keyUmbrella;
    private Key keyAction;
    private Key keyInteract;

    private string rebindingTarget = "";

    void Start()
    {
        float savedSens   = PlayerPrefs.GetFloat(SENSITIVITY_KEY, playerController != null ? playerController.mouseSensitivity : 2f);
        keyForward        = (Key)PlayerPrefs.GetInt(KEY_FORWARD, (int)Key.W);
        keyBack           = (Key)PlayerPrefs.GetInt(KEY_BACK,    (int)Key.S);
        keyLeft           = (Key)PlayerPrefs.GetInt(KEY_LEFT,    (int)Key.A);
        keyRight          = (Key)PlayerPrefs.GetInt(KEY_RIGHT,   (int)Key.D);
        keyJump           = (Key)PlayerPrefs.GetInt(KEY_JUMP,    (int)Key.Space);
        keyUmbrella = (Key)PlayerPrefs.GetInt(KEY_UMBRELLA, umbrellaTool != null ? (int)umbrellaTool.EquipKey : (int)Key.Digit1);
        keyAction   = (Key)PlayerPrefs.GetInt(KEY_ACTION,   toolHolder   != null ? (int)toolHolder.ActionKey  : (int)Key.Q);
        keyInteract = (Key)PlayerPrefs.GetInt(KEY_INTERACT, playerInteraction != null ? (int)playerInteraction.interactKey : (int)Key.E);
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

        if (umbrellaTool != null)
            umbrellaTool.EquipKey = keyUmbrella;

        if (toolHolder != null)
            toolHolder.ActionKey = keyAction;

        if (playerInteraction != null)
            playerInteraction.interactKey = keyInteract;

        if (pauseMenu != null)
            pauseMenu.pauseKey = savedPauseKey;

        if (sensitivityScrollbar != null)
        {
            sensitivityScrollbar.value = Mathf.InverseLerp(minSensitivity, maxSensitivity, savedSens);
            sensitivityScrollbar.onValueChanged.AddListener(OnSensitivityChanged);
        }

        InitAudioScrollbars();
        UpdateAllKeyTexts();
        UpdateSensitivityText(savedSens);
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ИНИЦИАЛИЗАЦИЯ АУДИО
    // ══════════════════════════════════════════════════════════════════════

    void InitAudioScrollbars()
    {
        if (AudioManager.Instance == null) return;

        if (musicVolumeScrollbar != null)
        {
            musicVolumeScrollbar.value = AudioManager.Instance.MusicVolume;
            musicVolumeScrollbar.onValueChanged.AddListener(OnMusicVolumeChanged);
            UpdateVolumeText(musicVolumeText, AudioManager.Instance.MusicVolume);
        }

        if (sfxVolumeScrollbar != null)
        {
            sfxVolumeScrollbar.value = AudioManager.Instance.SFXVolume;
            sfxVolumeScrollbar.onValueChanged.AddListener(OnSFXVolumeChanged);
            UpdateVolumeText(sfxVolumeText, AudioManager.Instance.SFXVolume);
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

    // SFX = все звуки кроме музыки (шаги, выстрелы, кнопки, меню и т.д.)
    void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SFXVolume = value;
        UpdateVolumeText(sfxVolumeText, value);
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
            case "Jump":     if (jumpKeyText     != null) jumpKeyText.text     = "Нажмите клавишу..."; break;
            case "Umbrella": if (umbrellaKeyText != null) umbrellaKeyText.text = "Нажмите клавишу..."; break;
            case "Action":   if (actionKeyText   != null) actionKeyText.text   = "Нажмите клавишу..."; break;
            case "Interact": if (interactKeyText != null) interactKeyText.text = "Нажмите клавишу..."; break;
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
            case "Umbrella":
                keyUmbrella = key;
                if (umbrellaTool != null) umbrellaTool.EquipKey = key;
                PlayerPrefs.SetInt(KEY_UMBRELLA, (int)key);
                break;
            case "Action":
                keyAction = key;
                if (toolHolder != null) toolHolder.ActionKey = key;
                PlayerPrefs.SetInt(KEY_ACTION, (int)key);
                break;
            case "Interact":
                keyInteract = key;
                if (playerInteraction != null) playerInteraction.interactKey = key;
                PlayerPrefs.SetInt(KEY_INTERACT, (int)key);
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
        if (umbrellaKeyText != null) umbrellaKeyText.text = keyUmbrella.ToString();
        if (actionKeyText   != null) actionKeyText.text   = keyAction.ToString();
        if (interactKeyText != null) interactKeyText.text = keyInteract.ToString();
    }

    void SetAllButtonsInteractable(bool state)
    {
        if (pauseKeyButton    != null) pauseKeyButton.interactable    = state;
        if (moveForwardButton != null) moveForwardButton.interactable = state;
        if (moveBackButton    != null) moveBackButton.interactable    = state;
        if (moveLeftButton    != null) moveLeftButton.interactable    = state;
        if (moveRightButton   != null) moveRightButton.interactable   = state;
        if (jumpKeyButton     != null) jumpKeyButton.interactable     = state;
        if (umbrellaKeyButton != null) umbrellaKeyButton.interactable = state;
        if (actionKeyButton   != null) actionKeyButton.interactable   = state;
        if (interactKeyButton != null) interactKeyButton.interactable = state;
    }
}
