using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to render a starfield with a distribution of a box.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtStarfieldBox")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Starfield Box")]
	public class SgtStarfieldBox : SgtStarfield
	{
		/// <summary>This allows you to set the random seed used during procedural generation.</summary>
		public int Seed { set { if (seed != value) { seed = value; DirtyMaterial(); } } get { return seed; } } [FSA("Seed")] [SerializeField] [SgtSeed] private int seed;

		/// <summary>The +- size of the starfield.</summary>
		public Vector3 Extents { set { if (extents != value) { extents = value; DirtyMaterial(); } } get { return extents; } } [FSA("Extents")] [SerializeField] private Vector3 extents = Vector3.one;

		/// <summary>How far from the center the distribution begins.</summary>
		public float Offset { set { if (offset != value) { offset = value; DirtyMaterial(); } } get { return offset; } } [FSA("Offset")] [SerializeField] [Range(0.0f, 1.0f)] private float offset;

		/// <summary>The amount of stars that will be generated in the starfield.</summary>
		public int StarCount { set { if (starCount != value) { starCount = value; DirtyMaterial(); } } get { return starCount; } } [FSA("StarCount")] [SerializeField] private int starCount = 1000;

		/// <summary>Each star is given a random color from this gradient.</summary>
		public Gradient StarColors { get { if (starColors == null) starColors = new Gradient(); return starColors; } } [FSA("StarColors")] [SerializeField] private Gradient starColors;

		/// <summary>The minimum radius of stars in the starfield.</summary>
		public float StarRadiusMin { set { if (starRadiusMin != value) { starRadiusMin = value; DirtyMaterial(); } } get { return starRadiusMin; } } [FSA("StarRadiusMin")] [SerializeField] private float starRadiusMin = 0.01f;

		/// <summary>The maximum radius of stars in the starfield.</summary>
		public float StarRadiusMax { set { if (starRadiusMax != value) { starRadiusMax = value; DirtyMaterial(); } } get { return starRadiusMax; } } [FSA("StarRadiusMax")] [SerializeField] private float starRadiusMax = 0.05f;

		/// <summary>How likely the size picking will pick smaller stars over larger ones (1 = default/linear).</summary>
		public float StarRadiusBias { set { if (starRadiusBias != value) { starRadiusBias = value; DirtyMaterial(); } } get { return starRadiusBias; } } [FSA("StarRadiusBias")] [SerializeField] private float starRadiusBias;

		/// <summary>The maximum amount a star's size can pulse over time. A value of 1 means the star can potentially pulse between its maximum size, and 0.</summary>
		public float StarPulseMax { set { if (starPulseMax != value) { starPulseMax = value; DirtyMaterial(); } } get { return starPulseMax; } } [FSA("StarPulseMax")] [SerializeField] [Range(0.0f, 1.0f)] private float starPulseMax = 1.0f;

		public static SgtStarfieldBox Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtStarfieldBox Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject   = SgtHelper.CreateGameObject("Starfield Box", layer, parent, localPosition, localRotation, localScale);
			var starfieldBox = gameObject.AddComponent<SgtStarfieldBox>();

			return starfieldBox;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Starfield/Box", false, 10)]
		private static void CreateMenuItem()
		{
			var parent       = SgtHelper.GetSelectedParent();
			var starfieldBox = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(starfieldBox);
		}
#endif

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			Gizmos.matrix = transform.localToWorldMatrix;

			Gizmos.DrawWireCube(Vector3.zero, 2.0f * extents);
			Gizmos.DrawWireCube(Vector3.zero, 2.0f * extents * offset);
		}
#endif

		protected override int BeginQuads()
		{
			SgtHelper.BeginRandomSeed(seed);

			if (starColors == null)
			{
				starColors = SgtHelper.CreateGradient(Color.white);
			}

			return starCount;
		}

		protected override void NextQuad(ref SgtStarfieldStar star, int starIndex)
		{
			var x        = Random.Range( -1.0f, 1.0f);
			var y        = Random.Range( -1.0f, 1.0f);
			var z        = Random.Range(offset, 1.0f);
			var position = default(Vector3);

			if (Random.value >= 0.5f)
			{
				z = -z;
			}

			switch (Random.Range(0, 3))
			{
				case 0: position = new Vector3(z, x, y); break;
				case 1: position = new Vector3(x, z, y); break;
				case 2: position = new Vector3(x, y, z); break;
			}

			star.Variant     = Random.Range(int.MinValue, int.MaxValue);
			star.Color       = starColors.Evaluate(Random.value);
			star.Radius      = Mathf.Lerp(starRadiusMin, starRadiusMax, SgtHelper.Sharpness(Random.value, starRadiusBias));
			star.Angle       = Random.Range(-180.0f, 180.0f);
			star.Position    = Vector3.Scale(position, extents);
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
	[CustomEditor(typeof(SgtStarfieldBox))]
	public class SgtStarfieldBox_Editor : SgtStarfield_Editor<SgtStarfieldBox>
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
			BeginError(Any(t => t.Extents == Vector3.zero));
				Draw("extents", ref dirtyMesh, "The +- size of the starfield.");
			EndError();
			Draw("offset", ref dirtyMesh, "How far from the center the distribution begins.");

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
			Draw("starRadiusBias", ref dirtyMesh, "How likely the size picking will pick smaller stars over larger ones (1 = default/linear).");
			Draw("starPulseMax", ref dirtyMesh, "The maximum amount a star's size can pulse over time. A value of 1 means the star can potentially pulse between its maximum size, and 0.");

			RequireCamera();

			serializedObject.ApplyModifiedProperties();

			if (dirtyMaterial == true) DirtyEach(t => t.DirtyMaterial());
			if (dirtyMesh     == true) DirtyEach(t => t.DirtyMesh    ());
		}
	}
}
#endif