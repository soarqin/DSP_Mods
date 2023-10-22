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

    public override void _OnCreate()
    {
        _windowTrans = GetComponent<RectTransform>();
        _windowTrans.sizeDelta = new Vector2(810f, 440f);

        OnUICreated?.Invoke(this, _windowTrans);
        SetCurrentTab(0);
        OnUpdateUI?.Invoke();
    }

    public override void _OnDestroy()
    {
    }

    public override bool _OnInit()
    {
        _windowTrans.anchoredPosition = new Vector2(0, 0);
        return true;
    }

    public override void _OnFree()
    {
    }

    public override void _OnOpen()
    {
    }

    public override void _OnClose()
    {
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
