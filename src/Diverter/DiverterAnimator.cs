using Godot;
using System;

public partial class DiverterAnimator : Node3D
{
	enum LightColor
	{
		Red = 3,
		Green = 4
	}
	
	MeshInstance3D pusherMeshInstance;
	StandardMaterial3D redLightMaterial;
	StandardMaterial3D greenLightMaterial;
	
	MeshInstance3D part1;
	Vector3 part1StartPos;
	float part1MaximumZPos = 0.32f;
	
	MeshInstance3D part2;
	Vector3 part2StartPos;
	float part2MaximumZPos = 0.65f;
	
	RigidBody3D partEnd;
	Vector3 partEndStartPos;
	float partEndMaximumZPos = 1.0f;
	
	bool firing = false;

	public override void _Ready()
	{
		pusherMeshInstance = GetNode<MeshInstance3D>("Pusher");
		pusherMeshInstance.Mesh = pusherMeshInstance.Mesh.Duplicate() as Mesh;
		
		redLightMaterial = pusherMeshInstance.Mesh.SurfaceGetMaterial(3).Duplicate() as StandardMaterial3D;
		greenLightMaterial = pusherMeshInstance.Mesh.SurfaceGetMaterial(4).Duplicate() as StandardMaterial3D;
		
		pusherMeshInstance.Mesh.SurfaceSetMaterial(3, redLightMaterial);
		pusherMeshInstance.Mesh.SurfaceSetMaterial(4, greenLightMaterial);
		
		part1 = GetNode<MeshInstance3D>("Pusher/part1");
		part1StartPos = part1.Position;
		
		part2 = GetNode<MeshInstance3D>("Pusher/part2");
		part2StartPos = part2.Position;
		
		partEnd = GetNode<RigidBody3D>("Pusher/PartEnd");
		partEndStartPos = partEnd.Position;
	}
	
	void SetLampLight(LightColor lightColor, bool enabled)
	{
		StandardMaterial3D currentMaterial = pusherMeshInstance.Mesh.SurfaceGetMaterial((int) lightColor) as StandardMaterial3D;
		if (enabled)
			currentMaterial.EmissionEnergyMultiplier = 1.0f;
		else
			currentMaterial.EmissionEnergyMultiplier = 0.0f;
	}
	
	void Push(float time, float distance)
	{
		SetLampLight(LightColor.Green, true);
		Tween tween = GetTree().CreateTween();
		tween.Parallel().TweenProperty(part1, "position", part1StartPos + Vector3.Forward * part1MaximumZPos * distance, time);
		tween.Parallel().TweenProperty(part2, "position", part2StartPos + Vector3.Forward * part2MaximumZPos * distance, time);
		tween.Parallel().TweenProperty(partEnd, "position", partEndStartPos + Vector3.Forward * partEndMaximumZPos * distance, time);
		tween.TweenCallback(Callable.From(Return));
		tween.Parallel().TweenProperty(part1, "position", part1StartPos, time);
		tween.Parallel().TweenProperty(part2, "position", part2StartPos, time);
		tween.Parallel().TweenProperty(partEnd, "position", partEndStartPos, time);
		tween.TweenCallback(Callable.From(Finish));
	}
	
	void Return()
	{
		SetLampLight(LightColor.Green, false);
		SetLampLight(LightColor.Red, true);
	}
	
	void Finish()
	{
		SetLampLight(LightColor.Red, false);
		firing = false;
	}
	
	public void Fire(float time, float distance)
	{
		if (!firing)
		{
			firing = true;
			Push(time, distance);
		}
	}
	
	public void Disable()
	{
		Finish();
	}
}
