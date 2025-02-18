using Godot;
using Godot.Collections;
using libplctag;
using libplctag.DataTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Opc.Ua.Client;
using Opc.Ua;
using Opc.Ua.Configuration;

public partial class Root : Node3D
{
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
				bool_tags.Clear();
				int_tags.Clear();
				float_tags.Clear();
				opc_tags.Clear();
				EmitSignal(SignalName.SimulationEnded);
			}
		}
	}


	private Protocols _protocol;

	[Export]
	private Protocols Protocol
	{
		get => _protocol;
		set
		{
			_protocol = value;
			NotifyPropertyListChanged();
		}
	}

	[Export]
	private string Gateway { get; set; }

	[Export]
	private string Path { get; set; }

	[Export]

	private PlcType PlcType { get; set; } = PlcType.ControlLogix;

	[Export]

	private string EndPoint { get; set; }

	readonly List<Vector3> positions = new();
	readonly List<Vector3> rotations = new();

	readonly System.Collections.Generic.Dictionary<Guid, Tag<RealPlcMapper, float>> float_tags = new();
	readonly System.Collections.Generic.Dictionary<Guid, Tag<BoolPlcMapper, bool>> bool_tags = new();
	readonly System.Collections.Generic.Dictionary<Guid, Tag<DintPlcMapper, int>> int_tags = new();
	readonly System.Collections.Generic.Dictionary<Guid, string> opc_tags = new();

	public bool paused = false;

	public Array<Godot.Node> selectedNodes;

	public Session session;

	public enum Protocols
	{
		opc_ua,
	}

	public enum DataType
	{
		Bool,
		Int,
		Float
	}

	public override void _ValidateProperty(Godot.Collections.Dictionary property)
	{
		string propertyName = property["name"].AsStringName();

		if (propertyName == PropertyName.EndPoint)
		{
			property["usage"] = (int)(Protocol == Protocols.opc_ua ? PropertyUsageFlags.Default : PropertyUsageFlags.NoEditor);
		}
		else if (propertyName == PropertyName.Gateway || propertyName == PropertyName.Path || propertyName == PropertyName.PlcType)
		{
			property["usage"] = (int)(Protocol == Protocols.opc_ua ? PropertyUsageFlags.NoEditor : PropertyUsageFlags.Default);
		}
	}


	private void OpcConnect()
	{
		var config = new ApplicationConfiguration()
		{
			ApplicationName = "Open Industry Project",
			ApplicationUri = Utils.Format(@"urn:{0}:Open Industry Project", System.Net.Dns.GetHostName()),
			ApplicationType = ApplicationType.Client,
			SecurityConfiguration = new SecurityConfiguration
			{
				ApplicationCertificate = new CertificateIdentifier { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault", SubjectName = "Open Industry Project" },
				TrustedIssuerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities" },
				TrustedPeerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications" },
				RejectedCertificateStore = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\RejectedCertificates" },
				AutoAcceptUntrustedCertificates = true
			},
			TransportConfigurations = new TransportConfigurationCollection(),
			TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
			ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
			TraceConfiguration = new TraceConfiguration()
		};
		config.Validate(ApplicationType.Client).GetAwaiter().GetResult();

		if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
		{
			config.CertificateValidator.CertificateValidation += (s, e) => { e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
		}

		var application = new ApplicationInstance
		{
			ApplicationName = "Open Industry Project",
			ApplicationType = ApplicationType.Client,
			ApplicationConfiguration = config
		};

		application.CheckApplicationInstanceCertificate(false, 2048).GetAwaiter().GetResult();

		EndpointDescription endpointDescription = CoreClientUtils.SelectEndpoint(EndPoint, false);
		EndpointConfiguration endpointConfiguration = EndpointConfiguration.Create(config);
		ConfiguredEndpoint endpoint = new(null, endpointDescription, endpointConfiguration);

		bool updateBeforeConnect = false;

		bool checkDomain = false;

		string sessionName = config.ApplicationName;

		uint sessionTimeout = 60000;

		List<string> preferredLocales = null;

		session = Session.Create(
					config,
					endpoint,
					updateBeforeConnect,
					checkDomain,
					sessionName,
					sessionTimeout,
					new UserIdentity(),
					preferredLocales
				).Result;
	}


	public void Connect(Guid guid, DataType dataType, string tagName)
	{
		// Acesse o singleton pelo nome configurado no autoload
		var simulationEvents = GetNodeOrNull("/root/GlobalVariables");

		GD.Print($">\n [Root.cs] [Connect()] - Protocol:{Protocol})");
		GD.Print($"- opc_da_comms_connected :{simulationEvents.Get("opc_da_comms_connected")}");
		GD.Print($"- Tag adicionada (guid:{guid}, tagName:{tagName})");
		opc_tags.Add(guid, tagName);

		// //OPC UA
		// if (Protocol == Protocols.opc_ua)
		// {
		// 	GD.Print($"--- Connect() Root.cs - Tag adicionada (guid:{guid}, tagName:{tagName})");
		// 	opc_tags.Add(guid, tagName);
		// }
		// else
		// {
		// 	if (dataType == DataType.Bool)
		// 	{
		// 		Tag<BoolPlcMapper, bool> tag = new()
		// 		{
		// 			Name = tagName,
		// 			Gateway = Gateway,
		// 			Path = Path,
		// 			PlcType = PlcType,
		// 			Protocol = (Protocol?)Protocol-1,
		// 			Timeout = TimeSpan.FromSeconds(5)
		// 		};

		// 		try
		// 		{
		// 			tag.Initialize();
		// 			bool_tags.Add(guid, tag);
		// 		}
		// 		catch(Exception e)
		// 		{
		// 			GD.PrintErr(tag.Name + ": " + e.Message);
		// 		}

		// 	}
		// 	else if (dataType == DataType.Int)
		// 	{
		// 		Tag<DintPlcMapper, int> tag = new()
		// 		{
		// 			Name = tagName,
		// 			Gateway = Gateway,
		// 			Path = Path,
		// 			PlcType = PlcType,
		// 			Protocol = (Protocol?)Protocol-1,
		// 			Timeout = TimeSpan.FromSeconds(5)
		// 		};

		// 		try
		// 		{
		// 			tag.Initialize();
		// 			int_tags.Add(guid, tag);
		// 		}
		// 		catch (Exception e)
		// 		{
		// 			GD.PrintErr(tag.Name + ": " + e.Message);
		// 		}
		// 	}
		// 	else if (dataType == DataType.Float)
		// 	{
		// 		Tag<RealPlcMapper, float> tag = new()
		// 		{
		// 			Name = tagName,
		// 			Gateway = Gateway,
		// 			Path = Path,
		// 			PlcType = PlcType,
		// 			Protocol = (Protocol?)Protocol-1,
		// 			Timeout = TimeSpan.FromSeconds(5)
		// 		};

		// 		try
		// 		{
		// 			tag.Initialize();
		// 			float_tags.Add(guid, tag);
		// 		}
		// 		catch (Exception e)
		// 		{
		// 			GD.PrintErr(tag.Name + ": " + e.Message);
		// 		}
		// 	}
		// }
	}

	private T HandleOpcUaRead<T>(Guid guid)
	{
		GD.Print($"--- HandleOpcUaRead - Guid:{guid})");
		var value = session.ReadValueAsync(opc_tags[guid]).Result.Value;

		if (value is T typedValue)
		{
			return typedValue;
		}
		else
		{
			string errorMessage = $"Expected {typeof(T)} but received {value.GetType()} for nodeid: {opc_tags[guid]}";
			GD.PrintErr(errorMessage);
			throw new InvalidCastException(errorMessage);
		}
	}

	public async Task<bool> ReadBool(Guid guid)
	{
		if (Protocol == Protocols.opc_ua)
			return HandleOpcUaRead<bool>(guid);
		else
			return Convert.ToBoolean(await bool_tags[guid].ReadAsync());
	}

	public async Task<int> ReadInt(Guid guid)
	{
		if (Protocol == Protocols.opc_ua)
			return HandleOpcUaRead<int>(guid);
		else
			return Convert.ToInt32(await bool_tags[guid].ReadAsync());
	}

	public async Task<float> LerFloat(string tagName)
	{
		GD.Print("\n> [Root.cs] [LerFloat()]");
		GD.Print($"- tagName:{tagName}");
		var valorVelocidade = CommsConfig.ReadOpcItem(tagName);
		GD.Print($"- valorVelocidade:{valorVelocidade}");
		string vel_string = valorVelocidade.ToString();
		GD.Print($"valorVelocidade: {vel_string}");
		return Convert.ToSingle(valorVelocidade);
	}

	public async Task<float> ReadFloat(Guid guid)
	{
		GD.Print("\n> [Root.cs] [ReadFloat()]");
		// Godot.Node commsConfig = GetTree().Root.GetNode("CommsConfigMenu");
		var valorVelocidade = CommsConfig.ReadOpcItem("PLC_GW3.Application.LOGICA_ESTEIRA_LD.VELOCIDADE_INT");

		string vel_string = valorVelocidade.ToString();
		GD.Print($"valorVelocidade: {vel_string}");
		return  Convert.ToSingle(valorVelocidade);
		// TODO: Implementar leitura da TAG OPC_DA

		// return HandleOpcUaRead<float>(guid);

		// if (Protocol == Protocols.opc_ua)
		// 	return HandleOpcUaRead<float>(guid);
		// else
		// 	return (float)(await float_tags[guid].ReadAsync());
	}
	public async Task Write(Guid guid, bool value)
	{
		if (Protocol == Protocols.opc_ua)
		{
			RequestHeader requestHeader = new();

			WriteValueCollection writeValues = new();

			WriteValue writeValue = new()
			{
				NodeId = new NodeId(opc_tags[guid]),
				AttributeId = Attributes.Value,
				Value = new DataValue
				{
					Value = Convert.ToBoolean(value)
				}
			};

			writeValues.Add(writeValue);

			await session.WriteAsync(requestHeader, writeValues, new System.Threading.CancellationToken());
		}
		else
		{
			bool_tags[guid].Value = value;

			try
			{
				bool_tags[guid].Value = value;
				await bool_tags[guid].WriteAsync();
			}
			catch (Exception e)
			{
				CallDeferred(nameof(PrintError), e.Message);
			}

		}
	}

	public async Task Write(Guid guid, int value)
	{
		if (Protocol == Protocols.opc_ua)
		{
			RequestHeader requestHeader = new();

			WriteValueCollection writeValues = new();

			WriteValue writeValue = new()
			{
				NodeId = new NodeId(opc_tags[guid]),
				AttributeId = Attributes.Value,
				Value = new DataValue
				{
					Value = Convert.ToInt16(value)
				}
			};

			writeValues.Add(writeValue);

			await session.WriteAsync(requestHeader, writeValues, new System.Threading.CancellationToken());
		}
		else
		{
			int_tags[guid].Value = value;
			await int_tags[guid].WriteAsync();
		}
	}

	public async Task Write(Guid guid, float value)
	{
		//OPC UA
		if (Protocol == Protocols.opc_ua)
		{
			RequestHeader requestHeader = new();

			WriteValueCollection writeValues = new();

			WriteValue writeValue = new()
			{
				NodeId = new NodeId(opc_tags[guid]),
				AttributeId = Attributes.Value,
				Value = new DataValue
				{
					Value = value
				}
			};

			writeValues.Add(writeValue);

			await session.WriteAsync(requestHeader, writeValues, new System.Threading.CancellationToken());
		}
		else
		{
			try
			{
				float_tags[guid].Value = value;
				await float_tags[guid].WriteAsync();
			}
			catch (Exception e)
			{
				CallDeferred(nameof(PrintError), e.Message);
			}
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
		GetNode<CanvasItem>("CommsConfigMenu").Visible = !GetNode<CanvasItem>("CommsConfigMenu").Visible;
		GetNode<PanelContainer>("RunBar").Visible = !GetNode<PanelContainer>("RunBar").Visible;
	}

	public override void _Process(double delta)
	{
		//selectedNodes = EditorInterface.Singleton.GetSelection().GetSelectedNodes();
	}

	void OnSimulationStarted()
	{
		GD.Print("\n> [Root.cs] [OnSimulationStarted()]");
		Start = true;

		// if (Protocol == Protocols.opc_ua && opc_tags.Count > 0)
		// {
		// 	if (session != null && session.Endpoint.EndpointUrl.TrimEnd('/') != EndPoint.TrimEnd('/'))
		// 	{
		// 		session.Close();
		// 		session = null;
		// 	}
		// 	if (session == null)
		// 	{
		// 		OpcConnect();
		// 	}
		// }
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
}
