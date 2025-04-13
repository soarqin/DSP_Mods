using BepInEx.Configuration;
using CommonAPI.Systems;
using UnityEngine;

namespace UXAssist.Common;

public static class KeyBindings
{
    public static PressKeyBind RegisterKeyBinding(BuiltinKey key)
    {
        return CustomKeyBindSystem.RegisterKeyBindWithReturn<PressKeyBind>(key);
    }
    public static CombineKey FromKeyboardShortcut(KeyboardShortcut shortcut)
    {
        byte mod = 0;
        foreach (var modifier in shortcut.Modifiers)
        {
            mod |= modifier switch
            {
                KeyCode.LeftShift => 1,
                KeyCode.RightShift => 1,
                KeyCode.LeftControl => 2,
                KeyCode.RightControl => 2,
                KeyCode.LeftAlt => 4,
                KeyCode.RightAlt => 4,
                _ => 0
            };
        }

        return new CombineKey((int)shortcut.MainKey, mod, ECombineKeyAction.OnceClick, false);
    }

    public static bool IsKeyPressing(this PressKeyBind keyBind)
    {
        var defBind = keyBind.defaultBind;
        var overrideKey = VFInput.override_keys[defBind.id];
        return overrideKey.IsNull() ? defBind.key.GetKey() : overrideKey.GetKey();
    }
}
