using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to render a planetary ring. This ring can be split into multiple segments to improve depth sorting.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtRing")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Ring")]
	public class SgtRing : MonoBehaviour
	{
		/// <summary>The base color will be multiplied by this.</summary>
		public Color Color { set { if (color != value) { color = value; UpdateMaterial(); } } get { return color; } } [FSA("Color")] [SerializeField] private Color color = Color.white;

		/// <summary>The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.</summary>
		public float Brightness { set { if (brightness != value) { brightness = value; UpdateMaterial(); } } get { return brightness; } } [FSA("Brightness")] [SerializeField] private float brightness = 1.0f;

		/// <summary>This allows you to adjust the render queue of the ring material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.</summary>
		public SgtRenderQueue Rayleigh { set { if (renderQueue != value) { renderQueue = value; UpdateMaterial(); } } get { return renderQueue; } } [FSA("Rayleigh")] [SerializeField] private SgtRenderQueue renderQueue = SgtRenderQueue.GroupType.Transparent;

		/// <summary>The texture applied to the ring, where the left side is the inside, and the right side is the outside.</summary>
		public Texture MainTex { set { if (mainTex != value) { mainTex = value; UpdateMaterial(); } } get { return mainTex; } } [FSA("MainTex")] [SerializeField] private Texture mainTex;

		/// <summary>This allows you to set the mesh used to render the ring.</summary>
		public Mesh Mesh { set { mesh = value; } get { return mesh; } } [FSA("Mesh")] [SerializeField] private Mesh mesh;

		/// <summary>This allows you to set how many copies of the Mesh are required to complete the ring. For example, if the Mesh is 1/4 of the ring, then Segments should be set to 4.</summary>
		public int Segments { set { segments = value; } get { return segments; } } [FSA("Segments")] [SerializeField] private int segments = 8;

		/// <summary>Should the ring have a detail texture? For example, dust noise when you get close.</summary>
		public bool Detail { set { if (detail != value) { detail = value; UpdateMaterial(); } } get { return detail; } } [FSA("Detail")] [SerializeField] private bool detail;

		/// <summary>This allows you to set the detail texture that gets repeated on the ring surface.</summary>
		public Texture DetailTex { set { if (detailTex != value) { detailTex = value; UpdateMaterial(); } } get { return detailTex; } } [FSA("DetailTex")] [SerializeField] private Texture detailTex;

		/// <summary>The detail texture horizontal tiling.</summary>
		public float DetailScaleX { set { if (detailScaleX != value) { detailScaleX = value; UpdateMaterial(); } } get { return detailScaleX; } } [FSA("DetailScaleX")] [SerializeField] private float detailScaleX = 1.0f;

		/// <summary>The detail texture vertical tiling.</summary>
		public int DetailScaleY { set { if (detailScaleY != value) { detailScaleY = value; UpdateMaterial(); } } get { return detailScaleY; } } [FSA("DetailScaleY")] [SerializeField] private int detailScaleY = 1;

		/// <summary>The UV offset of the detail texture.</summary>
		public Vector2 DetailOffset { set { if (detailOffset != value) { detailOffset = value; UpdateMaterial(); } } get { return detailOffset; } } [FSA("DetailOffset")] [SerializeField] private Vector2 detailOffset;

		/// <summary>The scroll speed of the detail texture UV offset.</summary>
		public Vector2 DetailSpeed { set { if (detailSpeed != value) { detailSpeed = value; UpdateMaterial(); } } get { return detailSpeed; } } [FSA("DetailSpeed")] [SerializeField] private Vector2 detailSpeed;

		/// <summary>The amount the detail texture is twisted around the ring.</summary>
		public float DetailTwist { set { if (detailTwist != value) { detailTwist = value; UpdateMaterial(); } } get { return detailTwist; } } [FSA("DetailTwist")] [SerializeField] private float detailTwist;

		/// <summary>The amount the twisting is pushed to the outer edge.</summary>
		public float DetailTwistBias { set { if (detailTwistBias != value) { detailTwistBias = value; UpdateMaterial(); } } get { return detailTwistBias; } } [FSA("DetailTwistBias")] [SerializeField] private float detailTwistBias = 1.0f;

		/// <summary>Enable this if you want the ring to fade out as the camera approaches.</summary>
		public bool Near { set { if (near != value) { near = value; UpdateMaterial(); } } get { return near; } } [FSA("Near")] [SerializeField] private bool near;

		/// <summary>The lookup table used to calculate the fade opacity based on distance, where the left side is used when the camera is close, and the right side is used when the camera is far.</summary>
		public Texture NearTex { set { if (nearTex != value) { nearTex = value; UpdateMaterial(); } } get { return nearTex; } } [FSA("NearTex")] [SerializeField] private Texture nearTex;

		/// <summary>The distance the fading begins from in world space.</summary>
		public float NearDistance { set { if (nearDistance != value) { nearDistance = value; UpdateMaterial(); } } get { return nearDistance; } } [FSA("NearDistance")] [SerializeField] private float nearDistance = 1.0f;

		/// <summary>If you enable this then light will scatter through the ring atmosphere. This means light entering the eye will come from all angles, especially around the light point.</summary>
		public bool Scattering { set { if (scattering != value) { scattering = value; UpdateMaterial(); } } get { return scattering; } } [FSA("Scattering")] [SerializeField] private bool scattering;

		/// <summary>The mie scattering term, allowing you to adjust the distribution of front scattered light.</summary>
		public float ScatteringMie { set { if (scatteringMie != value) { scatteringMie = value; UpdateMaterial(); } } get { return scatteringMie; } } [FSA("ScatteringMie")] [SerializeField] private float scatteringMie = 8.0f;

		/// <summary>The scattering is multiplied by this value, allowing you to easily adjust the brightness of the effect.</summary>
		public float ScatteringStrength { set { if (scatteringStrength != value) { scatteringStrength = value; UpdateMaterial(); } } get { return scatteringStrength; } } [FSA("ScatteringStrength")] [SerializeField] private float scatteringStrength = 25.0f;

		/// <summary>If you enable this then nearby SgtLight and SgtShadow casters will be found and applied to the lighting calculations.</summary>
		public bool Lit { set { if (lit != value) { lit = value; UpdateMaterial(); } } get { return lit; } } [FSA("Lit")] [SerializeField] private bool lit;

		/// <summary>The look up table associating light angle with surface color. The left side is used on the dark side, the middle is used on the horizon, and the right side is used on the light side.</summary>
		public Texture LightingTex { set { if (lightingTex != value) { lightingTex = value; UpdateMaterial(); } } get { return lightingTex; } } [FSA("LightingTex")] [SerializeField] private Texture lightingTex;

		/// <summary>The ring will always be lit by this amount.</summary>
		public Color AmbientColor { set { if (ambientColor != value) { ambientColor = value; UpdateMaterial(); } } get { return ambientColor; } } [FSA("AmbientColor")] [SerializeField] private Color ambientColor;

		// The material applied to all models
		[System.NonSerialized]
		private Material material;

		[ContextMenu("Update Material")]
		public virtual void UpdateMaterial()
		{
			if (material == null)
			{
				material = SgtHelper.CreateTempMaterial("Ring (Generated)", SgtHelper.ShaderNamePrefix + "Ring");
			}

			var color = SgtHelper.Brighten(this.color, brightness);

			material.renderQueue = renderQueue;

			material.SetColor(SgtShader._Color, color);
			material.SetTexture(SgtShader._MainTex, mainTex);

			if (detail == true)
			{
				SgtHelper.EnableKeyword("SGT_B", material); // Detail

				material.SetTexture(SgtShader._DetailTex, detailTex);
				material.SetVector(SgtShader._DetailOffset, detailOffset);
				material.SetVector(SgtShader._DetailScale, new Vector2(detailScaleX, detailScaleY));
				material.SetFloat(SgtShader._DetailTwist, detailTwist);
				material.SetFloat(SgtShader._DetailTwistBias, detailTwistBias);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_B", material); // Detail
			}

			if (near == true)
			{
				SgtHelper.EnableKeyword("SGT_C", material); // Near

				material.SetTexture(SgtShader._NearTex, nearTex);
				material.SetFloat(SgtShader._NearScale, SgtHelper.Reciprocal(nearDistance));
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_C", material); // Near
			}

			if (lit == true)
			{
				material.SetTexture(SgtShader._LightingTex, lightingTex);
				material.SetColor(SgtShader._AmbientColor, ambientColor);
			}

			if (scattering == true)
			{
				SgtHelper.EnableKeyword("SGT_A", material); // Scattering

				material.SetFloat(SgtShader._ScatteringMie, scatteringMie * scatteringMie);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_A", material); // Scattering
			}
		}

		public static SgtRing Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtRing Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject = SgtHelper.CreateGameObject("Ring", layer, parent, localPosition, localRotation, localScale);
			var ring       = gameObject.AddComponent<SgtRing>();

			return ring;
		}
#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Ring", false, 10)]
		public static void CreateMenuItem()
		{
			var parent = SgtHelper.GetSelectedParent();
			var ring   = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(ring);
		}
#endif
		protected virtual void OnEnable()
		{
			SgtCamera.OnCameraDraw += HandleCameraDraw;

			SgtHelper.OnCalculateOcclusion += CalculateOcclusion;

			UpdateMaterial();
		}

		protected virtual void OnDisable()
		{
			SgtCamera.OnCameraDraw -= HandleCameraDraw;

			SgtHelper.OnCalculateOcclusion -= CalculateOcclusion;
		}

		protected virtual void LateUpdate()
		{
			// Write lights and shadows
			SgtHelper.SetTempMaterial(material);

			var mask   = 1 << gameObject.layer;
			var lights = SgtLight.Find(lit, mask, transform.position);

			SgtShadow.Find(lit, mask, lights);
			SgtShadow.FilterOutRing(transform.position);
			SgtShadow.Write(lit, 2);

			SgtLight.FilterOut(transform.position);
			SgtLight.Write(lit, transform.position, null, null, scatteringStrength, 2);

			// Update scrolling?
			if (detail == true)
			{
				if (Application.isPlaying == true)
				{
					detailOffset += detailSpeed * Time.deltaTime;
				}

				if (material != null)
				{
					material.SetVector(SgtShader._DetailOffset, detailOffset);
				}
			}
		}

#if UNITY_EDITOR
		protected virtual void Start()
		{
			// Upgrade scene
			// NOTE: This must be done in Start because when done in OnEnable this fails to dirty the scene
			SgtHelper.DestroyOldGameObjects(transform, "Ring Model");
		}
#endif

		protected virtual void OnDestroy()
		{
			SgtHelper.Destroy(material);
		}

		private void HandleCameraDraw(Camera camera)
		{
			if (SgtHelper.CanDraw(gameObject, camera) == false) return;

			if (segments > 0)
			{
				var matrix   = transform.localToWorldMatrix;
				var rotation = Matrix4x4.Rotate(Quaternion.Euler(0.0f, 360.0f / segments, 0.0f));

				for (var i = 0; i < segments; i++)
				{
					Graphics.DrawMesh(mesh, matrix, material, gameObject.layer, camera);

					matrix *= rotation;
				}
			}
		}

		private void CalculateOcclusion(int layers, Vector4 eye, Vector4 tgt, ref float occlusion)
		{
			if (SgtOcclusion.IsValid(occlusion, layers, gameObject) == true)
			{
				var plane = new Plane(transform.up, transform.position);
				var ray   = new Ray(eye, tgt - eye);
				var dist  = default(float);

				if (plane.Raycast(ray, out dist) == true)
				{
					//var point = 
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtRing))]
	public class SgtRing_Editor : SgtEditor<SgtRing>
	{
		protected override void OnInspector()
		{
			var updateMaterial = false;

			Draw("color", ref updateMaterial, "The base color will be multiplied by this.");
			BeginError(Any(t => t.Brightness < 0.0f));
				Draw("brightness", ref updateMaterial, "The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.");
			EndError();
			Draw("renderQueue", ref updateMaterial, "This allows you to adjust the render queue of the ring material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.");

			Separator();

			BeginError(Any(t => t.MainTex == null));
				Draw("mainTex", ref updateMaterial, "The texture applied to the ring, where the left side is the inside, and the right side is the outside.");
			EndError();

			BeginError(Any(t => t.Segments < 1));
				Draw("segments", "This allows you to set how many copies of the Mesh are required to complete the ring. For example, if the Mesh is 1/4 of the ring, then Segments should be set to 4.");
			EndError();
			BeginError(Any(t => t.Mesh == null));
				Draw("mesh", "This allows you to set the mesh used to render the ring.");
			EndError();

			Separator();

			Draw("detail", ref updateMaterial, "Should the ring have a detail texture? For example, dust noise when you get close.");

			if (Any(t => t.Detail == true))
			{
				BeginIndent();
					BeginError(Any(t => t.DetailTex == null));
						Draw("detailTex", ref updateMaterial, "This allows you to set the detail texture that gets repeated on the ring surface.");
					EndError();
					BeginError(Any(t => t.DetailScaleX < 0.0f));
						Draw("detailScaleX", ref updateMaterial, "The detail texture horizontal tiling.");
					EndError();
					BeginError(Any(t => t.DetailScaleY < 1));
						Draw("detailScaleY", ref updateMaterial, "The detail texture vertical tiling.");
					EndError();
					Draw("detailOffset", ref updateMaterial, "The UV offset of the detail texture.");
					Draw("detailSpeed", ref updateMaterial, "The scroll speed of the detail texture UV offset.");
					Draw("detailTwist", ref updateMaterial, "The amount the detail texture is twisted around the ring.");
					BeginError(Any(t => t.DetailTwistBias < 1.0f));
						Draw("detailTwistBias", ref updateMaterial, "The amount the twisting is pushed to the outer edge.");
					EndError();
				EndIndent();
			}

			Separator();

			Draw("near", ref updateMaterial, "Enable this if you want the ring to fade out as the camera approaches.");

			if (Any(t => t.Near == true))
			{
				BeginIndent();
					BeginError(Any(t => t.NearTex == null));
						Draw("nearTex", ref updateMaterial, "The lookup table used to calculate the fade opacity based on distance, where the left side is used when the camera is close, and the right side is used when the camera is far.");
					EndError();
					BeginError(Any(t => t.NearDistance <= 0.0f));
						Draw("nearDistance", ref updateMaterial, "The distance the fading begins from in world space.");
					EndError();
				EndIndent();
			}

			Separator();

			Draw("lit", ref updateMaterial, "If you enable this then nearby SgtLight and SgtShadow casters will be found and applied to the lighting calculations.");

			if (Any(t => t.Lit == true))
			{
				if (SgtLight.InstanceCount == 0)
				{
					EditorGUILayout.HelpBox("You need to add the SgtLight component to your scene lights for them to work with SGT.", MessageType.Warning);
				}

				BeginIndent();
					BeginError(Any(t => t.LightingTex == null));
						Draw("lightingTex", ref updateMaterial, "The look up table associating light angle with surface color. The left side is used on the dark side, the middle is used on the horizon, and the right side is used on the light side.");
					EndError();
					Draw("ambientColor", ref updateMaterial, "The ring will always be lit by this amount.");
					Draw("scattering", ref updateMaterial, "If you enable this then light will scatter through the ring atmosphere. This means light entering the eye will come from all angles, especially around the light point.");

					if (Any(t => t.Scattering == true))
					{
						BeginIndent();
							BeginError(Any(t => t.ScatteringMie <= 0.0f));
								Draw("scatteringMie", ref updateMaterial, "The mie scattering term, allowing you to adjust the distribution of front scattered light.");
							EndError();
							Draw("scatteringStrength", "The scattering is multiplied by this value, allowing you to easily adjust the brightness of the effect."); // Updated in LateUpdate
						EndIndent();
					}
				EndIndent();
			}

			if (Any(t => t.Mesh == null && t.GetComponent<SgtRingMesh>() == null))
			{
				Separator();

				if (Button("Add Mesh") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtRingMesh>(t.gameObject));
				}
			}

			if (Any(t => t.Near == true && t.NearTex == null && t.GetComponent<SgtRingNearTex>() == null))
			{
				Separator();

				if (Button("Add NearTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtRingNearTex>(t.gameObject));
				}
			}

			if (Any(t => t.Lit == true && t.LightingTex == null && t.GetComponent<SgtRingLightingTex>() == null))
			{
				Separator();

				if (Button("Add LightingTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtRingLightingTex>(t.gameObject));
				}
			}

			if (updateMaterial == true) DirtyEach(t => t.UpdateMaterial());
		}
	}
}
#endif