using Godot;
using System;
using System.Threading.Tasks;

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
	private bool isCommsConnected;
	bool readSuccessful = false;

	bool running = false;
	private float scan_interval = 0;
	private bool opc_da_connected;
	public float Speed { get; set; }
	int updateRate = 2000;
	bool canGenerateBox = true;
	public int UpdateRate { get => updateRate; set => updateRate = value; }

	Root Main;

	// TODO: Mudar para atributo do objeto no godot
	static int CENA = 1;
	string tagGeradorCaixa;

	public override void _Ready()
	{
		GD.Print("\n> [BoxSpawner.cs] [_Ready()]");

		Main = GetTree().Root.GetNode("Cena_1") as Root;


		if (Main != null)
		{
			Main.SimulationStarted += OnSimulationStarted;
			Main.SimulationEnded += OnSimulationEnded;
		}

		SetProcess(false);
	}

	public override void _ExitTree()
	{
		// GD.Print("\n> [BoxSpawner.cs] [_ExitTree()]");
		if (Main == null) return;
		Main.SimulationStarted -= OnSimulationStarted;
		Main.SimulationEnded -= OnSimulationEnded;
	}

	public override void _Process(double delta)
	{
		// GD.Print("\n> [BoxSpawner.cs] [_Process()]");
		if (Main == null) return;

		if (running)
		{
			scan_interval += (float)delta;

			if (scan_interval > spawnInterval)
			{
				scan_interval = 0;
				SpawnBox();

				if (isCommsConnected && readSuccessful)
				{
					// scan_interval += (float)delta;
					// if (scan_interval > (float)updateRate / 1000 && readSuccessful)
					if (readSuccessful)
					{
						Task.Run(ScanTag);
					}
				}
			}
		}
	}

	private void SpawnBox()
	{
		GD.Print("\n> [BoxSpawner.cs] [SpawnBox()]");
		GD.Print($"- canGenerateBox:{canGenerateBox}");
		if (canGenerateBox)
		{
			var box = (Box)scene.Instantiate();

			if (SpawnRandomScale)
			{
				var x = (float)GD.RandRange(spawnRandomSize.X, spawnRandomSize.Y);
				var y = (float)GD.RandRange(spawnRandomSize.X, spawnRandomSize.Y);
				var z = (float)GD.RandRange(spawnRandomSize.X, spawnRandomSize.Y);
				box.Scale = new Vector3(x, y, z);
			}

			AddChild(box, forceReadableName: true);
			box.SetNewOwner(Main);
			box.SetPhysicsProcess(true);
			box.Position = GlobalPosition;

		}
	}

	void OnSimulationStarted()
	{
		// GD.Print("\n> [BoxSpawner.cs] [OnSimulationStarted()]");

		if (Main == null) return;

		tagGeradorCaixa = ObjetosCena.ObterObjetoPorNome("GeradorCaixa", CENA).Tag;

		var globalVariables = GetNodeOrNull("/root/GlobalVariables");
		isCommsConnected = (bool)globalVariables.Get("opc_da_connected");

		SetProcess(true);
		running = true;
		readSuccessful = true;
	}

	void OnSimulationEnded()
	{
		// GD.Print("\n> [BoxSpawner.cs] [OnSimulationEnded()]");
		readSuccessful = false;
		SetProcess(false);
	}

	async Task ScanTag()
	{
		try
		{
			// GD.Print("\n> [BeltConveyor.cs] [ScanTag()]");
			bool isActive = await Main.LerBooleano(tagGeradorCaixa);
			if (isActive == false)
			{
				canGenerateBox = isActive;
			}
			else
			{
				canGenerateBox = true;
			}
		}
		catch (Exception err)
		{
			GD.PrintErr($"\n> Failure to read: {tagGeradorCaixa}");
			GD.PrintErr(err);
			readSuccessful = false;
		}
	}
}
