using Godot;
using System;
using System.Threading.Tasks;

public partial class CurvedRollerConveyor : Node3D, IRollerConveyor
{
	private bool enableComms;

	[Export]
	public bool EnableComms
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
	public string Tag { get => tag; set => tag = value; }
	[Export]
	private int updateRate = 100;
	public int UpdateRate { get => updateRate; set => updateRate = value; }
	[Export]
	public float Speed { get; set; } = -1.0f;

	enum Scales { Low, Mid, High }
	Scales currentScale = Scales.Mid;
	Scales CurrentScale
	{
		get
		{
			return currentScale;
		}
		set
		{
			if (value != currentScale)
			{
				currentScale = value;
				switch (currentScale)
				{
					case Scales.Low:
						rollersLow.Visible = true;
						rollersMid.Visible = false;
						rollersHigh.Visible = false;
						break;
					case Scales.Mid:
						rollersLow.Visible = false;
						rollersMid.Visible = true;
						rollersHigh.Visible = false;
						break;
					case Scales.High:
						rollersLow.Visible = false;
						rollersMid.Visible = true;
						rollersHigh.Visible = true;
						break;
				}
			}
		}
	}

	bool run = true;

	readonly Guid id = Guid.NewGuid();
	double scan_interval = 0;
	bool running = false;
	bool readSuccessful = false;

	MeshInstance3D meshInstance;
	Material metalMaterial;

	Node3D rollersLow;
	Node3D rollersMid;
	Node3D rollersHigh;

	Node3D ends;

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
		meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");
		meshInstance.Mesh = meshInstance.Mesh.Duplicate() as Mesh;
		metalMaterial = meshInstance.Mesh.SurfaceGetMaterial(0).Duplicate() as Material;
		meshInstance.Mesh.SurfaceSetMaterial(0, metalMaterial);

		rollersLow = GetNode<Node3D>("RollersLow");
		rollersMid = GetNode<Node3D>("RollersMid");
		rollersHigh = GetNode<Node3D>("RollersHigh");

		ends = GetNode<Node3D>("Ends");

		SetCurrentScale();

		SetRollersSpeed(rollersLow, Speed);
		SetRollersSpeed(rollersMid, Speed);
		SetRollersSpeed(rollersHigh, Speed);

		Main = GetTree().CurrentScene as Root;

		if (Main != null)
		{
			Main.SimulationStarted += OnSimulationStarted;
			Main.SimulationEnded += OnSimulationEnded;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Scale = new Vector3(Scale.X, 1, Scale.X);

		if (Scale.X > 0.5f)
		{
			if (metalMaterial != null && Speed != 0)
				((ShaderMaterial)metalMaterial).SetShaderParameter("Scale", Scale.X);
		}

		if (ends != null)
		{
			foreach(MeshInstance3D end in ends.GetChildren())
			{
				end.Scale = new Vector3(1 / Scale.X, 1, 1);
			}
		}

		if (running)
		{
			SetRollersSpeed(rollersLow, Speed);
			SetRollersSpeed(rollersMid, Speed);
			SetRollersSpeed(rollersHigh, Speed);

			if (enableComms && running && readSuccessful)
			{
				scan_interval += delta;
				if (scan_interval > (float)updateRate / 1000 && readSuccessful)
				{
					scan_interval = 0;
					Task.Run(ScanTag);
				}
			}
		}

		SetCurrentScale();
	}

	void SetCurrentScale()
	{
		if (Scale.X < 0.8f)
		{
			CurrentScale = Scales.Low;
		}
		else if(Scale.X >= 0.8f && Scale.X < 1.6f)
		{
			CurrentScale = Scales.Mid;
		}
		else
		{
			CurrentScale = Scales.High;
		}
	}

	void SetRollersSpeed(Node3D rollers, float speed)
	{
		if (rollers != null)
		{
			foreach(RollerCorner roller in rollers.GetChildren())
			{
				roller.SetSpeed(speed);
			}
		}
	}

	void OnSimulationStarted()
	{
		if (enableComms)
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
			Speed = await Main.ReadFloat("id");
		}
		catch
		{
			GD.PrintErr("Failure to read: " + tag + " in Node: " + Name);
			readSuccessful = false;
		}
	}
}
