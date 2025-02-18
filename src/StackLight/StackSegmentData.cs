using Godot;
using System;

[GlobalClass]
public partial class StackSegmentData : Resource
{
	[Signal] public delegate void TagChangedEventHandler(string value);
	[Signal] public delegate void ActiveChangedEventHandler(bool value);
	[Signal] public delegate void ColorChangedEventHandler(Color value);
	
	public bool enableComms = false;
	
	string tag = "";
	[Export] public string Tag
	{
		get
		{
			return tag;
		}
		set
		{
			tag = value;
			EmitSignal(SignalName.TagChanged, tag);
		}
	}
	
	bool active = false;
	[Export] public bool Active
	{
		get
		{
			return active;
		}
		set
		{
			active = value;
			EmitSignal(SignalName.ActiveChanged, active);
		}
	}
	
	Color segmentColor = new(0.0f, 1.0f, 0.0f, 0.5f);
	[Export] public Color SegmentColor
	{
		get
		{
			return segmentColor;
		}
		set
		{
			segmentColor = value;
			EmitSignal(SignalName.ColorChanged, segmentColor);
		}
	}

	public override void _ValidateProperty(Godot.Collections.Dictionary property)
	{
		string propertyName = property["name"].AsStringName();

		if (propertyName == PropertyName.Tag)
		{
			property["usage"] = (int)(enableComms ? PropertyUsageFlags.Default : PropertyUsageFlags.NoEditor);
		}
	}
}
