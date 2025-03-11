using Godot;
using System;

public partial class Roller : Node3D
{	
	public float speed = 1.0f;
	MeshInstance3D meshInstance;
	RigidBody3D rigidBody;
	Root Main;

	public override void _Ready()
	{
		meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");
		rigidBody = GetNode<RigidBody3D>("RigidBody3D");
	}
	
	public override void _Process(double delta)
	{

		Main = GetTree().CurrentScene as Root;

		if (Main == null)
		{
			return;
		}

		if (Main.Start)
		{
			meshInstance.RotateZ(speed * MathF.PI * 2f * (float)delta);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 localFront = GlobalTransform.Basis.Z.Normalized();
		rigidBody.AngularVelocity = localFront * speed * MathF.PI * 2;
		
		rigidBody.Position = Vector3.Zero;
		rigidBody.Rotation = Vector3.Zero;
		rigidBody.Scale = new Vector3(1, 1, 1);
	}
}
