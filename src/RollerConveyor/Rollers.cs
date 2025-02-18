using Godot;

public partial class Rollers : Node3D
{
	[Export]
	PackedScene rollerScene;
	
	float rollersDistance = 0.33f;
		
	RollerConveyor owner;

	int initialForeignRollerCount;

	public void ChangeScale(float scale)
	{
		int roundedScale = Mathf.RoundToInt(scale / rollersDistance) + 1;
		int rollerCount = GetChildCount();
		int desiredRollerCount = roundedScale - 2;

		int difference = desiredRollerCount - rollerCount;
		int foreignRollersMissing = initialForeignRollerCount - rollerCount;

		if (difference > 0) 
		{
			for (int i = 0; i < difference; i++)
			{
				bool foreign = i < foreignRollersMissing;
				SpawnRoller(foreign);
			}
		}
		else if (difference < 0) 
		{
			for (int i = 1; i <= -difference; i++)
			{
				GetChild<Roller>(GetChildCount() - i).QueueFree();
			}
		}
	}


	public override void _Ready()
	{
		owner = GetParent() as RollerConveyor;

		initialForeignRollerCount = GetForeignRollerCount();

		FixRollers();
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (owner != null)
		{
			Scale = new Vector3(1 / owner.Scale.X, 1, 1);
		}
	}

		void SpawnRoller(bool foreign = false)
	{
		if (GetParent() == null || owner == null) return;
		Roller roller = rollerScene.Instantiate() as Roller;
		AddChild(roller, forceReadableName: true);
		roller.Owner = foreign ? owner : GetTree().GetEditedSceneRoot();
		roller.Position = new Vector3(rollersDistance * GetChildCount(), 0, 0);
		roller.speed = owner.Speed;
		roller.RotationDegrees = new Vector3(roller.RotationDegrees.X, owner.SkewAngle, roller.RotationDegrees.Z);
		FixRollers();
	}


	void FixRollers()
	{
		((Roller)GetChild(0)).Position = new Vector3(rollersDistance, 0, 0);
	}

	int GetForeignRollerCount()
	{
		int count = 0;
		Node editedScene = GetTree().GetEditedSceneRoot();
		foreach (Node child in GetChildren())
		{
			if (child.Owner == editedScene)
			{
				break;
			}
			count++;
		}
		return count;
	}
}
