using Godot;
using System;

public partial class LegBar : MeshInstance3D
{
	ShaderMaterial shaderMaterial;
	ConveyorLeg owner;
	
	public override void _Ready()
	{
		Mesh = Mesh.Duplicate() as Mesh;
		shaderMaterial = Mesh.SurfaceGetMaterial(0).Duplicate() as ShaderMaterial;
		Mesh.SurfaceSetMaterial(0, shaderMaterial);
		
		owner = Owner as ConveyorLeg;
	}

	public override void _Process(double delta)
	{
		if (owner != null)
			shaderMaterial.SetShaderParameter("Scale", owner.Scale.Z);
	}
}
