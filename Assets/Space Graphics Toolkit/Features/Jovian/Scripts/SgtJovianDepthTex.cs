using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you generate the SgtJovian.DepthTex field. If you want to improve performance then you can use the context menu to export the texture and manually apply it.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtJovian))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtJovianDepthTex")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Jovian DepthTex")]
	public class SgtJovianDepthTex : MonoBehaviour
	{
		/// <summary>The resolution of the optical depth color. A higher value can result in smoother results.</summary>
		public int Width { set { if (width != value) { width = value; DirtyTexture(); } } get { return width; } } [FSA("Width")] [SerializeField] private int width = 256;

		/// <summary>The format of the generated texture.</summary>
		public TextureFormat Format { set { if (format != value) { format = value; DirtyTexture(); } } get { return format; } } [FSA("Format")] [SerializeField] private TextureFormat format = TextureFormat.ARGB32;

		/// <summary>The rim transition style.</summary>
		public SgtEase.Type RimEase { set { if (rimEase != value) { rimEase = value; DirtyTexture(); } } get { return rimEase; } } [FSA("RimEase")] [SerializeField] private SgtEase.Type rimEase = SgtEase.Type.Exponential;

		/// <summary>The rim transition sharpness.</summary>
		public float RimPower { set { if (rimPower != value) { rimPower = value; DirtyTexture(); } } get { return rimPower; } } [FSA("RimPower")] [SerializeField] private float rimPower = 5.0f;

		/// <summary>The rim color.</summary>
		public Color RimColor { set { if (rimColor != value) { rimColor = value; DirtyTexture(); } } get { return rimColor; } } [FSA("RimColor")] [SerializeField] private Color rimColor = new Color(1.0f, 0.0f, 0.0f, 0.25f);

		/// <summary>The density of the atmosphere.</summary>
		public float AlphaDensity { set { if (alphaDensity != value) { alphaDensity = value; DirtyTexture(); } } get { return alphaDensity; } } [FSA("AlphaDensity")] [SerializeField] private float alphaDensity = 50.0f;

		/// <summary>The strength of the density fading in the upper atmosphere.</summary>
		public float AlphaFade { set { if (alphaFade != value) { alphaFade = value; DirtyTexture(); } } get { return alphaFade; } } [FSA("AlphaFade")] [SerializeField] private float alphaFade = 2.0f;

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
		/// Once done, you can remove this component, and set the <b>SgtJovian</b> component's <b>DepthTex</b> setting using the exported asset.</summary>
		[ContextMenu("Export Texture")]
		public void ExportTexture()
		{
			var importer = SgtHelper.ExportTextureDialog(generatedTexture, "Jovian Depth");

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
			CachedJovian.DepthTex = generatedTexture;
		}

		[ContextMenu("Remove Texture")]
		public void RemoveTexture()
		{
			if (CachedJovian.DepthTex == generatedTexture)
			{
				cachedJovian.DepthTex = null;
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
					if (generatedTexture.width != width || generatedTexture.height != 1 || generatedTexture.format != format)
					{
						generatedTexture = SgtHelper.Destroy(generatedTexture);
					}
				}

				// Create?
				if (generatedTexture == null)
				{
					generatedTexture = SgtHelper.CreateTempTexture2D("Depth (Generated)", width, 1, format);

					generatedTexture.wrapMode = TextureWrapMode.Clamp;

					ApplyTexture();
				}

				var color = Color.clear;
				var stepU = 1.0f / (width - 1);

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
			var rim   = 1.0f - SgtEase.Evaluate(rimEase, 1.0f - Mathf.Pow(1.0f - u, rimPower));
			var color = Color.Lerp(Color.white, rimColor, rim * rimColor.a);

			color.a = 1.0f - Mathf.Pow(1.0f - Mathf.Pow(u, alphaFade), alphaDensity);

			generatedTexture.SetPixel(x, 0, SgtHelper.Saturate(color));
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtJovianDepthTex))]
	public class SgtJovianDepthTex_Editor : SgtEditor<SgtJovianDepthTex>
	{
		protected override void OnInspector()
		{
			var dirtyTexture = false;

			BeginError(Any(t => t.Width < 1));
				Draw("width", ref dirtyTexture, "The resolution of the optical depth color. A higher value can result in smoother results.");
			EndError();
			Draw("format", ref dirtyTexture, "The format of the generated texture.");

			Separator();

			Draw("rimEase", ref dirtyTexture, "The rim transition style.");
			BeginError(Any(t => t.RimPower < 1.0f));
				Draw("rimPower", ref dirtyTexture, "The rim transition sharpness.");
			EndError();
			Draw("rimColor", ref dirtyTexture, "The rim color.");

			Separator();

			BeginError(Any(t => t.AlphaDensity < 1.0f));
				Draw("alphaDensity", ref dirtyTexture, "The density of the atmosphere.");
			EndError();
			BeginError(Any(t => t.AlphaFade < 1.0f));
				Draw("alphaFade", ref dirtyTexture, "The strength of the density fading in the upper atmosphere.");
			EndError();

			if (dirtyTexture == true) DirtyEach(t => t.DirtyTexture());
		}
	}
}
#endif