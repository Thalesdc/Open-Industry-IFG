using Godot;
using System;
using System.Threading.Tasks;

public partial class Diverter : Node3D
{
	private bool enableComms;

	[Export]
	private bool EnableComms
	{
		get => enableComms;
		set
		{
			enableComms = value;
			NotifyPropertyListChanged();
		}
	}
	[Export]
	public string tag;
	[Export]
	private int updateRate = 100;
	[Export]
	bool FireDivert = false;
	[Export]
	float divertTime = 0.5f;
	[Export]
	float divertDistance = 1.0f;

	bool keyHeld = false;
	bool keyPressed = false;

	bool readSuccessful = false;
	bool running = false;
	double scan_interval = 0;

	bool cycled = false;
	bool divert = false;
	private bool previousFireDivertState = false;

	readonly Guid id = Guid.NewGuid();
	DiverterAnimator diverterAnimator;
	Root Main;

	public override void _ValidateProperty(Godot.Collections.Dictionary property)
	{
		string propertyName = property["name"].AsStringName();

		if (propertyName == PropertyName.updateRate || propertyName == PropertyName.tag)
		{
			property["usage"] = (int)(EnableComms ? PropertyUsageFlags.Default : PropertyUsageFlags.NoEditor);
		}
	}

	public override void _Ready()
	{
		diverterAnimator = GetNode<DiverterAnimator>("DiverterAnimator");

		Main = GetParent().GetTree().EditedSceneRoot as Root;

		if (Main != null)
		{
			Main.SimulationStarted += OnSimulationStarted;
			Main.SimulationEnded += OnSimulationEnded;
		}
	}

    public override void _ExitTree()
    {
        if (Main == null) return;

        Main.SimulationStarted -= OnSimulationStarted;
        Main.SimulationEnded -= OnSimulationEnded;
    }
    public override void _PhysicsProcess(double delta)
	{
		if (!running)
		{
			FireDivert = false;
			return;
		}

		// if (Main != null)
		// {
		// 	if (Main.selectedNodes != null)
		// 	{
		// 		bool selected = Main.selectedNodes.Contains(this);

		// 		if (selected && Input.IsPhysicalKeyPressed(Key.G))
		// 		{
		// 			keyPressed = true;
		// 			FireDivert = true;
		// 		}
		// 	}
		// }

		if (!Input.IsPhysicalKeyPressed(Key.G))
		{
			keyHeld = false;
			if (keyPressed)
			{
				keyPressed = false;
				FireDivert = false;
			}

			if(FireDivert && !enableComms)
			{
				Task.Delay(updateRate * 3).ContinueWith(t => FireDivert = false);
			}
		}

		if (FireDivert && !previousFireDivertState)
		{
			divert = true;
			cycled = false;  
		}

		if (divert && !cycled)
		{
			diverterAnimator.Fire(divertTime, divertDistance);
			divert = false;
			cycled = true;  
		}

		previousFireDivertState = FireDivert;

		if (enableComms && readSuccessful)
		{
			scan_interval += delta;
			if (scan_interval > (float)updateRate / 1000 && readSuccessful)
			{
				scan_interval = 0;
				Task.Run(ScanTag);
			}
		}
	}
	void OnSimulationStarted()
	{
		if (Main == null) return;

		running = true;

		if (enableComms)
		{
			Main.Connect(id, Root.DataType.Bool, tag);
		}

		readSuccessful = true;
	}

	void OnSimulationEnded()
	{
		running = false;
		diverterAnimator.Disable();
	}

	async Task ScanTag()
	{
		try
		{
			FireDivert = await Main.ReadBool(id);
		}
		catch
		{
			GD.PrintErr("Failure to read: " + tag + " in Node: " + Name);
			readSuccessful = false;
		}
	}
}
