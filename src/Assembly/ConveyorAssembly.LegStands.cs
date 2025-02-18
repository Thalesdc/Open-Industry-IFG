using Godot;
using System;
using System.Diagnostics;

public partial class ConveyorAssembly : Node3D
{
	#region Leg Stands
	#region Leg Stands / Conveyor coverage extents
	private void UpdateLegStandCoverage() {
		(legStandCoverageMinPrev, legStandCoverageMaxPrev) = (legStandCoverageMin, legStandCoverageMax);
		(legStandCoverageMin, legStandCoverageMax) = GetLegStandCoverage();
	}

	protected virtual (float, float) GetLegStandCoverage() {
		if (legStands == null || conveyors == null) {
			return (0f, 0f);
		}
		float min = float.MaxValue;
		float max = float.MinValue;
		foreach (Node child in conveyors.GetChildren()) {
			Node3D conveyor = child as Node3D;
			if (IsConveyor(conveyor)) {
				// Conveyor's Transform in the legStands space.
				Transform3D localConveyorTransform = legStands.Transform.AffineInverse() * conveyors.Transform * conveyor.Transform;

				// Extent and offset positions in unscaled conveyor space
				Vector3 conveyorExtentFront = new Vector3(-Mathf.Abs(localConveyorTransform.Basis.Scale.X * 0.5f), 0f, 0f);
				Vector3 conveyorExtentRear = new Vector3(Mathf.Abs(localConveyorTransform.Basis.Scale.X * 0.5f), 0f, 0f);

				Vector3 marginOffsetFront = new Vector3(AutoLegStandsMarginEnds, 0f, 0f);
				Vector3 marginOffsetRear = new Vector3(-AutoLegStandsMarginEnds, 0f, 0f);

				// The tip of the leg stand has a rotating grab model that isn't counted towards its height.
				// Because the grab will rotate towards the conveyor, we account for its reach here.
				Vector3 grabOffset = new Vector3(0f, -AutoLegStandsModelGrabsOffset, 0f);

				// Final grab points in legStands space
				Vector3 legGrabPointFront = localConveyorTransform.Orthonormalized() * (conveyorExtentFront + marginOffsetFront + grabOffset);
				Vector3 legGrabPointRear = localConveyorTransform.Orthonormalized() * (conveyorExtentRear + marginOffsetRear + grabOffset);

				// Update min and max.
				min = Mathf.Min(min, Mathf.Min(legGrabPointRear.X, legGrabPointFront.X));
				max = Mathf.Max(max, Mathf.Max(legGrabPointRear.X, legGrabPointFront.X));
			}
		}
		return (min, max);
	}
	#endregion Leg Stands / Conveyor coverage extents

	#region Leg Stands / Update "LegStands" node
	private void UpdateLegStands()
	{
		if (legStands == null)
		{
			return;
		}

		LockLegStandsGroup();
		SyncLegStandsOffsets();

		// If the leg stand scene changes, we need to regenerate everything.
		if (AutoLegStandsModelScene != autoLegStandsModelScenePrev) {
			DeleteAllAutoLegStands();
		}

		SnapAllLegStandsToPath();

		var autoLegStandsUpdateIsNeeded = AutoLegStandsIntervalLegsEnabled != autoLegStandsIntervalLegsEnabledPrev
			|| AutoLegStandsIntervalLegsInterval != autoLegStandsIntervalLegsIntervalPrev
			|| AutoLegStandsEndLegFront != autoLegStandsEndLegFrontPrev
			|| AutoLegStandsEndLegRear != autoLegStandsEndLegRearPrev
			|| AutoLegStandsMarginEndLegs != autoLegStandsMarginEndLegsPrev
			|| AutoLegStandsModelScene != autoLegStandsModelScenePrev
			|| legStandCoverageMin != legStandCoverageMinPrev
			|| legStandCoverageMax != legStandCoverageMaxPrev;
		if (autoLegStandsUpdateIsNeeded) {
			//GD.Print("Updating leg stands. Reason: ", AutoLegStandsIntervalLegsEnabled != autoLegStandsIntervalLegsEnabledPrev
			//, AutoLegStandsIntervalLegsInterval != autoLegStandsIntervalLegsIntervalPrev
			//, AutoLegStandsEndLegFront != autoLegStandsEndLegFrontPrev
			//, AutoLegStandsEndLegRear != autoLegStandsEndLegRearPrev
			//, AutoLegStandsMarginEndLegs != autoLegStandsMarginEndLegsPrev
			//, AutoLegStandsModelScene != autoLegStandsModelScenePrev
			//, legStandCoverageMin != legStandCoverageMinPrev
			//, legStandCoverageMax != legStandCoverageMaxPrev);
			AdjustAutoLegStandPositions();
			CreateAndRemoveAutoLegStands();
		}
		UpdateLegStandsHeightAndVisibility();
	}

	protected virtual void LockLegStandsGroup() {
		// Always align LegStands group with Conveyors group.
		if (conveyors != null) {
			legStands.Position = new Vector3(legStands.Position.X, legStands.Position.Y, conveyors.Position.Z);
			// Conveyors can't rotate anymore, so this doesn't do much.
			legStands.Rotation = new Vector3(0f, conveyors.Rotation.Y, 0f);
		}
	}

	/**
	 * Synchronize the offset of the leg stands with the assembly's AutoLegStandsIntervalLegsOffset property.
	 *
	 * If the property changes, the leg stands are moved to match.
	 * If the leg stands are moved manually, the property is updated.
	 * If both happen at the same time, the property wins.
	 *
	 * This currently shouldn't do anything for curved assemblies.
	 */
	private void SyncLegStandsOffsets() {
		Basis assemblyScale = Basis.Identity.Scaled(this.Basis.Scale);
		Basis assemblyScalePrev = Basis.Identity.Scaled(transformPrev.Basis.Scale);
		Vector3 legStandsScaledPosition = assemblyScale * legStands.Position;
		Vector3 legStandsScaledPositionPrev = assemblyScalePrev * legStandsTransformPrev.Origin;

		// Sync properties to leg stands position if changed.
		float newPosX = AutoLegStandsIntervalLegsOffset != autoLegStandsIntervalLegsOffsetPrev ? AutoLegStandsIntervalLegsOffset : legStandsScaledPosition.X;
		float newPosY = AutoLegStandsFloorOffset != autoLegStandsFloorOffsetPrev ? AutoLegStandsFloorOffset : legStandsScaledPosition.Y;
		if (AutoLegStandsIntervalLegsOffset != autoLegStandsIntervalLegsOffsetPrev || AutoLegStandsFloorOffset != autoLegStandsFloorOffsetPrev) {
			Vector3 targetPosition = new Vector3(newPosX, newPosY, legStandsScaledPosition.Z);
			legStands.Position = assemblyScale.Inverse() * targetPosition;
		}

		// Sync X offset to property if needed.
		if (AutoLegStandsIntervalLegsOffset == autoLegStandsIntervalLegsOffsetPrev) {
			float offset = legStandsScaledPosition.X;
			float offsetPrev = legStandsScaledPositionPrev.X;
			float offsetDelta = Mathf.Abs(offset - offsetPrev);
			if (offsetDelta > 0.01f) {
				this.AutoLegStandsIntervalLegsOffset = offset;
				NotifyPropertyListChanged();
			}
		}
		autoLegStandsIntervalLegsOffsetPrev = AutoLegStandsIntervalLegsOffset;

		// Sync Y offset to property if needed.
		if (AutoLegStandsFloorOffset == autoLegStandsFloorOffsetPrev) {
			float offset = legStandsScaledPosition.Y;
			float offsetPrev = legStandsScaledPositionPrev.Y;
			float offsetDelta = Mathf.Abs(offset - offsetPrev);
			if (offsetDelta > 0.01f) {
				this.AutoLegStandsFloorOffset = offset;
				NotifyPropertyListChanged();
			}
		}
		autoLegStandsFloorOffsetPrev = AutoLegStandsFloorOffset;

		legStandsTransformPrev = legStands.Transform;
	}

	private void DeleteAllAutoLegStands() {
		if (legStands == null) {
			return;
		}
		foreach (Node child in legStands.GetChildren()) {
			if (IsAutoLegStand(child)) {
				legStands.RemoveChild(child);
				child.QueueFree();
			}
		}
	}

	private bool IsAutoLegStand(Node node) {
		return GetAutoLegStandIndex(node.Name) != LEG_INDEX_NON_AUTO;
	}
	#endregion Leg Stands / Update "LegStands" node

	#region Leg Stands / Basic constraints
	private void SnapAllLegStandsToPath() {
		// Force legStand alignment with LegStands group.
		float targetWidth = GetLegStandTargetWidth();
		foreach (Node child in legStands.GetChildren()) {
			ConveyorLeg legStand = child as ConveyorLeg;
			if (legStand == null) {
				continue;
			}
			SnapToLegStandsPath(legStand);
			legStand.Scale = new Vector3(1f, legStand.Scale.Y, targetWidth);
		}

	}

	private float GetLegStandTargetWidth() {
		Node3D firstConveyor = null;
		foreach (Node child in conveyors.GetChildren()) {
			Node3D conveyor = child as Node3D;
			if (IsConveyor(conveyor)) {
				firstConveyor = conveyor;
				break;
			}
		}
		// This is a hack to account for the fact that rolling conveyors are slightly wider than belt conveyors.
		if (firstConveyor is RollerConveyor || firstConveyor is CurvedRollerConveyor) {
			return this.Scale.Z * 1.055f;
		}
		return this.Scale.Z;
	}

	/**
	 * Snap a child leg stand to a position on the leg stands path.
	 *
	 * The leg stands path is a surface parallel to `legStands` Y axis.
	 * It represents any position that the conveyor line would be directly above or below at some length.
	 * For straight assemblies, this is `legStands` XY plane.
	 * For curved assemblies, this is overridden to be a cylinder centered on `legStands`.
	 *
	 * @param legStand The leg stand to reposition.
	 */
	private void SnapToLegStandsPath(Node3D legStand) {
		MoveLegStandToPathPosition(legStand, GetPositionOnLegStandsPath(legStand.Position));
	}

	/**
	 * Get the path position of a point projected onto the leg stands path.
	 *
	 * The path position is a linear representation of where a point is on the leg stands path.
	 * For straight assemblies, this is the X coordinate of the point.
	 * For curved assemblies, this is an angle of the point around the leg stands Y axis in degrees.
	 *
	 * @param position The point to project onto the leg stands path.
	 * @return The point's path position.
	 */
	protected virtual float GetPositionOnLegStandsPath(Vector3 position) {
		return position.X;
	}

	/**
	 * Move a leg stand to a given position on the leg stands path.
	 *
	 * The leg stand is moved and rotated to align with the path.
	 * The leg stand keeps its Y position and Z rotation.
	 * Curved assemblies override this and don't keep the Z rotation.
	 *
	 * @param legStand The leg stand to move.
	 * @param position The path position to move the leg stand to.
	 */
	protected virtual void MoveLegStandToPathPosition(Node3D legStand, float position) {
		legStand.Position = new Vector3(position, legStand.Position.Y, 0f);
		legStand.Rotation = new Vector3(0f, 0f, legStand.Rotation.Z);
	}
	#endregion Leg Stands / Basic constraints

	#region Leg Stands / Managing auto-instanced leg stands
	/**
	 * Adjust the positions of all auto-instanced leg stands to match changed settings or coverage.
	 */
	private void AdjustAutoLegStandPositions() {
		// Don't allow tiny or negative intervals.
		AutoLegStandsIntervalLegsInterval = Mathf.Max(0.5f, AutoLegStandsIntervalLegsInterval);
		if (AutoLegStandsIntervalLegsInterval == autoLegStandsIntervalLegsIntervalPrev && legStandCoverageMax == legStandCoverageMaxPrev && legStandCoverageMin == legStandCoverageMinPrev) {
			return;
		}
		foreach (Node child in legStands.GetChildren()) {
			if (child is not ConveyorLeg legStand) {
				continue;
			}
			int legStandIndex = GetAutoLegStandIndex(legStand.Name);
			switch (legStandIndex) {
				case LEG_INDEX_NON_AUTO:
					// Only adjust auto leg stands.
					break;
				default:
					// Update leg stand position to the new interval.
					MoveLegStandToPathPosition(legStand, GetAutoLegStandPosition(legStandIndex));
					break;
			}
		}
		autoLegStandsIntervalLegsIntervalPrev = AutoLegStandsIntervalLegsInterval;
	}

	private int GetAutoLegStandIndex(StringName name) {
		if (name.Equals(new StringName(AUTO_LEG_STAND_NAME_FRONT))) {
			return LEG_INDEX_FRONT;
		}
		if (name.Equals(new StringName(AUTO_LEG_STAND_NAME_REAR))) {
			return LEG_INDEX_REAR;
		}
		if (name.ToString().StartsWith(AUTO_LEG_STAND_NAME_PREFIX) &&
			int.TryParse(name.ToString().AsSpan(AUTO_LEG_STAND_NAME_PREFIX.Length), out int legStandIndex)) {
			// Names start at 1, but indices start at 0.
			return legStandIndex - 1;
		}
		return LEG_INDEX_NON_AUTO;
	}

	/**
	 * Get the correct path position of an auto-instanced leg stand.
	 *
	 * If the index is high, it's possible that the position will be outside the coverage range.
	 *
	 * @param index The index of an interval aligned leg stand or LEG_INDEX_FRONT or LEG_INDEX_REAR for fixed legs.
	 */
	private float GetAutoLegStandPosition(int index) {
		if (index == LEG_INDEX_FRONT) {
			return legStandCoverageMin;
		}
		if (index == LEG_INDEX_REAR) {
			return legStandCoverageMax;
		}
		return GetIntervalLegStandPosition(index);
	}


	/**
	 * Get the correct path position of an interval aligned leg stand.
	 *
	 * If the index is high, it's possible that the position will be outside the coverage range.
	 *
	 * @param index The index of the leg stand, starting with the first covered one.
	 */
	private float GetIntervalLegStandPosition(int index) {
		Debug.Assert(index >= 0);
		float frontMargin = AutoLegStandsEndLegFront ? AutoLegStandsMarginEndLegs : 0f;
		float firstPosition = (float) Math.Ceiling((legStandCoverageMin + frontMargin) / AutoLegStandsIntervalLegsInterval) * AutoLegStandsIntervalLegsInterval;
		return firstPosition + index * AutoLegStandsIntervalLegsInterval;
	}

	private void CreateAndRemoveAutoLegStands() {
		// Don't allow negative margins.
		AutoLegStandsMarginEndLegs = Mathf.Max(0f, AutoLegStandsMarginEndLegs);
		// Enforce a margin from fixed front and rear legs if they exist.
		float firstPosition = GetIntervalLegStandPosition(0);
		float rearMargin = AutoLegStandsEndLegRear ? AutoLegStandsMarginEndLegs : 0f;
		float lastPosition = (float) Math.Floor((legStandCoverageMax - rearMargin) / AutoLegStandsIntervalLegsInterval) * AutoLegStandsIntervalLegsInterval;
		int intervalLegStandCount;
		if (!AutoLegStandsIntervalLegsEnabled) {
			intervalLegStandCount = 0;
		} else if (firstPosition > lastPosition) {
			// Invalid range implies zero interval-aligned leg stands are needed.
			intervalLegStandCount = 0;
		} else {
			intervalLegStandCount = (int) ((lastPosition - firstPosition) / AutoLegStandsIntervalLegsInterval) + 1;
		}
		// Inventory our existing leg stands and delete the ones we don't need.
		bool hasFrontLeg = false;
		bool hasRearLeg = false;
		bool[] legStandsInventory = new bool[intervalLegStandCount];
		foreach (Node child in legStands.GetChildren()) {
			if (child is not ConveyorLeg legStand) {
				continue;
			}
			int legStandIndex = GetAutoLegStandIndex(legStand.Name);
			switch (legStandIndex) {
				case LEG_INDEX_NON_AUTO:
					// Only manage auto leg stands.
					break;
				case LEG_INDEX_FRONT:
					if (AutoLegStandsEndLegFront) {
						hasFrontLeg = true;
					} else {
						legStands.RemoveChild(legStand);
						legStand.QueueFree();
					}
					break;
				case LEG_INDEX_REAR:
					if (AutoLegStandsEndLegRear) {
						hasRearLeg = true;
					} else {
						legStands.RemoveChild(legStand);
						legStand.QueueFree();
					}
					break;
				default:
					// Mark existing leg stands that are in the new interval.
					if (legStandIndex < intervalLegStandCount && AutoLegStandsIntervalLegsEnabled) {
						legStandsInventory[legStandIndex] = true;
						break;
					}
					// Delete leg stands that are outside the new interval.
					legStands.RemoveChild(legStand);
					legStand.QueueFree();
					break;
			}
		}

		// Create the missing leg stands.
		if (AutoLegStandsModelScene == null) {
			return;
		}
		if (!hasFrontLeg && AutoLegStandsEndLegFront) {
			AddLegStandAtIndex(LEG_INDEX_FRONT);
		}
		for (int i = 0; i < intervalLegStandCount; i++) {
			if (!legStandsInventory[i]) {
				AddLegStandAtIndex(i);
			}
		}
		if (!hasRearLeg && AutoLegStandsEndLegRear) {
			AddLegStandAtIndex(LEG_INDEX_REAR);
		}
	}

	private ConveyorLeg AddLegStandAtIndex(int index) {
		float position = GetAutoLegStandPosition(index);
		StringName name = index switch
		{
			LEG_INDEX_FRONT => (StringName)AUTO_LEG_STAND_NAME_FRONT,
			LEG_INDEX_REAR => (StringName)AUTO_LEG_STAND_NAME_REAR,
			// Indices start at 0, but names start at 1.
			_ => (StringName)(AUTO_LEG_STAND_NAME_PREFIX + (index + 1).ToString()),
		};
		ConveyorLeg legStand = AddOrGetLegStandInstance(name) as ConveyorLeg;
		MoveLegStandToPathPosition(legStand, position);

		// It probably doesn't matter, but let's try to keep leg stands in order.
		int trueIndex = index switch {
			LEG_INDEX_FRONT => 0,
			LEG_INDEX_REAR => -1,
			_ => AutoLegStandsEndLegFront ? index + 1 : index,
		};
		if (trueIndex < legStands.GetChildCount()) {
			legStands.MoveChild(legStand, trueIndex);
		}
		return legStand;
	}

	private Node AddOrGetLegStandInstance(StringName name) {
		Node legStand = legStands.GetNodeOrNull<Node>(new NodePath(name));
		if (legStand != null) {
			return legStand;
		}
		legStand = AutoLegStandsModelScene.Instantiate();
		legStand.Name = name;
		legStands.AddChild(legStand);
		// If the leg stand used to exist, restore its original owner.
		legStand.Owner = foreignLegStandsOwners.TryGetValue(name, out Node originalOwner)
			? originalOwner
			: GetTree().GetEditedSceneRoot();
		return legStand;
	}
	#endregion Leg Stands / Managing auto-instanced leg stands

	#region Leg Stands / Auto-height and visibility
	private void UpdateLegStandsHeightAndVisibility() {
		// Extend LegStands to Conveyor line.
		if (conveyors == null)
		{
			return;
		}
		// Plane transformed from conveyors space into legStands space.
		Plane conveyorPlane = new Plane(Vector3.Up, new Vector3(0f, -AutoLegStandsModelGrabsOffset, 0f)) * conveyors.Transform.AffineInverse() * legStands.Transform;
		Vector3 conveyorPlaneGlobalNormal = conveyorPlane.Normal * legStands.GlobalBasis.Inverse();

		foreach (Node child in legStands.GetChildren()) {
			ConveyorLeg legStand = child as ConveyorLeg;
			if (legStand == null) {
				continue;
			}
			// Persist legStand changes into the Assembly's PackedScene.
			// Fixes ugly previews in the editor.
			SetEditableInstance(legStand, true);
			// Raycast from the minimum-height tip of the leg stand to the conveyor plane.
			Vector3? intersection = conveyorPlane.IntersectsRay(legStand.Position + legStand.Basis.Y.Normalized(), legStand.Basis.Y.Normalized());
			if (intersection == null) {
				legStand.Visible = false;
				// Set scale to minimum height.
				legStand.Scale = new Vector3(1f, 1f, legStand.Scale.Z);
				continue;
			}
			float legHeight = intersection.Value.DistanceTo(legStand.Position);
			legStand.Scale = new Vector3(1f, legHeight, legStand.Scale.Z);
			legStand.GrabsRotation = Mathf.RadToDeg(Vector3.Up.SignedAngleTo(conveyorPlaneGlobalNormal.Slide(legStand.GlobalBasis.Z), legStand.GlobalBasis.Z));
			// Only show leg stands that touch a conveyor.
			float tipPosition = GetPositionOnLegStandsPath(legStand.Position + legStand.Basis.Y);
			legStand.Visible = legStandCoverageMin <= tipPosition && tipPosition <= legStandCoverageMax;
		}
	}
	#endregion Leg Stands / Auto-height and visibility
	#endregion Leg Stands
}
