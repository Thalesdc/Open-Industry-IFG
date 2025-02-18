using Godot;
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc;
using OpcRcw.Dx;

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


	Opc.Da.Item[] opc_da_tags;

	static Opc.Da.Server opcServer;

	readonly System.Collections.Generic.Dictionary<Guid, string> opc_tags = new();

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
			// Obter o nome do item selecionado
			string selectedItem = il_opcServerList.GetItemText(index);
			// GD.Print($"Item selecionado: {selectedItem}");

			// TODO: Validar se item já está na lista
			opcServer = globalOpcServerList.FirstOrDefault(server =>
			server.Name.Equals(selectedItem));

			//Connect server
			opcServer.Connect();

			var globalVariables = GetNodeOrNull("/root/GlobalVariables");
			// GD.Print($"CommsConfig.cs _Ready() globalVariables: {globalVariables.Get("opc_da_connected")}");
			globalVariables.Set("opc_da_connected", true);
			GD.Print($"CommsConfig.cs _Ready() globalVariables: {globalVariables.Get("opc_da_connected")}");

			//Browse all items
			BrowsAllElement("", ref opcServer);
		}
		catch (Exception ex)
		{
			GD.Print(ex);
		}
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
		// Obter o nome do item selecionado
		GD.Print($"Item selecionado: {il_opcServerTagList.GetItemText(index)}");
	}

	private void OnOptionSelected(OptionButton optButton, long index)
	{
		// Obtendo o ID e o texto da opção selecionada
		int optButtonId = optButton.GetSelectedId();
		string selectedText = optButton.GetItemText((int)index);

		object value = ReadOpcItem(selectedText);

		switch (optButtonId)
		{
			case 0:
				ObjetosCena.ObterObjetoPorId(0, CENA).Tag = selectedText;
				break;
			case 1:
				ObjetosCena.ObterObjetoPorId(1, CENA).Tag = selectedText;
				break;
		}
	}

	// Lê um único item OPC DA
	public static object ReadOpcItem(string tagName)
	{
		try
		{
			GD.Print("\n> [CommsConfig.cs] [ReadOpcItem()]");
			if (opcServer == null || !opcServer.IsConnected)
			{
				GD.PrintErr("### CoomsConfig.cs - ReadOpcItem() - Servidor OPC não conectado!");
				return null;
			}
			else
			{
				// Criar um item OPC para leitura
				Opc.Da.Item item = new Opc.Da.Item { ItemName = tagName };

				// Ler diretamente do servidor para evitar cache
				Opc.Da.ItemValueResult[] results = opcServer.Read(new Opc.Da.Item[] { item });
				GD.Print($"- tagName: {tagName}");
				GD.Print($"- results: {results}");
				GD.Print($"- results[0].Value: {results[0].Value}");
				GD.Print($"- {tagName}\n>{results[0].Value.GetType()}\n>{results[0].Value}");

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
}
