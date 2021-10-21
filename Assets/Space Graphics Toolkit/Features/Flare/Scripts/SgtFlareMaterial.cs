using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate the material and texture for an SgtFlare.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtFlare))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtFlareMaterial")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Flare Material")]
	public class SgtFlareMaterial : MonoBehaviour
	{
		public enum ZTestState
		{
			//Less     = 0,
			//Greater  = 1,
			LEqual   = 2,
			//GEqual   = 3,
			//Equal    = 4,
			//NotEqual = 5,
			Always   = 6
		}

		public enum DstBlendState
		{
			//Zero = 0,
			One = 1,
			//DstColor = 2,
			//SrcColor = 3,
			//OneMinusDstColor = 4,
			//SrcAlpha = 5,
			OneMinusSrcColor = 6,
			//DstAlpha = 7,
			//OneMinusDstAlpha = 8,
			//SrcAlphaSaturate = 9,
			//OneMinusSrcAlpha = 10
		}

		/// <summary>The ZTest mode of the material (LEqual = default, Always = draw on top).</summary>
		public ZTestState ZTest { set { zTest = value; } get { return zTest; } } [FSA("ZTest")] [SerializeField] private ZTestState zTest = ZTestState.LEqual;

		/// <summary>The ZTest mode of the material (One = Additive, OneMinusSrcColor = Additive Smooth).</summary>
		public DstBlendState DstBlend { set { dstBlend = value; } get { return dstBlend; } } [FSA("DstBlend")] [SerializeField] private DstBlendState dstBlend = DstBlendState.One;

		/// <summary>This allows you to adjust the render queue of the flare material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.</summary>
		public SgtRenderQueue RenderQueue { set { renderQueue = value; } get { return renderQueue; } } [FSA("RenderQueue")] [SerializeField] private SgtRenderQueue renderQueue = SgtRenderQueue.GroupType.Transparent;

		/// <summary>The format of the generated texture.</summary>
		public TextureFormat Format { set { format = value; } get { return format; } } [FSA("Format")] [SerializeField] private TextureFormat format = TextureFormat.ARGB32;

		/// <summary>The width of the generated texture. A higher value can result in a smoother transition.</summary>
		public int Width { set { width = value; } get { return width; } } [FSA("Width")] [SerializeField] private int width = 256;

		/// <summary>The base color will be multiplied by this.</summary>
		public Color Color { set { color = value; } get { return color; } } [FSA("Color")] [SerializeField] private Color color = Color.white;

		/// <summary>The color transition style.</summary>
		public SgtEase.Type Ease { set { ease = value; } get { return ease; } } [FSA("Ease")] [SerializeField] private SgtEase.Type ease = SgtEase.Type.Exponential;

		/// <summary>The sharpness of the red transition.</summary>
		public float SharpnessR { set { sharpnessR = value; } get { return sharpnessR; } } [FSA("SharpnessR")] [SerializeField] private float sharpnessR = 3.0f;

		/// <summary>The sharpness of the green transition.</summary>
		public float SharpnessG { set { sharpnessG = value; } get { return sharpnessG; } } [FSA("SharpnessG")] [SerializeField] private float sharpnessG = 2.0f;

		/// <summary>The sharpness of the blue transition.</summary>
		public float SharpnessB { set { sharpnessB = value; } get { return sharpnessB; } } [FSA("SharpnessB")] [SerializeField] private float sharpnessB = 1.0f;

		[System.NonSerialized]
		private Material generatedMaterial;

		[System.NonSerialized]
		private Texture2D generatedTexture;

		[System.NonSerialized]
		private SgtFlare cachedFlare;

		[System.NonSerialized]
		private bool cachedFlareSet;

		public SgtFlare CachedFlare
		{
			get
			{
				if (cachedFlareSet == false)
				{
					cachedFlare    = GetComponent<SgtFlare>();
					cachedFlareSet = true;
				}

				return cachedFlare;
			}
		}

		public Material GeneratedMaterial
		{
			get
			{
				return generatedMaterial;
			}
		}

		public Texture2D GeneratedTexture
		{
			get
			{
				return generatedTexture;
			}
		}

#if UNITY_EDITOR
		/// <summary>This method allows you to export the generated texture as an asset.
		/// Once done, you can remove this component, and set the <b>SgtFlare</b> component's <b>Material</b> setting using the exported asset.</summary>
		[ContextMenu("Export Texture")]
		public void ExportTexture()
		{
			var importer = SgtHelper.ExportTextureDialog(generatedTexture, "Flare Texture (Generated)");

			if (importer != null)
			{
				importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
				importer.alphaSource        = UnityEditor.TextureImporterAlphaSource.None;
				importer.wrapMode           = TextureWrapMode.Clamp;
				importer.filterMode         = FilterMode.Trilinear;
				importer.anisoLevel         = 16;

				importer.SaveAndReimport();
			}
		}
#endif

		[ContextMenu("Update Material")]
		public void UpdateMaterial()
		{
			// Create?
			if (generatedMaterial == null)
			{
				generatedMaterial = SgtHelper.CreateTempMaterial("Flare Material (Generated)", "Space Graphics Toolkit/Flare");

				ApplyMaterial();
			}

			generatedMaterial.renderQueue = renderQueue;

			generatedMaterial.SetTexture(SgtShader._MainTex, generatedTexture);

			generatedMaterial.SetInt(SgtShader._ZTest, (int)zTest);
			generatedMaterial.SetInt(SgtShader._DstBlend, (int)dstBlend);
		}

		[ContextMenu("Update Texture")]
		public void UpdateTexture()
		{
			if (width > 0)
			{
				// Destroy if invalid
				if (generatedTexture != null)
				{
					if (generatedTexture.width != width || generatedTexture.height != 1 || generatedTexture.format != format)
					{
						generatedTexture = SgtHelper.Destroy(generatedTexture);
					}
				}

				// Create?
				if (generatedTexture == null)
				{
					generatedTexture = SgtHelper.CreateTempTexture2D("Flare Texture (Generated)", width, 1, format);

					generatedTexture.wrapMode = TextureWrapMode.Clamp;

					if (generatedMaterial != null)
					{
						generatedMaterial.SetTexture(SgtShader._MainTex, generatedTexture);
					}
				}

				var stepU = 1.0f / (width - 1);

				for (var x = 0; x < width; x++)
				{
					WritePixel(stepU * x, x);
				}

				generatedTexture.Apply();
			}
		}

		[ContextMenu("Apply Material")]
		public void ApplyMaterial()
		{
			CachedFlare.Material = generatedMaterial;
		}

		[ContextMenu("Remove Material")]
		public void RemoveMaterial()
		{
			if (CachedFlare.Material == generatedMaterial)
			{
				cachedFlare.Material = null;
			}
		}

		protected virtual void OnEnable()
		{
			UpdateMaterial();
			UpdateTexture();
			ApplyMaterial();
		}

		protected virtual void Update()
		{
			if (generatedMaterial != null)
			{
				generatedMaterial.color = color;
			}
		}

		protected virtual void OnDisable()
		{
			RemoveMaterial();
		}

		protected virtual void OnDestroy()
		{
			SgtHelper.Destroy(generatedMaterial);
			SgtHelper.Destroy(generatedTexture );
		}

		private void WritePixel(float u, int x)
		{
			var color = Color.white;

			color.r *= 1.0f - SgtEase.Evaluate(ease, SgtHelper.Sharpness(u, sharpnessR));
			color.g *= 1.0f - SgtEase.Evaluate(ease, SgtHelper.Sharpness(u, sharpnessG));
			color.b *= 1.0f - SgtEase.Evaluate(ease, SgtHelper.Sharpness(u, sharpnessB));
			color.a  = color.grayscale;

			generatedTexture.SetPixel(x, 0, SgtHelper.Saturate(color));
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtFlareMaterial))]
	public class SgtFlareMaterial_Editor : SgtEditor<SgtFlareMaterial>
	{
		protected override void OnInspector()
		{
			var updateMaterial = false;
			var updateTexture  = false;
			var applyMaterial    = false;

			Draw("zTest", ref updateMaterial, "The ZTest mode of the material (Always = draw on top).");
			Draw("dstBlend", ref updateMaterial, "The ZTest mode of the material (Always = draw on top).");
			Draw("renderQueue", ref updateMaterial, "This allows you to adjust the render queue of the aurora material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.");

			Separator();

			Draw("format", ref updateTexture, "The format of the generated texture.");
			BeginError(Any(t => t.Width < 1));
				Draw("width", ref updateTexture, "The width of the generated texture. A higher value can result in a smoother transition.");
			EndError();

			Separator();

			Draw("color", ref updateTexture, "The base color will be multiplied by this.");
			Draw("ease", ref updateTexture, "The color transition style.");
			Draw("sharpnessR", ref updateTexture, "The sharpness of the red transition.");
			Draw("sharpnessG", ref updateTexture, "The sharpness of the green transition.");
			Draw("sharpnessB", ref updateTexture, "The sharpness of the blue transition.");

			serializedObject.ApplyModifiedProperties();

			if (updateMaterial == true) DirtyEach(t => t.UpdateMaterial());
			if (updateTexture  == true) DirtyEach(t => t.UpdateTexture ());
			if (applyMaterial  == true) DirtyEach(t => t.ApplyMaterial ());
		}
	}
}
#endif