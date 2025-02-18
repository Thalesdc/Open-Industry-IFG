using Godot;
using System;
using System.Collections.Generic;

public partial class ConveyorAssembly : Node3D
{
	#region Conveyors
	#region Conveyors / Update "Conveyors" node
	private void UpdateConveyors()
	{
		if (conveyors == null)
		{
			return;
		}

		LockConveyorsGroup();
		SyncConveyorsAngle();
		var conveyorLineLength = GetConveyorLineLength();
		ScaleConveyorLine(conveyors, conveyorLineLength);
	}

	protected virtual void LockConveyorsGroup() {
		// Lock Z position
		conveyors.Position = new Vector3(conveyors.Position.X, conveyors.Position.Y, 0f);
		// Lock X and Y rotation
		if (conveyors.Rotation.X > 0.001f || conveyors.Rotation.X < -0.001f || conveyors.Rotation.Y > 0.001f || conveyors.Rotation.Y < -0.001) {
			// This seems to mess up scale, but at least that's fixed on the next frame.
			conveyors.Rotation = new Vector3(0f, 0f, conveyors.Rotation.Z);
		}
	}

	/**
	 * Synchronize the angle of the conveyors with the assembly's ConveyorAngle property.
	 *
	 * If the property changes, the conveyors are rotated to match.
	 * If the conveyors are rotated manually, the property is updated.
	 * If both happen at the same time, the property wins.
	 */
	private void SyncConveyorsAngle() {
		Basis scale = Basis.Identity.Scaled(this.Basis.Scale);
		Basis scalePrev = Basis.Identity.Scaled(transformPrev.Basis.Scale);
		if (ConveyorAngle != conveyorAnglePrev) {
			Basis targetRot = new Basis(new Vector3(0, 0, 1), ConveyorAngle);
			conveyors.Basis = scale.Inverse() * targetRot;
		} else {
			float angle = (scale * conveyors.Basis).GetEuler().Z;
			float anglePrev = (scalePrev * conveyorsTransformPrev.Basis).GetEuler().Z;
			double angleDelta = Mathf.Abs(angle - anglePrev) % (2 * Math.PI);
			if (angleDelta > Math.PI / 360.0) {
				this.ConveyorAngle = (scale * conveyors.Basis).GetEuler().Z;
				NotifyPropertyListChanged();
			}
		};
	}
	#endregion Conveyors / Update "Conveyors" node

	#region Conveyors / ScaleConveyorLine
	/**
	 * Get the length of the conveyor line.
	 *
	 * If ConveyorAutomaticLength is enabled, this is the length required for the conveyor line, at its current angle, to span the assembly's x-axis one meter per unit of assembly x-scale.
	 *
	 * If ConveyorAutomaticLength is disabled, this is the sum of the lengths of all conveyors in the line.
	 * We assume that they're parallel and aligned end-to-end.
	 *
	 * @return The length of the conveyor line along its x-axis.
	 */
	private float GetConveyorLineLength() {
		if (conveyors == null) {
			return this.Scale.X;
		}
		if (ConveyorAutomaticLength) {
			var cos = Mathf.Cos(conveyors.Basis.GetEuler().Z);
			return this.Scale.X * 1 / (Mathf.Abs(cos) >= 0.01f ? cos : 0.01f);
		}
		// Add up the length of all conveyors.
		// Assume all conveyors are aligned end-to-end.
		var sum = 0f;
		foreach (Node child in conveyors.GetChildren()) {
			Node3D conveyor = child as Node3D;
			if (IsConveyor(conveyor)) {
				// Assume conveyor scale == length.
				sum += conveyor.Scale.X;
			}
		}
		return sum;
	}

	/**
	 * Scale all conveyor children of a given node.
	 *
	 * This would be a great place to implement proportional scaling and positioning of the conveyors,
	 * but currently, we just scale every conveyor to the length of the whole line and leave its position alone.
	 *
	 * @param conveyorLine The parent of the conveyors.
	 * @param conveyorLineLength The length of the conveyor line to scale to. Ignored if ConveyorAutomaticLength is false.
	 */
	private void ScaleConveyorLine(Node3D conveyorLine, float conveyorLineLength) {
		foreach (Node child in conveyorLine.GetChildren()) {
			Node3D child3d = child as Node3D;
			if (IsConveyor(child3d)) {
				SetEditableInstance(child3d, true);
				ScaleConveyor(child3d, conveyorLineLength);
			}
		}
	}

	private static bool IsConveyor(Node node) {
		return node as IConveyor != null || node as IBeltConveyor != null || node as IRollerConveyor != null;
	}

	protected virtual void ScaleConveyor(Node3D conveyor, float conveyorLength) {
		if (ConveyorAutomaticLength) {
			conveyor.Scale = new Vector3(conveyorLength, 1f, this.Scale.Z);
		} else {
			// Always scale width.
			conveyor.Scale = new Vector3(conveyor.Scale.X, conveyor.Scale.Y, this.Scale.Z);
		}
	}
	#endregion Conveyors / ScaleConveyorLine

	#region Conveyors / Conveyor Extents
	/**
	 * Get the extents of all conveyors in the assembly.
	 *
	 * The extents are the (front, rear) X positions of each conveyor.
	 * They're used to determine where to place side guards.
	 *
	 * @return A list of (front, rear) extents of each conveyor.
	 */
	private List<(float, float)> GetAllConveyorExtents() {
		// Assume straight assembly.
		// Assume all conveyors are in the same reference frame, straight, aligned end-to-end, and parallel to the x-axis of that reference frame.
		// We will use the `conveyors` node's children, but this would work for any list that meets the above assumptions.

		// Additional thought: it would be neat to return extents as Vector3s instead, that way we could take any arbitrary arrangement of conveyors. As long as the gaps could still cut into it, it could work.
		// This approach would also bring us closer to automatic leg stands per conveyor instead of per conveyor line.

		List<(float, float)> results = new();
		if (conveyors == null) {
			return results;
		}
		foreach (Node3D node3D in conveyors.GetChildren()) {
			if (IsConveyor(node3D)) {
				// Assume conveyor length equal to X scale.
				// (Don't account for the end caps; they reach beyond the extents.)
				// This assumption is convenient because SideGuards' end caps overreach the same amount.
				float length = node3D.Scale.X;
				float extentFront = node3D.Position.X - length / 2f;
				float extentRear = node3D.Position.X + length / 2f;
				results.Add((extentFront, extentRear));
			}
		}
		return results;
	}
	#endregion Conveyors / Conveyor Extents
	#endregion Conveyors
}
