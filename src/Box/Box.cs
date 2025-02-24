using Godot;

public partial class Box : Node3D
{	
	RigidBody3D rigidBody;
	Vector3 initialPos;
	public bool instanced = false;
	// bool selected = false;
	bool keyHeld = false;

	Root Main;
	public override void _Ready()
	{
		// GD.Print("\n> [Box.cs] [_Ready()]");
		Main = GetTree().CurrentScene as Root;
		// GD.Print($"- Main:{Main}");

		if (Main == null)
		{
			return;
		}

		Main.SimulationStarted += Set;
		Main.SimulationEnded += Reset;
		Main.SimulationSetPaused += OnSetPaused;

		rigidBody = GetNode<RigidBody3D>("RigidBody3D");

		SetPhysicsProcess(false);
	}
	
	public override void _EnterTree()
	{
		// GD.Print("\n" + "_EnterTree() Box.cs");
		if (Main != null && !instanced)
		{
			Main.SimulationStarted += Set;
			Main.SimulationEnded += Reset;
			Main.SimulationSetPaused += OnSetPaused;
		}
	}
	
	public override void _ExitTree()
	{
		// GD.Print("\n" + "_ExitTree() Box.cs");
		if(Main == null) return;

		Main.SimulationStarted -= Set;
		Main.SimulationEnded -= Reset;
		Main.SimulationSetPaused -= OnSetPaused;
		
		if (instanced) QueueFree();
	}

	public override void _PhysicsProcess(double delta)
	{
		// GD.Print("\n> [Box.cs] [_PhysicsProcess()]");
		if (Main == null) return;

		// GD.Print($"- Main.selectedNodes:{Main.selectedNodes}");
		// selected = Main.selectedNodes.Contains(this);

		// if (selected && Input.IsPhysicalKeyPressed(Key.G) && !Main.paused)
		// {
		// 	if (!keyHeld)
		// 	{
		// 		keyHeld = true;
		// 		rigidBody.Freeze = !rigidBody.Freeze;
		// 	}
		// }

		if (!Input.IsPhysicalKeyPressed(Key.G))
		{
			keyHeld = false;
		}

		if (rigidBody.Freeze)
		{
			rigidBody.TopLevel = false;
			rigidBody.Position = Vector3.Zero;
			rigidBody.Rotation = Vector3.Zero;
			rigidBody.Scale = Vector3.One;
		}
		else
		{
			rigidBody.TopLevel = true;
			Position = rigidBody.Position;
			Rotation = rigidBody.Rotation;
			Scale = rigidBody.Scale;
		}
	}
	
	void Set()
	{
		if (Main == null) return;
		
		initialPos = GlobalPosition;
		rigidBody.TopLevel = true;
		rigidBody.Freeze = false;
		SetPhysicsProcess(true);
	}
	
	void Reset()
	{
		if (instanced)
		{
			Main.SimulationStarted -= Set;
			Main.SimulationEnded -= Reset;
			Main.SimulationSetPaused -= OnSetPaused;
			QueueFree();
		}
		else
		{
			SetPhysicsProcess(false);
			rigidBody.TopLevel = false;
			
			rigidBody.Position = Vector3.Zero;
			rigidBody.Rotation = Vector3.Zero;
			rigidBody.Scale = Vector3.One;
			
			rigidBody.LinearVelocity = Vector3.Zero;
			rigidBody.AngularVelocity = Vector3.Zero;
			
			GlobalPosition = initialPos;
			Rotation = Vector3.Zero;
		}
	}
	
	void OnSetPaused(bool paused)
	{
		rigidBody.Freeze = paused;
	}
	
	public void SetNewOwner(Root newOwner)
	{
		instanced = true;
		Owner = newOwner;
	}
}
