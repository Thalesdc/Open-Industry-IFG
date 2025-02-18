using Godot;

public partial class SideGuard : MeshInstance3D
{
	bool leftEnd = false;
	[Export]
	public bool LeftEnd
	{
		get
		{
			return leftEnd;
		}
		set
		{
			leftEnd = value;
			if (lEnd != null)
				lEnd.Visible = value;
		}
	}
	bool rightEnd = false;
	[Export]
	public bool RightEnd
	{
		get
		{
			return rightEnd;
		}
		set
		{
			rightEnd = value;
			if (rEnd != null)
				rEnd.Visible = value;
		}
	}

	MeshInstance3D meshInstance;
	ShaderMaterial metalMaterial;

	Node3D lEnd;
	Node3D rEnd;

	public override void _Ready()
	{
		meshInstance = this;
		meshInstance.Mesh = meshInstance.Mesh.Duplicate() as Mesh;
		metalMaterial = meshInstance.Mesh.SurfaceGetMaterial(0).Duplicate() as ShaderMaterial;
		meshInstance.Mesh.SurfaceSetMaterial(0, metalMaterial);

		lEnd = GetNodeOrNull<Node3D>("Ends/SideGuardEndL");
		rEnd = GetNodeOrNull<Node3D>("Ends/SideGuardEndR");

		lEnd.Visible = leftEnd;
		rEnd.Visible = rightEnd;
	}

	public override void _Process(double delta)
	{
		if (metalMaterial != null)
			metalMaterial.SetShaderParameter("Scale", Scale.X);

		lEnd.Scale = new Vector3(1 / Scale.X, 1, 1);
		rEnd.Scale = new Vector3(1 / Scale.X, 1, 1);
	}
}
