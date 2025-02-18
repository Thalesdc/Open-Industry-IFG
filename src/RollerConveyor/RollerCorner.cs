using Godot;
using System;

public partial class RollerCorner : Node3D
{	
	public float speed = 1.0f;
	
	MeshInstance3D meshInstance;
	RigidBody3D rigidBody;
	CollisionShape3D collisionShape;
	CurvedRollerConveyor owner;
	Root Main;
	
	public override void _Ready()
	{
		meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");
		rigidBody = GetNode<RigidBody3D>("RigidBody3D");
		collisionShape = GetNode<CollisionShape3D>("RigidBody3D/CollisionShape3D");
		owner = Owner as CurvedRollerConveyor;
	}

	public override void _Process(double delta)
	{
		Main = GetParent().GetTree().EditedSceneRoot as Root;

		if (Main == null)
		{
			return;
		}

		if (Main.Start)
		{
			meshInstance.RotateZ(speed * MathF.PI * (float)delta);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (owner != null)
			Scale = new Vector3(1 / owner.Scale.X, 1, 1);
			
		Vector3 localFront = GlobalTransform.Basis.Z.Normalized();
		rigidBody.AngularVelocity = localFront * speed * MathF.PI * 4f;
		
		rigidBody.Position = Vector3.Zero;
		rigidBody.Rotation = Vector3.Zero;
		rigidBody.Scale = new Vector3(1, 1, 1);
	}
	
	public void SetSpeed(float new_speed)
	{
		speed = new_speed;
	}
	
	public void SetActive(bool active)
	{
		collisionShape.SetDeferred("disabled", !active);
	}
}
