using Godot;
using System;
using System.Threading.Tasks;

public partial class ChainTransfer : Node3D
{
	PackedScene chainTransferBaseScene = (PackedScene)ResourceLoader.Load("res://src/ChainTransfer/Base.tscn");
	
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
	public string speedTag;
	[Export]
	public string popupTag;
	[Export]
	private int updateRate = 300;
	
	readonly Guid speedId = Guid.NewGuid();
	readonly Guid popupId = Guid.NewGuid();
	
	double scan_interval = 0;
	bool readSuccessful = false;
	
	// Implemented logic from StackLight.cs
	int chains = 2;
	[Export] int Chains
	{
		get 
		{
			return chains;
		}
		set
		{
			int new_value = Mathf.Clamp(value, 2, 6);
			if (new_value > chains)
			{
				SpawnChains(new_value - chains);
			}
			else
			{
				RemoveChains(chains - new_value);
			}

			chains = new_value;
			FixChains();
		}
	}
	
	float distance = 0.33f;
	[Export] float Distance
	{
		get
		{
			return distance;
		}
		set
		{
			distance = Mathf.Clamp(value, 0.25f, 5.0f);
			SetChainsDistance(distance);
		}
	}
	
	float speed = 2.0f;
	[Export] float Speed
	{
		get
		{
			return speed;
		}
		set
		{
			speed = value;
		}
	}
	
	
	bool popupChains = false;
	[Export] bool PopupChains
	{
		get
		{
			return popupChains;
		}
		set
		{
			popupChains = value;
		}
	}
	
	bool running = false;
	
	public Root Main { get; set; }
	
	public override void _ValidateProperty(Godot.Collections.Dictionary property)
	{
		string propertyName = property["name"].AsStringName();

		if (propertyName == PropertyName.updateRate || propertyName == PropertyName.speedTag || propertyName == PropertyName.popupTag)
		{
			property["usage"] = (int)(EnableComms ? PropertyUsageFlags.Default : PropertyUsageFlags.NoEditor);
		}
	}
	
	public override void _Ready()
	{
		Main = GetTree().CurrentScene as Root;
		if (Main != null)
		{
			Main.SimulationStarted += OnSimulationStarted;
			Main.SimulationEnded += OnSimulationEnded;
		}
		
		SpawnChains(chains - GetChildCount());
		SetChainsDistance(distance);
		SetChainsSpeed(speed);
		SetChainsPopupChains(popupChains);
	}

    public override void _ExitTree()
    {
        if (Main == null) return;

        Main.SimulationStarted -= OnSimulationStarted;
        Main.SimulationEnded -= OnSimulationEnded;
    }

    public override void _PhysicsProcess(double delta)
	{
		Scale = new Vector3(Scale.X, 1, 1);

		if(!running)
		{
			popupChains = false;
			return;
		}
		
		if (running)
		{
			SetChainsPopupChains(popupChains);
			SetChainsSpeed(speed);

			if (EnableComms && readSuccessful)
			{
				scan_interval += delta;
				if (scan_interval > (float)updateRate/1000 && readSuccessful)
				{
					scan_interval = 0;
					Task.Run(ScanTag);
				}
			}
		}
	}
	
	void SetChainsDistance(float distance)
	{
		foreach (ChainTransferBase chainBase in GetChildren())
		{
			chainBase.Position = new Vector3(0, 0, distance * chainBase.GetIndex());
		}
	}
	
	void SetChainsSpeed(float speed)
	{
		foreach (ChainTransferBase chainBase in GetChildren())
		{
			chainBase.Speed = speed;
		}
	}
	
	void SetChainsPopupChains(bool popupChains)
	{
		foreach (ChainTransferBase chainBase in GetChildren())
		{
			chainBase.Active = popupChains;
		}
	}
	
	void TurnOnChains() 
	{
		foreach (ChainTransferBase chainBase in GetChildren())
		{
			chainBase.TurnOn();
		}
	}
	
	void TurnOffChains() 
	{
		foreach (ChainTransferBase chainBase in GetChildren())
		{
			chainBase.TurnOff();
		}
	}
	
	void SpawnChains(int count)
	{
		if (chains <= 0) return;
		for (int i = 0; i < count; i++)
		{
			ChainTransferBase chainBase = chainTransferBaseScene.Instantiate() as ChainTransferBase;
			AddChild(chainBase, forceReadableName: true);
			chainBase.Owner = this; 
			chainBase.Position = new Vector3(0, 0, distance * chainBase.GetIndex());
			chainBase.Active = popupChains;
		}
	}
	
	void RemoveChains(int count)
	{
		for (int i = 0; i < count; i++)
		{
			GetChild(GetChildCount() - 1 - i).QueueFree();
		}
	}
	
	void FixChains()
	{
		int childCount = GetChildCount();
		int difference = childCount - chains;
		
		if (difference <= 0) return;
		
		for (int i = 0; i < difference; i++)
		{
			GetChild(GetChildCount() - 1 - i).QueueFree();
		}
	}
	
	async Task ScanTag()
	{
		try
		{
			Speed = await Main.ReadFloat("speedId");
		}
		catch
		{
			GD.PrintErr("Failure to read: " + speedTag + " in Node: " + Name);
			readSuccessful = false;
		}

		try
		{
			PopupChains = await Main.ReadBool("popupId");
		}
		catch
		{
			GD.PrintErr("Failure to write: " + popupTag + " in Node: " + Name);
			readSuccessful = false;
		}
	}
	
	void OnSimulationStarted()
	{
		if (Main == null) return;
		
		if (enableComms)
		{
			readSuccessful = true;
		}
		
		running = true;
		TurnOnChains();
	}
	
	void OnSimulationEnded()
	{
		this.PopupChains = false;
		TurnOffChains();		
		running = false;
	}
}
