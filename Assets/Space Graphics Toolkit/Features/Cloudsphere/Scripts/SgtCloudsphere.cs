using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to render a sphere around a planet with a cloud cubemap.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtCloudsphere")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Cloudsphere")]
	public class SgtCloudsphere : MonoBehaviour
	{
		/// <summary>The base color will be multiplied by this.</summary>
		public Color Color { set { if (color != value) { color = value; DirtyMaterial(); } } get { return color; } } [FSA("Color")] [SerializeField] private Color color = Color.white;

		/// <summary>The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.</summary>
		public float Brightness { set { if (brightness != value) { brightness = value; DirtyMaterial(); } } get { return brightness; } } [FSA("Brightness")] [SerializeField] private float brightness = 1.0f;

		/// <summary>This allows you to adjust the render queue of the cloudsphere material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.</summary>
		public SgtRenderQueue RenderQueue { set { if (renderQueue != value) { renderQueue = value; DirtyMaterial(); } } get { return renderQueue; } } [FSA("RenderQueue")] [SerializeField] private SgtRenderQueue renderQueue = SgtRenderQueue.GroupType.Transparent;

		/// <summary>This allows you to set the radius of the cloudsphere in local space.</summary>
		public float Radius { set { radius = value; } get { return radius; } } [FSA("Radius")] [SerializeField] private float radius = 1.5f;

		/// <summary>This allows you to offset the camera distance in world space when rendering the cloudsphere, giving you fine control over the render order.</summary>
		public float CameraOffset { set { cameraOffset = value; } get { return cameraOffset; } } [FSA("CameraOffset")] [SerializeField] private float cameraOffset;

		/// <summary>The cube map applied to the cloudsphere surface.</summary>
		public Cubemap MainTex { set { if (mainTex != value) { mainTex = value; DirtyMaterial(); } } get { return mainTex; } } [FSA("MainTex")] [SerializeField] private Cubemap mainTex;

		/// <summary>The look up table associating optical depth with cloud color. The left side is used when the depth is thin (e.g. edge of the cloudsphere when looking from space). The right side is used when the depth is thick (e.g. center of the cloudsphere when looking from space).</summary>
		public Texture2D DepthTex { set { if (depthTex != value) { depthTex = value; DirtyMaterial(); } } get { return depthTex; } } [FSA("DepthTex")] [SerializeField] private Texture2D depthTex;

		/// <summary>Should the stars fade out if they're intersecting solid geometry?</summary>
		public float Softness { set { if (softness != value) { softness = value; DirtyMaterial(); } } get { return softness; } } [FSA("Softness")] [SerializeField] [Range(0.0f, 1000.0f)] private float softness;

		/// <summary>If you enable this then nearby SgtLight and SgtShadow casters will be found and applied to the lighting calculations.</summary>
		public bool Lit { set { if (lit != value) { lit = value; DirtyMaterial(); } } get { return lit; } } [FSA("Lit")] [SerializeField] private bool lit;

		/// <summary>The look up table associating light angle with surface color. The left side is used on the dark side, the middle is used on the horizon, and the right side is used on the light side.</summary>
		public Texture LightingTex { set { if (lightingTex != value) { lightingTex = value; DirtyMaterial(); } } get { return lightingTex; } } [FSA("LightingTex")] [SerializeField] private Texture lightingTex;

		/// <summary>The cloudsphere will always be lit by this amount.</summary>
		public Color AmbientColor { set { if (ambientColor != value) { ambientColor = value; DirtyMaterial(); } } get { return ambientColor; } } [FSA("AmbientColor")] [SerializeField] private Color ambientColor;

		/// <summary>Enable this if you want the cloudsphere to fade out as the camera approaches.</summary>
		public bool Near { set { if (near != value) { near = value; DirtyMaterial(); } } get { return near; } } [FSA("Near")] [SerializeField] private bool near;

		/// <summary>The lookup table used to calculate the fade opacity based on distance, where the left side is used when the camera is close, and the right side is used when the camera is far.</summary>
		public Texture NearTex { set { if (nearTex != value) { nearTex = value; DirtyMaterial(); } } get { return nearTex; } } [FSA("NearTex")] [SerializeField] private Texture nearTex;

		/// <summary>The distance the fading begins from in world space.</summary>
		public float NearDistance { set { if (nearDistance != value) { nearDistance = value; DirtyMaterial(); } } get { return nearDistance; } } [FSA("NearDistance")] [SerializeField] private float nearDistance = 1.0f;

		/// <summary>Enable this if you want the cloud edges to be enhanced with more detail.</summary>
		public bool Detail { set { if (detail != value) { detail = value; DirtyMaterial(); } } get { return detail; } } [FSA("Detail")] [SerializeField] private bool detail;

		/// <summary>This allows you to set the detail map texture, the detail should be stored in the alpha channel.</summary>
		public Texture DetailTex { set { if (detailTex != value) { detailTex = value; DirtyMaterial(); } } get { return detailTex; } } [FSA("DetailTex")] [SerializeField] private Texture detailTex;

		/// <summary>This allows you to set how many times the detail texture is repeating along the cloud surface.</summary>
		public float DetailScale { set { if (detailScale != value) { detailScale = value; DirtyMaterial(); } } get { return detailScale; } } [FSA("DetailScale")] [SerializeField] private float detailScale = 8.0f;

		/// <summary>This allows you to set how many times the detail texture is repeating along the cloud surface.</summary>
		public float DetailTiling { set { if (detailTiling != value) { detailTiling = value; DirtyMaterial(); } } get { return detailTiling; } } [FSA("DetailTiling")] [SerializeField] private float detailTiling = 90.0f;

		/// <summary>This allows you to set the mesh used to render the cloudsphere. This should be a sphere.</summary>
		public Mesh Mesh { set { mesh = value; } get { return mesh; } } [FSA("Mesh")] [SerializeField] private Mesh mesh;

		/// <summary>This allows you to set the radius of the Mesh. If this is incorrectly set then the cloudsphere will render incorrectly.</summary>
		public float MeshRadius { set { meshRadius = value; } get { return meshRadius; } } [FSA("MeshRadius")] [SerializeField] private float meshRadius = 1.0f;

		[System.NonSerialized]
		private Material generatedMaterial;

		[System.NonSerialized]
		private Transform cachedTransform;

		public void DirtyMaterial()
		{
			UpdateMaterial();
		}

		public static SgtCloudsphere Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtCloudsphere Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject  = SgtHelper.CreateGameObject("Cloudsphere", layer, parent, localPosition, localRotation, localScale);
			var cloudsphere = gameObject.AddComponent<SgtCloudsphere>();

			return cloudsphere;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Cloudsphere", false, 10)]
		public static void CreateMenuItem()
		{
			var parent      = SgtHelper.GetSelectedParent();
			var cloudsphere = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(cloudsphere);
		}
#endif

		protected virtual void OnEnable()
		{
			SgtCamera.OnCameraDraw += HandleCameraDraw;

			cachedTransform = GetComponent<Transform>();

			UpdateMaterial();
		}

		protected virtual void OnDisable()
		{
			SgtCamera.OnCameraDraw -= HandleCameraDraw;
		}

		protected virtual void LateUpdate()
		{
			// Write lights and shadows
			SgtHelper.SetTempMaterial(generatedMaterial);

			var mask   = 1 << gameObject.layer;
			var lights = SgtLight.Find(lit, mask, cachedTransform.position);

			SgtShadow.Find(lit, mask, lights);
			SgtShadow.FilterOutSphere(cachedTransform.position);
			SgtShadow.Write(lit, 2);

			SgtLight.FilterOut(cachedTransform.position);
			SgtLight.Write(lit, cachedTransform.position, null, null, 1.0f, 2);
		}

		private void HandleCameraDraw(Camera camera)
		{
			if (SgtHelper.CanDraw(gameObject, camera) == false) return;

			var scale  = SgtHelper.Divide(radius, meshRadius);
			var matrix = cachedTransform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * scale);

			if (cameraOffset != 0.0f)
			{
				var direction = Vector3.Normalize(camera.transform.position - cachedTransform.position);

				matrix = Matrix4x4.Translate(direction * cameraOffset) * matrix;
			}

			Graphics.DrawMesh(mesh, matrix, generatedMaterial, gameObject.layer, camera);
		}

#if UNITY_EDITOR
		protected virtual void Start()
		{
			// Upgrade scene
			// NOTE: This must be done in Start because when done in OnEnable this fails to dirty the scene
			SgtHelper.DestroyOldGameObjects(transform, "Cloudsphere Model");
		}
#endif

		protected virtual void OnDestroy()
		{
			SgtHelper.Destroy(generatedMaterial);
		}

		private void UpdateMaterial()
		{
			if (generatedMaterial == null)
			{
				generatedMaterial = SgtHelper.CreateTempMaterial("Cloudsphere (Generated)", SgtHelper.ShaderNamePrefix + "Cloudsphere");
			}

			var color = SgtHelper.Brighten(this.color, brightness);

			generatedMaterial.renderQueue = renderQueue;

			generatedMaterial.SetColor(SgtShader._Color, color);
			generatedMaterial.SetTexture(SgtShader._CubeTex, mainTex);
			generatedMaterial.SetTexture(SgtShader._DepthTex, depthTex);

			if (lit == true)
			{
				generatedMaterial.SetTexture(SgtShader._LightingTex, lightingTex);
				generatedMaterial.SetColor(SgtShader._AmbientColor, ambientColor);
			}

			if (near == true)
			{
				SgtHelper.EnableKeyword("SGT_A", generatedMaterial); // Near

				generatedMaterial.SetTexture(SgtShader._NearTex, nearTex);
				generatedMaterial.SetFloat(SgtShader._NearScale, SgtHelper.Reciprocal(nearDistance));
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_A", generatedMaterial); // Near
			}

			if (detail == true)
			{
				SgtHelper.EnableKeyword("SGT_B", generatedMaterial); // Detail

				generatedMaterial.SetTexture(SgtShader._DetailTex, detailTex);
				generatedMaterial.SetFloat(SgtShader._DetailScale, detailScale);
				generatedMaterial.SetFloat(SgtShader._DetailTiling, detailTiling);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_B", generatedMaterial); // Detail
			}

			if (softness > 0.0f)
			{
				SgtHelper.EnableKeyword("SGT_C", generatedMaterial); // Softness

				generatedMaterial.SetFloat(SgtShader._SoftParticlesFactor, SgtHelper.Reciprocal(softness));
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_C", generatedMaterial); // Softness
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtCloudsphere))]
	public class SgtCloudsphere_Editor : SgtEditor<SgtCloudsphere>
	{
		protected override void OnInspector()
		{
			var dirtyMaterial = false;

			Draw("color", ref dirtyMaterial, "The base color will be multiplied by this.");
			BeginError(Any(t => t.Brightness <= 0.0f));
				Draw("brightness", ref dirtyMaterial, "The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.");
			EndError();
			Draw("renderQueue", ref dirtyMaterial, "This allows you to adjust the render queue of the cloudsphere material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.");

			Separator();

			BeginError(Any(t => t.MainTex == null));
				Draw("mainTex", ref dirtyMaterial, "The cube map applied to the cloudsphere surface.");
			EndError();
			BeginError(Any(t => t.DepthTex == null));
				Draw("depthTex", ref dirtyMaterial, "The look up table associating optical depth with cloud color. The left side is used when the depth is thin (e.g. edge of the cloudsphere when looking from space). The right side is used when the depth is thick (e.g. center of the cloudsphere when looking from space).");
			EndError();
			BeginError(Any(t => t.Radius < 0.0f));
				Draw("radius", "This allows you to set the radius of the cloudsphere in local space.");
			EndError();
			Draw("cameraOffset", "This allows you to offset the camera distance in world space when rendering the cloudsphere, giving you fine control over the render order."); // Updated automatically

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

			Separator();

			Draw("lit", ref dirtyMaterial, "If you enable this then nearby SgtLight and SgtShadow casters will be found and applied to the lighting calculations.");

			if (Any(t => t.Lit == true))
			{
				if (SgtLight.InstanceCount == 0)
				{
					EditorGUILayout.HelpBox("You need to add the SgtLight component to your scene lights for them to work with SGT.", MessageType.Warning);
				}

				BeginIndent();
					BeginError(Any(t => t.LightingTex == null));
						Draw("lightingTex", ref dirtyMaterial, "The look up table associating light angle with surface color. The left side is used on the dark side, the middle is used on the horizon, and the right side is used on the light side.");
					EndError();
					Draw("ambientColor", ref dirtyMaterial, "The cloudsphere will always be lit by this amount.");
				EndIndent();
			}

			Separator();

			Draw("near", ref dirtyMaterial, "Enable this if you want the cloudsphere to fade out as the camera approaches.");

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

			Separator();

			Draw("detail", ref dirtyMaterial, "");

			if (Any(t => t.Detail == true))
			{
				BeginIndent();
					BeginError(Any(t => t.DetailTex == null));
						Draw("detailTex", ref dirtyMaterial, "");
					EndError();
					BeginError(Any(t => t.DetailScale <= 0.0f));
						Draw("detailScale", ref dirtyMaterial, "");
					EndError();
					BeginError(Any(t => t.DetailTiling <= 0.0f));
						Draw("detailTiling", ref dirtyMaterial, "");
					EndError();
				EndIndent();
			}

			Separator();
			
			BeginError(Any(t => t.Mesh == null));
				Draw("mesh", "This allows you to set the mesh used to render the cloudsphere. This should be a sphere.");
			EndError();
			BeginError(Any(t => t.MeshRadius <= 0.0f));
				Draw("meshRadius", "This allows you to set the radius of the Mesh. If this is incorrectly set then the cloudsphere will render incorrectly.");
			EndError();

			if (Any(t => t.DepthTex == null && t.GetComponent<SgtCloudsphereDepthTex>() == null))
			{
				Separator();

				if (Button("Add InnerDepthTex & OuterDepthTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtCloudsphereDepthTex>(t.gameObject));
				}
			}

			if (Any(t => t.Lit == true && t.LightingTex == null && t.GetComponent<SgtCloudsphereLightingTex>() == null))
			{
				Separator();

				if (Button("Add LightingTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtCloudsphereLightingTex>(t.gameObject));
				}
			}

			if (Any(t => t.Near == true && t.NearTex == null && t.GetComponent<SgtCloudsphereNearTex>() == null))
			{
				Separator();

				if (Button("Add NearTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtCloudsphereNearTex>(t.gameObject));
				}
			}

			if (Any(t => SetMeshAndMeshRadius(t, false)))
			{
				Separator();

				if (Button("Set Mesh & Mesh Radius") == true)
				{
					Each(t => SetMeshAndMeshRadius(t, true));
				}
			}

			if (dirtyMaterial == true)
			{
				DirtyEach(t => t.DirtyMaterial());
			}
		}

		private bool SetMeshAndMeshRadius(SgtCloudsphere cloudsphere, bool apply)
		{
			if (cloudsphere.Mesh == null)
			{
				var mesh = SgtHelper.LoadFirstAsset<Mesh>("Geosphere40 t:mesh");

				if (mesh != null)
				{
					if (apply == true)
					{
						cloudsphere.Mesh       = mesh;
						cloudsphere.MeshRadius = SgtHelper.GetMeshRadius(mesh);
					}

					return true;
				}
			}

			return false;
		}
	}
}
#endif