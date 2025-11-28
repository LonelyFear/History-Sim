using Godot;
using System;
using System.Collections.Generic;

public partial class TabManager : TabBar
{
	List<Control> openTabs = new List<Control>();
	Panel panel;
	Panel tabBarBackground;
    public override void _Ready()
	{
		panel = GetNode<Panel>("Panel");
		tabBarBackground = GetNode<Panel>("TabBarBackground");
		ClearTabs();
		foreach (Node node in GetChildren())
		{
			if (node != panel && node != tabBarBackground)
            {
				OpenTab((Control)node);
            }
		}
		GD.Print("Readied Menu");
    }
	public void OpenTab(Control tab)
	{
		if (openTabs.Contains(tab)) return;
		if (tab.GetParent() != this)
        {
            AddChild(tab);
        }
		openTabs.Add(tab);
		AddTab(tab.Name);
	}
	public void CloseTab(Control tab)
	{
		CallDeferred("remove_tab", [openTabs.IndexOf(tab)]);
		//RemoveTab(openTabs.IndexOf(tab));
		openTabs.Remove(tab);
		tab.QueueFree();
	}
    public override void _Process(double delta)
    {
        for (int i = 0; i < openTabs.Count; i++)
        {
			Control tab = openTabs[i];
			SetTabTitle(i, tab.Name);
			tab.Visible = true;
			if (CurrentTab != i)
            {
				tab.Visible = false;
            }
        }
    }

}
