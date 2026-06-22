using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UXAssist.Common;
using UXAssist.UI;

namespace UXAssist.Functions.UI;

internal static class MilkyWayUI
{
    const int ClusterUploadResultKeepCount = 100;
    private static readonly ClusterUploadResult[] _clusterUploadResults = new ClusterUploadResult[ClusterUploadResultKeepCount];
    private static readonly object _clusterUploadResultsLock = new();
    private static int _clusterUploadResultsHead = 0;
    private static int _clusterUploadResultsCount = 0;

    private struct ClusterUploadResult
    {
        public DateTime UploadTime;
        public int Result;
        public float RequestTime;
    }

    private static ClusterPlayerData[] _topTenPlayerData = null;
    private static readonly StringBuilder _sb = new("         ", 12);

    public static MyCheckButton MilkyWayTopTenPlayersToggler;
    public static event Action OnMilkyWayTopTenPlayersUpdated;

    public static void Init()
    {
        I18N.Add("No recent milkyway upload results", "No recent milkyway upload results", "没有最近的银河系发电数据上传结果");
        I18N.Add("Success", "Success", "成功");
        I18N.Add("Failure: ", "Failure: ", "失败: ");
        I18N.Add("Show top players", "Show top players", "显示玩家排行榜");
        I18N.Add("Hide top players", "Hide top players", "隐藏玩家排行榜");
    }

    public static void Start()
    {
    }

    public static void Uninit()
    {
    }

    public static void OnInputUpdate()
    {
    }

    public static void OnUpdate()
    {
    }

    public static void AddClusterUploadResult(int result, float requestTime)
    {
        lock (_clusterUploadResultsLock)
        {
            if (_clusterUploadResultsCount >= ClusterUploadResultKeepCount)
            {
                _clusterUploadResults[_clusterUploadResultsHead] = new ClusterUploadResult { UploadTime = DateTime.Now, Result = result, RequestTime = requestTime };
                _clusterUploadResultsHead = (_clusterUploadResultsHead + 1) % ClusterUploadResultKeepCount;
            }
            else
            {
                _clusterUploadResults[(_clusterUploadResultsHead + _clusterUploadResultsCount) % ClusterUploadResultKeepCount] = new ClusterUploadResult { UploadTime = DateTime.Now, Result = result, RequestTime = requestTime };
                _clusterUploadResultsCount++;
            }
        }
    }

    public static void Export(BinaryWriter w)
    {
        lock (_clusterUploadResultsLock)
        {
            w.Write(_clusterUploadResultsCount);
            w.Write(_clusterUploadResultsHead);
            for (var i = 0; i < _clusterUploadResultsCount; i++)
            {
                ref var result = ref _clusterUploadResults[(i + _clusterUploadResultsHead) % ClusterUploadResultKeepCount];
                w.Write(result.UploadTime.ToBinary());
                w.Write(result.Result);
                w.Write(result.RequestTime);
            }
        }
    }

    public static void Import(BinaryReader r)
    {
        lock (_clusterUploadResultsLock)
        {
            _clusterUploadResultsCount = r.ReadInt32();
            _clusterUploadResultsHead = r.ReadInt32();
            for (var i = 0; i < _clusterUploadResultsCount; i++)
            {
                ref var result = ref _clusterUploadResults[(i + _clusterUploadResultsHead) % ClusterUploadResultKeepCount];
                result.UploadTime = DateTime.FromBinary(r.ReadInt64());
                result.Result = r.ReadInt32();
                result.RequestTime = r.ReadSingle();
            }
        }
    }

    public static void ClearClusterUploadResults()
    {
        lock (_clusterUploadResultsLock)
        {
            _clusterUploadResultsCount = 0;
            _clusterUploadResultsHead = 0;
        }
    }

    public static void ShowRecentMilkywayUploadResults()
    {
        lock (_clusterUploadResultsLock)
        {
            if (_clusterUploadResultsCount == 0)
            {
                UIMessageBox.Show("UXAssist".Translate(), "No recent milkyway upload results".Translate(), "确定".Translate(), UIMessageBox.INFO, null);
                return;
            }
            StringBuilder sb = new();
            var start = _clusterUploadResultsCount;
            var end = start > 10 ? start - 10 : 0;
            for (var i = start - 1; i >= end; i--)
            {
                var res = _clusterUploadResults[(i + _clusterUploadResultsHead) % ClusterUploadResultKeepCount];
                sb.AppendLine($"{res.UploadTime.ToString("yyyy-MM-dd HH:mm:ss")} - {((res.Result is 0 or 20) ? "Success".Translate() : ("Failure: ".Translate() + res.Result.ToString()))} - {res.RequestTime:F2}s");
            }
            UIMessageBox.Show("UXAssist".Translate(), sb.ToString(), "确定".Translate(), UIMessageBox.INFO, null);
        }
    }

    public static void SetTopPlayerCount(int count)
    {
        _topTenPlayerData = new ClusterPlayerData[count];
    }

    public static void SetTopPlayerData(int index, ref ClusterPlayerData playerData)
    {
        if (index < 0 || index >= _topTenPlayerData.Length) return;
        _topTenPlayerData[index] = playerData;
    }

    public static void UpdateMilkyWayTopTenPlayers()
    {
        if (_topTenPlayerData == null) return;
        OnMilkyWayTopTenPlayersUpdated?.Invoke();
    }

    public static void InitMilkyWayTopTenPlayers()
    {
        var uiRoot = UIRoot.instance;
        if (!uiRoot) return;

        var rect = uiRoot.uiMilkyWay.transform as RectTransform;
        var panel = new GameObject("uxassist-milkyway-top-ten-players-panel");
        var rtrans = panel.AddComponent<RectTransform>();
        rtrans.SetParent(rect);
        rtrans.sizeDelta = new Vector2(0f, 0f);
        rtrans.localScale = new Vector3(1f, 1f, 1f);
        rtrans.anchorMax = new Vector2(1f, 1f);
        rtrans.anchorMin = new Vector2(0f, 0f);
        rtrans.pivot = new Vector2(0f, 1f);
        rtrans.anchoredPosition3D = new Vector3(0, 0, 0f);

        MyFlatButton[] buttons = [];
        Text[] textFields = [];

        MilkyWayTopTenPlayersToggler = MyCheckButton.CreateCheckButton(0, 0, rtrans, false, "Show top players".Translate()).WithSize(120f, 24f);
        MilkyWayTopTenPlayersToggler.OnChecked += UpdateButtons;
        MilkyWayTopTenPlayersToggler.Checked = false;
        UpdateButtons();
        OnMilkyWayTopTenPlayersUpdated += UpdateButtons;

        Text CreateTextField(RectTransform parent)
        {
            var txt = UnityEngine.Object.Instantiate(UIRoot.instance.uiGame.assemblerWindow.stateText, parent);
            txt.gameObject.name = "uxassist-milkyway-top-ten-players-text-field";
            txt.text = "";
            txt.color = new Color(1f, 1f, 1f, 0.4f);
            txt.alignment = TextAnchor.MiddleLeft;
            txt.fontSize = 15;
            txt.rectTransform.sizeDelta = new Vector2(0, 18);
            return txt;
        }

        void UpdateButtons()
        {
            var chk = MilkyWayTopTenPlayersToggler.Checked;
            if (_topTenPlayerData == null)
            {
                MilkyWayTopTenPlayersToggler.gameObject.SetActive(false);
                return;
            }
            var count = _topTenPlayerData.Length;
            MilkyWayTopTenPlayersToggler.gameObject.SetActive(count > 0);
            if (count != buttons.Length)
            {
                for (var i = count; i < buttons.Length; i++)
                {
                    UnityEngine.Object.Destroy(buttons[i].gameObject);
                    UnityEngine.Object.Destroy(textFields[i * 4].gameObject);
                    UnityEngine.Object.Destroy(textFields[i * 4 + 1].gameObject);
                    UnityEngine.Object.Destroy(textFields[i * 4 + 2].gameObject);
                    UnityEngine.Object.Destroy(textFields[i * 4 + 3].gameObject);
                }
                Array.Resize(ref buttons, count);
                Array.Resize(ref textFields, count * 4);
            }
            float maxWidth0 = 0f;
            float maxWidth1 = 0f;
            float maxWidth2 = 0f;
            float maxWidth3 = 0f;
            for (var i = 0; i < count; i++)
            {
                var button = buttons[i];
                if (chk)
                {
                if (button == null)
                {
                    button = MyFlatButton.CreateFlatButton(0f, 20f * i + 24f, rtrans, "").WithSize(20f, 18f);
                    buttons[i] = button;
                    button.uiButton.data = i;
                    button.uiButton.onClick += data =>
                    {
                        if (data < 0 || data >= _topTenPlayerData.Length) return;
                        ref var playerData = ref _topTenPlayerData[data];
                        var seed = playerData.seedKey;
                        int combatValue = (int)(seed % 1000L);
                        int resourceMultiplier = (int)(seed / 1000L % 100L);
                        int starCount = (int)(seed / 100000L % 1000L);
                        int gameSeed = (int)(seed / 100000000L);
                        var uiMilkyWaySearchPanel = UIRoot.instance.uiMilkyWay.uiSearchPanel;
                        uiMilkyWaySearchPanel.selectSeed = gameSeed;
                        uiMilkyWaySearchPanel.selectStarCnt = starCount;
                        uiMilkyWaySearchPanel.selectResMulti = resourceMultiplier;
                        if (combatValue / 100 > 0)
                        {
                            uiMilkyWaySearchPanel.selectMode = 1;
                            uiMilkyWaySearchPanel.selectCombatDiff = combatValue % 100;
                        }
                        else
                        {
                            uiMilkyWaySearchPanel.selectMode = 0;
                            uiMilkyWaySearchPanel.selectCombatDiff = 0;
                        }
                        uiMilkyWaySearchPanel.RefreshInputText();
                        uiMilkyWaySearchPanel.OnSearchButtonClick(0);
                    };
                    textFields[i * 4] = CreateTextField(rtrans);
                    textFields[i * 4].alignment = TextAnchor.MiddleRight;
                    textFields[i * 4 + 1] = CreateTextField(rtrans);
                    textFields[i * 4 + 2] = CreateTextField(rtrans);
                    textFields[i * 4 + 3] = CreateTextField(rtrans);
                    textFields[i * 4 + 3].alignment = TextAnchor.MiddleRight;
                }
                button.SetLabelText(">>");
                textFields[i * 4].text = (i + 1).ToString();
                textFields[i * 4 + 1].text = _topTenPlayerData[i].name;
                textFields[i * 4 + 2].text = SeedToString(_topTenPlayerData[i].seedKey);
                textFields[i * 4 + 3].text = String.Format("{0}W", ToKMG(_topTenPlayerData[i].genCap * 60L));
                maxWidth0 = Math.Max(maxWidth0, textFields[i * 4].preferredWidth);
                maxWidth1 = Math.Max(maxWidth1, textFields[i * 4 + 1].preferredWidth);
                maxWidth2 = Math.Max(maxWidth2, textFields[i * 4 + 2].preferredWidth);
                maxWidth3 = Math.Max(maxWidth3, textFields[i * 4 + 3].preferredWidth);
                button.gameObject.SetActive(true);
                textFields[i * 4].gameObject.SetActive(true);
                textFields[i * 4 + 1].gameObject.SetActive(true);
                textFields[i * 4 + 2].gameObject.SetActive(true);
                textFields[i * 4 + 3].gameObject.SetActive(true);
            }
            else
            {
                if (button != null)
                {
                    button.gameObject.SetActive(false);
                    textFields[i * 4].gameObject.SetActive(false);
                    textFields[i * 4 + 1].gameObject.SetActive(false);
                    textFields[i * 4 + 2].gameObject.SetActive(false);
                    textFields[i * 4 + 3].gameObject.SetActive(false);
                }
            }
            }
            if (chk)
            {
                for (var i = 0; i < count; i++)
                {
                    var y = 20f * i + 24f;
                    global::UXAssist.UI.Util.NormalizeRectWithTopLeft(textFields[i * 4].rectTransform, 24f + maxWidth0 + 5f, y);
                    global::UXAssist.UI.Util.NormalizeRectWithTopLeft(textFields[i * 4 + 1].rectTransform, 24f + maxWidth0 + 10f, y);
                    global::UXAssist.UI.Util.NormalizeRectWithTopLeft(textFields[i * 4 + 2].rectTransform, 24f + maxWidth0 + 10f + maxWidth1 + 5f, y);
                    global::UXAssist.UI.Util.NormalizeRectWithTopLeft(textFields[i * 4 + 3].rectTransform, 24f + maxWidth0 + 10f + maxWidth1 + 5f + maxWidth2 + 5f + maxWidth3, y);
                }
            }
            MilkyWayTopTenPlayersToggler.SetLabelText(chk ? "Hide top players".Translate() : "Show top players".Translate());

            string ToKMG(long value)
            {
                StringBuilderUtility.WriteKMG(_sb, 8, value, true);
                return _sb.ToString();
            }

            string SeedToString(long seed)
            {
                int combatValue = (int)(seed % 1000L);
                int resourceMultiplier = (int)(seed / 1000L % 100L);
                int starCount = (int)(seed / 100000L % 1000L);
                int gameSeed = (int)(seed / 100000000L);
                string text;
                if (combatValue / 100 > 0)
                {
                    text = String.Format("{0:D8}-{1}-Z{2}-{3:00}", gameSeed, starCount, resourceMultiplier, combatValue % 100);
                }
                else
                {
                    text = String.Format("{0:D8}-{1}-A{2}", gameSeed, starCount, resourceMultiplier);
                }
                return text;
            }
        }
    }
}
