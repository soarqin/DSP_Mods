using System;
using UnityEngine;

namespace UXAssist.UI;

public class MyConfigWindow : MyWindowWithTabs
{
    public static Action<MyConfigWindow, RectTransform> OnUICreated;
    public static Action OnUpdateUI;

    private RectTransform _windowTrans;

    public static MyConfigWindow CreateInstance()
    {
        return MyWindowManager.CreateWindow<MyConfigWindow>("UXAConfigWindow", "UXAssist Config");
    }

    public static void DestroyInstance(MyConfigWindow win)
    {
        MyWindowManager.DestroyWindow(win);
    }

    public override void _OnCreate()
    {
        base._OnCreate();
        _windowTrans = GetComponent<RectTransform>();
        OnUICreated?.Invoke(this, _windowTrans);
        SetCurrentTab(0);
        OnUpdateUI?.Invoke();
        // Delay 500ms to run AutoFitWindowSize() after the window is created
        // Invoke(nameof(AutoFitWindowSize), 0f);
    }

    public override void _OnDestroy()
    {
        _windowTrans = null;
        base._OnDestroy();
    }

    public override bool _OnInit()
    {
        if (!base._OnInit()) return false;
        _windowTrans.anchoredPosition = new Vector2(0, 0);
        return true;
    }

    public override void _OnUpdate()
    {
        base._OnUpdate();
        if (VFInput.escape && !VFInput.inputing)
        {
            VFInput.UseEscape();
            _Close();
            return;
        }

        OnUpdateUI?.Invoke();
    }
}
