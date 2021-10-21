using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate the SgtAccretion.NearTex field.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtAccretion))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtAccretionNearTex")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Accretion NearTex")]
	public class SgtAccretionNearTex : MonoBehaviour
	{
		/// <summary>The width of the generated texture. A higher value can result in a smoother transition.</summary>
		public int Width { set { if (width != value) { width = value; DirtyTexture(); } } get { return width; } } [FSA("Width")] [SerializeField] private int width = 256;

		/// <summary>The format of the generated texture.</summary>
		public TextureFormat Format { set { if (format != value) { format = value; DirtyTexture(); } } get { return format; } } [FSA("Format")] [SerializeField] private TextureFormat format = TextureFormat.Alpha8;

		/// <summary>The ease type used for the transition.</summary>
		public SgtEase.Type Ease { set { if (ease != value) { ease = value; DirtyTexture(); } } get { return ease; } } [FSA("Ease")] [SerializeField] private SgtEase.Type ease = SgtEase.Type.Smoothstep;

		/// <summary>The sharpness of the transition.</summary>
		public float Sharpness { set { if (sharpness != value) { sharpness = value; DirtyTexture(); } } get { return sharpness; } } [FSA("Sharpness")] [SerializeField] private float sharpness = 1.0f;

		/// <summary>The start point of the fading.</summary>
		public float Offset { set { if (offset != value) { offset = value; DirtyTexture(); } } get { return offset; } } [FSA("Offset")] [Range(0.0f, 1.0f)] [SerializeField] private float offset;

		[System.NonSerialized]
		private Texture2D generatedTexture;

		[System.NonSerialized]
		private SgtAccretion cachedAccretion;

		[System.NonSerialized]
		private bool cachedAccretionSet;

		public SgtAccretion CachedAccretion
		{
			get
			{
				if (cachedAccretionSet == false)
				{
					cachedAccretion    = GetComponent<SgtAccretion>();
					cachedAccretionSet = true;
				}

				return cachedAccretion;
			}
		}

		public void DirtyTexture()
		{
			UpdateTexture();
		}

#if UNITY_EDITOR
		/// <summary>This method allows you to export the generated texture as an asset.
		/// Once done, you can remove this component, and set the <b>SgtAccretion</b> component's <b>NearTex</b> setting using the exported asset.</summary>
		[ContextMenu("Export Texture")]
		public void ExportTexture()
		{
			var importer = SgtHelper.ExportTextureDialog(generatedTexture, "Accretion Near");

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
			CachedAccretion.NearTex = generatedTexture;
		}

		[ContextMenu("Remove Texture")]
		public void RemoveTexture()
		{
			if (CachedAccretion.NearTex == generatedTexture)
			{
				cachedAccretion.NearTex = null;
			}
		}

		protected virtual void OnEnable()
		{
			UpdateTexture();
		}

		protected virtual void OnDestroy()
		{
			SgtHelper.Destroy(generatedTexture);
		}

		protected virtual void OnDidApplyAnimationProperties()
		{
			DirtyTexture();
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
					generatedTexture = SgtHelper.CreateTempTexture2D("Near (Generated)", width, 1, format);

					generatedTexture.wrapMode = TextureWrapMode.Clamp;
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
			var e     = SgtHelper.Saturate(SgtEase.Evaluate(ease, SgtHelper.Sharpness(Mathf.InverseLerp(offset, 1.0f, u), sharpness)));
			var color = new Color(1.0f, 1.0f, 1.0f, e);

			generatedTexture.SetPixel(x, 0, color);
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtAccretionNearTex))]
	public class SgtAccretionNearTex_Editor : SgtEditor<SgtAccretionNearTex>
	{
		protected override void OnInspector()
		{
			var dirtyTexture = false;

			BeginError(Any(t => t.Width < 1));
				Draw("width", ref dirtyTexture, "The width of the generated texture. A higher value can result in a smoother transition.");
			EndError();
			Draw("format", ref dirtyTexture, "The format of the generated texture.");

			Separator();

			Draw("ease", ref dirtyTexture, "The ease type used for the transition.");
			Draw("sharpness", ref dirtyTexture, "The sharpness of the transition.");
			BeginError(Any(t => t.Offset >= 1.0f));
				Draw("offset", ref dirtyTexture, "The start point of the fading.");
			EndError();

			if (dirtyTexture == true)
			{
				DirtyEach(t => t.DirtyTexture());
			}
		}
	}
}
#endif