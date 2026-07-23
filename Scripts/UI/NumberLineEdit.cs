using Godot;
using System;
using System.Text.RegularExpressions;

[GlobalClass]
public partial class NumberLineEdit : LineEdit
{
	[Export] bool includePeriods = false;
	string oldText = "";
    public override void _Ready()
    {
        TextChanged += OnTextChanged;
    }
	public void OnTextChanged(string newText)
	{
		if (!Regex.IsMatch(newText, "^[0-9-]*$"))
		{
			Text = Text[..^1];
		}
		CaretColumn = Text.Length;
	}
}
