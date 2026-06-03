using Godot;
using System;

public partial class ContinentsLabel : Label
{
	[Export] Slider slider;
	int numberIndex = 0;

    public override void _Ready()
    {
        if (slider == null)
		{
			GD.PushError("ERROR SLIDER NOT FOUND");
			return;
		}

		foreach (char c in Text)
		{
			if (c == '#')
			{
				numberIndex = Text.IndexOf(c);
			}
		}

		slider.ValueChanged += OnSliderChanged;
		OnSliderChanged(slider.Value);
    }

	public void OnSliderChanged(double value)
    {
		char[] textChars = Text.ToCharArray();
		textChars[numberIndex] = value.ToString()[0];
        Text = new string(textChars);
	}
}
