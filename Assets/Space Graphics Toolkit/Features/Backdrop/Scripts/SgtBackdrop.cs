using UnityEngine;
using System.Collections.Generic;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate procedurally placed quads on the edge of a sphere.
	/// The quads can then be textured using clouds or stars, and will follow the rendering camera, creating a backdrop.
	/// This backdrop is very quick to render, and provides a good alternative to skyboxes because of the vastly reduced memory requirements.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtBackdrop")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Backdrop")]
	public class SgtBackdrop : SgtQuads
	{
		/// <summary>This allows you to set the random seed used during procedural generation.</summary>
		public int Seed { set { if (seed != value) { seed = value; DirtyMesh(); } } get { return seed; } } [FSA("Seed")] [SerializeField] [SgtSeed] private int seed;

		/// <summary>The radius of the starfield.</summary>
		public float Radius { set { if (radius != value) { radius = value; DirtyMesh(); } } get { return radius; } } [FSA("Radius")] [SerializeField] private float radius = 1.0f;

		/// <summary>Should more stars be placed near the horizon?</summary>
		public float Squash { set { if (squash != value) { squash = value; DirtyMesh(); } } get { return squash; } } [FSA("Squash")] [SerializeField] [Range(0.0f, 1.0f)] private float squash;

		/// <summary>The amount of stars that will be generated in the starfield.</summary>
		public int StarCount { set { if (starCount != value) { starCount = value; DirtyMesh(); } } get { return starCount; } } [FSA("StarCount")] [SerializeField] private int starCount = 1000; private void SetStarCount(float value) { StarCount = (int)value; }

		/// <summary>Each star is given a random color from this gradient.</summary>
		public Gradient StarColors { get { if (starColors == null) starColors = new Gradient(); return starColors; } } [FSA("StarColors")] [SerializeField] private Gradient starColors;

		/// <summary>The minimum radius of stars in the starfield.</summary>
		public float StarRadiusMin { set { if (starRadiusMin != value) { starRadiusMin = value; DirtyMesh(); } } get { return starRadiusMin; } } [FSA("StarRadiusMin")] [SerializeField] private float starRadiusMin = 0.01f;

		/// <summary>The maximum radius of stars in the starfield.</summary>
		public float StarRadiusMax { set { if (starRadiusMax != value) { starRadiusMax = value; DirtyMesh(); } } get { return starRadiusMax; } } [FSA("StarRadiusMax")] [SerializeField] private float starRadiusMax = 0.05f;

		/// <summary>How likely the size picking will pick smaller stars over larger ones (1 = default/linear).</summary>
		public float StarRadiusBias { set { if (starRadiusBias != value) { starRadiusBias = value; DirtyMesh(); } } get { return starRadiusBias; } } [FSA("StarRadiusBias")] [SerializeField] private float starRadiusBias;

		/// <summary>Instead of just tinting the stars with the colors, should the RGB values be raised to the power of the color?</summary>
		public bool PowerRgb { set { if (powerRgb != value) { powerRgb = value; DirtyMaterial(); } } get { return powerRgb; } } [FSA("PowerRgb")] [SerializeField] private bool powerRgb;

		/// <summary>Prevent the quads from being too small on screen?</summary>
		public bool ClampSize { set { if (clampSize != value) { clampSize = value; DirtyMaterial(); } } get { return clampSize; } } [FSA("ClampSize")] [SerializeField] private bool clampSize;

		/// <summary>The minimum size each star can be on screen in pixels. If the star goes below this size, it loses opacity proportional to the amount it would have gone under.</summary>
		public float ClampSizeMin { set { if (clampSizeMin != value) { clampSizeMin = value; DirtyMaterial(); } } get { return clampSizeMin; } } [FSA("ClampSizeMin")] [SerializeField] private float clampSizeMin = 10.0f;

		public static SgtBackdrop Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtBackdrop Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject  = SgtHelper.CreateGameObject("Backdrop", layer, parent, localPosition, localRotation, localScale);
			var backdrop    = gameObject.AddComponent<SgtBackdrop>();
			var renderQueue = backdrop.RenderQueue;

			renderQueue.Offset = -1;

			backdrop.RenderQueue = renderQueue;

			return backdrop;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Backdrop", false, 10)]
		private static void CreateMenuItem()
		{
			var parent   = SgtHelper.GetSelectedParent();
			var backdrop = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(backdrop);
		}
#endif

		protected override void UpdateMaterial()
		{
			if (material == null)
			{
				material = SgtHelper.CreateTempMaterial("Starfield (Generated)", SgtHelper.ShaderNamePrefix + "Backdrop");
			}

			base.UpdateMaterial();

			if (blendMode == BlendModeType.Default)
			{
				BuildAdditive();
			}

			if (powerRgb == true)
			{
				SgtHelper.EnableKeyword("SGT_B", material); // PowerRgb
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_B", material); // PowerRgb
			}

			if (clampSize == true)
			{
				SgtHelper.EnableKeyword("SGT_C", material); // Clamp Size

				material.SetFloat(SgtShader._ClampSizeMin, clampSizeMin * radius);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_C", material); // Clamp Size
			}
		}

		protected override int BeginQuads()
		{
			SgtHelper.BeginRandomSeed(seed);

			if (starColors == null)
			{
				starColors = SgtHelper.CreateGradient(Color.white);
			}
		
			return starCount;
		}

		protected virtual void NextQuad(ref SgtBackdropQuad star, int starIndex)
		{
			var position = Random.insideUnitSphere;

			position.y *= 1.0f - squash;

			star.Variant  = Random.Range(int.MinValue, int.MaxValue);
			star.Color    = StarColors.Evaluate(Random.value);
			star.Radius   = Mathf.Lerp(starRadiusMin, starRadiusMax, SgtHelper.Sharpness(Random.value, starRadiusBias));
			star.Angle    = Random.Range(-180.0f, 180.0f);
			star.Position = position.normalized * radius;
		}

		protected override void EndQuads()
		{
			SgtHelper.EndRandomSeed();
		}

		private static List<Vector3> positions = new List<Vector3>();
		private static List<Color32> colors32  = new List<Color32>();
		private static List<Vector2> coords1   = new List<Vector2>();
		private static List<Vector3> coords2   = new List<Vector3>();
		private static List<int>     indices   = new List<int>();

		protected override void BuildMesh(Mesh mesh, int count)
		{
			var minMaxSet = false;
			var min       = default(Vector3);
			var max       = default(Vector3);

			SgtHelper.Resize(positions, count * 4);
			SgtHelper.Resize(colors32, count * 4);
			SgtHelper.Resize(coords1, count * 4);
			SgtHelper.Resize(coords2, count * 4);
			SgtHelper.Resize(indices, count * 6);

			for (var i = 0; i < count; i++)
			{
				NextQuad(ref SgtBackdropQuad.Temp, i);

				var offV     = i * 4;
				var offI     = i * 6;
				var radius   = SgtBackdropQuad.Temp.Radius;
				var uv       = tempCoords[SgtHelper.Mod(SgtBackdropQuad.Temp.Variant, tempCoords.Count)];
				var rotation = Quaternion.FromToRotation(Vector3.back, SgtBackdropQuad.Temp.Position) * Quaternion.Euler(0.0f, 0.0f, SgtBackdropQuad.Temp.Angle);
				var up       = rotation * Vector3.up    * radius;
				var right    = rotation * Vector3.right * radius;

				ExpandBounds(ref minMaxSet, ref min, ref max, SgtBackdropQuad.Temp.Position, radius);

				positions[offV + 0] = SgtBackdropQuad.Temp.Position - up - right;
				positions[offV + 1] = SgtBackdropQuad.Temp.Position - up + right;
				positions[offV + 2] = SgtBackdropQuad.Temp.Position + up - right;
				positions[offV + 3] = SgtBackdropQuad.Temp.Position + up + right;

				colors32[offV + 0] =
				colors32[offV + 1] =
				colors32[offV + 2] =
				colors32[offV + 3] = SgtBackdropQuad.Temp.Color;

				coords1[offV + 0] = new Vector2(uv.x, uv.y);
				coords1[offV + 1] = new Vector2(uv.z, uv.y);
				coords1[offV + 2] = new Vector2(uv.x, uv.w);
				coords1[offV + 3] = new Vector2(uv.z, uv.w);

				coords2[offV + 0] =
				coords2[offV + 1] =
				coords2[offV + 2] =
				coords2[offV + 3] = SgtBackdropQuad.Temp.Position;

				indices[offI + 0] = offV + 0;
				indices[offI + 1] = offV + 1;
				indices[offI + 2] = offV + 2;
				indices[offI + 3] = offV + 3;
				indices[offI + 4] = offV + 2;
				indices[offI + 5] = offV + 1;
			}

			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			mesh.bounds      = SgtHelper.NewBoundsFromMinMax(min, max);

			mesh.Clear();
			mesh.SetVertices(positions);
			mesh.SetColors(colors32);
			mesh.SetUVs(0, coords1);
			mesh.SetUVs(1, coords2);
			mesh.SetTriangles(indices, 0, false);
		}

		protected override void HandleCameraDraw(Camera camera)
		{
			if (SgtHelper.CanDraw(gameObject, camera) == false) return;

			var properties = shaderProperties.GetProperties(material, camera);

			if (camera.orthographic == true)
			{
				properties.SetFloat(SgtShader._ClampSizeScale, camera.orthographicSize * 0.0025f);
			}
			else
			{
				properties.SetFloat(SgtShader._ClampSizeScale, Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 2.0f);
			}

			var matrix = Matrix4x4.Translate(camera.transform.position - transform.position) * transform.localToWorldMatrix;

			Graphics.DrawMesh(mesh, matrix, material, gameObject.layer, camera, 0, properties);
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtBackdrop))]
	public class SgtBackdrop_Editor : SgtQuads_Editor<SgtBackdrop>
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

			Draw("seed", ref dirtyMesh, "This allows you to set the random seed used during procedural generation.");
			BeginError(Any(t => t.Radius <= 0.0f));
				Draw("radius", ref dirtyMaterial, ref dirtyMesh, "The radius of the starfield.");
			EndError();
			Draw("squash", ref dirtyMesh, "Should more stars be placed near the horizon?");

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

			Separator();

			Draw("powerRgb", ref dirtyMaterial, "Instead of just tinting the stars with the colors, should the RGB values be raised to the power of the color?");
			Draw("clampSize", ref dirtyMaterial, ref dirtyMesh, "Prevent the quads from being too small on screen?");
			if (Any(t => t.ClampSize == true))
			{
				BeginIndent();
					Draw("clampSizeMin", ref dirtyMaterial, "The minimum size each star can be on screen in pixels. If the star goes below this size, it loses opacity proportional to the amount it would have gone under.");
				EndIndent();
			}

			if (dirtyMaterial == true) DirtyEach(t => t.DirtyMaterial());
			if (dirtyMesh     == true) DirtyEach(t => t.DirtyMesh   ());
		}
	}
}
#endif