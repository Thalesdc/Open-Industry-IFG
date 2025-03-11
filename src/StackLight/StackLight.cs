using Godot;
using System;

public partial class StackLight : Node3D
{
	PackedScene segmentScene = (PackedScene)ResourceLoader.Load("res://src/StackLight/StackSegment.tscn");
	StackLightData data = (StackLightData)ResourceLoader.Load("res://src/StackLight/StackLightData.tres");

	bool enableComms = false;
	bool EnableComms
	{
		get
		{
			return enableComms;
		}
		set
		{
			enableComms = value;

			NotifyPropertyListChanged();

			if (segmentsContainer != null)
			{
				foreach (StackSegment segment in segmentsContainer.GetChildren())
				{
					segment.EnableComms = enableComms;
				}
			}
		}
	}

	int updateRate = 100;

	int UpdateRate
	{
		get
		{
			return updateRate;
		}
		set
		{
			updateRate = value;

			if (segmentsContainer != null)
			{
				foreach (StackSegment segment in segmentsContainer.GetChildren())
				{
					segment.updateRate = updateRate;
				}
			}
		}
	}


	int segments = 1;
	int Segments
	{
		get
		{
			return segments;
		}
		set
		{
			if (value == segments || running) return;

			int new_value = Mathf.Clamp(value, 1, 10);
			if (new_value > segments)
			{
				SpawnSegments(new_value - segments);
			}
			else
			{
				RemoveSegments(segments - new_value);
			}

			segments = new_value;
			FixSegments();
			
			if (segmentsContainer != null)
			{
				data.SetSegments(segments);
				InitSegments();

				if (topMesh != null)
					topMesh.Position = new Vector3(0, topMeshInitialYPos + (step * (segments - 1)), 0);
			}

			NotifyPropertyListChanged();
		}
	}

	float step = 0.048f;
	Node3D segmentsContainer;
	MeshInstance3D topMesh;
	float segmentInitialYPos;
	float topMeshInitialYPos = 0.087f;
	bool running = false;

	MeshInstance3D bottomMesh;
	MeshInstance3D stemMesh;
	MeshInstance3D midMesh;

	Root main;
	public Root Main { get; set; }

	public override Variant _Get(StringName property)
	{
		if (segmentsContainer == null) return default;
		for (int i = 0; i < segments; i++)
		{
			if (property == "Light " + (i + 1).ToString())
			{
				StackSegment segment = segmentsContainer.GetChild(i) as StackSegment;
				return segment.SegmentData;
			}
		}
		return default;
	}

	public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
	{
		Godot.Collections.Array<Godot.Collections.Dictionary> properties = new();

		properties.Add(new Godot.Collections.Dictionary()
			{
				{"name", "StackLight"},
				{"type", (int)Variant.Type.Nil},
				{"usage", (int)PropertyUsageFlags.Category}
			});
		properties.Add(new Godot.Collections.Dictionary()
			{
				{"name", "EnableComms"},
				{"type", (int)Variant.Type.Bool},
				{"usage", (int)PropertyUsageFlags.Default}
			});
		properties.Add(new Godot.Collections.Dictionary()
			{
				{"name", "UpdateRate"},
				{"type", (int)Variant.Type.Int},
				{"usage", (int)(EnableComms ? PropertyUsageFlags.Default : PropertyUsageFlags.NoEditor)}
			});
		properties.Add(new Godot.Collections.Dictionary()
			{
				{"name", "Segments"},
				{"type", (int)Variant.Type.Int},
				{"usage", (int)PropertyUsageFlags.Default}
			});
		properties.Add(new Godot.Collections.Dictionary()
			{
				{"name", "data"},
				{"type", (int)Variant.Type.Object},
				{"usage", (int)PropertyUsageFlags.NoEditor}
			});
		for (int i = segments - 1; i >= 0; i--)
		{
			properties.Add(new Godot.Collections.Dictionary()
			{
				{"name", "Light " + (i + 1).ToString()},
				{"class_name", "StackSegmentData"},
				{"type", (int)Variant.Type.Object},
				{"usage", (int)PropertyUsageFlags.Default}
			});
		}
		return properties;
	}

	public override void _Ready()
	{
		Main = GetTree().CurrentScene as Root;

		if (Main != null)
		{
			Main.SimulationStarted += OnSimulationStarted;
			Main.SimulationEnded += OnSimulationEnded;
		}

		data = data.Duplicate(true) as StackLightData;
		data.InitSegments(segments);

		segmentsContainer = GetNode<Node3D>("Mid/Segments");
		topMesh = GetNode<MeshInstance3D>("Mid/Top");

		bottomMesh = GetNode<MeshInstance3D>("Bottom");
		midMesh = GetNode<MeshInstance3D>("Mid");

		segmentInitialYPos = segmentsContainer.GetNode<Node3D>("StackSegment").Position.Y;

		if (segmentsContainer.GetChildCount() <= 1)
			SpawnSegments(segments - 1);

		topMesh.Position = new Vector3(0, topMeshInitialYPos + (step * (segments - 1)), 0);
		InitSegments();
	}

    public override void _ExitTree()
    {
        if (Main == null) return;

        Main.SimulationStarted -= OnSimulationStarted;
        Main.SimulationEnded -= OnSimulationEnded;
    }

    public override void _PhysicsProcess(double delta)
	{
		Scale = new Vector3(Scale.X, Scale.Y, Scale.X);
		bottomMesh.Scale = new Vector3(1, 1 / Scale.Y, 1);
		midMesh.Scale = new Vector3(1, (1 / Scale.Y) * Scale.X, 1);
	}

	void OnSimulationStarted()
	{
		if (Main == null) return;
		running = true;
	}

	void OnSimulationEnded()
	{
		running = false;
	}

	void InitSegments()
	{
		for (int i = 0; i < segments; i++)
		{
			StackSegment segment = segmentsContainer.GetChild(i) as StackSegment;
			segment.EnableComms = enableComms;
			segment.SegmentData = data.segmentDatas[i];
		}
	}

	void SpawnSegments(int count)
	{
		if (segments == 0 || segmentsContainer == null) return;

		for (int i = 0; i < count; i++)
		{
			Node3D segment = segmentScene.Instantiate() as Node3D;
			segmentsContainer.AddChild(segment, forceReadableName: true);
			segment.Position = new Vector3(0, segmentInitialYPos + (step * segment.GetIndex()), 0);
		}
	}

	void RemoveSegments(int count)
	{
		for (int i = 0; i < count; i++)
		{
			segmentsContainer.GetChild(segmentsContainer.GetChildCount() - 1 - i).QueueFree();
		}
	}
	
	void FixSegments()
	{
		int childCount = segmentsContainer.GetChildCount();
		int difference = childCount - segments;
		
		if (difference <= 0) return;
		
		for (int i = 0; i < difference; i++)
		{
			segmentsContainer.GetChild(segmentsContainer.GetChildCount() - 1 - i).QueueFree();
		}
	}
}
