using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to render a starfield that repeats forever.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtStarfieldInfinite")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Starfield Infinite")]
	public class SgtStarfieldInfinite : SgtStarfield
	{
		/// <summary>Should the stars fade out if they're intersecting solid geometry?</summary>
		public float Softness { set { if (softness != value) { softness = value; DirtyMaterial(); } } get { return softness; } } [FSA("Softness")] [SerializeField] [Range(0.0f, 1000.0f)] private float softness;

		/// <summary>Should the stars fade out when the camera gets too far away?</summary>
		public bool Far { set { if (far != value) { far = value; DirtyMaterial(); } } get { return far; } } [FSA("Far")] [SerializeField] private bool far;

		/// <summary>The lookup table used to calculate the fading amount based on the distance.</summary>
		public Texture FarTex { set { if (farTex != value) { farTex = value; DirtyMaterial(); } } get { return farTex; } } [FSA("FarTex")] [SerializeField] private Texture farTex;

		/// <summary>The radius of the fading effect in world coordinates.</summary>
		public float FarRadius { set { if (farRadius != value) { farRadius = value; DirtyMaterial(); } } get { return farRadius; } } [FSA("FarRadius")] [SerializeField] private float farRadius = 2.0f;

		/// <summary>The thickness of the fading effect in world coordinates.</summary>
		public float FarThickness { set { if (farThickness != value) { farThickness = value; DirtyMaterial(); } } get { return farThickness; } } [FSA("FarThickness")] [SerializeField] private float farThickness = 2.0f;

		/// <summary>This allows you to set the random seed used during procedural generation.</summary>
		public int Seed { set { if (seed != value) { seed = value; DirtyMaterial(); } } get { return seed; } } [FSA("Seed")] [SerializeField] [SgtSeed] private int seed;

		/// <summary>The size of the starfield in local space.</summary>
		public Vector3 Size { set { if (size != value) { size = value; DirtyMaterial(); } } get { return size; } } [FSA("Size")] [SerializeField] private Vector3 size = Vector3.one;

		/// <summary>The amount of stars that will be generated in the starfield.</summary>
		public int StarCount { set { if (starCount != value) { starCount = value; DirtyMaterial(); } } get { return starCount; } } [FSA("StarCount")] [SerializeField] private int starCount = 1000;

		/// <summary>Each star is given a random color from this gradient.</summary>
		public Gradient StarColors { get { if (starColors == null) starColors = new Gradient(); return starColors; } } [FSA("StarColors")] [SerializeField] private Gradient starColors;

		/// <summary>The minimum radius of stars in the starfield.</summary>
		public float StarRadiusMin { set { if (starRadiusMin != value) { starRadiusMin = value; DirtyMaterial(); } } get { return starRadiusMin; } } [FSA("StarRadiusMin")] [SerializeField] private float starRadiusMin = 0.01f;

		/// <summary>The maximum radius of stars in the starfield.</summary>
		public float StarRadiusMax { set { if (starRadiusMax != value) { starRadiusMax = value; DirtyMaterial(); } } get { return starRadiusMax; } } [FSA("StarRadiusMax")] [SerializeField] private float starRadiusMax = 0.05f;

		/// <summary>How likely the size picking will pick smaller stars over larger ones (1 = default/linear).</summary>
		public float StarRadiusBias { set { if (starRadiusBias != value) { starRadiusBias = value; DirtyMaterial(); } } get { return starRadiusBias; } } [FSA("StarRadiusBias")] [SerializeField] private float starRadiusBias = 1.0f;

		/// <summary>The maximum amount a star's size can pulse over time. A value of 1 means the star can potentially pulse between its maximum size, and 0.</summary>
		public float StarPulseMax { set { if (starPulseMax != value) { starPulseMax = value; DirtyMaterial(); } } get { return starPulseMax; } } [FSA("StarPulseMax")] [SerializeField] [Range(0.0f, 1.0f)] private float starPulseMax = 1.0f;

		public static SgtStarfieldInfinite Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtStarfieldInfinite Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject        = SgtHelper.CreateGameObject("Starfield Infinite", layer, parent, localPosition, localRotation, localScale);
			var starfieldInfinite = gameObject.AddComponent<SgtStarfieldInfinite>();

			return starfieldInfinite;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Starfield/Infinite", false, 10)]
		private static void CreateMenuItem()
		{
			var parent            = SgtHelper.GetSelectedParent();
			var starfieldInfinite = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(starfieldInfinite);
		}
#endif

		protected override void OnEnable()
		{
			base.OnEnable();

			SgtFloatingCamera.OnSnap += FloatingCameraSnap;
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			SgtFloatingCamera.OnSnap -= FloatingCameraSnap;
		}

		protected override void HandleDrawMesh(Camera camera, MaterialPropertyBlock properties)
		{
			Graphics.DrawMesh(mesh, transform.localToWorldMatrix, material, gameObject.layer, camera, 0, properties);
		}

		private void FloatingCameraSnap(SgtFloatingCamera floatingCamera, Vector3 delta)
		{
			var position = transform.position + delta;
			var extent   = size * SgtHelper.UniformScale(transform.lossyScale);

			position.x = position.x % extent.x;
			position.y = position.y % extent.y;
			position.z = position.z % extent.z;

			transform.position = position;
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			Gizmos.matrix = transform.localToWorldMatrix;

			Gizmos.DrawWireCube(Vector3.zero, size);
		}
#endif

		protected override void UpdateMaterial()
		{
			if (material == null)
			{
				material = SgtHelper.CreateTempMaterial("Starfield (Generated)", SgtHelper.ShaderNamePrefix + "StarfieldInfinite");
			}

			base.UpdateMaterial();

			if (softness > 0.0f)
			{
				SgtHelper.EnableKeyword("LIGHT_2", material); // Softness

				material.SetFloat(SgtShader._SoftParticlesFactor, SgtHelper.Reciprocal(softness));
			}
			else
			{
				SgtHelper.DisableKeyword("LIGHT_2", material); // Softness
			}

			if (far == true)
			{
				SgtHelper.EnableKeyword("SGT_E", material); // Far

				material.SetTexture(SgtShader._FarTex, farTex);
				material.SetFloat(SgtShader._FarRadius, farRadius);
				material.SetFloat(SgtShader._FarScale, SgtHelper.Reciprocal(farThickness));
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_E", material); // Far
			}

			material.SetVector(SgtShader._WrapSize, size);
			material.SetVector(SgtShader._WrapScale, SgtHelper.Reciprocal3(size));
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

		protected override void NextQuad(ref SgtStarfieldStar star, int starIndex)
		{
			var x = Random.Range(size.x * -0.5f, size.x * 0.5f);
			var y = Random.Range(size.y * -0.5f, size.y * 0.5f);
			var z = Random.Range(size.z * -0.5f, size.z * 0.5f);

			star.Variant     = Random.Range(int.MinValue, int.MaxValue);
			star.Color       = starColors.Evaluate(Random.value);
			star.Radius      = Mathf.Lerp(starRadiusMin, starRadiusMax, Mathf.Pow(Random.value, starRadiusBias));
			star.Angle       = Random.Range(-180.0f, 180.0f);
			star.Position    = new Vector3(x, y, z);
			star.PulseRange  = Random.value * starPulseMax;
			star.PulseSpeed  = Random.value;
			star.PulseOffset = Random.value;
		}

		protected override void BuildMesh(Mesh mesh, int count)
		{
			base.BuildMesh(mesh, count);

			mesh.bounds = new Bounds(Vector3.zero, Vector3.one * float.MaxValue);
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
	[CustomEditor(typeof(SgtStarfieldInfinite))]
	public class SgtStarfieldInfinite_Editor : SgtStarfield_Editor<SgtStarfieldInfinite>
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

			Draw("softness", ref dirtyMaterial, "Should the stars fade out if they're intersecting solid geometry?");

			if (Any(t => t.Softness > 0.0f))
			{
				foreach (var camera in Camera.allCameras)
				{
					if (SgtHelper.Enabled(camera) == true && camera.depthTextureMode == DepthTextureMode.None)
					{
						if ((camera.cullingMask & (1 << Target.gameObject.layer)) != 0)
						{
							if (HelpButton("You have enabled soft particles, but the '" + camera.name + "' camera does not write depth textures.", MessageType.Error, "Fix", 50.0f) == true)
							{
								var dtm = SgtHelper.GetOrAddComponent<SgtDepthTextureMode>(camera.gameObject);

								dtm.DepthMode = DepthTextureMode.Depth;

								dtm.UpdateDepthMode();

								Selection.activeObject = dtm;
							}
						}
					}
				}
			}

			DrawPointMaterial(ref dirtyMaterial);

			Draw("far", ref dirtyMaterial, "Should the stars fade out when the camera gets too far away?");

			if (Any(t => t.Far == true))
			{
				BeginIndent();
					BeginError(Any(t => t.FarTex == null));
						Draw("farTex", ref dirtyMaterial, "The lookup table used to calculate the fading amount based on the distance.");
					EndError();
					BeginError(Any(t => t.FarRadius < 0.0f));
						Draw("farRadius", ref dirtyMaterial, "The radius of the fading effect in world space.");
					EndError();
					BeginError(Any(t => t.FarThickness <= 0.0f));
						Draw("farThickness", ref dirtyMaterial, "The thickness of the fading effect in world space.");
					EndError();
				EndIndent();
			}

			Separator();

			Draw("seed", ref dirtyMesh, "This allows you to set the random seed used during procedural generation.");
			BeginError(Any(t => t.Size.x <= 0.0f || t.Size.y <= 0.0f || t.Size.z <= 0.0f));
				Draw("size", ref dirtyMesh, ref dirtyMaterial, "The radius of the starfield.");
			EndError();

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
			BeginError(Any(t => t.StarRadiusBias < 1.0f));
				Draw("starRadiusBias", ref dirtyMesh, "How likely the size picking will pick smaller stars over larger ones (1 = default/linear).");
			EndError();
			Draw("starPulseMax", ref dirtyMesh, "The maximum amount a star's size can pulse over time. A value of 1 means the star can potentially pulse between its maximum size, and 0.");
		
			RequireCamera();

			serializedObject.ApplyModifiedProperties();

			if (dirtyMaterial == true) DirtyEach(t => t.DirtyMaterial());
			if (dirtyMesh     == true) DirtyEach(t => t.DirtyMesh    ());

			if (Any(t => t.Far == true && t.FarTex == null && t.GetComponent<SgtStarfieldInfiniteFarTex>() == null))
			{
				Separator();

				if (Button("Add FarTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtStarfieldInfiniteFarTex>(t.gameObject));
				}
			}

			if (Any(t => t.GetComponentInParent<SgtFloatingObject>()))
			{
				EditorGUILayout.HelpBox("SgtStarfieldInfinite automatically snaps with the floating origin system, using SgtFloatingObject may cause issues with this GameObject.", MessageType.Warning);
			}

			if (SgtFloatingCamera.Instances.Count > 0 && Any(t => t.transform.rotation != Quaternion.identity))
			{
				EditorGUILayout.HelpBox("This transform is rotated, this may cause issues with the floating origin system.", MessageType.Warning);
			}

			if (SgtFloatingCamera.Instances.Count > 0 && Any(t => IsUniform(t.transform) == false))
			{
				EditorGUILayout.HelpBox("This transform is non-uniformly scaled, this may cause issues with the floating origin system.", MessageType.Warning);
			}
		}

		private bool IsUniform(Transform t)
		{
			var scale = t.localScale;

			if (scale.x != scale.y || scale.x != scale.z)
			{
				return false;
			}

			if (t.parent != null)
			{
				return IsUniform(t.parent);
			}

			return true;
		}
	}
}
#endif