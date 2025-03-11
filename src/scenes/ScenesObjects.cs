using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class SceneComponents
{

    public static List<SceneComponents> sceneOneComponents { get; } = new List<SceneComponents>
    {
        new SceneComponents(0, "Conveyor", "Esteira 1", "", "input"),
        new SceneComponents(1, "Conveyor2", "Esteira 2", "", "input"),
        new SceneComponents(2, "BoxSpawner", "Gerador Caixas", "", "input"),
        new SceneComponents(3, "PushButton", "Botão Início", "", "input"),
        new SceneComponents(4, "PushButton2", "Botão Fim", "", "input"),
        // This order is inportant to link wich tag was selected for each component
        // In the screen, first is listed inputs, then outputs
        new SceneComponents(5, "DiffuseSensor", "Sensor 1", "", "output"),
        new SceneComponents(6, "DiffuseSensor2", "Sensor 2", "", "output"),
    };

    public static List<SceneComponents> sceneTwoComponents { get; } = new List<SceneComponents>
    {
        new SceneComponents(0, "Conveyor", "Conveyor_Name", "", "input"),
        new SceneComponents(1, "Conveyor2", "Conveyor2_Name", "", "input"),
        new SceneComponents(2, "BoxSpawner", "BoxSpawner_Name", "", "input"),
        new SceneComponents(3, "LaserSensor", "LaserSensor_Name", "", "input"),
    };

    // Propriedades da classe
    public int Id { get; set; }
    public string Key { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; }
    public string Type { get; set; }
    // Construtor
    public SceneComponents(int id, string key, string name, string tag, string type)
    {
        Id = id;
        Key = key;
        Name = name;
        Tag = tag;
        Type = type;
    }

    // public static SceneComponents GetComponentByName(string name, int scene)
    // {
    //     switch (scene)
    //     {
    //         case 1: return sceneOneComponents.FirstOrDefault(f => f.Name.ToLower() == name.ToLower());
    //         case 2: return sceneTwoComponents.FirstOrDefault(f => f.Name.ToLower() == name.ToLower());
    //         default: return null;
    //     }
    // }
    public static SceneComponents GetComponentByKey(string key, int scene)
    {
        switch (scene)
        {
            case 1: return sceneOneComponents.FirstOrDefault(f => f.Key.ToLower() == key.ToLower());
            case 2: return sceneTwoComponents.FirstOrDefault(f => f.Key.ToLower() == key.ToLower());
            default: return null;
        }
    }

    public static SceneComponents GetComponentById(int id, int scene)
    {
        switch (scene)
        {
            case 1: return sceneOneComponents.FirstOrDefault(f => f.Id == id);
            case 2: return sceneTwoComponents.FirstOrDefault(f => f.Id == id);
            default: return null;
        }
    }
}
