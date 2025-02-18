using Godot;

public partial class BoxSpawner : Node3D
{
	[Export]
	PackedScene scene;
	[Export]
	public bool SpawnRandomScale = false;
	[Export]
	public Vector2 spawnRandomSize = new(0.5f, 1f);
	[Export]
	public float spawnInterval = 1f;

	private float scan_interval = 0;

	Root Main;
	

	public override void _Ready()
	{
		printDebug("_Ready", GetParent());
		
		Main = GetTree().Root.GetNode("Cena_1") as Root;
		printDebug("Main", Main);
		// GD.Print(Main.Get("Start"));
		

		if (Main != null)
		{
			// GD.Print("\n MAIN != NULL");
		
			Main.SimulationStarted += OnSimulationStarted;
			Main.SimulationEnded += OnSimulationEnded;
		}

		SetProcess(false);
	}

	public override void _ExitTree()
	{
		// GD.Print("\n _ExitTree() BoxSpawner");
		if (Main == null) return;
		// GD.Print("\n _ExitTree() BoxSpawner Main != null");

		Main.SimulationStarted -= OnSimulationStarted;
		Main.SimulationEnded -= OnSimulationEnded;
	}

	public override void _Process(double delta)
	{
		// GD.Print("\n _Process() BoxSpawner");
		if (Main == null) return;
		
		scan_interval += (float)delta;
		if (scan_interval > spawnInterval)
		{
			scan_interval = 0;
			SpawnBox();
		}
	}
	
	private void SpawnBox()
	{
		// GD.Print("\n SpawnBox() BoxSpawner");
		var box = (Box)scene.Instantiate();

		if (SpawnRandomScale)
		{
			var x = (float)GD.RandRange(spawnRandomSize.X, spawnRandomSize.Y);
			var y = (float)GD.RandRange(spawnRandomSize.X, spawnRandomSize.Y);
			var z = (float)GD.RandRange(spawnRandomSize.X, spawnRandomSize.Y);
			box.Scale = new Vector3(x, y, z);
		}

		AddChild(box, forceReadableName:true);
		box.SetNewOwner(Main);
		box.SetPhysicsProcess(true);
		box.Position = GlobalPosition;
	}
	
	void OnSimulationStarted()
	{
		// GD.Print("\n OnSimulationStarted() BoxSpawner");
		if (Main == null) return;
		
		SetProcess(true);
		scan_interval = spawnInterval;
	}
	
	void OnSimulationEnded()
	{
		// GD.Print("\n OnSimulationEnded() BoxSpawner");
		SetProcess(false);
	}
	
	void printDebug(string msg, object param)
	{
		string scriptFilePath = ((Script)GetScript()).ResourcePath;
		string scriptFileName = System.IO.Path.GetFileName(scriptFilePath);

		// GD.Print("\n" +msg + " " + scriptFilePath);
		// GD.Print(param);
	}
}
