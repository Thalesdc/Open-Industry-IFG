using Godot;
using System;
using System.Threading.Tasks;

public partial class LaserSensor : Node3D
{
	private int updateRate = 1000;
	[Export]
	float distance = 10.0f;
	[Export]
	Color collisionColor;
	[Export]
	Color scanColor;

	private bool isCommsConnected;
	string tagSensor;
	int value = 0;
	readonly Guid id = Guid.NewGuid();
	double scan_interval = 0;
	bool readSuccessful = false;
	bool running = false;
	bool debugBeam = true;
	float distanceToTarget = 0.0f;

	[Export]
	bool DebugBeam
	{
		get
		{
			return debugBeam;
		}
		set
		{
			debugBeam = value;
			if (rayMarker != null)
				rayMarker.Visible = value;
		}
	}

	Marker3D rayMarker;
	MeshInstance3D rayMesh;
	CylinderMesh cylinderMesh;
	StandardMaterial3D rayMaterial;

	Root Main;
	public override void _Ready()
	{
		GD.Print("\n> [LaserSensor.cs] [_Ready()]");
		Main = GetTree().CurrentScene as Root;

		if (Main != null)
		{
			Main.SimulationStarted += OnSimulationStarted;
			Main.SimulationEnded += OnSimulationEnded;
		}

		rayMarker = GetNode<Marker3D>("RayMarker");
		rayMesh = GetNode<MeshInstance3D>("RayMarker/MeshInstance3D");
		cylinderMesh = rayMesh.Mesh.Duplicate() as CylinderMesh;
		rayMesh.Mesh = cylinderMesh;
		rayMaterial = cylinderMesh.Material.Duplicate() as StandardMaterial3D;
		cylinderMesh.Material = rayMaterial;

		rayMarker.Visible = debugBeam;
	}

	public override void _ExitTree()
	{
		if (Main == null) return;

		Main.SimulationStarted -= OnSimulationStarted;
		Main.SimulationEnded -= OnSimulationEnded;
	}

	public override void _PhysicsProcess(double delta)
	{
		PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
		PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayMarker.GlobalPosition, rayMarker.GlobalPosition + GlobalTransform.Basis.Z * distance);
		var result = spaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			cylinderMesh.Height = rayMarker.GlobalPosition.DistanceTo((Vector3)result["position"]);
			rayMaterial.AlbedoColor = collisionColor;
			distanceToTarget = cylinderMesh.Height;
		}
		else
		{
			cylinderMesh.Height = distance;
			rayMaterial.AlbedoColor = scanColor;
			distanceToTarget = distance;
		}
		rayMesh.Position = new Vector3(0, 0, cylinderMesh.Height * 0.5f);

		if (
			isCommsConnected &&
			running &&
			readSuccessful &&
			tagSensor != null &&
			tagSensor != ""
		)
		{
			Task.Run(WriteTag);
		}
	}

	void OnSimulationStarted()
	{
		try
		{
			GD.Print("\n> [LaserSensor.cs] [OnSimulationStarted()]");
			tagSensor = SceneComponents.GetComponentByName(Name, Main.currentScene).Tag;
			GD.Print($"- tagSensor: {tagSensor}");
			running = true;

			var globalVariables = GetNodeOrNull("/root/GlobalVariables");
			isCommsConnected = (bool)globalVariables.Get("opc_da_connected");
			GD.Print($"- isCommsConnected: {isCommsConnected}");
			if (isCommsConnected)
			{
				readSuccessful = true;
			}

			GD.Print($"- isCommsConnected: {isCommsConnected}");
			GD.Print($"- running: {running}");
			GD.Print($"- readSuccessful: {readSuccessful}");
		}
		catch (Exception err)
		{
			GD.PrintErr("\n> [LaserSensor.cs] Failure OnSimulationStarted");
			GD.PrintErr(err);
			readSuccessful = false;
		}
	}

	void OnSimulationEnded()
	{
		running = false;
		cylinderMesh.Height = distance;
		rayMaterial.AlbedoColor = scanColor;
		rayMesh.Position = new Vector3(0, 0, cylinderMesh.Height * 0.5f);
	}

	async Task WriteTag()
	{
		try
		{
			await Main.Write(tagSensor, distanceToTarget);
		}
		catch
		{
			GD.PrintErr("Failure to write: " + tagSensor + " in Node: " + Name);
			readSuccessful = false;
		}
	}
}
