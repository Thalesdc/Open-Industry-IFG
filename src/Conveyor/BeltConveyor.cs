using Godot;
using System;
using System.Threading.Tasks;

public partial class BeltConveyor : Node3D, IBeltConveyor
{

	private bool isCommsConnected;
	private bool enableComms;
	private bool opc_da_connected;

	[Export]
	int updateRate = 100;
	public int UpdateRate { get => updateRate; set => updateRate = value; }

	Color beltColor = new Color(1, 1, 1, 1);
	[Export]
	public Color BeltColor
	{
		get
		{
			return beltColor;
		}
		set
		{
			beltColor = value;

			if (beltMaterial != null)
				((ShaderMaterial)beltMaterial).SetShaderParameter("ColorMix", beltColor);
			if (conveyorEnd1 != null)
				((ShaderMaterial)conveyorEnd1.beltMaterial).SetShaderParameter("ColorMix", beltColor);
			if (conveyorEnd2 != null)
				((ShaderMaterial)conveyorEnd2.beltMaterial).SetShaderParameter("ColorMix", beltColor);
		}
	}

	IBeltConveyor.ConvTexture beltTexture = IBeltConveyor.ConvTexture.Standard;
	[Export]
	public IBeltConveyor.ConvTexture BeltTexture
	{
		get
		{
			return beltTexture;
		}
		set
		{
			beltTexture = value;
			if (beltMaterial != null)
				((ShaderMaterial)beltMaterial).SetShaderParameter("BlackTextureOn", beltTexture == IBeltConveyor.ConvTexture.Standard);
			if (conveyorEnd1 != null)
				((ShaderMaterial)conveyorEnd1.beltMaterial).SetShaderParameter("BlackTextureOn", beltTexture == IBeltConveyor.ConvTexture.Standard);
			if (conveyorEnd2 != null)
				((ShaderMaterial)conveyorEnd2.beltMaterial).SetShaderParameter("BlackTextureOn", beltTexture == IBeltConveyor.ConvTexture.Standard);
		}
	}

	[Export]
	public float Speed { get; set; }

	private Vector3 origin;
	readonly Guid id = Guid.NewGuid();
	double scan_interval = 0;
	bool readSuccessful = false;

	RigidBody3D rb;
	MeshInstance3D mesh;
	Material beltMaterial;
	Material metalMaterial;

	bool running = false;
	public double beltPosition = 0.0;
	Vector3 boxSize;

	ConveyorEnd conveyorEnd1;
	ConveyorEnd conveyorEnd2;

	// TODO: Mudar para atributo do objeto no godot
	static int currentScene;
	string tagEsteira;

	public Root Main { get; set; }

	public override void _ValidateProperty(Godot.Collections.Dictionary property)
	{
		GD.Print("\n> [BeltConveyor.cs] [_ValidateProperty()]");
		string propertyName = property["name"].AsStringName();

		if (propertyName == PropertyName.updateRate)
		{
			property["usage"] = (int)(isCommsConnected ? PropertyUsageFlags.Default : PropertyUsageFlags.NoEditor);
		}
	}
	public override void _Ready()
	{
		GD.Print("\n> [BeltConveyor.cs] [_Ready()]");
		rb = GetNode<RigidBody3D>("RigidBody3D");

		mesh = GetNode<MeshInstance3D>("RigidBody3D/MeshInstance3D");
		mesh.Mesh = mesh.Mesh.Duplicate() as Mesh;
		beltMaterial = mesh.Mesh.SurfaceGetMaterial(0).Duplicate() as Material;
		metalMaterial = mesh.Mesh.SurfaceGetMaterial(1).Duplicate() as Material;
		mesh.Mesh.SurfaceSetMaterial(0, beltMaterial);
		mesh.Mesh.SurfaceSetMaterial(1, metalMaterial);
		mesh.Mesh.SurfaceSetMaterial(2, metalMaterial);

		conveyorEnd1 = GetNode<ConveyorEnd>("RigidBody3D/Ends/ConveyorEnd");
		conveyorEnd2 = GetNode<ConveyorEnd>("RigidBody3D/Ends/ConveyorEnd2");

		origin = rb.Position;

		((ShaderMaterial)beltMaterial).SetShaderParameter("BlackTextureOn", beltTexture == IBeltConveyor.ConvTexture.Standard);
		conveyorEnd1.beltMaterial.SetShaderParameter("BlackTextureOn", beltTexture == IBeltConveyor.ConvTexture.Standard);
		conveyorEnd2.beltMaterial.SetShaderParameter("BlackTextureOn", beltTexture == IBeltConveyor.ConvTexture.Standard);

		((ShaderMaterial)beltMaterial).SetShaderParameter("ColorMix", beltColor);
		conveyorEnd1.beltMaterial.SetShaderParameter("ColorMix", beltColor);
		conveyorEnd2.beltMaterial.SetShaderParameter("ColorMix", beltColor);

		Main = GetTree().CurrentScene as Root;
		currentScene = Main.currentScene;

		if (Main != null)
		{
			Main.SimulationStarted += OnSimulationStarted;
			Main.SimulationEnded += OnSimulationEnded;
		}
	}

	public override void _ExitTree()
	{
		GD.Print("\n> [BeltConveyor.cs] [_ExitTree()]");
		if (Main == null) return;

		Main.SimulationStarted -= OnSimulationStarted;
		Main.SimulationEnded -= OnSimulationEnded;
	}

	public override void _PhysicsProcess(double delta)
	{
		// GD.Print("\n> [BeltConveyor.cs] [_PhysicsProcess()]");
		if (Main == null) return;

		if (running)
		{
			var localLeft = rb.GlobalTransform.Basis.X.Normalized();
			var velocity = localLeft * Speed;
			rb.LinearVelocity = velocity;
			rb.Position = origin;

			beltPosition += Speed * delta;
			if (Speed != 0)
				((ShaderMaterial)beltMaterial).SetShaderParameter("BeltPosition", beltPosition * Mathf.Sign(Speed));
			if (beltPosition >= 1.0)
				beltPosition = 0.0;

			rb.Rotation = Vector3.Zero;
			rb.Scale = new Vector3(1, 1, 1);

			if (
				isCommsConnected &&
				readSuccessful &&
				tagEsteira != null &&
				tagEsteira != string.Empty
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

		Scale = new Vector3(Scale.X, 1, Scale.Z);

		if (beltMaterial != null && Speed != 0)
			((ShaderMaterial)beltMaterial).SetShaderParameter("Scale", Scale.X * Mathf.Sign(Speed));

		if (metalMaterial != null && Speed != 0)
			((ShaderMaterial)metalMaterial).SetShaderParameter("Scale", Scale.X);
	}

	void OnSimulationStarted()
	{
		GD.Print("\n> [BeltConveyor.cs] [OnSimulationStarted()]");

		var globalVariables = GetNodeOrNull("/root/GlobalVariables");
		isCommsConnected = (bool)globalVariables.Get("opc_da_connected");

		// Name is the Node proprety to get current Node name		
		tagEsteira = SceneComponents.GetComponentByKey(Name.ToString(), currentScene).Tag;

		if (Main == null) return;
		running = true;

		if (isCommsConnected)
		{
			readSuccessful = true;
		}
	}

	void OnSimulationEnded()
	{
		running = false;

		beltPosition = 0;
		((ShaderMaterial)beltMaterial).SetShaderParameter("BeltPosition", beltPosition);

		rb.Position = Vector3.Zero;
		rb.Rotation = Vector3.Zero;
		rb.LinearVelocity = Vector3.Zero;

		foreach (Node3D child in rb.GetChildren())
		{
			child.Position = Vector3.Zero;
			child.Rotation = Vector3.Zero;
		}
	}

	async Task ScanTag()
	{
		try
		{
			// GD.Print("\n> [BeltConveyor.cs] [ScanTag()]");
			Speed = await Main.ReadFloat(tagEsteira);
		}
		catch(Exception err)
		{
			GD.PrintErr($"\n> Failure to read: {tagEsteira} in Node: {Name}");
			GD.PrintErr(err);
			readSuccessful = false;
		}
	}
}
