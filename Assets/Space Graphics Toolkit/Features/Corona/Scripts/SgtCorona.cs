using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to draw a volumetric corona around a sphere.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtSharedMaterial))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtCorona")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Corona")]
	public class SgtCorona : MonoBehaviour
	{
		/// <summary>The base color will be multiplied by this.</summary>
		public Color Color { set { if (color != value) { color = value; UpdateMaterials(); } } get { return color; } } [FSA("Color")] [SerializeField] private Color color = Color.white;

		/// <summary>The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.</summary>
		public float Brightness { set { if (brightness != value) { brightness = value; UpdateMaterials(); } } get { return brightness; } } [FSA("Brightness")] [SerializeField] private float brightness = 1.0f;

		/// <summary>This allows you to adjust the render queue of the corona materials. You can normally adjust the render queue in the material settings, but since these materials are procedurally generated your changes will be lost.</summary>
		public SgtRenderQueue RenderQueue { set { if (renderQueue != value) { renderQueue = value; UpdateMaterials(); } } get { return renderQueue; } } [FSA("RenderQueue")] [SerializeField] private SgtRenderQueue renderQueue = SgtRenderQueue.GroupType.Transparent; public void SetRenderQueue(SgtRenderQueue value) { renderQueue = value; UpdateMaterials(); }

		/// <summary>This allows you to set the altitude where atmospheric density reaches its maximum point. The lower you set this, the foggier the horizon will appear when approaching the surface.</summary>
		public float Middle { set { middle = value; } get { return middle; } } [FSA("Middle")] [Range(0.0f, 1.0f)] [SerializeField] private float middle = 1.0f;

		/// <summary>This allows you to offset the camera distance in world space when rendering the corona, giving you fine control over the render order.</summary>
		public float CameraOffset { set { cameraOffset = value; } get { return cameraOffset; } } [FSA("CameraOffset")] [SerializeField] private float cameraOffset;

		/// <summary>The look up table associating optical depth with coronal color for the star surface. The left side is used when the corona is thin (e.g. center of the star when looking from space). The right side is used when the corona is thick (e.g. the horizon).</summary>
		public Texture InnerDepthTex { set { if (innerDepthTex != value) { innerDepthTex = value; UpdateMaterials(); } } get { return innerDepthTex; } } [FSA("InnerDepthTex")] [SerializeField] private Texture innerDepthTex;

		/// <summary>The radius of the inner renderers (surface) in local coordinates.</summary>
		public float InnerMeshRadius { set { if (innerMeshRadius != value) { innerMeshRadius = value; UpdateMaterials(); } } get { return innerMeshRadius; } } [FSA("InnerMeshRadius")] [SerializeField] private float innerMeshRadius = 1.0f;

		/// <summary>The look up table associating optical depth with coronal color for the star sky. The left side is used when the corona is thin (e.g. edge of the corona when looking from space). The right side is used when the corona is thick (e.g. the horizon).</summary>
		public Texture2D OuterDepthTex { set { if (outerDepthTex != value) { outerDepthTex = value; UpdateMaterials(); } } get { return outerDepthTex; } } [FSA("OuterDepthTex")] [SerializeField] private Texture2D outerDepthTex;

		/// <summary>This allows you to set the mesh used to render the atmosphere. This should be a sphere.</summary>
		public Mesh OuterMesh { set { outerMesh = value; } get { return outerMesh; } } [FSA("OuterMesh")] [SerializeField] private Mesh outerMesh;

		/// <summary>This allows you to set the radius of the OuterMesh. If this is incorrectly set then the corona will render incorrectly.</summary>
		public float OuterMeshRadius { set { outerMeshRadius = value; } get { return outerMeshRadius; } } [FSA("OuterMeshRadius")] [SerializeField] private float outerMeshRadius;

		/// <summary>Should the outer corona fade out against intersecting geometry?</summary>
		public float OuterSoftness { set { if (outerSoftness != value) { outerSoftness = value; UpdateMaterials(); } } get { return outerSoftness; } } [FSA("OuterSoftness")] [SerializeField] [Range(0.0f, 1000.0f)] private float outerSoftness;

		/// <summary>This allows you to set how high the corona extends above the surface of the star in local space.</summary>
		public float Height { set { if (height != value) { height = value; UpdateMaterials(); } } get { return height; } } [FSA("Height")] [SerializeField] private float height = 0.1f;

		/// <summary>If you want an extra-thin or extra-thick density, you can adjust that here (0 = default).</summary>
		public float InnerFog { set { if (innerFog != value) { innerFog = value; UpdateMaterials(); } } get { return innerFog; } } [FSA("InnerFog")] [SerializeField] private float innerFog;

		/// <summary>If you want an extra-thin or extra-thick density, you can adjust that here (0 = default).</summary>
		public float OuterFog { set { if (outerFog != value) { outerFog = value; UpdateMaterials(); } } get { return outerFog; } } [FSA("OuterFog")] [SerializeField] private float outerFog;

		/// <summary>This allows you to control how thick the corona is when the camera is inside its radius.</summary>
		public float Sky { set { sky = value; } get { return sky; } } [FSA("Sky")] [SerializeField] private float sky = 1.0f;

		/// <summary>The material applied to all inner renderers.</summary>
		[System.NonSerialized]
		private Material innerMaterial;

		/// <summary>The material applied to the outer model.</summary>
		[System.NonSerialized]
		private Material outerMaterial;

		[System.NonSerialized]
		private SgtSharedMaterial cachedSharedMaterial;

		[System.NonSerialized]
		private bool cachedSharedMaterialSet;

		[System.NonSerialized]
		private Transform cachedTransform;

		[System.NonSerialized]
		private bool cachedTransformSet;

		public float OuterRadius
		{
			get
			{
				return innerMeshRadius + height;
			}
		}

		public Transform CachedTransform
		{
			get
			{
				if (cachedTransformSet == false)
				{
					cachedTransform    = GetComponent<Transform>();
					cachedTransformSet = true;
				}

				return cachedTransform;
			}
		}

		public SgtSharedMaterial CachedSharedMaterial
		{
			get
			{
				if (cachedSharedMaterialSet == false)
				{
					cachedSharedMaterial    = GetComponent<SgtSharedMaterial>();
					cachedSharedMaterialSet = true;
				}

				return cachedSharedMaterial;
			}
		}

		public static SgtCorona Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtCorona Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject = SgtHelper.CreateGameObject("Corona", layer, parent, localPosition, localRotation, localScale);
			var corona     = gameObject.AddComponent<SgtCorona>();

			return corona;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Corona", false, 10)]
		public static void CreateMenuItem()
		{
			var parent = SgtHelper.GetSelectedParent();
			var corona = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(corona);
		}
#endif

		protected virtual void OnEnable()
		{
			SgtCamera.OnCameraDraw += HandleCameraDraw;

			SgtHelper.OnCalculateOcclusion += CalculateOcclusion;

			CachedSharedMaterial.Material = innerMaterial;

			UpdateMaterials();
		}

		protected virtual void OnDisable()
		{
			SgtCamera.OnCameraDraw -= HandleCameraDraw;

			SgtHelper.OnCalculateOcclusion -= CalculateOcclusion;

			cachedSharedMaterial.Material = null;
		}

#if UNITY_EDITOR
		protected virtual void Start()
		{
			// Upgrade scene
			// NOTE: This must be done in Start because when done in OnEnable this fails to dirty the scene
			SgtHelper.DestroyOldGameObjects(transform, "Corona Model");
		}
#endif

		protected virtual void OnDestroy()
		{
			SgtHelper.Destroy(outerMaterial);
			SgtHelper.Destroy(innerMaterial);
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			if (SgtHelper.Enabled(this) == true)
			{
				var r1 = innerMeshRadius;
				var r2 = OuterRadius;

				SgtHelper.DrawSphere(transform.position, transform.right * transform.lossyScale.x * r1, transform.up * transform.lossyScale.y * r1, transform.forward * transform.lossyScale.z * r1);
				SgtHelper.DrawSphere(transform.position, transform.right * transform.lossyScale.x * r2, transform.up * transform.lossyScale.y * r2, transform.forward * transform.lossyScale.z * r2);
			}
		}
#endif

		private void UpdateMaterials()
		{
			if (innerMaterial == null)
			{
				innerMaterial = SgtHelper.CreateTempMaterial("Corona Inner (Generated)", SgtHelper.ShaderNamePrefix + "CoronaInner");

				CachedSharedMaterial.Material = innerMaterial;
			}

			if (outerMaterial == null)
			{
				outerMaterial = SgtHelper.CreateTempMaterial("Corona Outer (Generated)", SgtHelper.ShaderNamePrefix + "CoronaOuter");
			}

			var color      = SgtHelper.Brighten(this.color, brightness);
			var innerRatio = SgtHelper.Divide(innerMeshRadius, OuterRadius);

			innerMaterial.renderQueue = outerMaterial.renderQueue = renderQueue;

			innerMaterial.SetColor(SgtShader._Color, color);
			outerMaterial.SetColor(SgtShader._Color, color);

			innerMaterial.SetTexture(SgtShader._DepthTex, innerDepthTex);
			outerMaterial.SetTexture(SgtShader._DepthTex, outerDepthTex);

			innerMaterial.SetFloat(SgtShader._InnerRatio, innerRatio);
			innerMaterial.SetFloat(SgtShader._InnerScale, 1.0f / (1.0f - innerRatio));

			if (outerSoftness > 0.0f)
			{
				SgtHelper.EnableKeyword("SGT_A", outerMaterial); // Softness

				outerMaterial.SetFloat(SgtShader._SoftParticlesFactor, SgtHelper.Reciprocal(outerSoftness));
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_A", outerMaterial); // Softness
			}

			UpdateMaterialNonSerialized();
		}

		private void HandleCameraDraw(Camera camera)
		{
			if (SgtHelper.CanDraw(gameObject, camera) == false) return;

			// Write camera-dependant shader values
			if (innerMaterial != null && outerMaterial != null)
			{
				var localPosition  = CachedTransform.InverseTransformPoint(camera.transform.position);
				var localDistance  = localPosition.magnitude;
				var innerThickness = default(float);
				var outerThickness = default(float);
				var innerRatio     = SgtHelper.Divide(innerMeshRadius, OuterRadius);
				var middleRatio    = Mathf.Lerp(innerRatio, 1.0f, middle);
				var distance       = SgtHelper.Divide(localDistance, OuterRadius);
				var innerDensity   = 1.0f - innerFog;
				var outerDensity   = 1.0f - outerFog;

				SgtHelper.CalculateHorizonThickness(innerRatio, middleRatio, distance, out innerThickness, out outerThickness);

				innerMaterial.SetFloat(SgtShader._HorizonLengthRecip, SgtHelper.Reciprocal(innerThickness * innerDensity));
				outerMaterial.SetFloat(SgtShader._HorizonLengthRecip, SgtHelper.Reciprocal(outerThickness * outerDensity));

				if (outerDepthTex != null)
				{
#if UNITY_EDITOR
					SgtHelper.MakeTextureReadable(outerDepthTex);
#endif
					outerMaterial.SetFloat(SgtShader._Sky, GetSky(localDistance));
				}

				UpdateMaterialNonSerialized();
			}

			var scale  = SgtHelper.Divide(OuterRadius, outerMeshRadius);
			var matrix = CachedTransform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * scale);

			if (cameraOffset != 0.0f)
			{
				var direction = Vector3.Normalize(camera.transform.position - cachedTransform.position);

				matrix = Matrix4x4.Translate(direction * cameraOffset) * matrix;
			}

			Graphics.DrawMesh(outerMesh, matrix, outerMaterial, gameObject.layer, camera);
		}

		private float GetSky(float localDistance)
		{
			var height01 = Mathf.InverseLerp(OuterRadius, innerMeshRadius, localDistance);

			return sky * outerDepthTex.GetPixelBilinear(height01 / (1.0f - outerFog), 0.0f).a;
		}

		private bool GetPoint(Vector3 ray, Vector3 dir, ref Vector3 point)
		{
			var a = Vector3.Dot(ray, dir);
			var b = Vector3.Dot(ray, ray) - 1.0f;

			if (b <= 0.0f) { point = ray; return true; } // Inside?

			var c = a * a - b;

			if (c < 0.0f) { return false; } // Miss?

			var d = -a - Mathf.Sqrt(c);

			if (d < 0.0f) { return false; } // Behind?

			point = ray + dir * d; return true;
		}

		private bool GetLength(Vector3 ray, Vector3 dir, float len, ref float length)
		{
			var a = default(Vector3);
			var b = default(Vector3);

			if (GetPoint(ray, dir, ref a) == true && GetPoint(ray + dir * len, -dir, ref b) == true)
			{
				length = Vector3.Distance(a, b); return true;
			}

			return false;
		}

		private void CalculateOcclusion(int layers, Vector4 worldEye, Vector4 worldTgt, ref float occlusion)
		{
			if (SgtOcclusion.IsValid(occlusion, layers, gameObject) == true && OuterRadius > 0.0f && outerDepthTex != null)
			{
				var eye = transform.InverseTransformPoint(worldEye) / OuterRadius;
				var tgt = transform.InverseTransformPoint(worldTgt) / OuterRadius;

				var dir    = Vector3.Normalize(tgt - eye);
				var len    = Vector3.Magnitude(tgt - eye);
				var length = default(float);

				if (GetLength(eye, dir, len, ref length) == true)
				{
					var depth         = outerDepthTex.GetPixelBilinear(length, length).a;
					var localPosition = CachedTransform.InverseTransformPoint(worldEye);
					var localDistance = localPosition.magnitude;

					depth = Mathf.Clamp01(depth + (1.0f - depth) * GetSky(localDistance));

					occlusion += (1.0f - occlusion) * depth;
				}
			}
		}

		private void UpdateMaterialNonSerialized()
		{
			var scale        = SgtHelper.Divide(outerMeshRadius, OuterRadius);
			var worldToLocal = Matrix4x4.Scale(new Vector3(scale, scale, scale)) * CachedTransform.worldToLocalMatrix;

			innerMaterial.SetMatrix(SgtShader._WorldToLocal, worldToLocal);
			outerMaterial.SetMatrix(SgtShader._WorldToLocal, worldToLocal);
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtCorona))]
	public class SgtCorona_Editor : SgtEditor<SgtCorona>
	{
		protected override void OnInspector()
		{
			Draw("color", "The base color will be multiplied by this.");
			BeginError(Any(t => t.Brightness < 0.0f));
				Draw("brightness", "The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.");
			EndError();
			Draw("renderQueue", "This allows you to adjust the render queue of the corona materials. You can normally adjust the render queue in the material settings, but since these materials are procedurally generated your changes will be lost.");

			Separator();

			BeginError(Any(t => t.InnerDepthTex == null));
				Draw("innerDepthTex", "The look up table associating optical depth with coronal color for the star surface. The left side is used when the corona is thin (e.g. center of the star when looking from space). The right side is used when the corona is thick (e.g. the horizon).");
			EndError();
			BeginError(Any(t => t.InnerMeshRadius <= 0.0f));
				Draw("innerMeshRadius", "The radius of the inner renderers (surface) in local coordinates.");
			EndError();

			Separator();

			BeginError(Any(t => t.OuterDepthTex == null));
				Draw("outerDepthTex", "The look up table associating optical depth with coronal color for the star sky. The left side is used when the corona is thin (e.g. edge of the corona when looking from space). The right side is used when the corona is thick (e.g. the horizon).");
			EndError();
			BeginError(Any(t => t.OuterMesh == null));
				Draw("outerMesh", "This allows you to set the mesh used to render the atmosphere. This should be a sphere.");
			EndError();
			BeginError(Any(t => t.OuterMeshRadius <= 0.0f));
				Draw("outerMeshRadius", "This allows you to set the radius of the OuterMesh. If this is incorrectly set then the corona will render incorrectly.");
			EndError();
			Draw("outerSoftness", "Should the outer corona fade out against intersecting geometry?");

			if (Any(t => t.OuterSoftness > 0.0f))
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

			Separator();

			BeginError(Any(t => t.Height <= 0.0f));
				Draw("height", "This allows you to set how high the corona extends above the surface of the star in local space.");
			EndError();
			BeginError(Any(t => t.InnerFog >= 1.0f));
				Draw("innerFog", "If you want an extra-thin or extra-thick density, you can adjust that here (0 = default).");
			EndError();
			BeginError(Any(t => t.OuterFog >= 1.0f));
				Draw("outerFog", "If you want an extra-thin or extra-thick density, you can adjust that here (0 = default).");
			EndError();
			BeginError(Any(t => t.Sky < 0.0f));
				Draw("sky", "This allows you to control how thick the corona is when the camera is inside its radius."); // Updated when rendering
			EndError();
			Draw("middle", "This allows you to set the altitude where atmospheric density reaches its maximum point. The lower you set this, the foggier the horizon will appear when approaching the surface."); // Updated automatically
			Draw("cameraOffset", "This allows you to offset the camera distance in world space when rendering the corona, giving you fine control over the render order."); // Updated automatically

			if (Any(t => (t.InnerDepthTex == null || t.OuterDepthTex == null) && t.GetComponent<SgtCoronaDepthTex>() == null))
			{
				Separator();

				if (Button("Add InnerDepthTex & OuterDepthTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtCoronaDepthTex>(t.gameObject));
				}
			}

			if (Any(t => SetOuterMeshAndOuterMeshRadius(t, false)))
			{
				Separator();

				if (Button("Set OuterMesh & OuterMeshRadius") == true)
				{
					Each(t => SetOuterMeshAndOuterMeshRadius(t, true));
				}
			}

			if (Any(t => AddInnerRendererAndSetInnerMeshRadius(t, false)))
			{
				Separator();

				if (Button("Add InnerRenderer & Set InnerMeshRadius") == true)
				{
					Each(t => AddInnerRendererAndSetInnerMeshRadius(t, true));
				}
			}
		}

		private bool SetOuterMeshAndOuterMeshRadius(SgtCorona corona, bool apply)
		{
			if (corona.OuterMesh == null)
			{
				var mesh = SgtHelper.LoadFirstAsset<Mesh>("Geosphere40 t:mesh");

				if (mesh != null)
				{
					if (apply == true)
					{
						corona.OuterMesh       = mesh;
						corona.OuterMeshRadius = SgtHelper.GetMeshRadius(mesh);
					}

					return true;
				}
			}

			return false;
		}

		private bool AddInnerRendererAndSetInnerMeshRadius(SgtCorona corona, bool apply)
		{
			if (corona.CachedSharedMaterial.RendererCount == 0)
			{
				var meshRenderer = corona.GetComponentInParent<MeshRenderer>();

				if (meshRenderer != null)
				{
					var meshFilter = meshRenderer.GetComponent<MeshFilter>();

					if (meshFilter != null)
					{
						var mesh = meshFilter.sharedMesh;

						if (mesh != null)
						{
							if (apply == true)
							{
								corona.CachedSharedMaterial.AddRenderer(meshRenderer);
								corona.InnerMeshRadius = SgtHelper.GetMeshRadius(mesh);
							}

							return true;
						}
					}
				}
			}

			return false;
		}
	}
}
#endif