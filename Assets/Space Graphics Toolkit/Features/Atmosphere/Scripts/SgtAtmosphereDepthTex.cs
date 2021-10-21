using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate the SgtAtmosphere.InnerDepthTex and SgtAtmosphere.OuterDepthTex fields.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtAtmosphere))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtAtmosphereDepthTex")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Atmosphere DepthTex")]
	public class SgtAtmosphereDepthTex : MonoBehaviour
	{
		/// <summary>The width of the generated texture. A higher value can result in a smoother transition.</summary>
		public int Width { set { if (width != value) { width = value; DirtyTextures(); } } get { return width; } } [FSA("Width")] [SerializeField] private int width = 256;

		/// <summary>The format of the generated texture.</summary>
		public TextureFormat Format { set { if (format != value) { format = value; DirtyTextures(); } } get { return format; } } [FSA("Format")] [SerializeField] private TextureFormat format = TextureFormat.ARGB32;

		/// <summary>This allows you to set the color that appears on the horizon.</summary>
		public Color HorizonColor { set { if (horizonColor != value) { horizonColor = value; DirtyTextures(); } } get { return horizonColor; } } [FSA("HorizonColor")] [SerializeField] private Color horizonColor = Color.white;

		/// <summary>The base color of the inner texture.</summary>
		public Color InnerColor { set { if (innerColor != value) { innerColor = value; DirtyTextures(); } } get { return innerColor; } } [FSA("InnerColor")] [SerializeField] private Color innerColor = new Color(0.15f, 0.54f, 1.0f);

		/// <summary>The transition style between the surface and horizon.</summary>
		public SgtEase.Type InnerEase { set { if (innerEase != value) { innerEase = value; DirtyTextures(); } } get { return innerEase; } } [FSA("InnerEase")] [SerializeField] private SgtEase.Type innerEase = SgtEase.Type.Exponential;

		/// <summary>The strength of the inner texture transition.</summary>
		public float InnerColorSharpness { set { if (innerColorSharpness != value) { innerColorSharpness = value; DirtyTextures(); } } get { return innerColorSharpness; } } [FSA("InnerColorSharpness")] [SerializeField] private float innerColorSharpness = 2.0f;

		/// <summary>The strength of the inner texture transition.</summary>
		public float InnerAlphaSharpness { set { if (innerAlphaSharpness != value) { innerAlphaSharpness = value; DirtyTextures(); } } get { return innerAlphaSharpness; } } [FSA("InnerAlphaSharpness")] [SerializeField] private float innerAlphaSharpness = 3.0f;

		/// <summary>The base color of the outer texture.</summary>
		public Color OuterColor { set { if (outerColor != value) { outerColor = value; DirtyTextures(); } } get { return outerColor; } } [FSA("OuterColor")] [SerializeField] private Color outerColor = new Color(0.29f, 0.73f, 1.0f);

		/// <summary>The transition style between the sky and horizon.</summary>
		public SgtEase.Type OuterEase { set { if (outerEase != value) { outerEase = value; DirtyTextures(); } } get { return outerEase; } } [FSA("OuterEase")] [SerializeField] private SgtEase.Type outerEase = SgtEase.Type.Quadratic;

		/// <summary>The strength of the outer texture transition.</summary>
		public float OuterColorSharpness { set { if (outerColorSharpness != value) { outerColorSharpness = value; DirtyTextures(); } } get { return outerColorSharpness; } } [FSA("OuterColorSharpness")] [SerializeField] private float outerColorSharpness = 2.0f;

		/// <summary>The strength of the outer texture transition.</summary>
		public float OuterAlphaSharpness { set { if (outerAlphaSharpness != value) { outerAlphaSharpness = value; DirtyTextures(); } } get { return outerAlphaSharpness; } } [FSA("OuterAlphaSharpness")] [SerializeField] private float outerAlphaSharpness = 3.0f;

		[System.NonSerialized]
		private Texture2D generatedInnerTexture;

		[System.NonSerialized]
		private Texture2D generatedOuterTexture;

		[System.NonSerialized]
		private SgtAtmosphere cachedAtmosphere;

		[System.NonSerialized]
		private bool cachedAtmosphereSet;

		public SgtAtmosphere CachedAtmosphere
		{
			get
			{
				if (cachedAtmosphereSet == false)
				{
					cachedAtmosphere    = GetComponent<SgtAtmosphere>();
					cachedAtmosphereSet = true;
				}

				return cachedAtmosphere;
			}
		}

		public void DirtyTextures()
		{
			UpdateTextures();
		}

#if UNITY_EDITOR
		/// <summary>This method allows you to export the generated texture as an asset.
		/// Once done, you can remove this component, and set the <b>SgtAtmosphere</b> component's <b>InnerDepth</b> setting using the exported asset.</summary>
		[ContextMenu("Export Inner Texture")]
		public void ExportInnerTexture()
		{
			var importer = SgtHelper.ExportTextureDialog(generatedOuterTexture, "Atmosphere InnerDepth");

			if (importer != null)
			{
				importer.textureType         = UnityEditor.TextureImporterType.SingleChannel;
				importer.textureCompression  = UnityEditor.TextureImporterCompression.Uncompressed;
				importer.alphaSource         = UnityEditor.TextureImporterAlphaSource.FromInput;
				importer.wrapMode            = TextureWrapMode.Clamp;
				importer.filterMode          = FilterMode.Trilinear;
				importer.anisoLevel          = 16;
				importer.alphaIsTransparency = true;

				importer.SaveAndReimport();
			}
		}
#endif

#if UNITY_EDITOR
		/// <summary>This method allows you to export the generated texture as an asset.
		/// Once done, you can remove this component, and set the <b>SgtAtmosphere</b> component's <b>OuterDepth</b> setting using the exported asset.</summary>
		[ContextMenu("Export Outer Texture")]
		public void ExportOuterTexture()
		{
			var importer = SgtHelper.ExportTextureDialog(generatedOuterTexture, "Atmosphere OuterDepth");

			if (importer != null)
			{
				importer.textureType         = UnityEditor.TextureImporterType.SingleChannel;
				importer.textureCompression  = UnityEditor.TextureImporterCompression.Uncompressed;
				importer.alphaSource         = UnityEditor.TextureImporterAlphaSource.FromInput;
				importer.wrapMode            = TextureWrapMode.Clamp;
				importer.filterMode          = FilterMode.Trilinear;
				importer.anisoLevel          = 16;
				importer.alphaIsTransparency = true;

				importer.SaveAndReimport();
			}
		}
#endif

		private void UpdateTextures()
		{
			if (width > 0)
			{
				ValidateTexture(ref generatedInnerTexture, "InnerDepth (Generated)");
				ValidateTexture(ref generatedOuterTexture, "OuterDepth (Generated)");

				var stepU = 1.0f / (width - 1);

				for (var x = 0; x < width; x++)
				{
					var u = stepU * x;

					WritePixel(generatedInnerTexture, u, x, innerColor, innerEase, innerColorSharpness, innerAlphaSharpness);
					WritePixel(generatedOuterTexture, u, x, outerColor, outerEase, outerColorSharpness, outerAlphaSharpness);
				}

				generatedInnerTexture.Apply();
				generatedOuterTexture.Apply();
			}

			ApplyTextures();
		}

		[ContextMenu("Apply Textures")]
		public void ApplyTextures()
		{
			CachedAtmosphere.InnerDepthTex = generatedInnerTexture;
			cachedAtmosphere.OuterDepthTex = generatedOuterTexture;
		}

		[ContextMenu("Remove Textures")]
		public void RemoveTextures()
		{
			if (CachedAtmosphere.InnerDepthTex == generatedInnerTexture)
			{
				cachedAtmosphere.InnerDepthTex = null;
			}

			if (cachedAtmosphere.OuterDepthTex == generatedOuterTexture)
			{
				cachedAtmosphere.OuterDepthTex = null;
			}
		}

		protected virtual void OnEnable()
		{
			UpdateTextures();
			ApplyTextures();
		}

		protected virtual void OnDisable()
		{
			RemoveTextures();
		}

		protected virtual void OnDestroy()
		{
			SgtHelper.Destroy(generatedInnerTexture);
			SgtHelper.Destroy(generatedOuterTexture);
		}

		protected virtual void OnDidApplyAnimationProperties()
		{
			UpdateTextures();
		}

		private void ValidateTexture(ref Texture2D texture2D, string createName)
		{
			// Destroy if invalid
			if (texture2D != null)
			{
				if (texture2D.width != width || texture2D.height != 1 || texture2D.format != format)
				{
					texture2D = SgtHelper.Destroy(texture2D);
				}
			}

			// Create?
			if (texture2D == null)
			{
				texture2D = SgtHelper.CreateTempTexture2D(createName, width, 1, format);

				texture2D.wrapMode = TextureWrapMode.Clamp;

				ApplyTextures();
			}
		}

		private void WritePixel(Texture2D texture2D, float u, int x, Color baseColor, SgtEase.Type ease, float colorSharpness, float alphaSharpness)
		{
			var colorU = SgtHelper.Sharpness(u, colorSharpness); colorU = SgtEase.Evaluate(ease, colorU);
			var alphaU = SgtHelper.Sharpness(u, alphaSharpness); alphaU = SgtEase.Evaluate(ease, alphaU);
			var color  = Color.Lerp(baseColor, horizonColor, colorU);

			color.a = alphaU;

			texture2D.SetPixel(x, 0, SgtHelper.Saturate(color));
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtAtmosphereDepthTex))]
	public class SgtAtmosphereDepthTex_Editor : SgtEditor<SgtAtmosphereDepthTex>
	{
		protected override void OnInspector()
		{
			var dirtyTextures = false;

			BeginError(Any(t => t.Width < 1));
				Draw("width", ref dirtyTextures, "The width of the generated texture. A higher value can result in a smoother transition.");
			EndError();
			Draw("format", ref dirtyTextures, "The format of the generated texture.");
			Draw("horizonColor", ref dirtyTextures, "This allows you to set the color that appears on the horizon.");

			Separator();

			Draw("innerColor", ref dirtyTextures, "The base color of the inner texture.");
			Draw("innerEase", ref dirtyTextures, "The transition style between the surface and horizon.");
			Draw("innerColorSharpness", ref dirtyTextures, "The strength of the inner texture transition.");
			Draw("innerAlphaSharpness", ref dirtyTextures, "The strength of the inner texture transition.");

			Separator();

			Draw("outerColor", ref dirtyTextures, "The base color of the outer texture.");
			Draw("outerEase", ref dirtyTextures, "The transition style between the sky and horizon.");
			Draw("outerColorSharpness", ref dirtyTextures, "The strength of the outer texture transition.");
			Draw("outerAlphaSharpness", ref dirtyTextures, "The strength of the outer texture transition.");

			if (dirtyTextures == true) DirtyEach(t => t.DirtyTextures());
		}
	}
}
#endif