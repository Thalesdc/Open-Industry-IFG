using Godot;

public interface IBeltConveyor: IConveyor
{
	public enum ConvTexture
	{
		Standard,
		Arrow
	}

	Color BeltColor { get; set; }
	ConvTexture BeltTexture { get; set; }
}
