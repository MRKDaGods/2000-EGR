using System.Collections.Generic;
using UnityEngine;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to render a planet that has been displaced with a heightmap, and has a dynamic water level.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtPlanet")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Planet")]
	public class SgtPlanet : MonoBehaviour
	{
		public enum ChannelType
		{
			Red,
			Green,
			Blue,
			Alpha
		}

		/// <summary>The sphere mesh used to render the planet.</summary>
		public Mesh Mesh { set { mesh = value; DirtyMesh(); } get { return mesh; } } [SerializeField] private Mesh mesh;

		/// <summary>If you want the generated mesh to have a matching collider, you can specify it here.</summary>
		public MeshCollider MeshCollider { set { meshCollider = value; DirtyMesh(); } get { return meshCollider; } } [SerializeField] private MeshCollider meshCollider;

		/// <summary>The radius of the planet in local space.</summary>
		public float Radius { set { radius = value; DirtyMesh(); } get { return radius; } } [SerializeField] private float radius = 1.0f;

		/// <summary>The material used to render the planet. For best results, this should use the SGT Planet shader.</summary>
		public Material Material { set { material = value; } get { return material; } } [SerializeField] private Material material;

		/// <summary>If you want to apply a shared material (e.g. atmosphere) to this terrain, then specify it here.</summary>
		public SgtSharedMaterial SharedMaterial { set { sharedMaterial = value; } get { return sharedMaterial; } } [SerializeField] private SgtSharedMaterial sharedMaterial;

		/// <summary>The heightmap texture, where the height data is stored in the alpha channel.
		/// NOTE: This should use an Equirectangular projection.
		/// NOTE: This texture should be marked as readable.</summary>
		public Texture2D Heightmap { set { heightmap = value; DirtyMesh(); } get { return heightmap; } } [SerializeField] private Texture2D heightmap;

		/// <summary>This allows you to choose which color channel from the heightmap texture will be used.
		/// NOTE: If your texture uses a 1 byte per channel format like Alpha8/R8, then this setting will be ignored.</summary>
		public ChannelType Channel { set { channel = value; DirtyMesh(); } get { return channel; } } [SerializeField] private ChannelType channel = ChannelType.Alpha;

		/// <summary>The maximum height displacement applied to the planet mesh when the heightmap alpha value is 1.</summary>
		public float Displacement { set { displacement = value; DirtyMesh(); } get { return displacement; } } [SerializeField] private float displacement = 0.1f;

		/// <summary>The current water level.
		/// 0 = Radius.
		/// 1 = Radius + Displacement.</summary>
		public float WaterLevel { set { waterLevel = value; DirtyMesh(); } get { return waterLevel; } } [Range(-2.0f, 2.0f)] [SerializeField] private float waterLevel;

		/// <summary>If you enable this then the water will not rise, instead the terrain will shrink down.</summary>
		public bool ClampWater { set { clampWater = value; DirtyMesh(); } get { return clampWater; } } [SerializeField] private bool clampWater;

		[System.NonSerialized]
		private Mesh generatedMesh;

		[System.NonSerialized]
		private List<Vector3> generatedPositions = new List<Vector3>();

		[System.NonSerialized]
		private List<Vector4> generatedCoords = new List<Vector4>();

		[System.NonSerialized]
		private MaterialPropertyBlock properties;

		[System.NonSerialized]
		private bool dirtyMesh;

		public void DirtyMesh()
		{
			dirtyMesh = true;
		}

		/// <summary>This method causes the planet mesh to update based on the current settings. You should call this after you finish modifying them.</summary>
		[ContextMenu("Rebuild")]
		public void Rebuild()
		{
			dirtyMesh         = false;
			generatedMesh = SgtHelper.Destroy(generatedMesh);

			if (mesh != null)
			{
				generatedMesh = Instantiate(mesh);

				generatedMesh.GetVertices(generatedPositions);
				generatedMesh.GetUVs(0, generatedCoords);

				var count = generatedMesh.vertexCount;

#if UNITY_EDITOR
				SgtHelper.MakeTextureReadable(heightmap);
#endif

				for (var i = 0; i < count; i++)
				{
					var height = radius;
					var vector = generatedPositions[i].normalized;
					var coord  = generatedCoords[i];

					if (vector.y > 0.0f)
					{
						coord.z = -vector.x * 0.5f;
						coord.w = vector.z * 0.5f;
					}
					else
					{
						coord.z = vector.x * 0.5f;
						coord.w = vector.z * 0.5f;
					}

					generatedCoords[i] = coord;

					generatedPositions[i] = vector * Sample(vector);
				}

				generatedMesh.bounds = new Bounds(Vector3.zero, Vector3.one * (radius + displacement) * 2.0f);

				generatedMesh.SetVertices(generatedPositions);
				generatedMesh.SetUVs(0, generatedCoords);

				generatedMesh.RecalculateNormals();
				generatedMesh.RecalculateTangents();

				if (meshCollider != null)
				{
					meshCollider.sharedMesh = null;
					meshCollider.sharedMesh = generatedMesh;
				}
			}
		}

		public static SgtPlanet Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtPlanet Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject = SgtHelper.CreateGameObject("Planet", layer, parent, localPosition, localRotation, localScale);
			var instance   = gameObject.AddComponent<SgtPlanet>();

			return instance;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Planet", false, 10)]
		public static void CreateMenuItem()
		{
			var parent   = SgtHelper.GetSelectedParent();
			var instance = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(instance);
		}
#endif

		protected virtual void OnEnable()
		{
			SgtCamera.OnCameraDraw += HandleCameraDraw;

			SgtHelper.OnCalculateDistance += HandleCalculateDistance;
		}

		protected virtual void OnDisable()
		{
			SgtCamera.OnCameraDraw -= HandleCameraDraw;

			SgtHelper.OnCalculateDistance -= HandleCalculateDistance;
		}

		protected virtual void LateUpdate()
		{
			if (generatedMesh == null || dirtyMesh == true)
			{
				Rebuild();
			}

			if (generatedMesh != null && material != null)
			{
				if (properties == null)
				{
					properties = new MaterialPropertyBlock();
				}

				properties.SetFloat(SgtShader._WaterLevel, waterLevel);
			}
		}

		private void HandleCameraDraw(Camera camera)
		{
			if (SgtHelper.CanDraw(gameObject, camera) == false) return;

			Graphics.DrawMesh(generatedMesh, transform.localToWorldMatrix, material, gameObject.layer, camera, 0, properties);

			if (sharedMaterial != null && sharedMaterial.Material != null)
			{
				Graphics.DrawMesh(generatedMesh, transform.localToWorldMatrix, sharedMaterial.Material, gameObject.layer, camera, 0, properties);
			}
		}

		protected virtual void OnDestroy()
		{
			SgtHelper.Destroy(generatedMesh);
		}

		private void HandleCalculateDistance(Vector3 worldPosition, ref float distance)
		{
			var localPosition = transform.InverseTransformPoint(worldPosition);

			localPosition = localPosition.normalized * Sample(localPosition);

			var surfacePosition = transform.TransformPoint(localPosition);
			var thisDistance    = Vector3.Distance(worldPosition, surfacePosition);

			if (thisDistance < distance)
			{
				distance = thisDistance;
			}
		}

		private float Sample(Vector3 vector)
		{
			var final = radius;

			if (heightmap != null)
			{
				var uv   = SgtHelper.CartesianToPolarUV(vector);
				var land = heightmap.GetPixelBilinear(uv.x, uv.y)[(int)channel];

				if (clampWater == true)
				{
					final += displacement * Mathf.InverseLerp(Mathf.Clamp01(waterLevel), 1.0f, land);
				}
				else
				{
					final += displacement * Mathf.Max(land, waterLevel);
				}
			}

			return final;
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtPlanet))]
	public class SgtPlanet_Editor : SgtEditor<SgtPlanet>
	{
		protected override void OnInspector()
		{
			var dirtyMesh = false;

			BeginError(Any(t => t.Mesh == null));
				Draw("mesh", ref dirtyMesh, "The sphere mesh used to render the planet.");
			EndError();
			Draw("meshCollider", ref dirtyMesh, "If you want the generated mesh to have a matching collider, you can specify it here.");
			BeginError(Any(t => t.Radius <= 0.0f));
				Draw("radius", ref dirtyMesh, "The radius of the planet in local space.");
			EndError();

			Separator();

			BeginError(Any(t => t.Material == null));
				Draw("material", "The material used to render the planet. For best results, this should use the SGT Planet shader.");
			EndError();
			Draw("sharedMaterial", "If you want to apply a shared material (e.g. atmosphere) to this terrain, then specify it here.");

			Separator();

			Draw("heightmap", ref dirtyMesh, "The heightmap texture, where the height data is stored in the alpha channel.\n\nNOTE: This should use an Equirectangular projection.\n\nNOTE: This texture should be marked as readable.");
			Draw("channel", ref dirtyMesh, "This allows you to choose which color channel from the heightmap texture will be used.");
			BeginError(Any(t => t.Displacement == 0.0f));
				Draw("displacement", ref dirtyMesh, "The maximum height displacement applied to the planet mesh when the heightmap alpha value is 1.");
			EndError();
			Draw("waterLevel", ref dirtyMesh, "The current water level.\n\n0 = Radius.\n\n1 = Radius + Displacement.");
			Draw("clampWater", ref dirtyMesh, "If you enable this then the water will not rise, instead the terrain will shrink down.");

			if (dirtyMesh == true) DirtyEach(t => t.DirtyMesh());
		}
	}
}
#endif