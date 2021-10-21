using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate the SgtStarfieldInfinite.FarTex field.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtStarfieldInfinite))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtStarfieldInfiniteFarTex")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Starfield Infinite FarTex")]
	public class SgtStarfieldInfiniteFarTex : MonoBehaviour
	{
		/// <summary>The width of the generated texture. A higher value can result in a smoother transition.</summary>
		public int Width { set { if (width != value) { width = value; DirtyTexture(); } } get { return width; } } [FSA("Width")] [SerializeField] private int width = 256;
	
		/// <summary>The format of the generated texture.</summary>
		public TextureFormat Format { set { if (format != value) { format = value; DirtyTexture(); } } get { return format; } } [FSA("Format")] [SerializeField] private TextureFormat format = TextureFormat.ARGB32;

		/// <summary>The transition style.</summary>
		public SgtEase.Type Ease { set { if (ease != value) { ease = value; DirtyTexture(); } } get { return ease; } } [FSA("Ease")] [SerializeField] private SgtEase.Type ease = SgtEase.Type.Smoothstep;

		/// <summary>The sharpness of the transition.</summary>
		public float Sharpness { set { if (sharpness != value) { sharpness = value; DirtyTexture(); } } get { return sharpness; } } [FSA("Sharpness")] [SerializeField] private float sharpness = 1.0f;

		[System.NonSerialized]
		private Texture2D generatedTexture;

		[System.NonSerialized]
		private SgtStarfieldInfinite cachedStarfieldInfinite;

		[System.NonSerialized]
		private bool cachedStarfieldInfiniteSet;

		public Texture2D GeneratedTexture
		{
			get
			{
				return generatedTexture;
			}
		}

		public SgtStarfieldInfinite CachedStarfieldInfinite
		{
			get
			{
				if (cachedStarfieldInfiniteSet == false)
				{
					cachedStarfieldInfinite    = GetComponent<SgtStarfieldInfinite>();
					cachedStarfieldInfiniteSet = true;
				}

				return cachedStarfieldInfinite;
			}
		}

		public void DirtyTexture()
		{
			UpdateTexture();
		}

#if UNITY_EDITOR
		/// <summary>This method allows you to export the generated texture as an asset.
		/// Once done, you can remove this component, and set the <b>SgtStarfieldInfinite</b> component's <b>FarTex</b> setting using the exported asset.</summary>
		[ContextMenu("Export Texture")]
		public void ExportTexture()
		{
			var importer = SgtHelper.ExportTextureDialog(generatedTexture, "StarfieldInfinite Far");

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
			CachedStarfieldInfinite.FarTex = generatedTexture;
		}

		[ContextMenu("Remove Texture")]
		public void RemoveTexture()
		{
			if (CachedStarfieldInfinite.FarTex == generatedTexture)
			{
				cachedStarfieldInfinite.FarTex = null;
			}
		}

		protected virtual void OnEnable()
		{
			UpdateTexture();
			ApplyTexture();
		}

		protected virtual void OnDisable()
		{
			RemoveTexture();
		}

		protected virtual void OnDestroy()
		{
			SgtHelper.Destroy(generatedTexture);
		}

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
					generatedTexture = SgtHelper.CreateTempTexture2D("Far (Generated)", width, 1, format);

					generatedTexture.wrapMode = TextureWrapMode.Clamp;

					ApplyTexture();
				}

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
			var fade  = SgtHelper.Saturate(SgtEase.Evaluate(ease, SgtHelper.Sharpness(u, sharpness)));
			var color = new Color(fade, fade, fade, fade);

			generatedTexture.SetPixel(x, 0, color);
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtStarfieldInfiniteFarTex))]
	public class SgtStarfieldInfiniteFarTex_Editor : SgtEditor<SgtStarfieldInfiniteFarTex>
	{
		protected override void OnInspector()
		{
			var dirtyTexture = false;

			BeginError(Any(t => t.Width < 1));
				Draw("width", ref dirtyTexture, "The width of the generated texture. A higher value can result in a smoother transition.");
			EndError();
			Draw("format", ref dirtyTexture, "The format of the generated texture.");

			Separator();

			Draw("ease", ref dirtyTexture, "The transition style.");
			Draw("sharpness", ref dirtyTexture, "The sharpness of the transition.");

			if (dirtyTexture == true) DirtyEach(t => t.DirtyTexture());
		}
	}
}
#endif