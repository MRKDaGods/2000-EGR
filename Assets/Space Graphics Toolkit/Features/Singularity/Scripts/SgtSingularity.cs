using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to render a singularity/black hole.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtSingularity")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Singularity")]
	public class SgtSingularity : MonoBehaviour
	{
		public enum EdgeFadeType
		{
			None,
			Center,
			Fragment
		}

		/// <summary>This allows you to set the mesh used to render the singularity. This should be a sphere.</summary>
		public Mesh Mesh { set { mesh = value; } get { return mesh; } } [FSA("Mesh")] [SerializeField] Mesh mesh;

		/// <summary>This allows you to set the radius of the Mesh. If this is incorrectly set then the singularity will render incorrectly.</summary>
		public float MeshRadius { set { meshRadius = value; } get { return meshRadius; } } [FSA("MeshRadius")] [SerializeField] float meshRadius = 1.0f;

		/// <summary>This allows you to set the radius of the singularity in local space.</summary>
		public float Radius { set { radius = value; } get { return radius; } } [FSA("Radius")] [SerializeField] float radius = 1.0f;

		/// <summary>This allows you to adjust the render queue of the singularity material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.</summary>
		public SgtRenderQueue RenderQueue { set { renderQueue = value; } get { return renderQueue; } } [FSA("RenderQueue")] [SerializeField] SgtRenderQueue renderQueue = SgtRenderQueue.GroupType.Transparent;

		/// <summary>How much the singulaity distorts the screen.</summary>
		public float PinchPower { set { pinchPower = value; } get { return pinchPower; } } [FSA("PinchPower")] [SerializeField] float pinchPower = 10.0f;

		/// <summary>How large the pinch start point is.</summary>
		public float PinchOffset { set { pinchOffset = value; } get { return pinchOffset; } } [FSA("PinchOffset")] [SerializeField] [Range(0.0f, 0.5f)] float pinchOffset = 0.02f;

		/// <summary>To prevent rendering issues the singularity can be faded out as it approaches the edges of the screen. This allows you to set how the fading is calculated.</summary>
		public EdgeFadeType EdgeFade { set { edgeFade = value; } get { return edgeFade; } } [FSA("EdgeFade")] [SerializeField] EdgeFadeType edgeFade = EdgeFadeType.Fragment;

		/// <summary>How sharp the fading effect is.</summary>
		public float EdgeFadePower { set { edgeFadePower = value; } get { return edgeFadePower; } } [FSA("EdgeFadePower")] [SerializeField] float edgeFadePower = 2.0f;

		/// <summary>The color of the pinched hole.</summary>
		public Color HoleColor { set { holeColor = value; } get { return holeColor; } } [FSA("HoleColor")] [SerializeField] Color holeColor = Color.black;

		/// <summary>How sharp the hole color gradient is.</summary>
		public float HolePower { set { holePower = value; } get { return holePower; } } [FSA("HolePower")] [SerializeField] float holePower = 2.0f;

		/// <summary>Enable this if you want the singulairty to tint nearby space.</summary>
		public bool Tint { set { tint = value; } get { return tint; } } [FSA("Tint")] [SerializeField] bool tint;

		/// <summary>The color of the tint.</summary>
		public Color TintColor { set { tintColor = value; } get { return tintColor; } } [FSA("TintColor")] [SerializeField] Color tintColor = Color.red;

		/// <summary>How sharp the tint color gradient is.</summary>
		public float TintPower { set { tintPower = value; } get { return tintPower; } } [FSA("TintPower")] [SerializeField] float tintPower = 2.0f;

		// The material applied to all models
		[System.NonSerialized]
		private Material material;

		[System.NonSerialized]
		private Transform cachedTransform;

		[System.NonSerialized]
		private bool dirty = true;

		public void MarkAsDirty()
		{
			dirty = true;
		}

		public static SgtSingularity Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtSingularity Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject  = SgtHelper.CreateGameObject("Singularity", layer, parent, localPosition, localRotation, localScale);
			var singularity = gameObject.AddComponent<SgtSingularity>();

			return singularity;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Singularity", false, 10)]
		public static void CreateMenuItem()
		{
			var parent      = SgtHelper.GetSelectedParent();
			var singularity = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(singularity);
		}
#endif

		protected virtual void OnEnable()
		{
			SgtCamera.OnCameraDraw += HandleCameraDraw;

			SgtRenderingPipeline.OnPipelineChanged += HandlePipelineChanged;

			cachedTransform = GetComponent<Transform>();
		}

		protected virtual void OnDisable()
		{
			SgtCamera.OnCameraDraw -= HandleCameraDraw;

			SgtRenderingPipeline.OnPipelineChanged -= HandlePipelineChanged;
		}

#if UNITY_EDITOR
		protected virtual void Start()
		{
			// Upgrade scene
			// NOTE: This must be done in Start because when done in OnEnable this fails to dirty the scene
			SgtHelper.DestroyOldGameObjects(transform, "Singularity Model");
		}
#endif

		protected virtual void LateUpdate()
		{
			if (dirty == true)
			{
				UpdateDirty();
			}
		}

		protected virtual void OnDestroy()
		{
			SgtHelper.Destroy(material);
		}

		private void HandleCameraDraw(Camera camera)
		{
			if (SgtHelper.CanDraw(gameObject, camera) == false) return;

			var scale  = SgtHelper.Divide(radius, meshRadius);
			var matrix = cachedTransform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * scale);

			Graphics.DrawMesh(mesh, matrix, material, gameObject.layer, camera);
		}

		private void HandlePipelineChanged(string title)
		{
			material = SgtHelper.Destroy(material);

			MarkAsDirty();
		}

		private void UpdateDirty()
		{
			dirty = false;

			if (material == null)
			{
				var shaderSuffix = SgtRenderingPipeline.IsScriptable ? "SingularitySRP" : "Singularity";

				material = SgtHelper.CreateTempMaterial("Singulairty (Generated)", SgtHelper.ShaderNamePrefix + shaderSuffix);
			}

			material.renderQueue = renderQueue;

			material.SetVector(SgtShader._Center, SgtHelper.NewVector4(cachedTransform.position, 1.0f));

			material.SetFloat(SgtShader._PinchPower, pinchPower);
			material.SetFloat(SgtShader._PinchScale, SgtHelper.Reciprocal(1.0f - pinchOffset));
			material.SetFloat(SgtShader._PinchOffset, pinchOffset);

			material.SetFloat(SgtShader._HolePower, holePower);
			material.SetColor(SgtShader._HoleColor, holeColor);

			SgtHelper.SetTempMaterial(material);

			if (tint == true)
			{
				SgtHelper.EnableKeyword("SGT_A"); // Tint

				material.SetFloat(SgtShader._TintPower, tintPower);
				material.SetColor(SgtShader._TintColor, tintColor);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_A"); // Tint
			}

			if (edgeFade == EdgeFadeType.Center)
			{
				SgtHelper.EnableKeyword("SGT_B"); // Fade Center

				material.SetFloat(SgtShader._EdgeFadePower, edgeFadePower);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_B"); // Fade Center
			}

			if (edgeFade == EdgeFadeType.Fragment)
			{
				SgtHelper.EnableKeyword("SGT_C"); // Fade Fragment

				material.SetFloat(SgtShader._EdgeFadePower, edgeFadePower);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_C"); // Fade Fragment
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtSingularity))]
	public class SgtSingularity_Editor : SgtEditor<SgtSingularity>
	{
		protected override void OnInspector()
		{
			var dirty = false;

			Draw("renderQueue", ref dirty, "This allows you to adjust the render queue of the singularity material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.");

			Separator();

			BeginError(Any(t => t.PinchPower < 0.0f));
				Draw("pinchPower", ref dirty, "How much the singulaity distorts the screen.");
			EndError();
			Draw("pinchOffset", ref dirty, "How large the pinch start point is.");

			Separator();

			BeginError(Any(t => t.HolePower < 0.0f));
				Draw("holePower", ref dirty, "How sharp the hole color gradient is.");
			EndError();
			Draw("holeColor", ref dirty, "The color of the pinched hole.");

			Separator();

			Draw("tint", ref dirty, "Enable this if you want the singulairty to tint nearby space.");

			if (Any(t => t.Tint == true))
			{
				BeginIndent();
					BeginError(Any(t => t.TintPower < 0.0f));
						Draw("tintPower", ref dirty, "How sharp the tint color gradient is.");
					EndError();
					Draw("tintColor", ref dirty, "The color of the tint.");
				EndIndent();
			}

			Separator();

			Draw("edgeFade", ref dirty, "To prevent rendering issues the singularity can be faded out as it approaches the edges of the screen. This allows you to set how the fading is calculated.");

			if (Any(t => t.EdgeFade != SgtSingularity.EdgeFadeType.None))
			{
				BeginError(Any(t => t.EdgeFadePower < 0.0f));
					Draw("edgeFadePower", ref dirty, "How sharp the fading effect is.");
				EndError();
			}

			Separator();

			BeginError(Any(t => t.Mesh == null));
				Draw("mesh", "This allows you to set the mesh used to render the singularity. This should be a sphere.");
			EndError();
			BeginError(Any(t => t.MeshRadius <= 0.0f));
				Draw("meshRadius", "This allows you to set the radius of the Mesh. If this is incorrectly set then the singularity will render incorrectly.");
			EndError();
			BeginError(Any(t => t.Radius <= 0.0f));
				Draw("radius", "This allows you to set the radius of the singularity in local space.");
			EndError();

			if (Any(t => SetMeshAndSetMeshRadius(t, false)))
			{
				Separator();

				if (Button("Set Mesh & Set Mesh Radius") == true)
				{
					Each(t => SetMeshAndSetMeshRadius(t, true));
				}
			}

			if (dirty == true) DirtyEach(t => t.MarkAsDirty());
		}

		private bool SetMeshAndSetMeshRadius(SgtSingularity singularity, bool apply)
		{
			if (singularity.Mesh == null)
			{
				var mesh = SgtHelper.LoadFirstAsset<Mesh>("Geosphere40 t:mesh");

				if (mesh != null)
				{
					if (apply == true)
					{
						singularity.Mesh       = mesh;
						singularity.MeshRadius = SgtHelper.GetMeshRadius(mesh);
					}

					return true;
				}
			}

			return false;
		}
	}
}
#endif