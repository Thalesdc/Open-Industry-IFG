using Godot;
using System;

public partial class ConveyorEnd : Node3D
{
	double beltPosition = 0.0;
	bool running = false;
	
	RigidBody3D rigidBody;
	MeshInstance3D mesh;
	public ShaderMaterial beltMaterial;
	Shader beltShader;
	
	IConveyor owner;
	Root main;
	
	public override void _Ready()
	{
		rigidBody = GetNode<RigidBody3D>("RigidBody3D");
		
		mesh = GetNode<MeshInstance3D>("MeshInstance3D");
		mesh.Mesh = mesh.Mesh.Duplicate() as Mesh;
		beltMaterial = mesh.Mesh.SurfaceGetMaterial(0).Duplicate() as ShaderMaterial;
		mesh.Mesh.SurfaceSetMaterial(0, beltMaterial);
		beltShader = beltMaterial.Shader.Duplicate() as Shader;
		beltMaterial.Shader = beltShader;
		
		owner = Owner as IConveyor;
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (owner != null)
		{
			RemakeMesh(owner);
			
			if (owner.Main == null) return;
		
			if (owner.Main.Start)
			{
				Vector3 localFront = -GlobalTransform.Basis.Z.Normalized();
				beltPosition += owner.Speed * delta;
				if (beltPosition >= 1.0)
					beltPosition = 0.0;
				rigidBody.AngularVelocity = -localFront * owner.Speed * MathF.PI;
			}
			else
			{
				beltPosition = 0; // Remove from here when signals are fixed.
				rigidBody.Rotation = Vector3.Zero;
				rigidBody.AngularVelocity = Vector3.Zero;
			}
			
			rigidBody.Position = Vector3.Zero;
			rigidBody.Scale = new Vector3(1, 1, 1);
			((ShaderMaterial)beltMaterial).SetShaderParameter("BeltPosition", beltPosition * Mathf.Sign(-owner.Speed));
		}
	}
	
	public void RemakeMesh(IConveyor conveyor)
	{
		Scale = new Vector3(1 / conveyor.Scale.X, 1, 1);
		if (conveyor.Speed != 0)
			((ShaderMaterial)beltMaterial).SetShaderParameter("Scale", Mathf.Sign(conveyor.Speed));
	}
}
