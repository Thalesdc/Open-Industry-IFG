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
	private VBoxContainer vBoxContainerTagsInput;
	private VBoxContainer vBoxContainerTagsOutput;


	List<string> opc_da_items_list = new List<string>();

	static Opc.Da.Server opcServer;
	static private Opc.Da.Subscription subscription;
	static Opc.Da.Item[] opc_da_items_array;

	public Root Main { get; set; }

	int currentScene;
	List<SceneComponents> sceneComponents;
	public override void _Ready()
	{
		try
		{
			GD.Print("\n> [CommsConfig.cs] [_Ready()]");
			Main = GetTree().CurrentScene as Root;
			currentScene = Main.currentScene;

			// Ocultando itens antes da conexão com o servidor OPC
			GetNode<ScrollContainer>("MarginContainer/HBoxContainer/VBoxContainer1/ScrollContainer").Visible = false;
			GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer1/RichTextTagsFound").Visible = false;
			GetNode<VBoxContainer>("MarginContainer/HBoxContainer/VBoxContainerMap").Visible = false;

			// Lista de servidores
			il_opcServerList = GetNode<ItemList>("MarginContainer/HBoxContainer/VBoxContainer1/ServerList");
			il_opcServerList.Visible = false;

			// Lista de tags do servidor OPC
			il_opcServerTagList = GetNode<ItemList>("MarginContainer/HBoxContainer/VBoxContainer1/ScrollContainer/VBoxContainer/OpcServerTagList");

			switch (currentScene)
			{
				case 1: sceneComponents = SceneComponents.sceneOneComponents; break;
				case 2: sceneComponents = SceneComponents.sceneTwoComponents; break;
				default: sceneComponents = null; break;
			}

			vBoxContainerTagsInput = GetNode<VBoxContainer>("MarginContainer/HBoxContainer/VBoxContainerMap/ScrollContainerTagsInput/VBoxContainerTagsInput");
			vBoxContainerTagsOutput = GetNode<VBoxContainer>("MarginContainer/HBoxContainer/VBoxContainerMap/ScrollContainerTagsOutputs/VBoxContainerTagsOutput");

			foreach (var component in sceneComponents)
			{
				HBoxContainer hbox = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ShrinkBegin };
				hbox.AddChild(new Label
				{
					Text = component.Name,
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

				if(component.Type == "input")
				{
					vBoxContainerTagsInput.AddChild(hbox);
				} else
				{
					vBoxContainerTagsOutput.AddChild(hbox);
				}
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
			if (opcServer == null || !opcServer.IsConnected)
			{
				//Find opc server on local machine
				Opc.IDiscovery discovery = new OpcCom.ServerEnumerator();
				il_opcServerList.Clear();
				il_opcServerTagList.Clear();
				Opc.Server[] opcServerList = discovery.GetAvailableServers(Opc.Specification.COM_DA_30);

				foreach (Opc.Server item in opcServerList)
				{
					il_opcServerList.AddItem(item.Name);
					globalOpcServerList.Add((Opc.Da.Server)item);
				}

				il_opcServerList.Visible = true;

			} else
			{
				GD.Print("\n> [CommsConfig.cs] [_on_btn_opc_pressed()]");
				GD.PrintErr("> Servidor já conectado!");
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

			// Exibindo itens antes da conexão com o servidor OPC
			GetNode<ScrollContainer>("MarginContainer/HBoxContainer/VBoxContainer1/ScrollContainer").Visible = true;
			GetNode<RichTextLabel>("MarginContainer/HBoxContainer/VBoxContainer1/RichTextTagsFound").Visible = true;
			GetNode<VBoxContainer>("MarginContainer/HBoxContainer/VBoxContainerMap").Visible = true;

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
		foreach (Node child in vBoxContainerTagsInput.GetChildren())
		{
			if (child is HBoxContainer hBox)
			{
				foreach (Node childVbox in hBox.GetChildren())
				{
					if (childVbox is OptionButton optionButton)
					{
						if (optionButton.ItemCount == 0)
						{
							optionButton.AddItem(null, -1);
							optionButton.Select(-1);
						}
						optionButton.AddItem(name, id);
						id += 1;
					}
				}
			}
		}
		foreach (Node child in vBoxContainerTagsOutput.GetChildren())
		{
			if (child is HBoxContainer hBox)
			{
				foreach (Node childVbox in hBox.GetChildren())
				{
					if (childVbox is OptionButton optionButton)
					{
						if (optionButton.ItemCount == 0)
						{
							optionButton.AddItem(null, -1);
							optionButton.Select(-1);
						}
						optionButton.AddItem(name, id);
						id += 1;
					}
				}
			}
		}
	}

	private void _on_tag_selected(int index)
	{
		// GD.Print("\n> [CommsConfig.cs] [_on_tag_selected()]");
		// GD.Print($"- Tag selecionada: {il_opcServerTagList.GetItemText(index)}");
		// GD.Print($"- Valor: {ReadOpcItem(il_opcServerTagList.GetItemText(index))}");
	}

	private void OnOptionSelected(OptionButton optButton, long index)
	{
		// Obtendo o ID e o texto da opção selecionada
		int optButtonId = optButton.GetSelectedId();
		string selectedText = optButton.GetItemText((int)index);

		SceneComponents.GetComponentById(optButtonId, currentScene).Tag = selectedText;
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

				if (results[0].Value != null)
				{
					// GD.Print($"- results[0].Value.GetType():{results[0].Value.GetType()}");
					// GD.Print($"- READ Tag:{tagName} | Valor:{results[0].Value} | Tipo:{results[0].Value.GetType()}");
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
		try
		{
			// GD.Print("\n> [CommsConfig.cs] [WriteOpcItem()]");
			// GD.Print($"\n> WRITE Tag:{tagName} | Valor:{value}");

			Opc.Da.Item opcItem = Array.Find(opc_da_items_array, x => x.ItemName.Equals(tagName));
			Opc.Da.ItemValue opcItemValue = new Opc.Da.ItemValue(opcItem)
			{
				Value = value
			};
			Opc.IdentifiedResult[] results = opcServer.Write(new Opc.Da.ItemValue[] { opcItemValue });
		}
		catch (Exception err)
		{
			GD.PrintErr("### CoomsConfig.cs - WriteOpcItem() bool");
			GD.PrintErr(err);
		}
	}

	public static void WriteOpcItem(string tagName, float value)
	{
		try
		{
			Opc.Da.Item opcItem = Array.Find(opc_da_items_array, x => x.ItemName.Equals(tagName));
			Opc.Da.ItemValue opcItemValue = new Opc.Da.ItemValue(opcItem)
			{
				Value = value
			};
			Opc.IdentifiedResult[] results = opcServer.Write(new Opc.Da.ItemValue[] { opcItemValue });
		}
		catch (Exception err)
		{
			GD.PrintErr("### CoomsConfig.cs - WriteOpcItem() float");
			GD.PrintErr(err);
		}
	}
}
