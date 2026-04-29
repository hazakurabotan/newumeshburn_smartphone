using System.Reflection;
using UnityEngine;

public static class MenuManagerCompatibilityExtensions
{
    public static void ToggleMenu(this MenuManager menuManager)
    {
        if (menuManager == null) return;

        if (InvokeIfExists(menuManager, "ToggleGirlsGearUI")) return;
        if (InvokeIfExists(menuManager, "ToggleUI")) return;
        if (InvokeIfExists(menuManager, "Toggle")) return;

        GameObject root = FindRootObject(menuManager);
        if (root != null)
        {
            if (root.activeSelf)
            {
                if (InvokeIfExists(menuManager, "CloseGirlsGearUI")) return;
                if (InvokeIfExists(menuManager, "CloseUI")) return;
                if (InvokeIfExists(menuManager, "Close")) return;
            }
            else
            {
                if (InvokeIfExists(menuManager, "OpenGirlsGearUI")) return;
                if (InvokeIfExists(menuManager, "OpenUI")) return;
                if (InvokeIfExists(menuManager, "Open")) return;
            }
        }

        Debug.LogError("[MenuManagerCompatibilityExtensions] MenuManager Ç… ToggleMenu ëäìñÇÃèàóùÇ™å©Ç¬Ç©ÇËÇ‹ÇπÇÒÇ≈ÇµÇΩÅB");
    }

    public static void CloseMenu(this MenuManager menuManager)
    {
        if (menuManager == null) return;

        if (InvokeIfExists(menuManager, "CloseGirlsGearUI")) return;
        if (InvokeIfExists(menuManager, "CloseUI")) return;
        if (InvokeIfExists(menuManager, "Close")) return;

        GameObject root = FindRootObject(menuManager);
        if (root != null)
        {
            root.SetActive(false);
            Time.timeScale = 1f;
            return;
        }

        Debug.LogError("[MenuManagerCompatibilityExtensions] MenuManager Ç… CloseMenu ëäìñÇÃèàóùÇ™å©Ç¬Ç©ÇËÇ‹ÇπÇÒÇ≈ÇµÇΩÅB");
    }

    private static bool InvokeIfExists(object target, string methodName)
    {
        if (target == null) return false;

        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            System.Type.EmptyTypes,
            null
        );

        if (method == null) return false;

        method.Invoke(target, null);
        return true;
    }

    private static GameObject FindRootObject(object target)
    {
        if (target == null) return null;

        System.Type type = target.GetType();

        string[] fieldNames =
        {
            "girlsGearUIRoot",
            "uiRoot",
            "menuRoot",
            "root"
        };

        foreach (string fieldName in fieldNames)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && typeof(GameObject).IsAssignableFrom(field.FieldType))
            {
                return field.GetValue(target) as GameObject;
            }
        }

        string[] propertyNames =
        {
            "girlsGearUIRoot",
            "uiRoot",
            "menuRoot",
            "root"
        };

        foreach (string propertyName in propertyNames)
        {
            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && typeof(GameObject).IsAssignableFrom(property.PropertyType))
            {
                return property.GetValue(target) as GameObject;
            }
        }

        return null;
    }
}