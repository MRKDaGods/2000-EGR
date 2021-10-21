using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you generate the SgtJovian.LightingTex field. If you want to improve performance then you can use the context menu to export the texture and manually apply it.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtJovian))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtJovianLighting")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Jovian Lighting")]
	public class SgtJovianLightingTex : MonoBehaviour
	{
		/// <summary>The width of the generated texture. A higher value can result in a smoother transition.</summary>
		public int Width { set { if (width != value) { width = value; DirtyTexture(); } } get { return width; } } [FSA("Width")] [SerializeField] private int width = 256;

		/// <summary>The format of the generated texture.</summary>
		public TextureFormat Format { set { if (format != value) { format = value; DirtyTexture(); } } get { return format; } } [FSA("Format")] [SerializeField] private TextureFormat format = TextureFormat.ARGB32;

		/// <summary>The transition style between the day and night.</summary>
		public SgtEase.Type SunsetEase { set { if (sunsetEase != value) { sunsetEase = value; DirtyTexture(); } } get { return sunsetEase; } } [FSA("SunsetEase")] [SerializeField] private SgtEase.Type sunsetEase = SgtEase.Type.Smoothstep;

		/// <summary>The start point of the sunset (0 = dark side, 1 = light side).</summary>
		public float SunsetStart { set { if (sunsetStart != value) { sunsetStart = value; DirtyTexture(); } } get { return sunsetStart; } } [FSA("SunsetStart")] [SerializeField] [Range(0.0f, 1.0f)] private float sunsetStart = 0.4f;

		/// <summary>The end point of the sunset (0 = dark side, 1 = light side).</summary>
		public float SunsetEnd { set { if (sunsetEnd != value) { sunsetEnd = value; DirtyTexture(); } } get { return sunsetEnd; } } [FSA("SunsetEnd")] [SerializeField] [Range(0.0f, 1.0f)] private float sunsetEnd = 0.6f;

		/// <summary>The sharpness of the sunset red channel transition.</summary>
		public float SunsetSharpnessR { set { if (sunsetSharpnessR != value) { sunsetSharpnessR = value; DirtyTexture(); } } get { return sunsetSharpnessR; } } [FSA("SunsetSharpnessR")] [SerializeField] private float sunsetSharpnessR = 1.0f;

		/// <summary>The sharpness of the sunset green channel transition.</summary>
		public float SunsetSharpnessG { set { if (sunsetSharpnessG != value) { sunsetSharpnessG = value; DirtyTexture(); } } get { return sunsetSharpnessG; } } [FSA("SunsetSharpnessG")] [SerializeField] private float sunsetSharpnessG = 1.0f;

		/// <summary>The sharpness of the sunset blue channel transition.</summary>
		public float SunsetSharpnessB { set { if (sunsetSharpnessB != value) { sunsetSharpnessB = value; DirtyTexture(); } } get { return sunsetSharpnessB; } } [FSA("SunsetSharpnessB")] [SerializeField] private float sunsetSharpnessB = 1.0f;

		[System.NonSerialized]
		private Texture2D generatedTexture;

		[System.NonSerialized]
		private SgtJovian cachedJovian;

		[System.NonSerialized]
		private bool cachedJovianSet;

		public Texture2D GeneratedTexture
		{
			get
			{
				return generatedTexture;
			}
		}

		public SgtJovian CachedJovian
		{
			get
			{
				if (cachedJovianSet == false)
				{
					cachedJovian    = GetComponent<SgtJovian>();
					cachedJovianSet = true;
				}

				return cachedJovian;
			}
		}

		public void DirtyTexture()
		{
			UpdateTexture();
		}

#if UNITY_EDITOR
		/// <summary>This method allows you to export the generated texture as an asset.
		/// Once done, you can remove this component, and set the <b>SgtJovian</b> component's <b>LightingTex</b> setting using the exported asset.</summary>
		[ContextMenu("Export Texture")]
		public void ExportTexture()
		{
			var importer = SgtHelper.ExportTextureDialog(generatedTexture, "Jovian Lighting");

			if (importer != null)
			{
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

		[ContextMenu("Apply Texture")]
		public void ApplyTexture()
		{
			CachedJovian.LightingTex = generatedTexture;
		}

		[ContextMenu("Remove Texture")]
		public void RemoveTexture()
		{
			if (CachedJovian.LightingTex == generatedTexture)
			{
				cachedJovian.LightingTex = null;
			}
		}

		protected virtual void OnEnable()
		{
			UpdateTexture();
		}

		protected virtual void OnDisable()
		{
			RemoveTexture();
		}

		protected virtual void OnDestroy()
		{
			SgtHelper.Destroy(generatedTexture);
		}

		protected virtual void OnDidApplyAnimationProperties()
		{
			UpdateTexture();
		}

#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			UpdateTexture();
		}
#endif

		private void UpdateTexture()
		{
			if (width > 0)
			{
				// Destroy if invalid
				if (generatedTexture != null)
				{
					if (generatedTexture.width != width || generatedTexture.height != 1 || generatedTexture.format != Format)
					{
						generatedTexture = SgtHelper.Destroy(generatedTexture);
					}
				}

				// Create?
				if (generatedTexture == null)
				{
					generatedTexture = SgtHelper.CreateTempTexture2D("Lighting (Generated)", width, 1, Format);

					generatedTexture.wrapMode = TextureWrapMode.Clamp;

					ApplyTexture();
				}

				var stepU = 1.0f / (width  - 1);

				for (var x = 0; x < width; x++)
				{
					WritePixel(stepU * x, x);
				}

				generatedTexture.Apply();
			}

			ApplyTexture();
		}

		private void WritePixel(float u, int x)
		{
			var sunsetU = Mathf.InverseLerp(sunsetEnd, sunsetStart, u);
			var color   = default(Color);

			color.r = SgtEase.Evaluate(sunsetEase, 1.0f - SgtHelper.Sharpness(sunsetU, sunsetSharpnessR));
			color.g = SgtEase.Evaluate(sunsetEase, 1.0f - SgtHelper.Sharpness(sunsetU, sunsetSharpnessG));
			color.b = SgtEase.Evaluate(sunsetEase, 1.0f - SgtHelper.Sharpness(sunsetU, sunsetSharpnessB));
			color.a = 0.0f;

			generatedTexture.SetPixel(x, 0, SgtHelper.Saturate(color));
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtJovianLightingTex))]
	public class SgtJovianLightingTex_Editor : SgtEditor<SgtJovianLightingTex>
	{
		protected override void OnInspector()
		{
			var dirtyTexture = false;

			BeginError(Any(t => t.Width <= 1));
				Draw("width", ref dirtyTexture, "The width of the generated texture. A higher value can result in a smoother transition.");
			EndError();
			Draw("format", ref dirtyTexture, "The format of the generated texture.");

			Separator();

			Draw("sunsetEase", ref dirtyTexture, "The transition style between the day and night.");
			BeginError(Any(t => t.SunsetStart >= t.SunsetEnd));
				Draw("sunsetStart", ref dirtyTexture, "The start point of the sunset (0 = dark side, 1 = light side).");
				Draw("sunsetEnd", ref dirtyTexture, "The end point of the sunset (0 = dark side, 1 = light side).");
			EndError();
			Draw("sunsetSharpnessR", ref dirtyTexture, "The sharpness of the sunset red channel transition.");
			Draw("sunsetSharpnessG", ref dirtyTexture, "The sharpness of the sunset green channel transition.");
			Draw("sunsetSharpnessB", ref dirtyTexture, "The sharpness of the sunset blue channel transition.");

			if (dirtyTexture == true) DirtyEach(t => t.DirtyTexture());
		}
	}
}
#endif