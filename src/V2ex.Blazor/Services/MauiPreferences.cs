﻿using System.Text.Json;
using IPreferences = V2ex.Api.IPreferences;

namespace V2ex.Blazor.Services;

public class MauiPreferences : IPreferences
{
    public T Get<T>(string key, T defaultValue)
    {
        string? value = null;
        var result = Preferences.Get(key, value);
        if (result == null)
        {
            return defaultValue;
        }
        return JsonSerializer.Deserialize<T>(result) ?? defaultValue;
    }

    public void Set<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
        Preferences.Set(key, json);
    }
}