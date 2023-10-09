using System;
using UnityEngine;
using UXAssist.UI;
using UXAssist.Common;

namespace UXAssist;

public static class UIConfigWindow
{
    private static RectTransform _windowTrans;
    private static MyConfigWindow _configWindow;

    public static void Init()
    {
        I18N.Add("UXAssist", "UXAssist", "UX助手");
        I18N.Apply();
        MyConfigWindow.OnUICreated += CreateUI;
    }

    private static void CreateUI(MyConfigWindow wnd, RectTransform trans)
    {
        _configWindow = wnd;
        _windowTrans = trans;
        var tab1 = wnd.AddTab(_windowTrans, "UXAssist");
    }
}
