using Godot;
using System;

public interface IConveyor
{
	public float Speed { get; set; }
	public Root Main { get; set; }
	public Transform3D GlobalTransform { get; set; }
	public Vector3 Scale { get; set; }
}
