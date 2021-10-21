using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component generates the SgtCorona.InnerDepthTex and SgtCorona.OuterDepthTex textures.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtCorona))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtCoronaDepthTex")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Corona DepthTex")]
	public class SgtCoronaDepthTex : MonoBehaviour
	{
		/// <summary>The resolution of the surface/space optical thickness transition in pixels.</summary>
		public int Width { set { width = value; } get { return width; } } [FSA("Width")] [SerializeField] private int width = 256;

		/// <summary>The format of the generated texture.</summary>
		public TextureFormat Format { set { if (format != value) { format = value; DirtyTextures(); } } get { return format; } } [FSA("Format")] [SerializeField] private TextureFormat format = TextureFormat.ARGB32;

		/// <summary>The horizon color for both textures.</summary>
		public Color HorizonColor { set { if (horizonColor != value) { horizonColor = value; DirtyTextures(); } } get { return horizonColor; } } [FSA("HorizonColor")] [SerializeField] private Color horizonColor = new Color(1.0f, 0.52f, 0.0f);

		/// <summary>The base color of the inner texture.</summary>
		public Color InnerColor { set { if (innerColor != value) { innerColor = value; DirtyTextures(); } } get { return innerColor; } } [FSA("InnerColor")] [SerializeField] private Color innerColor = new Color(1.0f, 0.76f, 0.0f);

		/// <summary>The transition style between the surface and horizon.</summary>
		public SgtEase.Type InnerEase { set { if (innerEase != value) { innerEase = value; DirtyTextures(); } } get { return innerEase; } } [FSA("InnerEase")] [SerializeField] private SgtEase.Type innerEase = SgtEase.Type.Exponential;

		/// <summary>The strength of the inner texture transition.</summary>
		public float InnerColorSharpness { set { if (innerColorSharpness != value) { innerColorSharpness = value; DirtyTextures(); } } get { return innerColorSharpness; } } [FSA("InnerColorSharpness")] [SerializeField] private float innerColorSharpness = 1.0f;

		/// <summary>The strength of the inner texture transition.</summary>
		public float InnerAlphaSharpness { set { if (innerAlphaSharpness != value) { innerAlphaSharpness = value; DirtyTextures(); } } get { return innerAlphaSharpness; } } [FSA("InnerAlphaSharpness")] [SerializeField] private float innerAlphaSharpness = 1.0f;

		/// <summary>The base color of the outer texture.</summary>
		public Color OuterColor { set { if (outerColor != value) { outerColor = value; DirtyTextures(); } } get { return outerColor; } } [FSA("OuterColor")] [SerializeField] private Color outerColor = new Color(1.0f, 0.39f, 0.0f);

		/// <summary>The transition style between the sky and horizon.</summary>
		public SgtEase.Type OuterEase { set { if (outerEase != value) { outerEase = value; DirtyTextures(); } } get { return outerEase; } } [FSA("OuterEase")] [SerializeField] private SgtEase.Type outerEase = SgtEase.Type.Quadratic;

		/// <summary>The strength of the outer texture transition.</summary>
		public float OuterColorSharpness { set { if (outerColorSharpness != value) { outerColorSharpness = value; DirtyTextures(); } } get { return outerColorSharpness; } } [FSA("OuterColorSharpness")] [SerializeField] private float outerColorSharpness = 0.0f;

		/// <summary>The strength of the outer texture transition.</summary>
		public float OuterAlphaSharpness { set { if (outerAlphaSharpness != value) { outerAlphaSharpness = value; DirtyTextures(); } } get { return outerAlphaSharpness; } } [FSA("OuterAlphaSharpness")] [SerializeField] private float outerAlphaSharpness = 2.7f;

		[System.NonSerialized]
		private SgtCorona cachedCorona;

		[System.NonSerialized]
		private bool cachedCoronaSet;

		[System.NonSerialized]
		private Texture2D generatedInnerTexture;

		[System.NonSerialized]
		private Texture2D generatedOuterTexture;

		public SgtCorona CachedCorona
		{
			get
			{
				if (cachedCoronaSet == false)
				{
					cachedCorona    = GetComponent<SgtCorona>();
					cachedCoronaSet = true;
				}

				return cachedCorona;
			}
		}

		public Texture2D GeneratedInnerTexture
		{
			get
			{
				return generatedInnerTexture;
			}
		}

		public Texture2D GeneratedOuterTexture
		{
			get
			{
				return generatedOuterTexture;
			}
		}

		public void DirtyTextures()
		{
			UpdateTextures();
		}

#if UNITY_EDITOR
		/// <summary>This method allows you to export the generated texture as an asset.
		/// Once done, you can remove this component, and set the <b>SgtCorona</b> component's <b>InnerDepthTex</b> setting using the exported asset.</summary>
		[ContextMenu("Export Inner Texture")]
		public void ExportInnerTexture()
		{
			var importer = SgtHelper.ExportTextureDialog(generatedOuterTexture, "Corona InnerDepth");

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
		/// Once done, you can remove this component, and set the <b>SgtCorona</b> component's <b>OuterDepthTex</b> setting using the exported asset.</summary>
		[ContextMenu("Export Outer Texture")]
		public void ExportOuterTexture()
		{
			var importer = SgtHelper.ExportTextureDialog(generatedOuterTexture, "Corona OuterDepth");

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

		[ContextMenu("Apply Textures")]
		public void ApplyTextures()
		{
			CachedCorona.InnerDepthTex = generatedInnerTexture;
			cachedCorona.OuterDepthTex = generatedOuterTexture;
		}

		[ContextMenu("Remove Textures")]
		public void RemoveTextures()
		{
			if (CachedCorona.InnerDepthTex == generatedInnerTexture)
			{
				cachedCorona.InnerDepthTex = null;
			}

			if (cachedCorona.OuterDepthTex == generatedOuterTexture)
			{
				cachedCorona.OuterDepthTex = null;
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

#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			UpdateTextures();
		}
#endif

		private void UpdateTextures()
		{
			if (width > 0)
			{
				ValidateTexture(ref generatedInnerTexture, "InnerDepth (Generated)");
				ValidateTexture(ref generatedOuterTexture, "OuterDepth (Generated)");

				var color = Color.clear;
				var stepU  = 1.0f / (width - 1);

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

		private bool ValidateTexture(ref Texture2D texture2D, string createName)
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

				return true;
			}

			return false;
		}

		private void WritePixel(Texture2D texture2D, float u, int x, Color baseColor, SgtEase.Type ease, float colorSharpness, float alphaSharpness)
		{
			var colorU = SgtHelper.Sharpness(u, colorSharpness); colorU = SgtEase.Evaluate(ease, colorU);
			var alphaU = SgtHelper.Sharpness(u, alphaSharpness); alphaU = SgtEase.Evaluate(ease, alphaU);

			var color = Color.Lerp(baseColor, horizonColor, colorU);

			color.a = alphaU;

			color.r = Mathf.Clamp01(color.r);
			color.g = Mathf.Clamp01(color.g);
			color.b = Mathf.Clamp01(color.b);
			color.a = Mathf.Clamp01(color.a);

			texture2D.SetPixel(x, 0, SgtHelper.Saturate(color));
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtCoronaDepthTex))]
	public class SgtCoronaDepthTex_Editor : SgtEditor<SgtCoronaDepthTex>
	{
		protected override void OnInspector()
		{
			var dirtyTexture = false;

			BeginError(Any(t => t.Width < 1));
				Draw("width", ref dirtyTexture, "The resolution of the surface/space optical thickness transition in pixels.");
			EndError();
			Draw("format", ref dirtyTexture, "The format of the generated texture.");
			Draw("horizonColor", ref dirtyTexture, "The horizon color for both textures.");

			Separator();

			Draw("innerColor", ref dirtyTexture, "The base color of the inner texture.");
			Draw("innerEase", ref dirtyTexture, "The transition style between the surface and horizon.");
			Draw("innerColorSharpness", ref dirtyTexture, "The strength of the inner texture transition.");
			Draw("innerAlphaSharpness", ref dirtyTexture, "The strength of the inner texture transition.");

			Separator();

			Draw("outerColor", ref dirtyTexture, "The base color of the outer texture.");
			Draw("outerEase", ref dirtyTexture, "The transition style between the sky and horizon.");
			Draw("outerColorSharpness", ref dirtyTexture, "The strength of the outer texture transition.");
			Draw("outerAlphaSharpness", ref dirtyTexture, "The strength of the outer texture transition.");

			if (dirtyTexture == true) DirtyEach(t => t.DirtyTextures());
		}
	}
}
#endif