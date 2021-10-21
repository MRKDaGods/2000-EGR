using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you generate the SgtJovian.ScatteringTex field. If you want to improve performance then you can use the context menu to export the texture and manually apply it.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtJovian))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtJovianScattering")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Jovian Scattering")]
	public class SgtJovianScatteringTex : MonoBehaviour
	{
		/// <summary>The resolution of the day/sunset/night color transition in pixels. A higher value can result in smoother results.</summary>
		public int Width { set { if (width != value) { width = value; DirtyTexture(); } } get { return width; } } [FSA("Width")] [SerializeField] private int width = 64;

		/// <summary>The resolution of the scattering transition in pixels.</summary>
		public int Height { set { if (height != value) { height = value; DirtyTexture(); } } get { return height; } } [FSA("Height")] [SerializeField] private int height = 512;

		/// <summary>The format of the generated texture.</summary>
		public TextureFormat Format { set { if (format != value) { format = value; DirtyTexture(); } } get { return format; } } [FSA("Format")] [SerializeField] private TextureFormat format = TextureFormat.ARGB32;

		/// <summary>The sharpness of the forward scattered light.</summary>
		public float Mie { set { if (mie != value) { mie = value; DirtyTexture(); } } get { return mie; } } [FSA("Mie")] [SerializeField] private float mie = 150.0f;

		/// <summary>The brightness of the front and back scattered light.</summary>
		public float Rayleigh { set { if (rayleigh != value) { rayleigh = value; DirtyTexture(); } } get { return rayleigh; } } [FSA("Rayleigh")] [SerializeField] private float rayleigh = 0.1f;

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
		/// Once done, you can remove this component, and set the <b>SgtJovian</b> component's <b>ScatteringTex</b> setting using the exported asset.</summary>
		[ContextMenu("Export Texture")]
		public void ExportTexture()
		{
			var importer = SgtHelper.ExportTextureDialog(generatedTexture, "Jovian Scattering");

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
			CachedJovian.ScatteringTex = generatedTexture;
		}

		[ContextMenu("Remove Texture")]
		public void RemoveTexture()
		{
			if (CachedJovian.ScatteringTex == generatedTexture)
			{
				cachedJovian.ScatteringTex = null;
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
			if (width > 0 && height > 0)
			{
				// Destroy if invalid
				if (generatedTexture != null)
				{
					if (generatedTexture.width != width || generatedTexture.height != height || generatedTexture.format != format)
					{
						generatedTexture = SgtHelper.Destroy(generatedTexture);
					}
				}

				// Create?
				if (generatedTexture == null)
				{
					generatedTexture = SgtHelper.CreateTempTexture2D("Scattering (Generated)", width, height, format);

					generatedTexture.wrapMode = TextureWrapMode.Clamp;
				}

				var stepU = 1.0f / (width  - 1);
				var stepV = 1.0f / (height - 1);

				for (var y = 0; y < height; y++)
				{
					var v = y * stepV;

					for (var x = 0; x < width; x++)
					{
						WritePixel(stepU * x, v, x, y);
					}
				}

				generatedTexture.Apply();
			}

			ApplyTexture();
		}

		private void WritePixel(float u, float v, int x, int y)
		{
			var ray        = Mathf.Abs(v * 2.0f - 1.0f); ray = rayleigh * ray * ray;
			var mie        = Mathf.Pow(v, this.mie);
			var scattering = ray + mie * (1.0f - ray);
			var sunsetU    = Mathf.InverseLerp(sunsetEnd, sunsetStart, u);
			var color      = default(Color);

			color.r = 1.0f - SgtEase.Evaluate(sunsetEase, SgtHelper.Sharpness(sunsetU, sunsetSharpnessR));
			color.g = 1.0f - SgtEase.Evaluate(sunsetEase, SgtHelper.Sharpness(sunsetU, sunsetSharpnessG));
			color.b = 1.0f - SgtEase.Evaluate(sunsetEase, SgtHelper.Sharpness(sunsetU, sunsetSharpnessB));
			color.a = (color.r + color.g + color.b) / 3.0f;

			generatedTexture.SetPixel(x, y, SgtHelper.Saturate(color * scattering));
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtJovianScatteringTex))]
	public class SgtJovianScatteringTex_Editor : SgtEditor<SgtJovianScatteringTex>
	{
		protected override void OnInspector()
		{
			var dirtyTexture = false;

			BeginError(Any(t => t.Width <= 1));
				Draw("width", ref dirtyTexture, "The resolution of the day/sunset/night color transition in pixels. A higher value can result in smoother results.");
			EndError();
			BeginError(Any(t => t.Height <= 1));
				Draw("height", ref dirtyTexture, "The resolution of the scattering transition in pixels.");
			EndError();
			Draw("format", ref dirtyTexture, "The format of the generated texture.");

			Separator();

			BeginError(Any(t => t.Mie < 1.0f));
				Draw("mie", ref dirtyTexture, "The sharpness of the forward scattered light.");
			EndError();
			BeginError(Any(t => t.Rayleigh < 0.0f));
				Draw("rayleigh", ref dirtyTexture, "The brightness of the front and back scattered light.");
			EndError();

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