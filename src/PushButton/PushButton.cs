using Godot;
using System;
using System.Threading.Tasks;

public partial class PushButton : Node3D
{
	private bool isCommsConnected;
	[Export] int updateRate = 2000;
	string text = "stop";
	[Export]
	String Text
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
	[Export]
	bool Toggle
	{
		get
		{
			return toggle;
		}
		set
		{
			toggle = value;
			if (!toggle)
				pushbutton = false;
		}
	}

	bool pushbutton = false;

	bool lamp = false;
	[Export]
	bool Lamp
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
	[Export]
	Color ButtonColor
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
	double scan_interval = 2000;
	string tagPushButton;
	Root main;
	public Root Main { get; set; }

	public override void _Ready()
	{
		GD.Print($"\n> [PushButton.cs] [{Name}] [_Ready()]");
		Main = GetTree().CurrentScene as Root;

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

	private void _on_static_body_3d_input_event(Node camera, InputEvent inputEvent, Vector3 position, Vector3 normal, int shapeIdx)
	{
		if (inputEvent is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
			{
				GD.Print("\n>[PushButton.cs] [_on_static_body_3d_input_event]");

				GD.Print($" - running: {running}");
				if (!running)
				{
					pushbutton = false;
					return;
				}

				pushbutton = !pushbutton;
				GD.Print($" - pushbutton: {pushbutton}");

				SetActive(pushbutton);

				if (pushbutton)
				{
					buttonMesh.Position = new Vector3(0, 0, buttonPressedZPos);
				}
				else
				{
					buttonMesh.Position = Vector3.Zero;
				}

				if (
					isCommsConnected &&
					readSuccessful &&
					tagPushButton != null &&
					tagPushButton != string.Empty
				)
				{
					Task.Run(WriteTag);
				}
			}
		}
	}

	public override void _ExitTree()
	{
		GD.Print($"\n> [PushButton.cs] [{Name}] [_ExitTree()]");
		if (Main == null) return;

		Main.SimulationStarted -= OnSimulationStarted;
		Main.SimulationEnded -= OnSimulationEnded;
	}

	async Task WriteTag()
	{
		try
		{
			Main.Write(tagPushButton, pushbutton);
		}
		catch
		{
			GD.PrintErr("Failure to write: " + tagPushButton + " in Node: " + Name);
			readSuccessful = false;
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
		GD.Print($"\n> [PushButton.cs] [{Name}] [OnSimulationStarted()]");
		tagPushButton = SceneComponents.GetComponentByKey(Name, Main.currentScene).Tag;

		var globalVariables = GetNodeOrNull("/root/GlobalVariables");
		isCommsConnected = (bool)globalVariables.Get("opc_da_connected");

		if (isCommsConnected)
		{
			readSuccessful = true;
		}

		running = true;
		GD.Print($"- running:{running}");
	}

	void OnSimulationEnded()
	{
		running = false;
		Lamp = false;
	}
}
