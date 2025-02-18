using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class ObjetosCena
{


    // Lista fixa de frutas já inicializada
    public static List<ObjetosCena> objetosFixosCena1 { get; } = new List<ObjetosCena>
    {
        new ObjetosCena(0, "Esteira", ""),
        new ObjetosCena(1, "GeradorCaixa", ""),
        new ObjetosCena(2, "Sensor", ""),
    };

    // Propriedades da classe
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Tag { get; set; }

    // Construtor
    public ObjetosCena(int id, string nome, string tag)
    {
        Id = id;
        Nome = nome;
        Tag = tag;
    }

    public static ObjetosCena ObterObjetoPorNome(string nome, int cena)
    {
        if(cena == 1)
        {
            return objetosFixosCena1.FirstOrDefault(f => f.Nome.ToLower() == nome.ToLower());
        }
        return null;
    }

    public static ObjetosCena ObterObjetoPorId(int id, int cena)
    {
        if (cena == 1)
        {
            return objetosFixosCena1.FirstOrDefault(f => f.Id == id);
        }
        return null;
    }

    // public Dictionary<int, string> objetosCena { get; set; } = new Dictionary<int, string>
    // {
    //     { 0, "" }, // Esteira
    //     { 1, "" }, // GeradorCaixa
    // };

}
