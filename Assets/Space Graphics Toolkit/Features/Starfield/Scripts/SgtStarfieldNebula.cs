using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to render a nebula as a starfield from a single pixture.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtStarfieldNebula")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Starfield Nebula")]
	public class SgtStarfieldNebula : SgtStarfield
	{
		public enum SourceType
		{
			None,
			Red,
			Green,
			Blue,
			Alpha,
			AverageRgb,
			MinRgb,
			MaxRgb
		}

		/// <summary>This allows you to set the random seed used during procedural generation.</summary>
		public int Seed { set { if (seed != value) { seed = value; DirtyMaterial(); } } get { return seed; } } [FSA("Seed")] [SerializeField] [SgtSeed] private int seed;

		/// <summary>This texture used to color the nebula particles.</summary>
		public Texture SourceTex { set { if (sourceTex != value) { sourceTex = value; DirtyMaterial(); } } get { return sourceTex; } } [FSA("SourceTex")] [SerializeField] private Texture sourceTex;

		/// <summary>This brightness of the sampled SourceTex pixel for a particle to be spawned.</summary>
		public float Threshold { set { if (threshold != value) { threshold = value; DirtyMaterial(); } } get { return threshold; } } [FSA("Threshold")] [SerializeField] [Range(0.0f, 1.0f)] private float threshold = 0.1f;

		/// <summary>The amount of times a nebula point is randomly sampled, before the brightest sample is used.</summary>
		public int Samples { set { if (samples != value) { samples = value; DirtyMaterial(); } } get { return samples; } } [FSA("Samples")] [SerializeField] [Range(1, 5)] private int samples = 2;

		/// <summary>This allows you to randomly offset each nebula particle position.</summary>
		public float Jitter { set { if (jitter != value) { jitter = value; DirtyMaterial(); } } get { return jitter; } } [FSA("Jitter")] [SerializeField] [Range(0.0f, 1.0f)] private float jitter;

		/// <summary>The calculation used to find the height offset of a particle in the nebula.</summary>
		public SourceType HeightSource { set { if (heightSource != value) { heightSource = value; DirtyMaterial(); } } get { return heightSource; } } [FSA("HeightSource")] [SerializeField] private SourceType heightSource = SourceType.None;

		/// <summary>The calculation used to find the scale modified of each particle in the nebula.</summary>
		public SourceType ScaleSource { set { if (scaleSource != value) { scaleSource = value; DirtyMaterial(); } } get { return scaleSource; } } [FSA("ScaleSource")] [SerializeField] private SourceType scaleSource = SourceType.None;

		/// <summary>The size of the generated nebula.</summary>
		public Vector3 Size { set { if (size != value) { size = value; DirtyMaterial(); } } get { return size; } } [FSA("Size")] [SerializeField] private Vector3 size = new Vector3(1.0f, 1.0f, 1.0f);

		/// <summary>The brightness of the nebula when viewed from the side (good for galaxies).</summary>
		public float HorizontalBrightness { set { if (horizontalBrightness != value) { horizontalBrightness = value; DirtyMaterial(); } } get { return horizontalBrightness; } } [FSA("HorizontalBrightness")] [SerializeField] private float horizontalBrightness = 0.25f;

		/// <summary>The relationship between the Brightness and HorizontalBrightness relative to the viweing angle.</summary>
		public float HorizontalPower { set { if (horizontalPower != value) { horizontalPower = value; DirtyMaterial(); } } get { return horizontalPower; } } [FSA("HorizontalPower")] [SerializeField] private float horizontalPower = 1.0f;

		/// <summary>The amount of stars that will be generated in the starfield.</summary>
		public int StarCount { set { if (starCount != value) { starCount = value; DirtyMaterial(); } } get { return starCount; } } [FSA("StarCount")] [SerializeField] private int starCount = 1000;

		/// <summary>Each star is given a random color from this gradient.</summary>
		public Gradient StarColors { get { if (starColors == null) starColors = new Gradient(); return starColors; } } [FSA("StarColors")] [SerializeField] private Gradient starColors;

		/// <summary>This allows you to control how much the underlying nebula pixel color influences the generated star color.
		/// 0 = StarColors gradient will be used directly.
		/// 1 = Colors will be multiplied together.</summary>
		public float StarTint { set { if (starTint != value) { starTint = value; DirtyMaterial(); } } get { return starTint; } } [FSA("StarTint")] [SerializeField] [Range(0.0f, 1.0f)] private float starTint = 1.0f;

		/// <summary>Should the star color luminosity be boosted?</summary>
		public float StarBoost { set { if (starBoost != value) { starBoost = value; DirtyMaterial(); } } get { return starBoost; } } [FSA("StarBoost")] [SerializeField] [Range(0.0f, 5.0f)] private float starBoost;

		/// <summary>The minimum radius of stars in the starfield.</summary>
		public float StarRadiusMin { set { if (starRadiusMin != value) { starRadiusMin = value; DirtyMaterial(); } } get { return starRadiusMin; } } [FSA("StarRadiusMin")] [SerializeField] private float starRadiusMin = 0.0f;

		/// <summary>The maximum radius of stars in the starfield.</summary>
		public float StarRadiusMax { set { if (starRadiusMax != value) { starRadiusMax = value; DirtyMaterial(); } } get { return starRadiusMax; } } [FSA("StarRadiusMax")] [SerializeField] private float starRadiusMax = 0.05f;

		/// <summary>How likely the size picking will pick smaller stars over larger ones (1 = default/linear).</summary>
		public float StarRadiusBias { set { if (starRadiusBias != value) { starRadiusBias = value; DirtyMaterial(); } } get { return starRadiusBias; } } [FSA("StarRadiusBias")] [SerializeField] private float starRadiusBias = 1.0f;

		/// <summary>The maximum amount a star's size can pulse over time. A value of 1 means the star can potentially pulse between its maximum size, and 0.</summary>
		public float StarPulseMax { set { if (starPulseMax != value) { starPulseMax = value; DirtyMaterial(); } } get { return starPulseMax; } } [FSA("StarPulseMax")] [SerializeField] [Range(0.0f, 1.0f)] private float starPulseMax = 1.0f;

		// Temp vars used during generation
		private static Texture2D sourceTex2D;
		private static Vector3   halfSize;

		public static SgtStarfieldNebula Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtStarfieldNebula Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject      = SgtHelper.CreateGameObject("Starfield Nebula", layer, parent, localPosition, localRotation, localScale);
			var starfieldNebula = gameObject.AddComponent<SgtStarfieldNebula>();

			return starfieldNebula;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Starfield/Nebula", false, 10)]
		private static void CreateMenuItem()
		{
			var parent          = SgtHelper.GetSelectedParent();
			var starfieldNebula = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(starfieldNebula);
		}
#endif

		protected override void HandleCameraDraw(Camera camera)
		{
			if (SgtHelper.CanDraw(gameObject, camera) == false) return;

			var properties = shaderProperties.GetProperties(material, camera);

			// Change brightness based on viewing angle?
			var dir    = (transform.position - camera.transform.position).normalized;
			var theta  = Mathf.Abs(Vector3.Dot(transform.up, dir));
			var bright = Mathf.Lerp(horizontalBrightness, Brightness, Mathf.Pow(theta, horizontalPower));
			var color  = SgtHelper.Brighten(Color, Color.a * bright);

			properties.SetColor(SgtShader._Color, color);

			Graphics.DrawMesh(mesh, transform.localToWorldMatrix, material, gameObject.layer, camera, 0, properties);
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			Gizmos.matrix = transform.localToWorldMatrix;

			Gizmos.DrawWireCube(Vector3.zero, size);
		}
#endif

		protected override int BeginQuads()
		{
			SgtHelper.BeginRandomSeed(seed);

			if (starColors == null)
			{
				starColors = SgtHelper.CreateGradient(Color.white);
			}

			sourceTex2D = sourceTex as Texture2D;

			if (sourceTex2D != null && samples > 0)
			{
#if UNITY_EDITOR
				SgtHelper.MakeTextureReadable(sourceTex2D);
				SgtHelper.MakeTextureTruecolor(sourceTex2D);
#endif
				halfSize = size * 0.5f;

				return starCount;
			}

			return 0;
		}

		protected override void NextQuad(ref SgtStarfieldStar star, int starIndex)
		{
			for (var i = samples - 1; i >= 0; i--)
			{
				var sampleX = Random.Range(0.0f, 1.0f);
				var sampleY = Random.Range(0.0f, 1.0f);
				var pixel   = sourceTex2D.GetPixelBilinear(sampleX, sampleY);
				var gray    = pixel.grayscale;

				if (gray > threshold || i == 0)
				{
					var position = -halfSize + Random.insideUnitSphere * jitter * starRadiusMax;

					position.x += size.x * sampleX;
					position.y += size.y * GetWeight(heightSource, pixel, 0.5f);
					position.z += size.z * sampleY;

					star.Variant     = Random.Range(int.MinValue, int.MaxValue);
					star.Color       = starColors.Evaluate(Random.value) * Color.LerpUnclamped(Color.white, GetBoosted(pixel), starTint);
					star.Radius      = Mathf.Lerp(starRadiusMin, starRadiusMax, Mathf.Pow(Random.value, starRadiusBias)) * GetWeight(scaleSource, pixel, 1.0f);
					star.Angle       = Random.Range(-180.0f, 180.0f);
					star.Position    = position;
					star.PulseRange  = Random.value * starPulseMax;
					star.PulseSpeed  = Random.value;
					star.PulseOffset = Random.value;

					return;
				}
			}
		}

		private Color GetBoosted(Color c)
		{
			if (starBoost > 0.0f)
			{
				float h; float s; float v;
						
				Color.RGBToHSV(c, out h, out s, out v);

				v = 1.0f - Mathf.Pow(1.0f - v, 1.0f + starBoost);

				var n = Color.HSVToRGB(h,s,v);

				return new Color(n.r, n.g, n.b, c.a);
			}

			return c;
		}

		protected override void EndQuads()
		{
			SgtHelper.EndRandomSeed();
		}

		private float GetWeight(SourceType source, Color pixel, float defaultWeight)
		{
			switch (source)
			{
				case SourceType.Red: return pixel.r;
				case SourceType.Green: return pixel.g;
				case SourceType.Blue: return pixel.b;
				case SourceType.Alpha: return pixel.a;
				case SourceType.AverageRgb: return (pixel.r + pixel.g + pixel.b) / 3.0f;
				case SourceType.MinRgb: return Mathf.Min(pixel.r, Mathf.Min(pixel.g, pixel.b));
				case SourceType.MaxRgb: return Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
			}

			return defaultWeight;
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtStarfieldNebula))]
	public class SgtStarfieldNebula_Editor : SgtStarfield_Editor<SgtStarfieldNebula>
	{
		protected override void OnInspector()
		{
			var dirtyMaterial        = false;
			var dirtyMesh = false;

			DrawMaterial(ref dirtyMaterial);

			Separator();

			DrawMainTex(ref dirtyMaterial, ref dirtyMesh);
			DrawLayout(ref dirtyMaterial, ref dirtyMesh);

			Separator();

			DrawPointMaterial(ref dirtyMaterial);

			Separator();

			Draw("seed", ref dirtyMesh, "This allows you to set the random seed used during procedural generation.");
			BeginError(Any(t => t.SourceTex == null));
				Draw("sourceTex", ref dirtyMesh, "This texture used to color the nebula particles.");
			EndError();
			Draw("threshold", ref dirtyMesh, "This brightness of the sampled SourceTex pixel for a particle to be spawned.");
			Draw("samples", ref dirtyMesh, "The amount of times a nebula point is randomly sampled, before the brightest sample is used.");
			Draw("jitter", ref dirtyMesh, "This allows you to randomly offset each nebula particle position.");
			Draw("heightSource", ref dirtyMesh, "The calculation used to find the height offset of a particle in the nebula.");
			Draw("scaleSource", ref dirtyMesh, "The calculation used to find the scale modified of each particle in the nebula.");
			BeginError(Any(t => t.Size.x <= 0.0f || t.Size.y <= 0.0f || t.Size.z <= 0.0f));
				Draw("size", ref dirtyMesh, "The size of the generated nebula.");
			EndError();

			Separator();

			BeginError(Any(t => t.HorizontalBrightness < 0.0f));
				Draw("horizontalBrightness", "The brightness of the nebula when viewed from the side (good for galaxies).");
			EndError();
			BeginError(Any(t => t.HorizontalPower < 0.0f));
				Draw("horizontalPower", "The relationship between the Brightness and HorizontalBrightness relative to the viweing angle.");
			EndError();

			Separator();

			Draw("starCount", ref dirtyMesh, "The amount of stars that will be generated in the starfield.");
			Draw("starColors", ref dirtyMesh, "Each star is given a random color from this gradient.");
			Draw("starTint", ref dirtyMesh, "This allows you to control how much the underlying nebula pixel color influences the generated star color.\n\n0 = StarColors gradient will be used directly.\n\n1 = Colors will be multiplied together.");
			Draw("starBoost", ref dirtyMesh, "Should the star color luminosity be boosted?");
			BeginError(Any(t => t.StarRadiusMin < 0.0f || t.StarRadiusMin > t.StarRadiusMax));
				Draw("starRadiusMin", ref dirtyMesh, "The minimum radius of stars in the starfield.");
			EndError();
			BeginError(Any(t => t.StarRadiusMax < 0.0f || t.StarRadiusMin > t.StarRadiusMax));
				Draw("starRadiusMax", ref dirtyMesh, "The maximum radius of stars in the starfield.");
			EndError();
			BeginError(Any(t => t.StarRadiusBias < 1.0f));
				Draw("starRadiusBias", ref dirtyMesh, "How likely the size picking will pick smaller stars over larger ones (1 = default/linear).");
			EndError();
			Draw("starPulseMax", ref dirtyMesh, "The maximum amount a star's size can pulse over time. A value of 1 means the star can potentially pulse between its maximum size, and 0.");

			RequireCamera();

			serializedObject.ApplyModifiedProperties();

			if (dirtyMaterial == true) DirtyEach(t => t.DirtyMaterial());
			if (dirtyMesh     == true) DirtyEach(t => t.DirtyMesh    ());
		}
	}
}
#endif