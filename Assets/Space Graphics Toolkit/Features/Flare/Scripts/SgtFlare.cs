using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate a high resolution mesh flare.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtFlare")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Flare")]
	public class SgtFlare : MonoBehaviour
	{
		/// <summary>This allows you to set the mesh used to render the flare.</summary>
		public Mesh Mesh { set { mesh = value; } get { return mesh; } } [FSA("Mesh")] [SerializeField] private Mesh mesh;

		/// <summary>The material used to render this flare.</summary>
		public Material Material { set { material = value; } get { return material; } } [FSA("Material")] [SerializeField] private Material material;

		/// <summary>Should the flare automatically snap to cameras.</summary>
		public bool FollowCameras { set { followCameras = value; } get { return followCameras; } } [FSA("FollowCameras")] [SerializeField] private bool followCameras;

		/// <summary>The distance from the camera this flare will be placed in world space.</summary>
		public float FollowDistance { set { followDistance = value; } get { return followDistance; } } [FSA("FollowDistance")] [SerializeField] private float followDistance = 100.0f;

		/// <summary>This allows you to offset the camera distance in world space when rendering the flare, giving you fine control over the render order.</summary>
		public float CameraOffset { set { cameraOffset = value; } get { return cameraOffset; } } [FSA("CameraOffset")] [SerializeField] private float cameraOffset;

		public static SgtFlare Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtFlare Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject = SgtHelper.CreateGameObject("Flare", layer, parent, localPosition, localRotation, localScale);
			var flare      = gameObject.AddComponent<SgtFlare>();

			return flare;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Flare", false, 10)]
		public static void CreateItem()
		{
			var parent = SgtHelper.GetSelectedParent();
			var flare  = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(flare);
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

#if UNITY_EDITOR
		protected virtual void Start()
		{
			// Upgrade scene
			// NOTE: This must be done in Start because when done in OnEnable this fails to dirty the scene
			SgtHelper.DestroyOldGameObjects(transform, "Flare Model");
		}
#endif

		private void HandleCameraDraw(Camera camera)
		{
			if (SgtHelper.CanDraw(gameObject, camera) == false) return;

			var matrix = transform.localToWorldMatrix;

			if (cameraOffset != 0.0f)
			{
				var direction = Vector3.Normalize(camera.transform.position - transform.position);

				matrix = Matrix4x4.Translate(direction * cameraOffset) * matrix;
			}

			Graphics.DrawMesh(mesh, matrix, material, gameObject.layer, camera);
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtFlare))]
	public class SgtFlare_Editor : SgtEditor<SgtFlare>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.Mesh == null));
				Draw("mesh", "This allows you to set the mesh used to render the flare.");
			EndError();
			BeginError(Any(t => t.Material == null));
				Draw("material", "The material used to render this flare.");
			EndError();
			Draw("cameraOffset", "This allows you to offset the camera distance in world space when rendering the flare, giving you fine control over the render order."); // Updated automatically
			Draw("followCameras", "Should the flare automatically snap to cameras."); // Automatically updated

			if (Any(t => t.FollowCameras == true))
			{
				BeginIndent();
					BeginError(Any(t => t.FollowDistance <= 0.0f));
						Draw("followDistance", "The distance from the camera this flare will be placed in world space."); // Automatically updated
					EndError();
				EndIndent();
			}

			if (Any(t => t.Mesh == null && t.GetComponent<SgtFlareMesh>() == null))
			{
				Separator();

				if (Button("Add Mesh") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtFlareMesh>(t.gameObject));
				}
			}

			if (Any(t => t.Material == null && t.GetComponent<SgtFlareMaterial>() == null))
			{
				Separator();

				if (Button("Add Material") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtFlareMaterial>(t.gameObject));
				}
			}
		}
	}
}
#endif