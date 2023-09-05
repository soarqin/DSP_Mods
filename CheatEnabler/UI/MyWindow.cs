using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CheatEnabler.UI;

// MyWindowManager modified from LSTM: https://github.com/hetima/DSP_LSTM/blob/main/LSTM/MyWindowCtl.cs
public static class MyWindowManager
{
    private static readonly List<ManualBehaviour> Windows = new(4);
    private static bool _inited = false;

    public static T CreateWindow<T>(string name, string title = "") where T : Component
    {
        var srcWin = UIRoot.instance.uiGame.tankWindow;
        var src = srcWin.gameObject;
        var go = Object.Instantiate(src, srcWin.transform.parent);
        go.name = name;
        go.SetActive(false);
        Object.Destroy(go.GetComponent<UITankWindow>());
        var win = go.AddComponent<T>() as ManualBehaviour;
        if (win == null)
            return null;
        //shadow 
        for (var i = 0; i < go.transform.childCount; i++)
        {
            var child = go.transform.GetChild(i).gameObject;
            if (child.name == "panel-bg")
            {
                var btn = child.GetComponentInChildren<Button>();
                //close-btn
                if (btn != null)
                {
                    btn.onClick.AddListener(win._Close);
                }
                else
                {

                }
            }
            else if (child.name != "shadow" && child.name != "panel-bg")
            {
                Object.Destroy(child);
            }
        }

        SetTitle(win, title);
        win._Create();
        if (_inited)
        {
            win._Init(win.data);
        }
        Windows.Add(win);
        return win as T;
    }

    public static void SetTitle(ManualBehaviour win, string title)
    {
        var txt = GetTitleText(win);
        if (txt)
        {
            txt.text = title;
        }
    }
    public static Text GetTitleText(ManualBehaviour win)
    {
        return win.gameObject.transform.Find("panel-bg/title-text")?.gameObject.GetComponent<Text>();
    }


    public static RectTransform GetRectTransform(ManualBehaviour win)
    {
        return win.GetComponent<RectTransform>();
    }

    /*
    public static void SetRect(ManualBehaviour win, RectTransform rect)
    {
        var rectTransform = win.GetComponent<RectTransform>();
        //rectTransform.position =
        //rectTransform.sizeDelta = rect;
    }
    */

    public static void OpenWindow(ManualBehaviour win)
    {
        win._Open();
        win.transform.SetAsLastSibling();
    }

    public static void CloseWindow(ManualBehaviour win)
    {
        win._Close();
    }

    public static class Patch
    {

        /*
        //_Create -> _Init
        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnCreate")]
        public static void UIGame__OnCreate_Postfix()
        {
        }
        */

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnDestroy")]
        public static void UIGame__OnDestroy_Postfix()
        {
            foreach (var win in Windows)
            {
                win._Destroy();
            }
            Windows.Clear();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnInit")]
        public static void UIGame__OnInit_Postfix(UIGame __instance)
        {
            foreach (var win in Windows)
            {
                win._Init(win.data);
            }
            _inited = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnFree")]
        public static void UIGame__OnFree_Postfix()
        {
            foreach (var win in Windows)
            {
                win._Free();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "_OnUpdate")]
        public static void UIGame__OnUpdate_Postfix()
        {
            if (GameMain.isPaused || !GameMain.isRunning)
            {
                return;
            }
            foreach (var win in Windows)
            {
                win._Update();
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIGame), "ShutAllFunctionWindow")]
        public static void UIGame_ShutAllFunctionWindow_Postfix()
        {
            foreach (var win in Windows)
            {
                win._Close();
            }
        }
    }
}