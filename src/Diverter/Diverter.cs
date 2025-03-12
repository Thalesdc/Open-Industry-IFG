using Godot;
using System;
using System.Threading.Tasks;

public partial class Diverter : Node3D
{
	private bool isCommsConnected;

	[Export]
	private int updateRate = 300;
	bool fireDivert = false;
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
	DiverterAnimator diverterAnimator;
	Root Main;
	string tagDiverter;
	public override void _Ready()
	{
		diverterAnimator = GetNode<DiverterAnimator>("DiverterAnimator");

		Main = GetTree().CurrentScene as Root;

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
			fireDivert = false;
			return;
		}

		if (fireDivert && !previousFireDivertState)
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

		previousFireDivertState = fireDivert;

		if (
			isCommsConnected &&
			running &&
			readSuccessful &&
			tagDiverter != null &&
			tagDiverter != string.Empty
		)
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
		tagDiverter = SceneComponents.GetComponentByKey(Name, Main.currentScene).Tag;

		var globalVariables = GetNodeOrNull("/root/GlobalVariables");
		isCommsConnected = (bool)globalVariables.Get("opc_da_connected");

		running = true;
		if (isCommsConnected)
		{
			readSuccessful = true;
		}

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
			fireDivert = await Main.ReadBool(tagDiverter);
		}
		catch
		{
			GD.PrintErr("Failure to read: " + tagDiverter + " in Node: " + Name);
			readSuccessful = false;
		}
	}
}
