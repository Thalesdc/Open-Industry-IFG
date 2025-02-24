using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class SceneComponents
{

    public static List<SceneComponents> sceneOneComponents { get; } = new List<SceneComponents>
    {
        new SceneComponents(0, "Conveyor", ""),
        new SceneComponents(1, "Conveyor2", ""),
        new SceneComponents(2, "BoxSpawner", ""),
        new SceneComponents(3, "DiffuseSensor", ""),
        new SceneComponents(4, "DiffuseSensor2", ""),
    };

    public static List<SceneComponents> sceneTwoComponents { get; } = new List<SceneComponents>
    {
        new SceneComponents(0, "Conveyor", ""),
        new SceneComponents(1, "Conveyor2", ""),
        new SceneComponents(2, "BoxSpawner", ""),
        new SceneComponents(3, "LaserSensor", ""),
    };

    // Propriedades da classe
    public int Id { get; set; }
    public string Name { get; set; }
    public string Tag { get; set; }

    // Construtor
    public SceneComponents(int id, string name, string tag)
    {
        Id = id;
        Name = name;
        Tag = tag;
    }

    public static SceneComponents GetComponentByName(string name, int scene)
    {
        switch (scene)
        {
            case 1: return sceneOneComponents.FirstOrDefault(f => f.Name.ToLower() == name.ToLower());
            case 2: return sceneTwoComponents.FirstOrDefault(f => f.Name.ToLower() == name.ToLower());
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
