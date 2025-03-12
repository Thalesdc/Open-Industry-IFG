using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class Root : Node3D
{
	public int currentScene;
	[Export] int CurrentScene
	{
		get
		{
			return currentScene;
		}
		set
		{
			currentScene = value;
		}
	}

	[Signal]
	public delegate void SimulationStartedEventHandler();
	[Signal]
	public delegate void SimulationSetPausedEventHandler(bool paused);
	[Signal]
	public delegate void SimulationEndedEventHandler();

	private bool _start = false;
	public bool Start
	{
		get
		{
			return _start;
		}
		set
		{
			_start = value;

			if (_start)
			{
				PhysicsServer3D.SetActive(true);
				EmitSignal(SignalName.SimulationStarted);
			}
			else
			{
				PhysicsServer3D.SetActive(false);
				EmitSignal(SignalName.SimulationEnded);
			}
		}
	}

	readonly List<Vector3> positions = new();
	readonly List<Vector3> rotations = new();

	public bool paused = false;
	public enum DataType
	{
		Bool,
		Int,
		Float
	}
	RichTextLabel textoCommsState;

	public async Task<float> ReadFloat(string tagName)
	{
		// GD.Print("\n> [Root.cs] [ReadFloat()]");
		var floatValue = CommsConfig.ReadOpcItem(tagName);
		// GD.Print($"- {tagName} : {floatValue}");
		float value = Convert.ToSingle(floatValue);
		return Convert.ToSingle(floatValue);
	}

	public async Task<bool> ReadBool(string tagName)
	{
		// GD.Print("\n> [Root.cs] [ReadBool()]");
		var boolValue = CommsConfig.ReadOpcItem(tagName);
		// GD.Print($"- {tagName} : {boolValue}");
		return Convert.ToBoolean(boolValue);
	}

	public async Task Write(String tagName, bool value)
	{
		try
		{
			CommsConfig.WriteOpcItem(tagName, value);
		}
		catch (Exception e)
		{
			CallDeferred(nameof(PrintError), e.Message);
		}
	}

	public async Task Write(String tagName, float value)
	{
		try
		{
			CommsConfig.WriteOpcItem(tagName, value);
		}
		catch (Exception e)
		{
			CallDeferred(nameof(PrintError), e.Message);
		}
	}

	private static void PrintError(string error)
	{
		GD.PrintErr(error);
	}

	private Node3D building;
	private void SavePositions()
	{
		GD.Print("\n> [Root.cs] [SavePositions()]");
		foreach (Node3D node in GetNode<Node3D>("Building").GetChildren())
		{
			positions.Add(node.Position);
			rotations.Add(node.Rotation);
		}
	}

	private void ResetPositions()
	{
		GD.Print("\n> [Root.cs] [ResetPositions()]");
		int i = 0;
		foreach (Node3D node in GetNode<Node3D>("Building").GetChildren())
		{
			node.Position = positions[i];
			node.Rotation = rotations[i];
			i++;
		}
	}

	public override void _Ready()
	{
		GD.Print("\n> [Root.cs] [_Ready()]");
		GetNode<CanvasItem>("CommsConfigMenu").Visible = false;
		textoCommsState = GetNode<RichTextLabel>("TextoCommsState");
		textoCommsState.Visible = true;
		DefinirTextStatusConexao();

		var simulationEvents = GetNodeOrNull("/root/GlobalVariables");
		if (simulationEvents != null)
		{
			simulationEvents.Connect("simulation_started", new Callable(this, nameof(OnSimulationStarted)));
			simulationEvents.Connect("simulation_set_paused", new Callable(this, nameof(OnSimulationSetPaused)));
			simulationEvents.Connect("simulation_ended", new Callable(this, nameof(OnSimulationEnded)));
		}
	}

	void _on_bt_show_comms_config_menu_pressed()
	{
		DefinirTextStatusConexao();
		GetNode<CanvasItem>("CommsConfigMenu").Visible = !GetNode<CanvasItem>("CommsConfigMenu").Visible;
		GetNode<PanelContainer>("RunBar").Visible = !GetNode<PanelContainer>("RunBar").Visible;
		GetNode<RichTextLabel>("TextoCommsState").Visible = !GetNode<RichTextLabel>("TextoCommsState").Visible;
	}

	public override void _Process(double delta)
	{
		//selectedNodes = EditorInterface.Singleton.GetSelection().GetSelectedNodes();
	}

	void OnSimulationStarted()
	{
		GD.Print("\n> [Root.cs] [OnSimulationStarted()]");
		Start = true;
	}

	void OnSimulationSetPaused(bool _paused)
	{
		GD.Print("\n> [Root.cs] [OnSimulationSetPaused()]");
		paused = _paused;

		if (paused)
		{
			ProcessMode = ProcessModeEnum.Disabled;
		}
		else
		{
			ProcessMode = ProcessModeEnum.Inherit;
		}

		EmitSignal(SignalName.SimulationSetPaused, paused);
	}

	void OnSimulationEnded()
	{
		GD.Print("\n> [Root.cs] [OnSimulationEnded()]");
		Start = false;
	}

	void DefinirTextStatusConexao()
	{
		// GD.Print("\n> [Root.cs] [DefinirTextStatusConexao()]");

		var globalVariables = GetNodeOrNull("/root/GlobalVariables");
		bool status = (bool)globalVariables.Get("opc_da_connected");

		if (status)
		{
			// RGB: (102, 255, 102) Verde Claro
			textoCommsState.Text = "Comunicação OPC DA: Conectado";
			textoCommsState.AddThemeColorOverride("default_color", new Color(0.4f, 1.0f, 0.4f, 1.0f));
		}
		else
		{
			textoCommsState.Text = "Comunicação OPC DA: Desconectado";
		}

	}
}
