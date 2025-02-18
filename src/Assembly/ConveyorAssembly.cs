using Godot;
using System.Collections.Generic;

public partial class ConveyorAssembly : Node3D, IComms
{
	#region Constants
	#region Constants / Leg Stands
	private const string AUTO_LEG_STAND_NAME_PREFIX = "AutoLegsStand";
	private const string AUTO_LEG_STAND_NAME_FRONT = "AutoLegsStandFront";
	private const string AUTO_LEG_STAND_NAME_REAR = "AutoLegsStandRear";
	private const int LEG_INDEX_FRONT = -1;
	private const int LEG_INDEX_REAR = -2;
	private const int LEG_INDEX_NON_AUTO = -3;
	#endregion Constants / Leg Stands

	#region Constants / Side Guards
	private const string AUTO_SIDE_GUARD_NAME_PREFIX = "AutoSideGuard";
	#endregion Constants / Side Guards
	#endregion Constants

	#region Fields
	#region Fields / Nodes
	protected Node3D conveyors
	{
		get
		{
			if (!IsInstanceValid(_conveyors))
			{
				_conveyors = GetNodeOrNull<Node3D>("Conveyors");
			}
			return _conveyors;
		}
		set
		{
			_conveyors = value;
		}
	}
	private Node3D _conveyors;
	private Node3D rightSide;
	private Node3D leftSide;
	protected Node3D legStands;
	#endregion Fields / Nodes
	protected Transform3D transformPrev;
	private Transform3D conveyorsTransformPrev;
	private Transform3D legStandsTransformPrev;

	#region Fields / Exported properties
	[ExportGroup("Conveyor", "Conveyor")]
	[Export(PropertyHint.None, "radians_as_degrees")]
	public float ConveyorAngle { get; set; } = 0f;
	private float conveyorAnglePrev = 0f;

	[Export]
	public bool ConveyorAutomaticLength { get; set; } = true;

	// This rest of this section is for properties proxied to the conveyor parts.
	[ExportSubgroup("Comms")]
	[Export]
	public bool EnableComms {
		get
		{
			// We're going to assume that the first child is a conveyor part.
			// This could be improved to be more robust, but should work most of the time.
			Node conveyor = conveyors?.GetChildOrNull<Node>(0);
			if (IsConveyor(conveyor))
			{
				return (conveyor as IComms).EnableComms;
			}
			// If conveyor is null, it's more likely because `conveyors` isn't available yet.
			return false;
		}
		set
		{
			if (conveyors == null)
			{
				return;
			}
			foreach (Node conveyor in conveyors.GetChildren())
			{
				if (IsConveyor(conveyor))
				{
					(conveyor as IComms).EnableComms = value;
				}
			}
			NotifyPropertyListChanged();
		}
	}

	[Export]
	public string Tag {
		get
		{
			Node conveyor = conveyors?.GetChildOrNull<Node>(0);
			if (IsConveyor(conveyor))
			{
				return (conveyor as IComms).Tag;
			}
			return null;
		}
		set
		{
			if (conveyors == null)
			{
				return;
			}
			foreach (Node conveyor in conveyors.GetChildren())
			{
				if (IsConveyor(conveyor))
				{
					(conveyor as IComms).Tag = value;
				}
			}
		}
	}

	[Export]
	public int UpdateRate {
		get
		{
			Node conveyor = conveyors?.GetChildOrNull<Node>(0);
			if (IsConveyor(conveyor))
			{
				return (conveyor as IComms).UpdateRate;
			}
			return 100;
		}
		set
		{
			if (conveyors == null)
			{
				return;
			}
			foreach (Node conveyor in conveyors.GetChildren())
			{
				if (IsConveyor(conveyor))
				{
					(conveyor as IComms).UpdateRate = value;
				}
			}
		}
	}
	[ExportSubgroup("BeltConveyor", "BeltConveyor")]
	[Export]
	public Color BeltConveyorBeltColor {
		get
		{
			Node conveyor = conveyors?.GetChildOrNull<Node>(0);
			return (conveyor as IBeltConveyor)?.BeltColor ?? new Color(1, 1, 1, 1);
		}
		set
		{
			if (conveyors == null)
			{
				return;
			}
			foreach (Node node in conveyors.GetChildren())
			{
				if (node is IBeltConveyor conveyor)
				{
					conveyor.BeltColor = value;
				}
			}
		}
	}

	[Export]
	public IBeltConveyor.ConvTexture BeltConveyorBeltTexture {
		get
		{
			Node conveyor = conveyors?.GetChildOrNull<Node>(0);
			if (IsConveyor(conveyor))
			{
				return (conveyor as BeltConveyor)?.BeltTexture ?? IBeltConveyor.ConvTexture.Standard;
			}
			return IBeltConveyor.ConvTexture.Standard;
		}
		set
		{
			if (conveyors == null)
			{
				return;
			}
			foreach (Node node in conveyors.GetChildren())
			{
				if (node is IBeltConveyor conveyor)
				{
					conveyor.BeltTexture = value;
				}
			}
		}
	}

	[Export]
	public float BeltConveyorSpeed {
		get
		{
			IBeltConveyor conveyor = conveyors?.GetChildOrNull<IBeltConveyor>(0);
			return conveyor?.Speed ?? -2.0f;
		}
		set
		{
			if (conveyors == null)
			{
				return;
			}
			foreach (Node node in conveyors.GetChildren())
			{
				if (node is IBeltConveyor conveyor)
				{
					conveyor.Speed = value;
				}
			}
		}
	}

	[ExportSubgroup("RollerConveyor", "RollerConveyor")]
	[Export]
	public float RollerConveyorSpeed {
		get
		{
			IRollerConveyor conveyor = conveyors?.GetChildOrNull<IRollerConveyor>(0);
			return conveyor?.Speed ?? -2.0f;
		}
		set
		{
			if (conveyors == null)
			{
				return;
			}
			foreach (Node node in conveyors.GetChildren())
			{
				if (node is IRollerConveyor conveyor)
				{
					conveyor.Speed = value;
				}
			}
		}
	}
	[Export]
	public float RollerConveyorSkewAngle {
		get
		{
			RollerConveyor conveyor = conveyors?.GetChildOrNull<RollerConveyor>(0);
			return conveyor?.SkewAngle ?? 0.0f;
		}
		set
		{
			if (conveyors == null)
			{
				return;
			}
			foreach (Node node in conveyors.GetChildren())
			{
				if (node is RollerConveyor conveyor)
				{
					conveyor.SkewAngle = value;
				}
			}
		}
	}

	[ExportGroup("Side Guards", "SideGuards")]
	[Export]
	public bool SideGuardsLeftSide
	{
		get => _sideGuardsLeftSide;
		set
		{
			_sideGuardsLeftSide = value;
			UpdateSide(false);
		}
	}
	private bool _sideGuardsLeftSide = true;
	[Export]
	public bool SideGuardsRightSide
	{
		get => _sideGuardsRightSide;
		set
		{
			_sideGuardsRightSide = value;
			UpdateSide(true);
		}
	}
	private bool _sideGuardsRightSide = true;
	// This only exists to give us a single checkbox for CurvedConveyorAssembly, which doesn't have separate left and right side guard models.
	// We hide it in the editor by default.
	[Export]
	public bool SideGuardsBothSides
	{
		get => SideGuardsLeftSide && SideGuardsRightSide;
		set
		{
			SideGuardsLeftSide = value;
			SideGuardsRightSide = value;
		}
	}
	[Export]
	public Godot.Collections.Array<SideGuardGap> SideGuardsGaps
	{
		get => _sideGuardsGaps;
		set
		{
			// Unsubscribe from previous gaps. They may be replaced with new ones.
			foreach (SideGuardGap gap in _sideGuardsGaps)
			{
				if (gap == null)
				{
					// Shouldn't be possible since all nulls should have been replaced by instances.
					continue;
				}
				gap.Changed -= UpdateSides;
			}

			// Workaround for faulty duplicate behavior in the editor.
			// See issue #74918.
			if (_sideGuardsGaps.Count == 0) {
				// Assume that we're initializing for the first time.
				foreach (SideGuardGap gap in value)
				{
					// Any gaps we see in the new array possibly came from an original that this instance is a duplicate of.
					// There's no way to know for sure.
					// Make all the gaps unique to this instance to prevent editing the originals.
					_sideGuardsGaps.Add(gap?.Duplicate(true) as SideGuardGap);
				}
			} else {
				// If we're not initializing, avoid making unnecessary duplicates.
				_sideGuardsGaps = value;
			}

			// Replace null with a new gap so users don't have to do this by hand.
			for (int i = 0; i < _sideGuardsGaps.Count; i++)
			{
				_sideGuardsGaps[i] ??= new SideGuardGap();
			}

			// Subscribe to ensure that side guards update whenever gaps change.
			foreach (SideGuardGap gap in _sideGuardsGaps)
			{
				// null gaps shouldn't be possible since we just replaced them above.
				gap.Changed += UpdateSides;
			}

			// Update side guards to account for added or removed gaps.
			UpdateSides();
		}
	}
	private Godot.Collections.Array<SideGuardGap> _sideGuardsGaps = new();
	[Export]
	public PackedScene SideGuardsModelScene
	{
		get => _sideGuardsModelScene;
		set
		{
			bool hasChanged = _sideGuardsModelScene != value;
			_sideGuardsModelScene = value;
			if (hasChanged) {
				UpdateSides();
			}
		}
	}
	private PackedScene _sideGuardsModelScene = GD.Load<PackedScene>("res://parts/SideGuard.tscn");

	[ExportGroup("Leg Stands", "AutoLegStands")]
	[Export(PropertyHint.None, "suffix:m")]
	public float AutoLegStandsFloorOffset = 0f;
	public float autoLegStandsFloorOffsetPrev;

	[ExportSubgroup("Interval Legs", "AutoLegStandsIntervalLegs")]
	[Export]
	public bool AutoLegStandsIntervalLegsEnabled { get; set; } = true;
	private bool autoLegStandsIntervalLegsEnabledPrev = false;

	[Export(PropertyHint.Range, "0.5,10,or_greater,suffix:m")]
	public float AutoLegStandsIntervalLegsInterval { get; set; } = 2f;
	private float autoLegStandsIntervalLegsIntervalPrev;

	[Export(PropertyHint.Range, "-5,5,or_less,or_greater,suffix:m")]
	public float AutoLegStandsIntervalLegsOffset { get; set; } = 0f;

	private float autoLegStandsIntervalLegsOffsetPrev;

	[ExportSubgroup("End Legs", "AutoLegStandsEndLeg")]
	[Export]
	public bool AutoLegStandsEndLegFront = true;
	private bool autoLegStandsEndLegFrontPrev = false;
	[Export]
	public bool AutoLegStandsEndLegRear = true;
	private bool autoLegStandsEndLegRearPrev = false;

	[ExportSubgroup("Placement Margins", "AutoLegStandsMargin")]
	[Export(PropertyHint.Range, "0,1,or_less,or_greater,suffix:m")]
	public float AutoLegStandsMarginEnds = 0.2f;
	[Export(PropertyHint.Range, "0.5,5,or_greater,suffix:m")]
	public float AutoLegStandsMarginEndLegs = 0.5f;
	private float autoLegStandsMarginEndLegsPrev = 0.5f;

	[ExportSubgroup("Leg Model", "AutoLegStandsModel")]
	[Export(PropertyHint.None, "suffix:m")]
	public float AutoLegStandsModelGrabsOffset = 0.382f;

	[Export]
	public PackedScene AutoLegStandsModelScene = GD.Load<PackedScene>("res://parts/ConveyorLegBC.tscn");
	private PackedScene autoLegStandsModelScenePrev;
	#endregion Fields / Exported properties

	#region Fields / Leg stand coverage
	private float legStandCoverageMin;
	private float legStandCoverageMax;
	private float legStandCoverageMinPrev;
	private float legStandCoverageMaxPrev;
	#endregion Fields / Leg stand coverage

	// This variable is used to store the names of the pre-existing leg stands that can't be owned by the edited scene.
	private Dictionary <StringName, Node> foreignLegStandsOwners = new();

	#region Fields / Property method overrides
	public override void _ValidateProperty(Godot.Collections.Dictionary property) {
		string propertyName = property["name"].AsStringName();

		// Hide this property as it's only useful for CurvedConveyorAssembly; it clutters the UI otherwise.
		if (propertyName == PropertyName.SideGuardsBothSides) {
			// We don't even want it stored. SideGuardsLeftSide and SideGuardsRightSide are the source of truth.
			property["usage"] = (int) PropertyUsageFlags.None;
		}
		// These only have an effect if EnableComms is true.
		else if (propertyName == PropertyName.UpdateRate || propertyName == PropertyName.Tag)
		{
			property["usage"] = (int)(EnableComms ? PropertyUsageFlags.Default : PropertyUsageFlags.NoEditor);
		}
		// Only show if a IBeltConveyor is present.
		else if (propertyName == PropertyName.BeltConveyorBeltColor
			|| propertyName == PropertyName.BeltConveyorBeltTexture
			|| propertyName == PropertyName.BeltConveyorSpeed) {
			property["usage"] = (int)(conveyors?.GetChildOrNull<IBeltConveyor>(0) != null ? PropertyUsageFlags.Default : PropertyUsageFlags.NoEditor);
		}
		// Only show if a IRollerConveyor is present.
		else if (propertyName == PropertyName.RollerConveyorSpeed) {
			property["usage"] = (int)(conveyors?.GetChildOrNull<IRollerConveyor>(0) != null ? PropertyUsageFlags.Default : PropertyUsageFlags.NoEditor);
		}
		// Only show if a RollerConveyor is present. (CurvedRollerConveyors don't have skew angles.)
		else if (propertyName == PropertyName.RollerConveyorSkewAngle) {
			property["usage"] = (int)(conveyors?.GetChildOrNull<RollerConveyor>(0) != null ? PropertyUsageFlags.Default : PropertyUsageFlags.NoEditor);
		}
		else
		{
			base._ValidateProperty(property);
		}
	}

	public override bool _PropertyCanRevert(StringName property) {
		return property == PropertyName.SideGuardsBothSides
			|| property == PropertyName.UpdateRate
			|| property == PropertyName.BeltConveyorBeltColor
			|| property == PropertyName.BeltConveyorSpeed
			|| property == PropertyName.RollerConveyorSpeed
			|| base._PropertyCanRevert(property);
	}

	public override Variant _PropertyGetRevert(StringName property) {
		if (property == PropertyName.SideGuardsBothSides) {
			return true;
		}
		if (property == PropertyName.UpdateRate) {
			return 100;
		}
		if (property == PropertyName.BeltConveyorBeltColor) {
			return new Color(1, 1, 1, 1);
		}
		if (property == PropertyName.BeltConveyorSpeed) {
			return -2.0f;
		}
		if (property == PropertyName.RollerConveyorSpeed) {
			return -2.0f;
		}
		return base._PropertyGetRevert(property);
	}
	#endregion Fields / Property method overrides
	#endregion Fields

	#region _Ready and _PhysicsProcess
	public override void _Ready()
	{
		conveyors = GetNode<Node3D>("Conveyors");
		legStands = GetNodeOrNull<Node3D>("LegStands");

		transformPrev = this.Transform;

		// Apply the ConveyorsAngle property if needed.
		Basis assemblyScale = Basis.Identity.Scaled(this.Basis.Scale);
		if (conveyors != null) {
			float conveyorsStartingAngle = (assemblyScale * conveyors.Basis).GetEuler().Z;
			conveyorAnglePrev = conveyorsStartingAngle;
			conveyorsTransformPrev = conveyors.Transform;
			SyncConveyorsAngle();
			conveyorAnglePrev = ConveyorAngle;
			conveyorsTransformPrev = conveyors.Transform;
		}

		// Apply the AutoLegStandsFloorOffset and AutoLegStandsIntervalLegsOffset properties if needed.
		if (legStands != null) {
			Vector3 legStandsStartingOffset = assemblyScale * legStands.Position;
			autoLegStandsFloorOffsetPrev = legStandsStartingOffset.Y;
			autoLegStandsIntervalLegsOffsetPrev = legStandsStartingOffset.X;
			legStandsTransformPrev = legStands.Transform;
			SyncLegStandsOffsets();
		}

		UpdateSides();

		autoLegStandsIntervalLegsIntervalPrev = AutoLegStandsIntervalLegsInterval;
		autoLegStandsModelScenePrev = AutoLegStandsModelScene;
		UpdateLegStandCoverage();

		if (legStands != null) {
			Node editedScene = GetTree().GetEditedSceneRoot();
			foreach (Node legStand in legStands.GetChildren()) {
				if (legStand.Owner != editedScene) {
					foreignLegStandsOwners[legStand.Name] = legStand.Owner;
				}
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		ApplyAssemblyScaleConstraints();
		PreventAllChildScaling();
		UpdateConveyors();
		if (conveyorsTransformPrev != conveyors.Transform) {
			UpdateSides();
		}
		UpdateLegStandCoverage();
		UpdateLegStands();
		transformPrev = this.Transform;
		conveyorAnglePrev = ConveyorAngle;
		conveyorsTransformPrev = conveyors.Transform;
		autoLegStandsIntervalLegsEnabledPrev = AutoLegStandsIntervalLegsEnabled;
		autoLegStandsEndLegFrontPrev = AutoLegStandsEndLegFront;
		autoLegStandsEndLegRearPrev = AutoLegStandsEndLegRear;
		autoLegStandsMarginEndLegsPrev = AutoLegStandsMarginEndLegs;
		autoLegStandsModelScenePrev = AutoLegStandsModelScene;
	}

	protected virtual void ApplyAssemblyScaleConstraints()
	{
		// There are no constraints for this assembly.
		// This is where one would lock scale components equal to each other or a constant value, for example.
	}
	#endregion _Ready and _PhysicsProcess

	#region Decouple assembly scale from child scale
	private void PreventAllChildScaling() {
		foreach (Node3D child in GetChildren()) {
			Node3D child3D = child as Node3D;
			if (child3D != null) {
				PreventChildScaling(child3D);
			}
		}
	}

	/**
	 * Counteract the scaling of child nodes as the parent node scales.
	 *
	 * This is a hack to allow us to decouple the scaling of the assembly from the scaling of its parts.
	 *
	 * Child nodes will appear not to scale, but actually, scale inversely to the parent.
	 * Parent scale will still affect the child's position, but not its apparent rotation.
	 *
	 * The downside is the child's scale will be appear locked to (1, 1, 1).
	 * This is why all of our scalable parts aren't direct children of the assembly.
	 *
	 * @param child The child node to prevent scaling.
	 */
	private void PreventChildScaling(Node3D child) {
		var basisRotation = this.Transform.Basis.Orthonormalized();
		var basisScale = basisRotation.Inverse() * this.Transform.Basis;
		var xformScaleInverse = new Transform3D(basisScale, new Vector3(0, 0, 0)).AffineInverse();

		var basisRotationPrev = transformPrev.Basis.Orthonormalized();
		var basisScalePrev = basisRotationPrev.Inverse() * transformPrev.Basis;
		var xformScalePrev = new Transform3D(basisScalePrev, new Vector3(0, 0, 0));

		// The child transform without the effects of the parent's scale.
		var childTransformUnscaled = xformScalePrev * child.Transform;

		// Remove any remaining scale. This effectively locks child's scale to (1, 1, 1).
		childTransformUnscaled.Basis = childTransformUnscaled.Basis.Orthonormalized();

		// Adjust child's position with changes in the parent's scale.
		childTransformUnscaled.Origin *= basisScalePrev.Inverse() * basisScale;

		// Reapply inverse parent scaling to child.
		child.Transform = xformScaleInverse * childTransformUnscaled;
	}
	#endregion Decouple assembly scale from child scale
}
