using Godot;
using System;

public partial class LegBars : Node3D
{
	[Export]
	PackedScene legsBarScene;
	
	float barsDistance = 1.0f;
	float parentScale = 1.0f;
	[Export]
	public float ParentScale
	{
		get
		{
			return parentScale;
		}
		set
		{
			int roundedScale = Mathf.FloorToInt(value) + 1;
			int barsCount = GetChildCount();
			
			for (; roundedScale - 1 > barsCount && roundedScale != 0; barsCount++)
			{
				SpawnBar();
			}
			for (; barsCount > roundedScale - 1 && barsCount > 1; barsCount--)
			{
				RemoveBar();
			}
			
			parentScale = value;
		}
	}
	
	ConveyorLeg owner;
	
	public override void _Ready()
	{
		owner = Owner as ConveyorLeg;
		FixBars();

		ParentScale = parentScale;
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (owner != null)
		{
			Scale = new Vector3(1 / owner.Scale.X, 1 / owner.Scale.Y, 1);
		}
	}
	
	void SpawnBar()
	{
		Node3D legsBar = legsBarScene.Instantiate() as Node3D;
		AddChild(legsBar, forceReadableName: true);
		legsBar.Owner = this;
		legsBar.Position = new Vector3(0, barsDistance * GetChildCount(), 0);
		FixBars();
	}
	
	void RemoveBar()
	{
		GetChild(GetChildCount() - 1).QueueFree();
	}

	void FixBars()
	{
		if (GetParent() == null) return;
		((Node3D)GetChild(0)).Owner = GetParent();
		((Node3D)GetChild(0)).Position = new Vector3(0, barsDistance, 0);
	}
}
