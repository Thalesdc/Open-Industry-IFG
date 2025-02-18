using Godot;
using System;
using System.Threading.Tasks;

public partial class LaserSensor : Node3D
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
	string tag;
	[Export]
	private int updateRate = 100;
	[Export]
	float distance = 10.0f;
	int value = 0;
	
	[Export]
	Color collisionColor;
	[Export]
	Color scanColor;

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
		GD.Print("\n> [LaserSensor.cs] [_Ready()]");
		// Main = GetParent().GetTree().EditedSceneRoot as Root;
		// TODO: Deixar de forma dinâmica
		Main = GetTree().Root.GetNode("Cena_1") as Root;
		GD.Print($"\n- Main: {Main}");

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

    // public override void _ExitTree()
    // {
    //     if (Main == null) return;

    //     Main.SimulationStarted -= OnSimulationStarted;
    //     Main.SimulationEnded -= OnSimulationEnded;
    // }

    // public override void _PhysicsProcess(double delta)
	// {
	// 	PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
	// 	PhysicsRayQueryParameters3D query = PhysicsRayQueryParameters3D.Create(rayMarker.GlobalPosition, rayMarker.GlobalPosition + GlobalTransform.Basis.Z * distance);
	// 	var result = spaceState.IntersectRay(query);
		
	// 	if (result.Count > 0)
	// 	{
	// 		cylinderMesh.Height = rayMarker.GlobalPosition.DistanceTo((Vector3)result["position"]);
	// 		rayMaterial.AlbedoColor = collisionColor;
	// 		distanceToTarget = cylinderMesh.Height;
	// 	}
	// 	else
	// 	{
	// 		cylinderMesh.Height = distance;
	// 		rayMaterial.AlbedoColor = scanColor;
	// 		distanceToTarget = distance;
	// 	}
	// 	rayMesh.Position = new Vector3(0, 0, cylinderMesh.Height * 0.5f);

	// 	if (enableComms && running && readSuccessful)
	// 	{
	// 		scan_interval += delta;
	// 		if (scan_interval > (float)updateRate / 1000 && readSuccessful)
	// 		{
	// 			scan_interval = 0;
	// 			Task.Run(ScanTag);
	// 		}
	// 	}
	// }

	void OnSimulationStarted()
	{
		running = true;
		if(enableComms)
		{
			Main.Connect(id, Root.DataType.Float, tag);
		}
		readSuccessful = true;
	}

	void OnSimulationEnded()
	{
		running = false;
		cylinderMesh.Height = distance;
		rayMaterial.AlbedoColor = scanColor;
		rayMesh.Position = new Vector3(0, 0, cylinderMesh.Height * 0.5f);
	}

	async Task ScanTag()
	{
		try
		{
			await Main.Write(id, distanceToTarget);
		}
		catch
		{
			GD.PrintErr("Failure to write: " + tag + " in Node: " + Name);
		}
	}
}
