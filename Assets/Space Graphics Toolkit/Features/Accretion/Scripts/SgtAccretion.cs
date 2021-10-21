using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to render an accretion disc. This disc can be animated to spiral dust into the center. This disc can be split into multiple segments to improve depth sorting.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtAccretion")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Accretion")]
	public class SgtAccretion : MonoBehaviour
	{
		/// <summary>The base color will be multiplied by this.</summary>
		public Color Color { set { if (color != value) { color = value; DirtyMaterial(); } } get { return color; } } [FSA("Color")] [SerializeField] private Color color = Color.white;

		/// <summary>The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.</summary>
		public float Brightness { set { if (brightness != value) { brightness = value; DirtyMaterial(); } } get { return brightness; } } [FSA("Brightness")] [SerializeField] private float brightness = 1.0f;

		/// <summary>This allows you to adjust the render queue of the disc material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.</summary>
		public SgtRenderQueue RenderQueue { set { if (renderQueue != value) { renderQueue = value; DirtyMaterial(); } } get { return renderQueue; } } [FSA("RenderQueue")] [SerializeField] private SgtRenderQueue renderQueue = SgtRenderQueue.GroupType.Transparent;

		/// <summary>The texture applied to the disc, where the left side is the inside, and the right side is the outside.</summary>
		public Texture MainTex { set { if (mainTex != value) { mainTex = value; DirtyMaterial(); } } get { return mainTex; } } [FSA("MainTex")] [SerializeField] private Texture mainTex;

		/// <summary>This allows you to set the mesh used to render the disc.</summary>
		public Mesh Mesh { set { mesh = value; } get { return mesh; } } [FSA("Mesh")] [SerializeField] private Mesh mesh;

		/// <summary>This allows you to set how many copies of the Mesh are required to complete the disc. For example, if the Mesh is 1/4 of the disc, then Segments should be set to 4.</summary>
		public int Segments { set { segments = value; } get { return segments; } } [FSA("Segments")] [SerializeField] private int segments = 8;

		/// <summary>Should the disc have a detail texture? For example, dust noise when you get close.</summary>
		public bool Detail { set { if (detail != value) { detail = value; DirtyMaterial(); } } get { return detail; } } [FSA("Detail")] [SerializeField] private bool detail;

		/// <summary>This allows you to set the detail texture that gets repeated on the disc surface.</summary>
		public Texture DetailTex { set { if (detailTex != value) { detailTex = value; DirtyMaterial(); } } get { return detailTex; } } [FSA("DetailTex")] [SerializeField] private Texture detailTex;

		/// <summary>The detail texture horizontal tiling.</summary>
		public float DetailScaleX { set { if (detailScaleX != value) { detailScaleX = value; DirtyMaterial(); } } get { return detailScaleX; } } [FSA("DetailScaleX")] [SerializeField] private float detailScaleX = 1.0f;

		/// <summary>The detail texture vertical tiling.</summary>
		public int DetailScaleY { set { if (detailScaleY != value) { detailScaleY = value; DirtyMaterial(); } } get { return detailScaleY; } } [FSA("DetailScaleY")] [SerializeField] private int detailScaleY = 1;

		/// <summary>The UV offset of the detail texture.</summary>
		public Vector2 DetailOffset { set { if (detailOffset != value) { detailOffset = value; DirtyMaterial(); } } get { return detailOffset; } } [FSA("DetailOffset")] [SerializeField] private Vector2 detailOffset;

		/// <summary>The scroll speed of the detail texture UV offset.</summary>
		public Vector2 DetailSpeed { set { if (detailSpeed != value) { detailSpeed = value; DirtyMaterial(); } } get { return detailSpeed; } } [FSA("DetailSpeed")] [SerializeField] private Vector2 detailSpeed = new Vector2(1.0f, 1.0f);

		/// <summary>The amount the detail texture is twisted around the disc.</summary>
		public float DetailTwist { set { if (detailTwist != value) { detailTwist = value; DirtyMaterial(); } } get { return detailTwist; } } [FSA("DetailTwist")] [SerializeField] private float detailTwist;

		/// <summary>The amount the twisting is pushed to the outer edge.</summary>
		public float DetailTwistBias { set { if (detailTwistBias != value) { detailTwistBias = value; DirtyMaterial(); } } get { return detailTwistBias; } } [FSA("DetailTwistBias")] [SerializeField] private float detailTwistBias = 1.0f;

		/// <summary>Enable this if you want the disc to fade out as the camera approaches.</summary>
		public bool Near { set { if (near != value) { near = value; DirtyMaterial(); } } get { return near; } } [FSA("Near")] [SerializeField] private bool near;

		/// <summary>The lookup table used to calculate the fade opacity based on distance, where the left side is used when the camera is close, and the right side is used when the camera is far.</summary>
		public Texture NearTex { set { if (nearTex != value) { nearTex = value; DirtyMaterial(); } } get { return nearTex; } } [FSA("NearTex")] [SerializeField] private Texture nearTex;

		/// <summary>The world space distance the fade will begin from.</summary>
		public float NearDistance { set { if (nearDistance != value) { nearDistance = value; DirtyMaterial(); } } get { return nearDistance; } } [FSA("NearDistance")] [SerializeField] private float nearDistance = 1.0f;

		public void DirtyMaterial()
		{
			dirtyMaterial = true;
		}

		/// <summary>The material applied to each model.</summary>
		[System.NonSerialized]
		private Material material;

		[System.NonSerialized]
		private bool dirtyMaterial = true;

		public static SgtAccretion Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtAccretion Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject = SgtHelper.CreateGameObject("Accretion", layer, parent, localPosition, localRotation, localScale);
			var accretion  = gameObject.AddComponent<SgtAccretion>();

			return accretion;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Accretion", false, 10)]
		public static void CreateMenuItem()
		{
			var parent    = SgtHelper.GetSelectedParent();
			var accretion = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(accretion);
		}
#endif

		protected virtual void OnEnable()
		{
			SgtCamera.OnCameraDraw += HandleCameraDraw;
		}

		protected virtual void OnDisable()
		{
			SgtCamera.OnCameraDraw -= HandleCameraDraw;
		}

		protected virtual void LateUpdate()
		{
			if (dirtyMaterial == true)
			{
				dirtyMaterial = false; UpdateMaterial();
			}

			if (detail == true && material != null)
			{
				if (Application.isPlaying == true)
				{
					detailOffset += detailSpeed * Time.deltaTime;
				}

				material.SetVector(SgtShader._DetailOffset, detailOffset);
			}
		}

#if UNITY_EDITOR
		protected virtual void Start()
		{
			// Upgrade scene
			// NOTE: This must be done in Start because when done in OnEnable this fails to dirty the scene
			SgtHelper.DestroyOldGameObjects(transform, "Accretion Model");
		}
#endif

		protected virtual void OnDestroy()
		{
			SgtHelper.Destroy(material);
		}

		protected virtual void OnDidApplyAnimationProperties()
		{
			DirtyMaterial();
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

		private void UpdateMaterial()
		{
			if (material == null)
			{
				material = SgtHelper.CreateTempMaterial("Accretion (Generated)", SgtHelper.ShaderNamePrefix + "Accretion");
			}

			material.renderQueue = renderQueue;

			material.SetColor(SgtShader._Color, SgtHelper.Brighten(color, brightness));
			material.SetTexture(SgtShader._MainTex, mainTex);

			if (detail == true)
			{
				SgtHelper.EnableKeyword("SGT_B", material); // Detail

				material.SetTexture(SgtShader._DetailTex, detailTex);
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
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtAccretion))]
	public class SgtAccretion_Editor : SgtEditor<SgtAccretion>
	{
		protected override void OnInspector()
		{
			var dirtyMaterial = false;

			Draw("color", ref dirtyMaterial, "The base color will be multiplied by this.");
			BeginError(Any(t => t.Brightness < 0.0f));
				Draw("brightness", ref dirtyMaterial, "The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.");
			EndError();
			Draw("renderQueue", ref dirtyMaterial, "This allows you to adjust the render queue of the disc material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.");

			Separator();

			BeginError(Any(t => t.MainTex == null));
				Draw("mainTex", ref dirtyMaterial, "The texture applied to the accretion, where the left side is the inside, and the right side is the outside.");
			EndError();

			BeginError(Any(t => t.Segments < 1));
				Draw("segments", "This allows you to set how many copies of the Mesh are required to complete the disc. For example, if the Mesh is 1/4 of the ring, then Segments should be set to 4.");
			EndError();
			BeginError(Any(t => t.Mesh == null));
				Draw("mesh", "This allows you to set the mesh used to render the disc.");
			EndError();

			Separator();

			Draw("detail", ref dirtyMaterial, "Should the disc have a detail texture? For example, dust noise when you get close.");

			if (Any(t => t.Detail == true))
			{
				BeginIndent();
					BeginError(Any(t => t.DetailTex == null));
						Draw("detailTex", ref dirtyMaterial, "This allows you to set the detail texture that gets repeated on the disc surface.");
					EndError();
					BeginError(Any(t => t.DetailScaleX < 0.0f));
						Draw("detailScaleX", ref dirtyMaterial, "The detail texture horizontal tiling.");
					EndError();
					BeginError(Any(t => t.DetailScaleY < 1));
						Draw("detailScaleY", ref dirtyMaterial, "The detail texture vertical tiling.");
					EndError();
					Draw("detailOffset", ref dirtyMaterial, "The UV offset of the detail texture.");
					Draw("detailSpeed", ref dirtyMaterial, "The scroll speed of the detail texture UV offset.");
					Draw("detailTwist", ref dirtyMaterial, "The amount the detail texture is twisted around the disc.");
					BeginError(Any(t => t.DetailTwistBias < 1.0f));
						Draw("detailTwistBias", ref dirtyMaterial, "The amount the twisting is pushed to the outer edge.");
					EndError();
				EndIndent();
			}

			Separator();

			Draw("near", ref dirtyMaterial, "Enable this if you want the disc to fade out as the camera approaches.");

			if (Any(t => t.Near == true))
			{
				BeginIndent();
					BeginError(Any(t => t.NearTex == null));
						Draw("nearTex", ref dirtyMaterial, "The lookup table used to calculate the fade opacity based on distance, where the left side is used when the camera is close, and the right side is used when the camera is far.");
					EndError();
					BeginError(Any(t => t.NearDistance <= 0.0f));
						Draw("nearDistance", ref dirtyMaterial, "The distance the fading begins from in world space.");
					EndError();
				EndIndent();
			}

			if (Any(t => t.Mesh == null && t.GetComponent<SgtAccretionMesh>() == null))
			{
				Separator();

				if (Button("Add Mesh") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtAccretionMesh>(t.gameObject));
				}
			}

			if (Any(t => t.Near == true && t.NearTex == null && t.GetComponent<SgtAccretionNearTex>() == null))
			{
				Separator();

				if (Button("Add NearTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtAccretionNearTex>(t.gameObject));
				}
			}

			if (dirtyMaterial == true)
			{
				DirtyEach(t => t.DirtyMaterial());
			}
		}
	}
}
#endif