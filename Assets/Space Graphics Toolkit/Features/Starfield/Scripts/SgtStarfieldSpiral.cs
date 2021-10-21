using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate a starfield in a spiral pattern.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtStarfieldSpiral")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Starfield Spiral")]
	public class SgtStarfieldSpiral : SgtStarfield
	{
		/// <summary>This allows you to set the random seed used during procedural generation.</summary>
		public int Seed { set { if (seed != value) { seed = value; DirtyMaterial(); } } get { return seed; } } [FSA("Seed")] [SerializeField] [SgtSeed] private int seed;

		/// <summary>The radius of the starfield.</summary>
		public float Radius { set { if (radius != value) { radius = value; DirtyMaterial(); } } get { return radius; } } [FSA("Radius")] [SerializeField] private float radius = 1.0f;

		/// <summary>The amount of spiral arms.</summary>
		public int ArmCount { set { if (armCount != value) { armCount = value; DirtyMaterial(); } } get { return armCount; } } [FSA("ArmCount")] [SerializeField] private int armCount = 1;

		/// <summary>The amound each arm twists.</summary>
		public float Twist { set { if (twist != value) { twist = value; DirtyMaterial(); } } get { return twist; } } [FSA("Twist")] [SerializeField] private float twist = 1.0f;

		/// <summary>This allows you to set the thickness of the star distribution at the center of the spiral.</summary>
		public float ThicknessInner { set { if (thicknessInner != value) { thicknessInner = value; DirtyMaterial(); } } get { return thicknessInner; } } [FSA("ThicknessInner")] [SerializeField] private float thicknessInner = 0.1f;

		/// <summary>This allows you to set the thickness of the star distribution at the edge of the spiral.</summary>
		public float ThicknessOuter { set { if (thicknessOuter != value) { thicknessOuter = value; DirtyMaterial(); } } get { return thicknessOuter; } } [FSA("ThicknessOuter")] [SerializeField] private float thicknessOuter = 0.3f;

		/// <summary>This allows you to push stars away from the spiral, giving you a smoother distribution.</summary>
		public float ThicknessPower { set { if (thicknessPower != value) { thicknessPower = value; DirtyMaterial(); } } get { return thicknessPower; } } [FSA("ThicknessPower")] [SerializeField] private float thicknessPower = 1.0f;

		/// <summary>The amount of stars that will be generated in the starfield.</summary>
		public int StarCount { set { if (starCount != value) { starCount = value; DirtyMaterial(); } } get { return starCount; } } [FSA("StarCount")] [SerializeField] private int starCount = 1000;

		/// <summary>Each star is given a random color from this gradient.</summary>
		public Gradient StarColors { get { if (starColors == null) starColors = new Gradient(); return starColors; } } [FSA("StarColors")] [SerializeField] private Gradient starColors;

		/// <summary>The minimum radius of stars in the starfield.</summary>
		public float StarRadiusMin { set { if (starRadiusMin != value) { starRadiusMin = value; DirtyMaterial(); } } get { return starRadiusMin; } } [FSA("StarRadiusMin")] [SerializeField] private float starRadiusMin = 0.0f;

		/// <summary>The maximum radius of stars in the starfield.</summary>
		public float StarRadiusMax { set { if (starRadiusMax != value) { starRadiusMax = value; DirtyMaterial(); } } get { return starRadiusMax; } } [FSA("StarRadiusMax")] [SerializeField] private float starRadiusMax = 0.05f;

		/// <summary>How likely the size picking will pick smaller stars over larger ones (1 = default/linear).</summary>
		public float StarRadiusBias { set { if (starRadiusBias != value) { starRadiusBias = value; DirtyMaterial(); } } get { return starRadiusBias; } } [FSA("StarRadiusBias")] [SerializeField] private float starRadiusBias = 0.0f;

		/// <summary>The maximum amount a star's size can pulse over time. A value of 1 means the star can potentially pulse between its maximum size, and 0.</summary>
		public float StarPulseMax { set { if (starPulseMax != value) { starPulseMax = value; DirtyMaterial(); } } get { return starPulseMax; } } [FSA("StarPulseMax")] [SerializeField] [Range(0.0f, 1.0f)] private float starPulseMax = 1.0f;

		// Temp vars used during generation
		private static float armStep;
		private static float twistStep;

		public static SgtStarfieldSpiral Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtStarfieldSpiral Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject      = SgtHelper.CreateGameObject("Starfield Spiral", layer, parent, localPosition, localRotation, localScale);
			var starfieldSpiral = gameObject.AddComponent<SgtStarfieldSpiral>();

			return starfieldSpiral;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Starfield/Spiral", false, 10)]
		private static void CreateMenuItem()
		{
			var parent          = SgtHelper.GetSelectedParent();
			var starfieldSpiral = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(starfieldSpiral);
		}
#endif

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			Gizmos.matrix = transform.localToWorldMatrix;

			Gizmos.DrawWireSphere(Vector3.zero, radius);
		}
#endif

		protected override int BeginQuads()
		{
			SgtHelper.BeginRandomSeed(seed);

			if (starColors == null)
			{
				starColors = SgtHelper.CreateGradient(Color.white);
			}

			armStep   = 360.0f * SgtHelper.Reciprocal(armCount);
			twistStep = 360.0f * twist;

			return starCount;
		}

		protected override void NextQuad(ref SgtStarfieldStar star, int starIndex)
		{
			var magnitude = Random.value;
			var position  = Random.insideUnitSphere * Mathf.Lerp(thicknessInner, thicknessOuter, magnitude);

			position *= Mathf.Pow(2.0f, Random.value * thicknessPower);

			position += Quaternion.AngleAxis(starIndex * armStep + magnitude * twistStep, Vector3.up) * Vector3.forward * magnitude;

			star.Variant     = Random.Range(int.MinValue, int.MaxValue);
			star.Color       = starColors.Evaluate(Random.value);
			star.Radius      = Mathf.Lerp(starRadiusMin, starRadiusMax, SgtHelper.Sharpness(Random.value, starRadiusBias));
			star.Angle       = Random.Range(-180.0f, 180.0f);
			star.Position    = position * radius;
			star.PulseRange  = Random.value * starPulseMax;
			star.PulseSpeed  = Random.value;
			star.PulseOffset = Random.value;
		}

		protected override void EndQuads()
		{
			SgtHelper.EndRandomSeed();
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtStarfieldSpiral))]
	public class SgtStarfieldSpiral_Editor : SgtStarfield_Editor<SgtStarfieldSpiral>
	{
		protected override void OnInspector()
		{
			var dirtyMaterial = false;
			var dirtyMesh     = false;

			DrawMaterial(ref dirtyMaterial);

			Separator();

			DrawMainTex(ref dirtyMaterial, ref dirtyMesh);
			DrawLayout(ref dirtyMaterial, ref dirtyMesh);

			Separator();

			DrawPointMaterial(ref dirtyMaterial);

			Separator();

			Draw("seed", ref dirtyMesh, "This allows you to set the random seed used during procedural generation.");
			Draw("radius", ref dirtyMesh, "The radius of the starfield.");
			BeginError(Any(t => t.ArmCount <= 0));
				Draw("armCount", ref dirtyMesh, "The amount of spiral arms.");
			EndError();
			Draw("twist", ref dirtyMesh, "The amound each arm twists.");
			Draw("thicknessInner", ref dirtyMesh, "This allows you to set the thickness of the star distribution at the center of the spiral.");
			Draw("thicknessOuter", ref dirtyMesh, "This allows you to set the thickness of the star distribution at the edge of the spiral.");
			Draw("thicknessPower", ref dirtyMesh, "This allows you to push stars away from the spiral, giving you a smoother distribution.");

			Separator();

			BeginError(Any(t => t.StarCount < 0));
				Draw("starCount", ref dirtyMesh, "The amount of stars that will be generated in the starfield.");
			EndError();
			Draw("starColors", ref dirtyMesh, "Each star is given a random color from this gradient.");
			BeginError(Any(t => t.StarRadiusMin < 0.0f || t.StarRadiusMin > t.StarRadiusMax));
				Draw("starRadiusMin", ref dirtyMesh, "The minimum radius of stars in the starfield.");
			EndError();
			BeginError(Any(t => t.StarRadiusMax < 0.0f || t.StarRadiusMin > t.StarRadiusMax));
				Draw("starRadiusMax", ref dirtyMesh, "The maximum radius of stars in the starfield.");
			EndError();
			Draw("starRadiusBias", ref dirtyMesh, "How likely the size picking will pick smaller stars over larger ones (0 = default/linear).");
			Draw("starPulseMax", ref dirtyMesh, "The maximum amount a star's size can pulse over time. A value of 1 means the star can potentially pulse between its maximum size, and 0.");

			RequireCamera();

			serializedObject.ApplyModifiedProperties();

			if (dirtyMaterial == true) DirtyEach(t => t.DirtyMaterial());
			if (dirtyMesh     == true) DirtyEach(t => t.DirtyMesh    ());
		}
	}
}
#endif