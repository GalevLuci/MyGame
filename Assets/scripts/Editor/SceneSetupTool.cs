using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

/// <summary>
/// Инструмент установки AudioManager и VFXManager в сцену.
/// Меню: Tools ▶ MyGame Setup ▶ ...
/// </summary>
public static class SceneSetupTool
{
    // ══════════════════════════════════════════════════════════════════════
    //  ПОЛНАЯ УСТАНОВКА (одна кнопка)
    // ══════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/MyGame Setup/✅ Setup ALL (Audio + VFX)")]
    public static void SetupAll()
    {
        SetupAudioManager();
        SetupVFXManager();
        Debug.Log("[MyGame Setup] AudioManager + VFXManager добавлены в сцену!");
        EditorUtility.DisplayDialog(
            "MyGame Setup",
            "✅ Готово!\n\nAudioManager и VFXManager созданы в сцене.\n\n" +
            "Следующие шаги:\n" +
            "• Назначь аудиоклипы в AudioManager (Inspector)\n" +
            "• Назначь Global Volume в VFXManager (если используешь URP)\n" +
            "• Привяжи слайдеры в SettingsMenu",
            "Понятно");
    }

    // ══════════════════════════════════════════════════════════════════════
    //  AUDIO MANAGER
    // ══════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/MyGame Setup/Setup AudioManager")]
    public static void SetupAudioManager()
    {
        // Проверяем — уже есть?
        AudioManager existing = Object.FindFirstObjectByType<AudioManager>();
        if (existing != null)
        {
            Debug.LogWarning("[MyGame Setup] AudioManager уже есть в сцене: " + existing.gameObject.name);
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        // Корневой объект
        GameObject root = new GameObject("AudioManager");
        Undo.RegisterCreatedObjectUndo(root, "Create AudioManager");

        AudioManager am = root.AddComponent<AudioManager>();

        // Music Source
        GameObject musicGO = new GameObject("MusicSource");
        Undo.RegisterCreatedObjectUndo(musicGO, "Create MusicSource");
        musicGO.transform.SetParent(root.transform);
        AudioSource musicSrc = musicGO.AddComponent<AudioSource>();
        musicSrc.loop        = true;
        musicSrc.playOnAwake = false;
        musicSrc.volume      = 0.5f;

        // SFX Source
        GameObject sfxGO = new GameObject("SFXSource");
        Undo.RegisterCreatedObjectUndo(sfxGO, "Create SFXSource");
        sfxGO.transform.SetParent(root.transform);
        AudioSource sfxSrc = sfxGO.AddComponent<AudioSource>();
        sfxSrc.loop        = false;
        sfxSrc.playOnAwake = false;
        sfxSrc.volume      = 1f;

        // UI Source
        GameObject uiGO = new GameObject("UISource");
        Undo.RegisterCreatedObjectUndo(uiGO, "Create UISource");
        uiGO.transform.SetParent(root.transform);
        AudioSource uiSrc = uiGO.AddComponent<AudioSource>();
        uiSrc.loop        = false;
        uiSrc.playOnAwake = false;
        uiSrc.volume      = 1f;

        // Привязываем источники через SerializedObject
        SerializedObject so = new SerializedObject(am);
        so.FindProperty("musicSource").objectReferenceValue = musicSrc;
        so.FindProperty("sfxSource").objectReferenceValue   = sfxSrc;
        so.FindProperty("uiSource").objectReferenceValue    = uiSrc;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("[MyGame Setup] AudioManager создан! " +
                  "Назначь аудиоклипы (Pause Open/Close, Button Click) в Inspector.");
    }

    // ══════════════════════════════════════════════════════════════════════
    //  VFX MANAGER
    // ══════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/MyGame Setup/Setup VFXManager")]
    public static void SetupVFXManager()
    {
        // Проверяем — уже есть?
        VFXManager existing = Object.FindFirstObjectByType<VFXManager>();
        if (existing != null)
        {
            Debug.LogWarning("[MyGame Setup] VFXManager уже есть в сцене: " + existing.gameObject.name);
            Selection.activeGameObject = existing.gameObject;
            return;
        }

        GameObject root = new GameObject("VFXManager");
        Undo.RegisterCreatedObjectUndo(root, "Create VFXManager");

        VFXManager vm = root.AddComponent<VFXManager>();

        // Попробуем найти Global Volume на сцене автоматически
        Volume globalVol = Object.FindFirstObjectByType<Volume>();
        if (globalVol != null)
        {
            SerializedObject so = new SerializedObject(vm);
            so.FindProperty("globalVolume").objectReferenceValue = globalVol;
            so.ApplyModifiedProperties();
            Debug.Log("[MyGame Setup] Global Volume найден и привязан автоматически: " + globalVol.gameObject.name);
        }
        else
        {
            Debug.Log("[MyGame Setup] Global Volume не найден — назначь вручную в Inspector VFXManager.");
        }

        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        Debug.Log("[MyGame Setup] VFXManager создан!");
    }

    // ══════════════════════════════════════════════════════════════════════
    //  ПРОВЕРКА НАЛИЧИЯ
    // ══════════════════════════════════════════════════════════════════════

    [MenuItem("Tools/MyGame Setup/Check Scene Setup")]
    public static void CheckSceneSetup()
    {
        bool hasAudio = Object.FindFirstObjectByType<AudioManager>() != null;
        bool hasVFX   = Object.FindFirstObjectByType<VFXManager>()   != null;
        bool hasPause = Object.FindFirstObjectByType<PauseMenu>()     != null;

        string status =
            (hasAudio ? "✅" : "❌") + " AudioManager\n" +
            (hasVFX   ? "✅" : "❌") + " VFXManager\n"   +
            (hasPause ? "✅" : "❌") + " PauseMenu";

        EditorUtility.DisplayDialog("Статус сцены", status, "OK");
        Debug.Log("[MyGame Setup] Статус сцены:\n" + status);
    }
}
