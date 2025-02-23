using Godot;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc;
using OpcRcw.Dx;
using GodotPlugins.Game;

public partial class CommsConfig : Control
{
	// Lista para armazenar os servidores OPC
	private ItemList il_opcServerList;

	// Lista global de servidores OPC
	List<Opc.Da.Server> globalOpcServerList = new List<Opc.Da.Server>();


	// Lista para tags OPC encontradas
	private ItemList il_opcServerTagList;

	// Container com lista de Tags e OptionButtons
	private VBoxContainer vBoxContainerTag; // Contêiner para os OptionButtons


	List<string> opc_da_items_list = new List<string>();

	static Opc.Da.Server opcServer;
	static private Opc.Da.Subscription subscription;
	static Opc.Da.Item[] opc_da_items_array;

	// TODO: Mudar para atributo do objeto no godot
	int CENA = 1;
	List<ObjetosCena> objetosCena;
	public override void _Ready()
	{
		try
		{
			GD.Print("\n> [CommsConfig.cs] [_Ready()]");

			// Lista de servidores
			il_opcServerList = GetNode<ItemList>("MarginContainer/HBoxContainer/VBoxContainer1/ServerList");

			// Lista de tags do servidor OPC
			il_opcServerTagList = GetNode<ItemList>("MarginContainer/HBoxContainer/VBoxContainer1/ScrollContainer/VBoxContainer/OpcServerTagList");

			if (CENA == 1)
			{
				objetosCena = ObjetosCena.objetosFixosCena1;

			}

			foreach (var objeto in objetosCena)
			{
				// GD.Print($"objeto.Id: {objeto.Id}");
				// GD.Print($"objeto.Nome: {objeto.Nome}");
				// GD.Print($"objeto.Tag: {objeto.Tag}");
				HBoxContainer hbox = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ShrinkBegin };
				hbox.AddChild(new Label
				{
					Text = objeto.Nome,
					SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
					SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
				});

				OptionButton opt = new OptionButton
				{
					SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
					SizeFlagsVertical = Control.SizeFlags.ShrinkBegin
				};
				opt.ItemSelected += (index) => OnOptionSelected(opt, index);
				hbox.AddChild(opt);
				// GD.Print($"opt: {opt}");

				vBoxContainerTag = GetNode<VBoxContainer>("MarginContainer/HBoxContainer/VBoxContainerMap/ScrollContainerTag/VBoxContainerTag");
				// GD.Print($"vBoxContainerTag: {vBoxContainerTag}");
				vBoxContainerTag.AddChild(hbox);
			}
		}
		catch (Exception e)
		{
			GD.PrintErr("Erro CommsConfig.cs - _Ready");
			GD.PrintErr(e);
		}
	}
	private void _on_btn_opc_pressed()
	{
		try
		{
			//Find opc server on local machine
			Opc.IDiscovery discovery = new OpcCom.ServerEnumerator();
			il_opcServerList.Clear();
			il_opcServerTagList.Clear();
			Opc.Server[] opcServerList = discovery.GetAvailableServers(Opc.Specification.COM_DA_30);

			foreach (Opc.Server item in opcServerList)
			{
				// GD.Print($"Server: {item.Name}");
				il_opcServerList.AddItem(item.Name);
				globalOpcServerList.Add((Opc.Da.Server)item);
			}
		}
		catch (Exception e)
		{
			GD.PrintErr("Erro ao comunicar com OPC DA");
			GD.PrintErr(e);
		}
	}

	private void _on_server_selected(int index)
	{
		try
		{
			// GD.Print("\n> [CommsConfig.cs] [_on_server_selected()]");

			// Obter o nome do item selecionado
			string selectedItem = il_opcServerList.GetItemText(index);

			// TODO: Validar se item já está na lista
			opcServer = globalOpcServerList.FirstOrDefault(server =>
			server.Name.Equals(selectedItem));

			//Connect server
			opcServer.Connect();

			//Browse all items
			BrowsAllElement("", ref opcServer);

			Opc.Da.SubscriptionState state = new Opc.Da.SubscriptionState
			{
				Name = "OIIFG",
				Active = true,  // Habilitar a leitura automática
				UpdateRate = 1000,  // Tempo de atualização (1 segundo)
				Deadband = 0,  // Sem filtro de variação mínima
			};

			subscription = (Opc.Da.Subscription)opcServer.CreateSubscription(state);

			opc_da_items_array = new Opc.Da.Item[opc_da_items_list.Count];
			for (int i = 0; i < opc_da_items_list.Count; i++) // Item initial assignment
			{
				opc_da_items_array[i] = new Opc.Da.Item();
				opc_da_items_array[i].ClientHandle = Guid.NewGuid().ToString();
				opc_da_items_array[i].ItemPath = null;
				opc_da_items_array[i].ItemName = opc_da_items_list[i]; // The name of the data item in the server.
			}

			subscription.AddItems(opc_da_items_array);
			subscription.DataChanged += new Opc.Da.DataChangedEventHandler(OnDataChange);

			var globalVariables = GetNodeOrNull("/root/GlobalVariables");
			globalVariables.Set("opc_da_connected", true);
			GD.Print($"- globalVariables.opc_da_connected: {globalVariables.Get("opc_da_connected")}");

		}
		catch (Exception ex)
		{
			GD.Print(ex);
		}
	}

	void OnDataChange(object subscriptionHandle, object requestHandle, Opc.Da.ItemValueResult[] values)
	{
		// GD.Print("\n> [CommsConfig.cs] [OnDataChange()]");
		// foreach (var value in values)
		// {
			// GD.Print($"🔹 Tag: {value.ItemName}, Valor: {value.Value}, Qualidade: {value.Quality}");
		// }
	}

	// public void UpdateOPCData(SensorData tItem)
	// {
	// 	GD.Print("\n> [CommsConfig.cs] [UpdateOPCData()]");
	// 	Item OPC_WriteItem = Array.Find(mMonitoringSubscription.Items, x => x.ItemName.Equals(tItem.Name));
	// 	ItemValue[] writeValues = new ItemValue[1];
	// 	writeValues[0] = new ItemValue(OPC_WriteItem);
	// 	writeValues[0].Value = tItem.Value;
	// 	IdentifiedResult[] retValues = mMonitoringSubscription.Write(writeValues);
	// }

	private void BrowsAllElement(string itemName, ref Opc.Da.Server opcServer)
	{
		try
		{
			Opc.Da.BrowseFilters filters = new Opc.Da.BrowseFilters();
			Opc.Da.BrowsePosition position = null;
			Opc.ItemIdentifier id = new Opc.ItemIdentifier(itemName);
			Opc.Da.BrowseElement[] children = opcServer.Browse(id, filters, out position);
			if (children != null)
			{
				foreach (var item in children)
				{
					if (item.HasChildren)
					{
						if (string.IsNullOrEmpty(itemName))
						{
							BrowsAllElement(item.Name, ref opcServer);
							preencherListaTags(item.Name);
						}
						else
						{
							string tagPath = itemName + "." + item.Name;
							BrowsAllElement(tagPath, ref opcServer);
							preencherListaTags(tagPath);
						}
					}
					else
					{
						string tagPath = itemName + "." + item.Name;
						preencherListaTags(tagPath);
					}
				}
			}
		}
		catch (Exception e)
		{
			GD.PrintErr("\n Erro BrowsAllElement()");
			GD.PrintErr(e);
		}
	}
	private void preencherListaTags(String name)
	{
		il_opcServerTagList.AddItem(name);
		opc_da_items_list.Add(name);
		int id = 0;
		foreach (Node child in vBoxContainerTag.GetChildren())
		{
			if (child is HBoxContainer hBox)
			{
				foreach (Node childVbox in hBox.GetChildren())
				{
					if (childVbox is OptionButton optionButton)
					{
						optionButton.AddItem(name, id);
						id += 1;
					}
				}
			}
		}
	}

	private void _on_tag_selected(int index)
	{
		GD.Print("\n> [CommsConfig.cs] [_on_tag_selected()]");
		// Obter o nome do item selecionado
		GD.Print($"- Tag selecionada: {il_opcServerTagList.GetItemText(index)}");
		GD.Print($"- Valor: {ReadOpcItem(il_opcServerTagList.GetItemText(index))}");
	}

	private void OnOptionSelected(OptionButton optButton, long index)
	{
		// Obtendo o ID e o texto da opção selecionada
		int optButtonId = optButton.GetSelectedId();
		string selectedText = optButton.GetItemText((int)index);

		// GD.Print($"- optButtonId: {optButtonId}");
		// GD.Print($"- selectedText: {selectedText}");

		ObjetosCena.ObterObjetoPorId(optButtonId, CENA).Tag = selectedText;
		// switch (optButtonId)
		// {
		// 	case 0:
		// 		ObjetosCena.ObterObjetoPorId(optButtonId, CENA).Tag = selectedText;
		// 		break;
		// 	case 1:
		// 		ObjetosCena.ObterObjetoPorId(1, CENA).Tag = selectedText;
		// 		break;
		// 	case 2:
		// 		ObjetosCena.ObterObjetoPorId(2, CENA).Tag = selectedText;
		// 		break;
		// }
	}
	public static object ReadOpcItem(string tagName)
	{
		// GD.Print("\n> [CommsConfig.cs] [ReadOpcItem()]");
		try
		{
			// GD.Print("\n> [CommsConfig.cs] [ReadOpcItem()]");
			if (opcServer == null || !opcServer.IsConnected)
			{
				// GD.PrintErr("### CoomsConfig.cs - ReadOpcItem() - Servidor OPC não conectado!");
				return null;
			}
			else
			{
				// Criar um item OPC para leitura
				Opc.Da.Item item = new Opc.Da.Item { ItemName = tagName };

				// Ler diretamente do servidor para evitar cache
				Opc.Da.ItemValueResult[] results = opcServer.Read(new Opc.Da.Item[] { item });
				// GD.Print($"- tagName:{tagName}");
				// GD.Print($"- results:{results}");
				// GD.Print($"- results[0].Value:{results[0].Value}");
				// GD.Print($"- results[0].Quality:{results[0].Quality}");
				if (results[0].Value != null)
				{
					// GD.Print($"- results[0].Value.GetType():{results[0].Value.GetType()}");
					// GD.Print($"- Tag:{tagName} | Valor:{results[0].Value} | Tipo:{results[0].Value.GetType()}");
				}
				else
				{
					GD.Print($"- {tagName} NULL");
				}

				return results[0].Value;
			}

		}
		catch (Exception err)
		{
			GD.PrintErr("### CoomsConfig.cs - ReadOpcItem() - Erro ao ler a TAG");
			GD.PrintErr(err);
			return null;
		}
	}
	public static void WriteOpcItem(string tagName, bool value)
	{
		GD.Print("\n> [CommsConfig.cs] [WriteOpcItem()] Boolean");
		GD.Print($"- tagName: {tagName}");
		GD.Print($"- value: {value}");
		try
		{
			// GD.Print("\n> [CommsConfig.cs] [ReadOpcItem()]");
			if (opcServer == null || !opcServer.IsConnected)
			{
				GD.PrintErr("### CoomsConfig.cs - ReadOpcItem() - Servidor OPC não conectado!");
			}
			else
			{
				Opc.Da.Item OPC_WriteItem = Array.Find(opc_da_items_array, x => x.ItemName.Equals(tagName));
				GD.Print($"- OPC_WriteItem: {OPC_WriteItem}");
				Opc.Da.ItemValue[] writeValues = new Opc.Da.ItemValue[1];
				writeValues[0] = new Opc.Da.ItemValue(OPC_WriteItem);
				writeValues[0].Value = value;
				Opc.IdentifiedResult[] retValues = subscription.Write(writeValues);
			}
		}
		catch (Exception err)
		{
			GD.PrintErr("### CoomsConfig.cs - ReadOpcItem() - Erro ao ler a TAG");
			GD.PrintErr(err);
		}
	}
}
