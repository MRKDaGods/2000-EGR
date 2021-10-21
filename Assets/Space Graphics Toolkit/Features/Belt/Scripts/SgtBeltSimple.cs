using UnityEngine;
using UnityEngine.Serialization;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate an asteroid belt with a simple exponential distribution.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtBeltSimple")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Belt Simple")]
	public class SgtBeltSimple : SgtBelt
	{
		/// <summary>This allows you to set the random seed used during procedural generation.</summary>
		public int Seed { set { if (seed != value) { seed = value; DirtyMaterial(); } } get { return seed; } } [FSA("Seed")] [SerializeField] [SgtSeed] private int seed;

		/// <summary>The thickness of the belt in local coordinates.</summary>
		public float Thickness { set { if (thickness != value) { thickness = value; DirtyMaterial(); } } get { return thickness; } } [FSA("Thickness")] [SerializeField] private float thickness;

		/// <summary>The higher this value, the less large asteroids will be generated.</summary>
		public float ThicknessBias { set { if (thicknessBias != value) { thicknessBias = value; DirtyMaterial(); } } get { return thicknessBias; } } [FSA("ThicknessBias")] [SerializeField] private float thicknessBias = 1.0f;

		/// <summary>The radius of the inner edge of the belt in local coordinates.</summary>
		public float InnerRadius { set { if (innerRadius != value) { innerRadius = value; DirtyMaterial(); } } get { return innerRadius; } } [FSA("InnerRadius")] [SerializeField] private float innerRadius = 1.0f;

		/// <summary>The speed of asteroids orbiting on the inner edge of the belt in radians.</summary>
		public float InnerSpeed { set { if (innerSpeed != value) { innerSpeed = value; DirtyMaterial(); } } get { return innerSpeed; } } [FSA("InnerSpeed")] [SerializeField] private float innerSpeed = 0.1f;

		/// <summary>The radius of the outer edge of the belt in local coordinates.</summary>
		public float OuterRadius { set { if (outerRadius != value) { outerRadius = value; DirtyMaterial(); } } get { return outerRadius; } } [FSA("OuterRadius")] [SerializeField] private float outerRadius = 2.0f;

		/// <summary>The speed of asteroids orbiting on the outer edge of the belt in radians.</summary>
		public float OuterSpeed { set { if (outerSpeed != value) { outerSpeed = value; DirtyMaterial(); } } get { return outerSpeed; } } [FSA("OuterSpeed")] [SerializeField] private float outerSpeed = 0.05f;

		/// <summary>The higher this value, the more likely asteroids will spawn on the inner edge of the ring.</summary>
		public float RadiusBias { set { if (radiusBias != value) { radiusBias = value; DirtyMaterial(); } } get { return radiusBias; } } [FSA("RadiusBias")] [SerializeField] private float radiusBias = 0.25f;

		/// <summary>How much random speed can be added to each asteroid.</summary>
		public float SpeedSpread { set { if (speedSpread != value) { speedSpread = value; DirtyMaterial(); } } get { return speedSpread; } } [FSA("SpeedSpread")] [SerializeField] private float speedSpread;

		/// <summary>The amount of asteroids generated in the belt.</summary>
		public int AsteroidCount { set { if (asteroidCount != value) { asteroidCount = value; DirtyMaterial(); } } get { return asteroidCount; } } [FSA("AsteroidCount")] [SerializeField] private int asteroidCount = 1000;

		/// <summary>Each asteroid is given a random color from this gradient.</summary>
		public Gradient AsteroidColors { get { if (asteroidColors == null) asteroidColors = new Gradient(); return asteroidColors; } } [FSA("AsteroidColors")] [SerializeField] private Gradient asteroidColors;

		/// <summary>The maximum amount of angular velcoity each asteroid has.</summary>
		public float AsteroidSpin { set { if (asteroidSpin != value) { asteroidSpin = value; DirtyMaterial(); } } get { return asteroidSpin; } } [FSA("AsteroidSpin")] [SerializeField] private float asteroidSpin = 1.0f;

		/// <summary>The minimum asteroid radius in local coordinates.</summary>
		public float AsteroidRadiusMin { set { if (asteroidRadiusMin != value) { asteroidRadiusMin = value; DirtyMaterial(); } } get { return asteroidRadiusMin; } } [FSA("AsteroidRadiusMin")] [SerializeField] private float asteroidRadiusMin = 0.025f;

		/// <summary>The maximum asteroid radius in local coordinates.</summary>
		public float AsteroidRadiusMax { set { if (asteroidRadiusMax != value) { asteroidRadiusMax = value; DirtyMaterial(); } } get { return asteroidRadiusMax; } } [FSA("AsteroidRadiusMax")] [SerializeField] private float asteroidRadiusMax = 0.05f;

		/// <summary>How likely the size picking will pick smaller asteroids over larger ones (1 = default/linear).</summary>
		public float AsteroidRadiusBias { set { if (asteroidRadiusBias != value) { asteroidRadiusBias = value; DirtyMaterial(); } } get { return asteroidRadiusBias; } } [FSA("AsteroidRadiusBias")] [SerializeField] private float asteroidRadiusBias = 0.0f;

		public static SgtBeltSimple Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtBeltSimple Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject = SgtHelper.CreateGameObject("Belt Simple", layer, parent, localPosition, localRotation, localScale);
			var simpleBelt = gameObject.AddComponent<SgtBeltSimple>();

			return simpleBelt;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Belt Simple", false, 10)]
		public static void CreateMenuItem()
		{
			var parent     = SgtHelper.GetSelectedParent();
			var simpleBelt = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(simpleBelt);
		}
#endif

		protected override int BeginQuads()
		{
			SgtHelper.BeginRandomSeed(seed);

			if (asteroidColors == null)
			{
				asteroidColors = SgtHelper.CreateGradient(Color.white);
			}

			return asteroidCount;
		}

		protected override void NextQuad(ref SgtBeltAsteroid asteroid, int asteroidIndex)
		{
			var distance01 = SgtHelper.Sharpness(Random.value * Random.value, radiusBias);

			asteroid.Variant       = Random.Range(int.MinValue, int.MaxValue);
			asteroid.Color         = asteroidColors.Evaluate(Random.value);
			asteroid.Radius        = Mathf.Lerp(asteroidRadiusMin, asteroidRadiusMax, SgtHelper.Sharpness(Random.value, asteroidRadiusBias));
			asteroid.Height        = Mathf.Pow(Random.value, thicknessBias) * thickness * (Random.value < 0.5f ? -0.5f : 0.5f);
			asteroid.Angle         = Random.Range(0.0f, Mathf.PI * 2.0f);
			asteroid.Spin          = Random.Range(-asteroidSpin, asteroidSpin);
			asteroid.OrbitAngle    = Random.Range(0.0f, Mathf.PI * 2.0f);
			asteroid.OrbitSpeed    = Mathf.Lerp(innerSpeed, outerSpeed, distance01);
			asteroid.OrbitDistance = Mathf.Lerp(innerRadius, outerRadius, distance01);

			asteroid.OrbitSpeed += Random.Range(-speedSpread, speedSpread) * asteroid.OrbitSpeed;
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
	[CustomEditor(typeof(SgtBeltSimple))]
	public class SgtBeltSimple_Editor : SgtBelt_Editor<SgtBeltSimple>
	{
		protected override void OnInspector()
		{
			var dirtyMaterial = false;
			var dirtyMesh     = false;

			DrawMaterial(ref dirtyMaterial);

			Separator();

			DrawMainTex(ref dirtyMaterial, ref dirtyMesh);

			Separator();

			DrawLighting(ref dirtyMaterial);

			Separator();

			Draw("seed", ref dirtyMesh, "This allows you to set the random seed used during procedural generation.");
			Draw("thickness", ref dirtyMesh, "The thickness of the belt in local coordinates.");
			BeginError(Any(t => t.ThicknessBias < 1.0f));
				Draw("thicknessBias", ref dirtyMesh, "The higher this value, the less large asteroids will be generated.");
			EndError();
			BeginError(Any(t => t.InnerRadius < 0.0f || t.InnerRadius > t.OuterRadius));
				Draw("innerRadius", ref dirtyMesh, "The radius of the inner edge of the belt in local coordinates.");
			EndError();
			Draw("innerSpeed", ref dirtyMesh, "The speed of asteroids orbiting on the inner edge of the belt in radians.");
			BeginError(Any(t => t.OuterRadius < 0.0f || t.InnerRadius > t.OuterRadius));
				Draw("outerRadius", ref dirtyMesh, "The radius of the outer edge of the belt in local coordinates.");
			EndError();
			Draw("outerSpeed", ref dirtyMesh, "The speed of asteroids orbiting on the outer edge of the belt in radians.");

			Separator();

			Draw("radiusBias", ref dirtyMesh, "The higher this value, the more likely asteroids will spawn on the inner edge of the ring.");
			Draw("speedSpread", ref dirtyMesh, "How much random speed can be added to each asteroid.");

			Separator();

			BeginError(Any(t => t.AsteroidCount < 0));
				Draw("asteroidCount", ref dirtyMesh, "The amount of asteroids generated in the belt.");
			EndError();
			Draw("asteroidColors", ref dirtyMesh, "Each asteroid is given a random color from this gradient.");
			Draw("asteroidSpin", ref dirtyMesh, "The maximum amount of angular velcoity each asteroid has.");
			BeginError(Any(t => t.AsteroidRadiusMin < 0.0f || t.AsteroidRadiusMin > t.AsteroidRadiusMax));
				Draw("asteroidRadiusMin", ref dirtyMesh, "The minimum asteroid radius in local coordinates.");
			EndError();
			BeginError(Any(t => t.AsteroidRadiusMax < 0.0f || t.AsteroidRadiusMin > t.AsteroidRadiusMax));
				Draw("asteroidRadiusMax", ref dirtyMesh, "The maximum asteroid radius in local coordinates.");
			EndError();
			Draw("asteroidRadiusBias", ref dirtyMesh, "How likely the size picking will pick smaller asteroids over larger ones (0 = default/linear).");

			RequireCamera();

			serializedObject.ApplyModifiedProperties();

			if (dirtyMaterial == true) DirtyEach(t => t.DirtyMaterial());
			if (dirtyMesh     == true) DirtyEach(t => t.DirtyMesh    ());
		}
	}
}
#endif