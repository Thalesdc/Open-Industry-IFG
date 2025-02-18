using Godot;
using System;
using System.Threading.Tasks;

[GlobalClass]
public partial class StackSegment : Node3D
{
	private bool enableComms;

	[Export]
	public bool EnableComms
	{
		get => enableComms;
		set
		{
			enableComms = value;
			segmentData.enableComms = enableComms;
			NotifyPropertyListChanged();
		}
	}
	public int updateRate = 100;

	bool readSuccessful = false;
	bool running = false;
	double scan_interval = 0;

	readonly Guid segmentId = Guid.NewGuid();
	
	Root main;
	public Root Main { get; set; }
	
	public string tag = "";
	StackSegmentData segmentData;
	[Export]
	public Resource SegmentData
	{
		get
		{
			return segmentData;
		}
		set
		{
			if (segmentData != null)
			{
				segmentData.TagChanged -= SetTag;
				segmentData.ActiveChanged -= SetActive;
				segmentData.ColorChanged -= SetSegmentColor;
			}

			segmentData = value as StackSegmentData;
			
			if (material != null && segmentData != null)
			{
				SetActive(segmentData.Active);
				SetSegmentColor(segmentData.SegmentColor);
			}
			
			segmentData.TagChanged += SetTag;
			segmentData.ActiveChanged += SetActive;
			segmentData.ColorChanged += SetSegmentColor;
		}
	}
	
	MeshInstance3D meshInstance;
	StandardMaterial3D material;
	public override void _Ready()
	{
		Main = GetParent().GetTree().EditedSceneRoot as Root;
		
		if (Main != null)
		{
			Main.SimulationStarted += OnSimulationStarted;
			Main.SimulationEnded += OnSimulationEnded;
		}
		
		meshInstance = GetNode<MeshInstance3D>("LightMesh");
		meshInstance.Mesh = meshInstance.Mesh.Duplicate() as Mesh;
		
		material = meshInstance.Mesh.SurfaceGetMaterial(0).Duplicate() as StandardMaterial3D;
		meshInstance.Mesh.SurfaceSetMaterial(0, material);
		
		if (segmentData != null)
		{
			segmentData.TagChanged -= SetTag;
			segmentData.ActiveChanged -= SetActive;
			segmentData.ColorChanged -= SetSegmentColor;
		}
		
		segmentData.enableComms = enableComms;
		SetActive(segmentData.Active);
		SetSegmentColor(segmentData.SegmentColor);
		
		segmentData.TagChanged += SetTag;
		segmentData.ActiveChanged += SetActive;
		segmentData.ColorChanged += SetSegmentColor;
	}
	
	public override void _PhysicsProcess(double delta)
	{
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
	
	async Task ScanTag()
	{
		if (segmentData.Tag != string.Empty)
		{
			segmentData.Active = await Main.ReadBool(segmentId);
		}
	}
	
	void SetTag(string newValue)
	{
		tag = newValue;
	}
	
	void SetActive(bool newValue)
	{
		if (material == null) return;
		if (newValue)
		{
			material.EmissionEnergyMultiplier = 1.0f;
		}
		else
		{
			material.EmissionEnergyMultiplier = 0.0f;
		}
	}
	
	void SetSegmentColor(Color newValue)
	{
		if (material != null)
		{
			material.AlbedoColor = newValue;
			material.Emission = newValue;
		}
	}
	
	void OnSimulationStarted()
	{
		if (enableComms)
		{
			Main.Connect(segmentId, Root.DataType.Bool, segmentData.Tag);
		}
		
		running = true;
		readSuccessful = true;
	}
	
	void OnSimulationEnded()
	{
		running = false;
		segmentData.Active = false;
	}
}
