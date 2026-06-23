using System.Collections.Generic;
using UnityEngine;

namespace UXAssist.UI;

public class ConfigTabGroup
{
    public class Tab
    {
        public RectTransform RectTransform { get; set; }
        public UIButton Button { get; set; }
    }

    public class Group
    {
        public string Label { get; set; }
        public List<Tab> Tabs { get; } = [];
    }

    private readonly List<Group> _groups = [];
    private int _currentTabIndex = -1;

    public IReadOnlyList<Group> Groups => _groups;

    public int CurrentTabIndex => _currentTabIndex;

    public int TabCount
    {
        get
        {
            var count = 0;
            foreach (var group in _groups)
            {
                count += group.Tabs.Count;
            }

            return count;
        }
    }

    public void AddGroup(string label)
    {
        _groups.Add(new Group { Label = label });
    }

    public int AddTab(RectTransform rectTransform, UIButton button)
    {
        if (_groups.Count == 0)
        {
            AddGroup(string.Empty);
        }

        var group = _groups[_groups.Count - 1];
        group.Tabs.Add(new Tab { RectTransform = rectTransform, Button = button });
        return TabCount - 1;
    }

    public void SetCurrentTab(int index)
    {
        _currentTabIndex = index;
        var current = 0;
        foreach (var group in _groups)
        {
            foreach (var tab in group.Tabs)
            {
                if (current != index)
                {
                    tab.Button.highlighted = false;
                    tab.RectTransform.gameObject.SetActive(false);
                }
                else
                {
                    tab.Button.highlighted = true;
                    tab.RectTransform.gameObject.SetActive(true);
                }

                current++;
            }
        }
    }
}
