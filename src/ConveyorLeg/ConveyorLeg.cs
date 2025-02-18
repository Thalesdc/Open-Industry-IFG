using Godot;
using System;

public partial class ConveyorLeg : Node3D
{
	float grabsRotation = 0.0f;
	[Export(PropertyHint.Range, "-60,60,0.1")]
	public float GrabsRotation
	{
		get
		{
			return grabsRotation;
		}
		set
		{
			grabsRotation = value;
		}
	}

	float nodeScaleY = 1.0f;
	float nodeScaleZ = 1.0f;

	MeshInstance3D grab1;
	MeshInstance3D grab2;

	MeshInstance3D top;

	MeshInstance3D legsSidesMesh1;
	MeshInstance3D legsSidesMesh2;
	ShaderMaterial legsSidesMaterial;
	LegBars legsBars;
	Node3D ends;

	public override void _Ready()
	{
		grab1 = GetNode<MeshInstance3D>("Ends/LegsTop1/LegsGrab1");
		grab2 = GetNode<MeshInstance3D>("Ends/LegsTop2/LegsGrab2");

		legsSidesMesh1 = GetNode<MeshInstance3D>("Sides/LegsSide1");
		legsSidesMesh2 = GetNode<MeshInstance3D>("Sides/LegsSide2");
		legsSidesMesh1.Mesh = legsSidesMesh1.Mesh.Duplicate() as Mesh;
		legsSidesMesh2.Mesh = legsSidesMesh1.Mesh;
		legsSidesMaterial = legsSidesMesh1.Mesh.SurfaceGetMaterial(0) as ShaderMaterial;
		legsSidesMesh1.Mesh.SurfaceSetMaterial(0, legsSidesMaterial);
		legsBars = GetNode<LegBars>("LegsBars");
		ends = GetNode<Node3D>("Ends");
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Scale.Y >= 1.0f)
			nodeScaleY = Scale.Y;

		nodeScaleZ = Scale.Z;

		Scale = new Vector3(1, nodeScaleY, nodeScaleZ);

		if (legsSidesMaterial != null)
			legsSidesMaterial.SetShaderParameter("Scale", Scale.Y);

		if (legsBars != null)
			legsBars.ParentScale = Scale.Y;

		foreach (Node3D end in ends.GetChildren())
		{
			end.Scale = new Vector3(1 / Scale.X, 1 / Scale.Y, 1 / Scale.Z);
		}

		legsSidesMesh1.Scale = new Vector3(1 / Scale.X, 1, 1 / Scale.Z);
		legsSidesMesh2.Scale = new Vector3(1 / Scale.X, 1, 1 / Scale.Z);

		grab1.GlobalRotationDegrees = new Vector3(0, GlobalRotationDegrees.Y, grabsRotation);
		grab2.GlobalRotationDegrees = new Vector3(0, GlobalRotationDegrees.Y + 180, -grabsRotation);

		grab1.Scale = Vector3.One;
		grab2.Scale = Vector3.One;
	}
}
