using Godot;
using System;
using System.Threading.Tasks;

public partial class BladeStop : Node3D
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
	
	readonly Guid id = Guid.NewGuid();
	double scan_interval = 0;
	bool readSuccessful = false;
	
	bool active = false;
	[Export] bool Active
	{
		get
		{
			return active;
		}
		set
		{
			active = value;
		}
	}
	float activePos = 0.24f;
	
	float airPressureHeight = 0.0f;
	[Export] float AirPressureHeight
	{
		get
		{
			return airPressureHeight;
		}
		set
		{
			airPressureHeight = value;
			if (blade != null && airPressureR != null && airPressureL != null)
			{
				blade.Position = new Vector3(blade.Position.X, Active ? airPressureHeight + activePos : airPressureHeight, blade.Position.Z);
				airPressureR.Position = new Vector3(airPressureR.Position.X, airPressureHeight, airPressureR.Position.Z);
				airPressureL.Position = new Vector3(airPressureL.Position.X, airPressureHeight, airPressureL.Position.Z);
			}
		}
	}
	
	StaticBody3D blade;
	MeshInstance3D airPressureR;
	MeshInstance3D airPressureL;
	MeshInstance3D bladeCornerR;
	MeshInstance3D bladeCornerL;
	Node3D corners;
	
	bool keyHeld = false;
	bool keyPressed = false;
	bool running = false;
	
	Root main;
	public Root Main { get; set; }
	
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
		Main = GetTree().CurrentScene as Root;
		if (Main != null)
		{
			Main.SimulationStarted += OnSimulationStarted;
			Main.SimulationEnded += OnSimulationEnded;
		}
		
		blade = GetNode<StaticBody3D>("Blade");
		airPressureR = GetNode<MeshInstance3D>("Corners/AirPressureR");
		airPressureL = GetNode<MeshInstance3D>("Corners/AirPressureL");
		bladeCornerR = GetNode<MeshInstance3D>("Corners/AirPressureR/BladeCornerR");
		bladeCornerL = GetNode<MeshInstance3D>("Corners/AirPressureL/BladeCornerL");
		corners = GetNode<Node3D>("Corners");
		
		blade.Position = new Vector3(blade.Position.X, airPressureHeight, blade.Position.Z);
		airPressureR.Position = new Vector3(airPressureR.Position.X, airPressureHeight, airPressureR.Position.Z);
		airPressureL.Position = new Vector3(airPressureL.Position.X, airPressureHeight, airPressureL.Position.Z);
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
			Active = false;
			return;
		}

		if (running)
		{
			// if (Main != null)
			// {
			// 	if(Main.selectedNodes != null)
			// 	{
			// 		bool selected = Main.selectedNodes.Contains(this);
			// 		if (selected && Input.IsPhysicalKeyPressed(Key.G))
			// 		{
			// 			keyPressed = true;
			// 			if (!keyHeld)
			// 			{
			// 				keyHeld = true;
			// 				Active = !Active;
			// 			}
			// 		}
			// 	}
			// }

			if (blade != null && bladeCornerR != null && bladeCornerL != null)
			{
				if (active) Up();
				else Down();
			}

			if (!Input.IsPhysicalKeyPressed(Key.G))
			{
				keyHeld = false;
				if (keyPressed)
				{
					keyPressed = false;
				}
			}
			
			if (EnableComms && readSuccessful)
			{
				scan_interval += delta;
				if (scan_interval > (float)updateRate/1000 && readSuccessful)
				{
					scan_interval = 0;
					Task.Run(ScanTag);
				}
			}
		}
		
		Scale = new Vector3(1, 1, Scale.Z);
		foreach(Node3D child in corners.GetChildren())
		{
			child.Scale = new Vector3(1, 1, 1 / Scale.Z);
		}
	}
	
	void Up()
	{
		Tween tween = GetTree().CreateTween().SetEase(0).SetParallel(); // Set EaseIn
		tween.TweenProperty(blade, "position", new Vector3(blade.Position.X, airPressureHeight + activePos, blade.Position.Z), 0.15f);
		tween.TweenProperty(bladeCornerR, "position", new Vector3(bladeCornerR.Position.X, activePos, bladeCornerR.Position.Z), 0.15f);
		tween.TweenProperty(bladeCornerL, "position", new Vector3(bladeCornerL.Position.X, activePos, bladeCornerL.Position.Z), 0.15f);
	}
	
	void Down()
	{
		Tween tween = GetTree().CreateTween().SetEase(0).SetParallel(); // Set EaseIn
		tween.TweenProperty(blade, "position", new Vector3(blade.Position.X, airPressureHeight, blade.Position.Z), 0.15f);
		tween.TweenProperty(bladeCornerR, "position", new Vector3(bladeCornerR.Position.X, 0, bladeCornerR.Position.Z), 0.15f);
		tween.TweenProperty(bladeCornerL, "position", new Vector3(bladeCornerL.Position.X, 0, bladeCornerL.Position.Z), 0.15f);
	}
	
	void OnSimulationStarted()
	{
		if (Main == null) return;
		if (EnableComms)
		{
			readSuccessful = true;
		}
		
		running = true;
	}

	void OnSimulationEnded()
	{
		running = false;
	}
	
	async Task ScanTag()
	{
		try
		{
			Active = await Main.ReadBool("id");
		}
		catch
		{
			GD.PrintErr("Failure to read: " + tag + " in Node: " + Name);
			readSuccessful = false;
		}
	}
}
