using Godot;
using System;

public partial class ChainTransferBase : Node3D
{
	bool active = false;
	public bool Active
	{
		get
		{
			return active;
		}
		set
		{
			active = value;
			if (active) Up();
			else Down();
		}
	}
	
	public float Speed { get; set; }
	
	StaticBody3D containerBody;
	Node3D bas;
	Node3D container;
	Node3D chain;
	float childrenInitialY;
	float activePos = 0.095f;
	
	RigidBody3D rb;
	Vector3 origin;
	
	bool running = false;
	
	int chainScale = 32;
	int chainEndScale = 6;
	
	MeshInstance3D chainMesh;
	MeshInstance3D chainEndLMesh;
	MeshInstance3D chainEndRMesh;
	
	ShaderMaterial chainMaterial;
	ShaderMaterial chainEndLMaterial;
	ShaderMaterial chainEndRMaterial;
	
	double chainPosition = 0.0;
	double chainEndPosition = 0.0;
	
	ChainTransfer owner;
	
	void InitMesh(ref MeshInstance3D mesh, String path, ref ShaderMaterial material)
	{
		mesh = GetNode<MeshInstance3D>(path);
		mesh.Mesh = mesh.Mesh.Duplicate() as Mesh;
		material = mesh.Mesh.SurfaceGetMaterial(0).Duplicate() as ShaderMaterial;
		mesh.Mesh.SurfaceSetMaterial(0, material);
	}
	
	void SetChainPosition(ShaderMaterial material, double pos)
	{
		if (material != null && Speed != 0)
		{
			material.SetShaderParameter("ChainPosition", pos * Mathf.Sign(Speed));
		}
	}
	
	public override void _Ready()
	{
		containerBody = GetNode<StaticBody3D>("ContainerBody");
		bas = GetNode<Node3D>("Base");
		container = GetNode<Node3D>("Container");
		chain = GetNode<Node3D>("Chain");
		
		childrenInitialY = container.Position.Y;
		
		rb = GetNode<RigidBody3D>("Chain/RigidBody3D");
		origin = rb.Position;
		
		InitMesh(ref chainMesh, "Chain", ref chainMaterial);
		InitMesh(ref chainEndLMesh, "Chain/ChainL", ref chainEndLMaterial);
		InitMesh(ref chainEndRMesh, "Chain/ChainR", ref chainEndRMaterial);
		
		chainPosition = 0.0;
		chainEndPosition = 0.0;
		SetChainPosition(chainMaterial, 0);
		SetChainPosition(chainEndLMaterial, 0);
		SetChainPosition(chainEndRMaterial, 0);

		owner = GetParent() as ChainTransfer;
    }

	public override void _PhysicsProcess(double delta)
	{
		if (running)
		{
			var localLeft = rb.GlobalTransform.Basis.X.Normalized();
			var velocity = localLeft * (Speed / (Mathf.Round(owner.Scale.X * chainScale * 0.5f)));
			rb.LinearVelocity = velocity;
			rb.Position = origin;
			
			rb.Rotation = Vector3.Zero;
			rb.Scale = new Vector3(1, 1, 1);
			
			if (chainMaterial != null && owner != null)
			{
				chainPosition += (Mathf.Abs(Speed) / (Mathf.Round(owner.Scale.X * chainScale))) * delta;
				if (chainPosition > 1.0) {
					chainPosition = chainPosition - 1.0;
				}
				chainEndPosition += (Mathf.Abs(Speed) / chainEndScale) * delta;
				if (chainEndPosition > 1.0) {
					chainEndPosition = chainEndPosition - 1.0;
				}
				SetChainPosition(chainMaterial, chainPosition);
				SetChainPosition(chainEndLMaterial, chainEndPosition);
				SetChainPosition(chainEndRMaterial, chainEndPosition);
			}
		}
		
		if (chainMaterial != null && owner != null)
			chainMaterial.SetShaderParameter("Scale", owner.Scale.X * chainScale);
		
		ScaleChildren(bas);
		ScaleChildren(container);
		ScaleChildren(chain);
	}
	
	public void TurnOn()
	{
		running = true;
	}
	
	public void TurnOff()
	{
		running = false;
		
		chainPosition = 0.0;
		chainEndPosition = 0.0;
		SetChainPosition(chainMaterial, 0);
		SetChainPosition(chainEndLMaterial, 0);
		SetChainPosition(chainEndRMaterial, 0);
		
		rb.Position = Vector3.Zero;
		rb.Rotation = Vector3.Zero;
		rb.LinearVelocity = Vector3.Zero;
	}
	
	// Moves the chain up
	void Up()
	{
		Tween tween = GetTree().CreateTween().SetEase(0).SetParallel(); // Set EaseIn
		tween.TweenProperty(containerBody, "position", new Vector3(containerBody.Position.X, childrenInitialY + activePos, containerBody.Position.Z), 0.15f);
		tween.TweenProperty(container, "position", new Vector3(container.Position.X, childrenInitialY + activePos, container.Position.Z), 0.15f);
		tween.TweenProperty(chain, "position", new Vector3(chain.Position.X, childrenInitialY + activePos, chain.Position.Z), 0.15f);
	}
	
	// Moves the chain down
	void Down()
	{
		Tween tween = GetTree().CreateTween().SetEase(0).SetParallel(); // Set EaseIn
		tween.TweenProperty(containerBody, "position", new Vector3(containerBody.Position.X, childrenInitialY, containerBody.Position.Z), 0.15f);
		tween.TweenProperty(container, "position", new Vector3(container.Position.X, childrenInitialY, container.Position.Z), 0.15f);
		tween.TweenProperty(chain, "position", new Vector3(chain.Position.X, childrenInitialY, chain.Position.Z), 0.15f);
	}
	
	void ScaleChildren(Node3D nodesContainer)
	{
		if (owner == null) return;
		foreach (Node3D node in nodesContainer.GetChildren())
		{
			if (node is RigidBody3D) continue;
			node.Scale = new Vector3(1 / owner.Scale.X, 1, 1);
		}
	}
}
