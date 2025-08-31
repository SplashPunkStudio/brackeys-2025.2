using UnityEngine;

public static class PlayerPrefsUtils
{

    public enum SettingsKeys
    {
        VOLUME_MUSIC, VOLUME_SFX, LANGUAGE
    }
    
    public static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetBool(SettingsKeys key, bool value)
    {
        SetBool(key.ToString(), value);
    }

    public static bool GetBool(string key, bool defaultValue = false)
    {
        if (!PlayerPrefs.HasKey(key))
            return defaultValue;

        return PlayerPrefs.GetInt(key) == 1;
    }

    public static bool GetBool(SettingsKeys key, bool defaultValue = false)
    {
        return GetBool(key.ToString(), defaultValue);
    }

    public static void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    public static void SetInt(SettingsKeys key, int value)
    {
        SetInt(key.ToString(), value);
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
        if (!PlayerPrefs.HasKey(key))
            return defaultValue;

        return PlayerPrefs.GetInt(key);
    }

    public static int GetInt(SettingsKeys key, int defaultValue = 0)
    {
        return GetInt(key.ToString(), defaultValue);
    }

    public static void SetFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    public static void SetFloat(SettingsKeys key, float value)
    {
        SetFloat(key.ToString(), value);
    }

    public static float GetFloat(string key, float defaultValue = 0f)
    {
        if (!PlayerPrefs.HasKey(key))
            return defaultValue;

        return PlayerPrefs.GetFloat(key);
    }

    public static float GetFloat(SettingsKeys key, float defaultValue = 0f)
    {
        return GetFloat(key.ToString(), defaultValue);
    }

    public static void SetString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    public static void SetString(SettingsKeys key, string value)
    {
        SetString(key.ToString(), value);
    }

    public static string GetString(string key, string defaultValue = null)
    {
        if (!PlayerPrefs.HasKey(key))
            return defaultValue;

        return PlayerPrefs.GetString(key);
    }

    public static string GetString(SettingsKeys key, string defaultValue = null)
    {
        return GetString(key.ToString(), defaultValue);
    }

}
