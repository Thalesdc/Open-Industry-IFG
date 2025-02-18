using Godot;

[GlobalClass]
public partial class SideGuardGap : Resource
{
	public enum SideGuardGapSide
	{
		Left,
		Right,
		Both
	}

	private float _position = 0f;
	[Export(PropertyHint.None, "suffix:m")]
	public float Position {
		get => _position;
		set {
			bool hasChanged = _position != value;
			_position = value;
			if (hasChanged) {
				EmitChanged();
			}
		}
	}

	private float _width = 1f;
	[Export(PropertyHint.None, "suffix:m")]
	public float Width {
		get => _width;
		set {
			bool hasChanged = _width != value;
			_width = value;
			if (hasChanged) {
				EmitChanged();
			}
		}
	}

	private SideGuardGapSide _side = SideGuardGapSide.Left;
	[Export]
	public SideGuardGapSide Side {
		get => _side;
		set {
			bool hasChanged = _side != value;
			_side = value;
			if (hasChanged) {
				EmitChanged();
			}
		}
	}

	public SideGuardGap() : this(0f, 1f) {}

	public SideGuardGap(float position, float width)
	{
		Position = position;
		Width = width;
	}
}
