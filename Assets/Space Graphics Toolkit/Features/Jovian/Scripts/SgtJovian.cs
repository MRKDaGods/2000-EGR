using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to render volumetric jovian (gas giant) planets.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtJovian")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Jovian")]
	public class SgtJovian : MonoBehaviour
	{
		/// <summary>The base color will be multiplied by this.</summary>
		public Color Color { set { if (color != value) { color = value; UpdateMaterial(); } } get { return color; } } [FSA("Color")] [SerializeField] private Color color = Color.white;

		/// <summary>The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.</summary>
		public float Brightness { set { if (brightness != value) { brightness = value; UpdateMaterial(); } } get { return brightness; } } [FSA("Brightness")] [SerializeField] private float brightness = 1.0f;

		/// <summary>This allows you to adjust the render queue of the jovian material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.</summary>
		public SgtRenderQueue RenderQueue { set { if (renderQueue != value) { renderQueue = value; UpdateMaterial(); } } get { return renderQueue; } } [FSA("RenderQueue")] [SerializeField] private SgtRenderQueue renderQueue = SgtRenderQueue.GroupType.Transparent;

		/// <summary>This allows you to offset the camera distance in world space when rendering the jovian, giving you fine control over the render order.</summary>
		public float CameraOffset { set { cameraOffset = value; } get { return cameraOffset; } } [SerializeField] private float cameraOffset;

		/// <summary>The cube map used as the base texture for the jovian.</summary>
		public Cubemap MainTex { set { if (mainTex != value) { mainTex = value; UpdateMaterial(); } } get { return mainTex; } } [FSA("MainTex")] [SerializeField] private Cubemap mainTex;

		/// <summary>The look up table associating optical depth with atmosphere color. The left side is used when the atmosphere is thin (e.g. edge of the jovian when looking from space). The right side is used when the atmosphere is thick (e.g. the center of the jovian when looking from space).</summary>
		public Texture2D DepthTex { set { if (depthTex != value) { depthTex = value; UpdateMaterial(); } } get { return depthTex; } } [FSA("DepthTex")] [SerializeField] private Texture2D depthTex;

		/// <summary>This allows you to control how thick the atmosphere is when the camera is inside its radius.</summary>
		public float Sky { set { if (sky != value) { sky = value; UpdateMaterial(); } } get { return sky; } } [FSA("Sky")] [SerializeField] private float sky = 1.0f;

		/// <summary>If you enable this then nearby SgtLight and SgtShadow casters will be found and applied to the lighting calculations.</summary>
		public bool Lit { set { if (lit != value) { lit = value; UpdateMaterial(); } } get { return lit; } } [FSA("Lit")] [SerializeField] private bool lit;

		/// <summary>The look up table associating light angle with surface color. The left side is used on the dark side, the middle is used on the horizon, and the right side is used on the light side.</summary>
		public Texture LightingTex { set { if (lightingTex != value) { lightingTex = value; UpdateMaterial(); } } get { return lightingTex; } } [FSA("LightingTex")] [SerializeField] private Texture lightingTex;

		/// <summary>The jovian will always be lit by this amount.</summary>
		public Color AmbientColor { set { if (ambientColor != value) { ambientColor = value; UpdateMaterial(); } } get { return ambientColor; } } [FSA("AmbientColor")] [SerializeField] private Color ambientColor;

		/// <summary>If you enable this then light will scatter through the jovian atmosphere. This means light entering the eye will come from all angles, especially around the light point.</summary>
		public bool Scattering { set { if (scattering != value) { scattering = value; UpdateMaterial(); } } get { return scattering; } } [FSA("Scattering")] [SerializeField] private bool scattering;

		/// <summary>The look up table associating light angle with scattering color. The left side is used on the dark side, the middle is used on the horizon, and the right side is used on the light side.</summary>
		public Texture ScatteringTex { set { if (scatteringTex != value) { scatteringTex = value; UpdateMaterial(); } } get { return scatteringTex; } } [FSA("ScatteringTex")] [SerializeField] private Texture scatteringTex;

		/// <summary>The scattering is multiplied by this value, allowing you to easily adjust the brightness of the effect.</summary>
		public float ScatteringStrength { set { if (scatteringStrength != value) { scatteringStrength = value; UpdateMaterial(); } } get { return scatteringStrength; } } [FSA("ScatteringStrength")] [SerializeField] private float scatteringStrength = 3.0f;

		/// <summary>This allows you to set the mesh used to render the jovian. This should be a sphere.</summary>
		public Mesh Mesh { set { if (mesh != value) { mesh = value; UpdateMaterial(); } } get { return mesh; } } [FSA("Mesh")] [SerializeField] private Mesh mesh;

		/// <summary>This allows you to set the radius of the Mesh. If this is incorrectly set then the jovian will render incorrectly.</summary>
		public float MeshRadius { set { if (meshRadius != value) { meshRadius = value; UpdateMaterial(); } } get { return meshRadius; } } [FSA("MeshRadius")] [SerializeField] private float meshRadius = 1.0f;

		/// <summary>This allows you to set the radius of the jovian in local space.</summary>
		public float Radius { set { if (radius != value) { radius = value; UpdateMaterial(); } } get { return radius; } } [FSA("Radius")] [SerializeField] private float radius = 1.0f;

		/// <summary>The temporary material rendering the jovian.</summary>
		[System.NonSerialized]
		private Material material;

		[System.NonSerialized]
		private bool dirtyMaterial = true;

		private void UpdateMaterial()
		{
			if (material == null)
			{
				material = SgtHelper.CreateTempMaterial("Jovian Material (Generated)", SgtHelper.ShaderNamePrefix + "Jovian");
			}

			material.renderQueue = renderQueue;

			material.SetTexture(SgtShader._CubeTex, mainTex);
			material.SetTexture(SgtShader._DepthTex, depthTex);
			material.SetColor(SgtShader._Color, SgtHelper.Brighten(color, brightness));

			if (lit == true)
			{
				material.SetTexture(SgtShader._LightingTex, lightingTex);
				material.SetColor(SgtShader._AmbientColor, ambientColor);
			}

			SgtHelper.SetTempMaterial(material);

			if (scattering == true)
			{
				material.SetTexture(SgtShader._ScatteringTex, scatteringTex);

				SgtHelper.EnableKeyword("SGT_B"); // Scattering
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_B"); // Scattering
			}
		}

		public static SgtJovian Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtJovian Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject = SgtHelper.CreateGameObject("Jovian", layer, parent, localPosition, localRotation, localScale);
			var jovian     = gameObject.AddComponent<SgtJovian>();

			return jovian;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Jovian", false, 10)]
		public static void CreateMenuItem()
		{
			var parent = SgtHelper.GetSelectedParent();
			var jovian = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(jovian);
		}
#endif

		protected virtual void OnEnable()
		{
			SgtCamera.OnCameraDraw += HandleCameraDraw;

			SgtHelper.OnCalculateOcclusion += CalculateOcclusion;
		}

		protected virtual void OnDisable()
		{
			SgtCamera.OnCameraDraw -= HandleCameraDraw;

			SgtHelper.OnCalculateOcclusion -= CalculateOcclusion;
		}

#if UNITY_EDITOR
		protected virtual void Start()
		{
			// Upgrade scene
			// NOTE: This must be done in Start because when done in OnEnable this fails to dirty the scene
			SgtHelper.DestroyOldGameObjects(transform, "Jovian Model");
		}
#endif

		protected virtual void LateUpdate()
		{
			if (dirtyMaterial == true)
			{
				UpdateMaterial(); dirtyMaterial = false;
			}

			// Write lights and shadows
			SgtHelper.SetTempMaterial(material);

			var mask   = 1 << gameObject.layer;
			var lights = SgtLight.Find(lit, mask, transform.position);

			SgtLight.FilterOut(transform.position);

			SgtShadow.Find(lit, mask, lights);
			SgtShadow.FilterOutSphere(transform.position);
			SgtShadow.Write(lit, 2);

			SgtLight.Write(lit, transform.position, transform, null, scatteringStrength, 2);

			// Write matrices
			var scale        = radius;
			var localToWorld = transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(scale, scale, scale)); // Double mesh radius so the max thickness caps at 1.0

			material.SetMatrix(SgtShader._WorldToLocal, localToWorld.inverse);
			material.SetMatrix(SgtShader._LocalToWorld, localToWorld);
		}

		protected virtual void OnDestroy()
		{
			SgtHelper.Destroy(material);
		}

		protected virtual void OnDidApplyAnimationProperties()
		{
			UpdateMaterial();
		}

#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			UpdateMaterial();
		}
#endif

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			if (SgtHelper.Enabled(this) == true)
			{
				var r0 = transform.lossyScale;

				SgtHelper.DrawSphere(transform.position, transform.right * r0.x, transform.up * r0.y, transform.forward * r0.z);
			}
		}
#endif

		private void HandleCameraDraw(Camera camera)
		{
			if (SgtHelper.CanDraw(gameObject, camera) == false) return;

			if (depthTex != null)
			{
#if UNITY_EDITOR
				SgtHelper.MakeTextureReadable(depthTex);
#endif
				material.SetFloat(SgtShader._Sky, GetSky(camera.transform.position));
			}

			var scale  = SgtHelper.Divide(radius, meshRadius);
			var matrix = transform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * scale);

			if (cameraOffset != 0.0f)
			{
				var direction = Vector3.Normalize(camera.transform.position - transform.position);

				matrix = Matrix4x4.Translate(direction * cameraOffset) * matrix;
			}

			Graphics.DrawMesh(mesh, matrix, material, gameObject.layer, camera);
		}

		private float GetSky(Vector3 eye)
		{
			var localCameraPosition = transform.InverseTransformPoint(eye);
			var localDistance       = localCameraPosition.magnitude;
			var scaleDistance       = SgtHelper.Divide(localDistance, radius);

			return sky * depthTex.GetPixelBilinear(1.0f - scaleDistance, 0.0f).a;
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

		private void CalculateOcclusion(int layers, Vector4 eye, Vector4 tgt, ref float occlusion)
		{
			if (SgtOcclusion.IsValid(occlusion, layers, gameObject) == true && radius > 0.0f && depthTex != null)
			{
				eye = transform.InverseTransformPoint(eye) / radius;
				tgt = transform.InverseTransformPoint(tgt) / radius;

				var dir    = Vector3.Normalize(tgt - eye);
				var len    = Vector3.Magnitude(tgt - eye);
				var length = default(float);

				if (GetLength(eye, dir, len, ref length) == true)
				{
					var depth = depthTex.GetPixelBilinear(length, length).a;

					depth = Mathf.Clamp01(depth + (1.0f - depth) * GetSky(eye));

					occlusion += (1.0f - occlusion) * depth;
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
	[CustomEditor(typeof(SgtJovian))]
	public class SgtJovian_Editor : SgtEditor<SgtJovian>
	{
		protected override void OnInspector()
		{
			Draw("color", "The base color will be multiplied by this.");
			BeginError(Any(t => t.Brightness < 0.0f));
				Draw("brightness", "The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.");
			EndError();
			Draw("renderQueue", "This allows you to adjust the render queue of the jovian material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.");
			Draw("cameraOffset", "This allows you to offset the camera distance in world space when rendering the jovian, giving you fine control over the render order."); // Updated automatically

			Separator();

			BeginError(Any(t => t.MainTex == null));
				Draw("mainTex", "The cube map used as the base texture for the jovian.");
			EndError();
			BeginError(Any(t => t.DepthTex == null));
				Draw("depthTex", "The look up table associating optical depth with atmosphere color. The left side is used when the atmosphere is thin (e.g. edge of the jovian when looking from space). The right side is used when the atmosphere is thick (e.g. the center of the jovian when looking from space).");
			EndError();

			Separator();

			BeginError(Any(t => t.Sky < 0.0f));
				Draw("sky", "This allows you to control how thick the atmosphere is when the camera is inside its radius"); // Updated when rendering
			EndError();

			Draw("lit", "If you enable this then nearby SgtLight and SgtShadow casters will be found and applied to the lighting calculations.");

			if (Any(t => t.Lit == true))
			{
				if (SgtLight.InstanceCount == 0)
				{
					EditorGUILayout.HelpBox("You need to add the SgtLight component to your scene lights for them to work with SGT.", MessageType.Warning);
				}

				BeginIndent();
					BeginError(Any(t => t.LightingTex == null));
						Draw("lightingTex", "The look up table associating light angle with surface color. The left side is used on the dark side, the middle is used on the horizon, and the right side is used on the light side.");
					EndError();
					Draw("ambientColor", "The jovian will always be lit by this amount.");
					Draw("scattering", "If you enable this then light will scatter through the jovian atmosphere. This means light entering the eye will come from all angles, especially around the light point.");

					if (Any(t => t.Scattering == true))
					{
						BeginIndent();
							BeginError(Any(t => t.ScatteringTex == null));
								Draw("scatteringTex", "The look up table associating light angle with scattering color. The left side is used on the dark side, the middle is used on the horizon, and the right side is used on the light side.");
							EndError();
							Draw("scatteringStrength", "The scattering is multiplied by this value, allowing you to easily adjust the brightness of the effect.");
						EndIndent();
					}
				EndIndent();
			}

			Separator();

			BeginError(Any(t => t.Mesh == null));
				Draw("mesh", "This allows you to set the mesh used to render the jovian. This should be a sphere.");
			EndError();
			BeginError(Any(t => t.MeshRadius <= 0.0f));
				Draw("meshRadius", "This allows you to set the radius of the Mesh. If this is incorrectly set then the jovian will render incorrectly.");
			EndError();
			BeginError(Any(t => t.Radius <= 0.0f));
				Draw("radius", "This allows you to set the radius of the jovian in local space.");
			EndError();

			if (Any(t => t.DepthTex == null && t.GetComponent<SgtJovianDepthTex>() == null))
			{
				Separator();

				if (Button("Add DepthTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtJovianDepthTex>(t.gameObject));
				}
			}

			if (Any(t => t.Lit == true && t.LightingTex == null && t.GetComponent<SgtJovianLightingTex>() == null))
			{
				Separator();

				if (Button("Add LightingTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtJovianLightingTex>(t.gameObject));
				}
			}

			if (Any(t => t.Lit == true && t.Scattering == true && t.ScatteringTex == null && t.GetComponent<SgtJovianScatteringTex>() == null))
			{
				Separator();

				if (Button("Add ScatteringTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtJovianScatteringTex>(t.gameObject));
				}
			}

			if (Any(t => SetMeshAndMeshRadius(t, false)))
			{
				Separator();

				if (Button("Set Mesh & MeshRadius") == true)
				{
					Each(t => SetMeshAndMeshRadius(t, true));
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		private bool SetMeshAndMeshRadius(SgtJovian jovian, bool apply)
		{
			if (jovian.Mesh == null)
			{
				var mesh = SgtHelper.LoadFirstAsset<Mesh>("Geosphere40 t:mesh");

				if (mesh != null)
				{
					if (apply == true)
					{
						jovian.Mesh       = mesh;
						jovian.MeshRadius = SgtHelper.GetMeshRadius(mesh);
					}

					return true;
				}
			}

			return false;
		}
	}
}
#endif