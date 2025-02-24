using Godot;
using System;
using System.Threading.Tasks;

public partial class DiffuseSensor : Node3D
{
	[Export]
	float distance = 6.0f;
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
	[Export]
	Color collisionColor;
	[Export]
	private int updateRate = 100;
	Color scanColor;
	private bool isCommsConnected;
	string tagDiffuseSensor;

	double scan_interval = 0;
	bool readSuccessful = false;
	bool running = false;

	bool blocked = false;

	bool debugBeam = true;


	Marker3D rayMarker;
	MeshInstance3D rayMesh;
	CylinderMesh cylinderMesh;
	StandardMaterial3D rayMaterial;

	Root Main;
	public override void _Ready()
	{
		GD.Print("\n> [DiffuseSensor.cs] [_Ready()]");
		rayMarker = GetNode<Marker3D>("RayMarker");
		rayMesh = GetNode<MeshInstance3D>("RayMarker/MeshInstance3D");
		cylinderMesh = rayMesh.Mesh.Duplicate() as CylinderMesh;
		rayMesh.Mesh = cylinderMesh;
		rayMaterial = cylinderMesh.Material.Duplicate() as StandardMaterial3D;
		cylinderMesh.Material = rayMaterial;

		Main = GetTree().CurrentScene as Root;

		if (Main != null)
		{
			Main.SimulationStarted += OnSimulationStarted;
			Main.SimulationEnded += OnSimulationEnded;
		}

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
		query.CollisionMask = 8;
		var result = spaceState.IntersectRay(query);

		if (result.Count > 0)
		{
			blocked = true;
			float resultDistance = rayMarker.GlobalPosition.DistanceTo((Vector3)result["position"]);
			if (cylinderMesh.Height != resultDistance)
				cylinderMesh.Height = resultDistance;
			if (rayMaterial.AlbedoColor != collisionColor)
				rayMaterial.AlbedoColor = collisionColor;
		}
		else
		{
			blocked = false;
			if (cylinderMesh.Height != distance)
				cylinderMesh.Height = distance;
			if (rayMaterial.AlbedoColor != scanColor)
				rayMaterial.AlbedoColor = scanColor;
		}

		if (
			isCommsConnected &&
			running &&
			readSuccessful &&
			tagDiffuseSensor != null &&
			tagDiffuseSensor != ""
		)
		{
			Task.Run(WriteTag);
			// scan_interval += delta;
			// if (
			// 	scan_interval > (float)updateRate / 1000 &&
			// 	readSuccessful &&
			// 	tagDiffuseSensor != null &&
			// 	tagDiffuseSensor != ""
			// )
			// {
			// 	scan_interval = 0;
			// 	Task.Run(WriteTag);
			// }
		}


		rayMesh.Position = new Vector3(0, 0, cylinderMesh.Height * 0.5f);
	}

	void OnSimulationStarted()
	{
		GD.Print("\n> [DiffuseSensor.cs] [OnSimulationStarted()]");
		tagDiffuseSensor = SceneComponents.GetComponentByName(Name, Main.currentScene).Tag;

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
		cylinderMesh.Height = distance;
		rayMaterial.AlbedoColor = scanColor;
		rayMesh.Position = new Vector3(0, 0, cylinderMesh.Height * 0.5f);
	}

	async Task WriteTag()
	{
		try
		{
			await Main.Write(tagDiffuseSensor, blocked);
		}
		catch
		{
			GD.PrintErr("Failure to write: " + tagDiffuseSensor + " in Node: " + Name);
			readSuccessful = false;
		}
	}
}
