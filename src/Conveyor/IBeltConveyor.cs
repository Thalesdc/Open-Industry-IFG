using Godot;

public interface IBeltConveyor: IConveyor, IComms
{
	public enum ConvTexture
	{
		Standard,
		Arrow
	}

	Color BeltColor { get; set; }
	ConvTexture BeltTexture { get; set; }
}
