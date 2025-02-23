using Godot;
using System;
using System.Threading.Tasks;

public partial class PushButton : Node3D
{
	bool enableComms = false;
	[Export]
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
		}
	}

	[Export] string PushbuttonTag = "";
	[Export] string LampTag = "";
	[Export] int updateRate = 100;
	
	string text = "stop";
	[Export] String Text
	{
		get
		{
			return text;
		}
		set
		{
			text = value;
			
			if (textMesh != null)
			{
				textMesh.Text = text;
			}
		}
	}
	
	bool toggle = false;
	[Export] bool Toggle
	{
		get
		{
			return toggle;
		}
		set
		{
			toggle = value;
			if (!toggle)
				Pushbutton = false;
		}
	}
	
	bool pushbutton = false;
	[Export] bool Pushbutton 
	{
		get 
		{
			return pushbutton;
		}
		set
		{

			if (!running)
			{
				return;
			}
			pushbutton = value;
			
			if (!Toggle && pushbutton)
			{
				if (!Input.IsPhysicalKeyPressed(Key.G))
				{
					Task.Delay(updateRate * 3).ContinueWith(t => pushbutton = false);
				}
				Tween tween = GetTree().CreateTween();
				tween.TweenProperty(buttonMesh, "position", new Vector3(0, 0, buttonPressedZPos), 0.035f);
				tween.TweenInterval(0.2f);
				tween.TweenProperty(buttonMesh, "position", Vector3.Zero, 0.02f);
			}
			else if (buttonMesh != null)
			{
				if (pushbutton)
					buttonMesh.Position = new Vector3(0, 0, buttonPressedZPos);
				else
					buttonMesh.Position = Vector3.Zero;
			}
		}
	}
	
	bool lamp = false;
	[Export] bool Lamp
	{
		get
		{
			return lamp;
		}
		set
		{
			lamp = value;
			SetActive(lamp);
		}
	}
	
	Color buttonColor = new("#e73d30");
	[Export] Color ButtonColor
	{
		get
		{
			return buttonColor;
		}
		set
		{
			buttonColor = value;
			SetButtonColor(buttonColor);
		}
	}
	
	MeshInstance3D textMeshInstance;
	TextMesh textMesh;
	
	MeshInstance3D buttonMesh;
	StandardMaterial3D buttonMaterial;
	float buttonPressedZPos = -0.04f;
	bool keyHeld = false;
	bool keyPressed = false;
	
	bool readSuccessful = false;
	bool running = false;
	double scan_interval = 0;
	
	readonly Guid buttonId = Guid.NewGuid();
	readonly Guid activeId = Guid.NewGuid();
	
	Root main;
	public Root Main { get; set; }
	
	public override void _ValidateProperty(Godot.Collections.Dictionary property)
	{
		string propertyName = property["name"].AsStringName();

		if (propertyName == PropertyName.updateRate || propertyName == PropertyName.PushbuttonTag || propertyName == PropertyName.LampTag)
		{
			property["usage"] = (int)(EnableComms ? PropertyUsageFlags.Default : PropertyUsageFlags.NoEditor);
		}
	}
	
	public override void _Ready()
	{
		Main = GetParent().GetTree().EditedSceneRoot as Root;
		
		if (Main != null)
		{
			Main.SimulationStarted += OnSimulationStarted;
			Main.SimulationEnded += OnSimulationEnded;
		}
		
		// Assign 3D text
		textMeshInstance = GetNode<MeshInstance3D>("TextMesh");
		textMesh = textMeshInstance.Mesh.Duplicate() as TextMesh;
		textMeshInstance.Mesh = textMesh;
		textMesh.Text = text;
		
		// Assign button
		buttonMesh = GetNode<MeshInstance3D>("Meshes/Button");
		buttonMesh.Mesh = buttonMesh.Mesh.Duplicate() as Mesh;
		buttonMaterial = buttonMesh.Mesh.SurfaceGetMaterial(0).Duplicate() as StandardMaterial3D;
		buttonMesh.Mesh.SurfaceSetMaterial(0, buttonMaterial);
		
		// Initialize properties' states
		SetButtonColor(ButtonColor);
		SetActive(Lamp);
	}

    public override void _ExitTree()
    {
        if (Main == null) return;

        Main.SimulationStarted -= OnSimulationStarted;
        Main.SimulationEnded -= OnSimulationEnded;
    }

    public override void _PhysicsProcess(double delta)
	{
		if(!running)
		{
			Pushbutton = false;
			return;
		}
		// if (Main != null)
		// {
		// 	if(Main.selectedNodes != null)
		// 	{
		// 		bool selected = Main.selectedNodes.Contains(this);

		// 		if (selected && Input.IsPhysicalKeyPressed(Key.G))
		// 		{
		// 			keyPressed = true;
		// 			if (!keyHeld && Toggle)
		// 			{
		// 				keyHeld = true;
		// 				Pushbutton = !Pushbutton;
		// 			}
		// 			else if (!Toggle)
		// 			{
		// 				Pushbutton = true;
		// 			}
		// 		}
		// 	}
		// }
		
		if (!Input.IsPhysicalKeyPressed(Key.G))
		{
			keyHeld = false;
			if (keyPressed)
			{
				keyPressed = false;
				if(!Toggle)
				{
					Pushbutton = false;
				}
			}
		}
		
		if (enableComms && readSuccessful)
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
		if (PushbuttonTag != string.Empty)
		{
			await Main.Write("buttonId", Pushbutton);
		}
		
		if (LampTag != string.Empty)
		{
			Lamp = await Main.ReadBool(activeId);
		}
	}
	
	void SetActive(bool newValue)
	{
		if (buttonMaterial == null) return;
		
		if (newValue)
		{
			buttonMaterial.EmissionEnergyMultiplier = 1.0f;
		}
		else
		{
			buttonMaterial.EmissionEnergyMultiplier = 0.0f;
		}
	}
	
	void SetButtonColor(Color newValue)
	{
		if (buttonMaterial != null)
		{
			buttonMaterial.AlbedoColor = newValue;
			buttonMaterial.Emission = newValue;
		}
	}
	
	void OnSimulationStarted()
	{
		if (enableComms)
		{
			Main.Connect(buttonId, Root.DataType.Bool, PushbuttonTag);
			Main.Connect(activeId, Root.DataType.Bool, LampTag);
		}
		
		running = true;
		readSuccessful = true;
	}
	
	void OnSimulationEnded()
	{
		running = false;
		Lamp = false;
	}
}
