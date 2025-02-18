using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class ConveyorAssembly : Node3D
{
	#region SideGuards
	#region SideGuards / Update "LeftSide" and "RightSide" nodes
	private void UpdateSides()
	{
		UpdateSide(true);
		UpdateSide(false);
	}


	private void UpdateSide(bool isRight) {
		Node3D side;
		if (isRight) {
			rightSide = IsInstanceValid(rightSide) ? rightSide : GetNodeOrNull<Node3D>("RightSide");
			side = rightSide;
		} else {
			leftSide = IsInstanceValid(leftSide) ? leftSide : GetNodeOrNull<Node3D>("LeftSide");
			side = leftSide;
		}
		if (side == null) {
			return;
		}
		if (conveyors != null) {
			LockSidePosition(side, isRight);
		}
		UpdateAutoSideGuards(side, isRight);
	}

	protected virtual void LockSidePosition(Node3D side, bool isRight) {
		// Sides always snap onto the conveyor line
		side.Transform = conveyors.Transform;
		var offsetZ = (isRight? 1 : -1) * side.Basis.Z * (this.Scale.Z - 1f);
		side.Position += offsetZ;
	}
	#endregion SideGuards / Update "LeftSide" and "RightSide" nodes

	#region SideGuards / ScaleSideGuardLine
	/**
	 * Scale all side guard children of a given node.
	 *
	 * This would be a great place to implement proportional scaling and positioning of the guards,
	 * but currently, we just scale every guard to the length of the whole line and leave its position alone.
	 *
	 * @param guardLine The parent of the side guards.
	 * @param conveyorLineLength The length of the conveyor line to scale to. Ignored if SideGuardsAutoScale is false.
	 */
	private void ScaleSideGuardLine(Node3D guardLine, float conveyorLineLength) {
		foreach (Node child in guardLine.GetChildren()) {
			Node3D child3d = child as Node3D;
			if (IsSideGuard(child3d)) {
				SetEditableInstance(child3d, true);
				ScaleSideGuard(child3d, conveyorLineLength);
			}
		}
	}

	private bool IsSideGuard(Node node) {
		return node as SideGuard != null || node as SideGuardCBC != null;
	}

	protected virtual void ScaleSideGuard(Node3D guard, float guardLength) {
		guard.Scale = new Vector3(guardLength, 1f, 1f);
	}
	#endregion SideGuards / ScaleSideGuardLine

	#region SideGuards / Auto SideGuards
	private void UpdateAutoSideGuards(Node3D side, bool isRight) {
		var targetSideGuardExtents = GetTargetAutoSideGuardExtents(isRight);
		ApplyAutoSideGuardExtents(side, targetSideGuardExtents, isRight);
	}

	/**
	 * Given existing conveyors and desired gaps, determine the desired extents of side guards.
	 *
	 * @param isRight Whether to calculate for the right or left side.
	 */
	private List<(float, float)> GetTargetAutoSideGuardExtents(bool isRight) {
		// Assume that the conveyor extents are in the same space as the side guards and gaps.
		if (isRight && !SideGuardsRightSide || !isRight && !SideGuardsLeftSide) {
			// No side guards.
			return new();
		}
		List<(float, float)> extentPairs = GetAllConveyorExtents();
		ApplySideGuardGapsToExtents(ref extentPairs, isRight);
		return extentPairs;
	}

	/**
	 * Cut gaps into the conveyor extents list.
	 *
	 * Shrink or remove conveyor extents to make room for side guard gaps.
	 *
	 * @param extentPairs The list of conveyor extents to cut gaps into.
	 */
	private void ApplySideGuardGapsToExtents(ref List<(float, float)> extentPairs, bool isRight) {
		// Sort and merge the gaps for the current side. Zero-width gaps allowed.
		List<(float, float)> gaps = SideGuardsGaps.ToList()
			//.Select<Resource, SideGuardGap>(gap => gap as SideGuardGap)
			.Where((SideGuardGap gap) => gap != null && (gap.Side == SideGuardGap.SideGuardGapSide.Both
			|| isRight && gap.Side == SideGuardGap.SideGuardGapSide.Right
			|| !isRight && gap.Side == SideGuardGap.SideGuardGapSide.Left))
			.Select(gap => (gap.Position - Mathf.Abs(gap.Width) / 2f, gap.Position + Mathf.Abs(gap.Width) / 2f))
			.OrderBy(gap => gap.Item1)
			.Aggregate(new List<(float, float)>(), (acc, gap) => {
				if (acc.Count == 0) {
					acc.Add(gap);
					return acc;
				}
				// Merge overlapping gaps.
				// Gaps are already sorted by leading edge.
				var last = acc.Last();
				if (last.Item2 < gap.Item1) {
					// No overlap.
					acc.Add(gap);
				} else if (last.Item2 < gap.Item2) {
					// Partial overlap.
					acc[^1] = (last.Item1, gap.Item2);
				}
				// Otherwise, full overlap; ignore.
				return acc;
			}).ToList();

		// Cut gaps into the given extents.
		// Don't assume any sorting on the extents.
		for (int i = 0; i < extentPairs.Count; i++) {
			(float extentFront, float extentRear) = extentPairs[i];
			if (extentRear < extentFront) {
				// Sort extents.
				(extentFront, extentRear) = (extentRear, extentFront);
				extentPairs[i] = (extentFront, extentRear);
			}
			// Drop empty extents.
			if (extentFront == extentRear) {
				extentPairs.RemoveAt(i);
				i--;
				continue;
			}
			foreach ((float gapFront, float gapRear) in gaps) {
				if (extentRear <= gapFront) {
					// Extent is entirely before the gap.
					continue;
				}
				if (extentFront < gapFront && gapRear < extentRear) {
					// Gap is fully inside the extent; split it.
					extentPairs.Insert(i + 1, (gapRear, extentRear));
					extentPairs[i] = (extentFront, gapFront);
					break;
				}
				if (gapFront < extentFront && extentRear < gapRear) {
					// Extent is fully inside the gap; remove it.
					extentPairs.RemoveAt(i);
					i--;
					break;
				}
				if (extentFront < gapFront && gapFront < extentRear) {
					// Gap clips the end of the extent.
					extentPairs[i] = (extentFront, gapFront);
					// Gaps are sorted by leading edge, so we can break because no more could overlap.
					break;
				}
				if (extentFront < gapRear && gapRear < extentRear) {
					// Gap clips the start of the extent.
					extentPairs[i] = (gapRear, extentRear);
					// More gaps might overlap.
					(extentFront, extentRear) = extentPairs[i];
				}
			}
		}
	}

	/**
	 * Create, update, or remove auto side guards to match the target extents.
	 *
	 * The `side` node is assumed to have the same global rotation as the `conveyors` node.
	 * The position should be the same as the `conveyors` node, but with a possible Z offset.
	 * All generated side guards will be placed and aligned on `side`'s X axis.
	 *
	 * @param side The parent node of the side guards.
	 * @param targetExtents A list of x position pairs for each desired side guard describing the positions of its ends.
	 * @param isRight Which direction side guards should be rotated for.
	 */
	private void ApplyAutoSideGuardExtents(Node3D side, List<(float, float)> targetExtents, bool isRight)
	{
		Node[] existingGuards = IndexOrRemoveExistingSideGuards(side, targetExtents.Count);
		// Iterate through the guards and extents together.
		for (int i = 0; i < targetExtents.Count; i++)
		{
			(float extentFront, float extentRear) = targetExtents[i];
			Node node = existingGuards[i];
			if (node != null && (!IsSideGuard(node) || node.SceneFilePath != SideGuardsModelScene.ResourcePath))
			{
				// We don't like this node.
				// It is either a non-SideGuard with a SideGuard name or it's a SideGuard of the wrong scene.
				// Delete it so it can be replaced.
				side.RemoveChild(node);
				node.QueueFree();
				existingGuards[i] = null;
			}
			Node3D guard = existingGuards[i] as Node3D;
			if (guard == null)
			{
				// Create and add new guard.
				guard = InstanceAutoSideGuard(i);
				existingGuards[i] = guard;
				if (i == 0)
				{
					// Add to the front.
					side.AddChild(guard);
					side.MoveChild(guard, 0);
				}
				else
				{
					// Add as sibling to the previous guard.
					existingGuards[i - 1].AddSibling(guard);
				}
				SetGuardOwner(guard);
			}
			// Position and scale the guard.
			guard.Position = new Vector3((extentFront + extentRear) / 2f, 0, 0);
			guard.RotationDegrees = new Vector3(0, isRight ? 180 : 0, 0);
			ScaleSideGuard(guard, extentRear - extentFront);
		}

		// I don't think it's really going to matter if guards jump around when gaps are added or removed.
		// They're all the same. No one's going to want to edit them to be unique.
	}

	/**
	 * Creates an array of the first `count` child nodes named with the auto side guard prefix.
	 * The index in the array is the index of the guard. Removes and deletes any excess guards.
	 */
	private Node[] IndexOrRemoveExistingSideGuards(Node3D side, int count)
	{
		Node[] existingGuards = new Node[count];
		foreach ((int index, Node guard) in side.GetChildren()
			.Select(guard => (GetAutoSideGuardIndex(guard.Name), guard))
			.Where(pair => pair.Item1.HasValue)
			.Select(pair => (pair.Item1.Value, pair.guard)))
		{
			if (index < 0 || index >= count)
			{
				// Remove excess guards.
				side.RemoveChild(guard);
				guard.QueueFree();
			}
			else
			{
				existingGuards[index] = guard;
			}
		}

		return existingGuards;
	}

	private int? GetAutoSideGuardIndex(StringName name) {
		if (name.ToString().StartsWith(AUTO_SIDE_GUARD_NAME_PREFIX) &&
			int.TryParse(name.ToString().AsSpan(AUTO_SIDE_GUARD_NAME_PREFIX.Length), out int sideGuardIndex)) {
			// Names start at 1, but indices start at 0.
			return sideGuardIndex - 1;
		}
		return null;
	}

	private Node3D InstanceAutoSideGuard(int index) {
		Node3D guard = SideGuardsModelScene.InstantiateOrNull<Node3D>();
		if (guard == null) {
			// If there's something wrong with the scene, just make a plain Node3D and hope for the best.
			guard = new Node3D();
		}
		guard.Name = new StringName($"{AUTO_SIDE_GUARD_NAME_PREFIX}{index + 1}");
		return guard;
	}

	private void SetGuardOwner(Node3D guard) {
		// Use conveyors to determine the owner.
		// We want the guards to have the same owner as everything else in the assembly regardless of whether this is an instance or part of the currently-edited scene.
		// This is a good way to do it because we know the conveyors node exists if we're instancing guards.
		guard.Owner = conveyors?.Owner;
	}
	#endregion SideGuards / Auto SideGuards
	#endregion SideGuards
}
