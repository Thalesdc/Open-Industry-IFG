using Godot;

public partial class Despawner : Node3D
{
	Area3D area;
	Root Main;

	private Vector3 origin;
	bool running = false;
	public override void _Ready()
	{
		area = GetNode<Area3D>("Area3D");

		origin = area.Position;

		// Main = GetParent().GetTree().EditedSceneRoot as Root;
		Main = GetTree().Root.GetNode("Cena_1") as Root;

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

	public override void _Process(double delta)
	{
		if (Main == null) return;

		if (running)
		{
			area.Position = origin;
			area.Rotation = Vector3.Zero;
			area.Scale = new Vector3(1, 1, 1);

			if (area.GetOverlappingBodies().Count > 0)
			{
				foreach (var body in area.GetOverlappingBodies())
				{
					var owner = body.GetParent();
					if (owner is Box box && box.instanced)
					{
						box.QueueFree();
					}
				}
			}
		}
	}

	private void OnSimulationStarted()
	{
		running = true;
	}

	private void OnSimulationEnded()
	{
		running = false;
	}
}
