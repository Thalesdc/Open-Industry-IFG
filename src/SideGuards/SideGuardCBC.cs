using Godot;

public partial class SideGuardCBC : MeshInstance3D
{
	MeshInstance3D meshInstance;
	ShaderMaterial shaderMaterial;

	public override void _Ready()
	{
		meshInstance = this;
		meshInstance.Mesh = meshInstance.Mesh.Duplicate() as Mesh;
		shaderMaterial = meshInstance.Mesh.SurfaceGetMaterial(0).Duplicate() as ShaderMaterial;
		meshInstance.Mesh.SurfaceSetMaterial(0, shaderMaterial);
	}

	public override void _PhysicsProcess(double delta)
	{
		Scale = new Vector3(Scale.X, 1, Scale.X);

		if (Scale.X > 0.5f)
		{
			if (shaderMaterial != null)
				shaderMaterial.SetShaderParameter("Scale", Scale.X);
		}
	}
}
