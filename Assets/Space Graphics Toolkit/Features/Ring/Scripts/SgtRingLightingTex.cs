using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate the SgtRing.LightingTex field.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtRing))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtRingLightingTex")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Ring LightingTex")]
	public class SgtRingLightingTex : MonoBehaviour
	{
		/// <summary>The width of the generated texture. A higher value can result in a smoother transition.</summary>
		public int Width { set { if (width != value) { width = value; DirtyTexture(); } } get { return width; } } [FSA("Width")] [SerializeField] private int width = 256;

		/// <summary>The format of the generated texture.</summary>
		public TextureFormat Format { set { if (format != value) { format = value; DirtyTexture(); } } get { return format; } } [FSA("Format")] [SerializeField] private TextureFormat format = TextureFormat.ARGB32;

		/// <summary>How sharp the incoming light scatters forward.</summary>
		public float FrontPower { set { if (frontPower != value) { frontPower = value; DirtyTexture(); } } get { return frontPower; } } [FSA("FrontPower")] [SerializeField] private float frontPower = 2.0f;

		/// <summary>How sharp the incoming light scatters backward.</summary>
		public float BackPower { set { if (backPower != value) { backPower = value; DirtyTexture(); } } get { return backPower; } } [FSA("BackPower")] [SerializeField] private float backPower = 3.0f;

		/// <summary>The strength of the back scattered light.</summary>
		public float BackStrength { set { if (backStrength != value) { backStrength = value; DirtyTexture(); } } get { return backStrength; } } [FSA("Rayleigh")] [SerializeField] [Range(0.0f, 1.0f)] private float backStrength = 0.5f;

		/// <summary>The of the perpendicular scattered light.</summary>
		public float BaseStrength { set { if (baseStrength != value) { baseStrength = value; DirtyTexture(); } } get { return baseStrength; } } [FSA("Rayleigh")] [SerializeField] [Range(0.0f, 1.0f)] private float baseStrength = 0.2f;

		[System.NonSerialized]
		private Texture2D generatedTexture;

		[System.NonSerialized]
		private SgtRing cachedRing;

		[System.NonSerialized]
		private bool cachedRingSet;

		public SgtRing CachedRing
		{
			get
			{
				if (cachedRingSet == false)
				{
					cachedRing    = GetComponent<SgtRing>();
					cachedRingSet = true;
				}

				return cachedRing;
			}
		}

		public Texture2D GeneratedTexture
		{
			get
			{
				return generatedTexture;
			}
		}

		public void DirtyTexture()
		{
			UpdateTexture();
		}

#if UNITY_EDITOR
		/// <summary>This method allows you to export the generated texture as an asset.
		/// Once done, you can remove this component, and set the <b>SgtRing</b> component's <b>LightingTex</b> setting using the exported asset.</summary>
		[ContextMenu("Export Texture")]
		public void ExportTexture()
		{
			var importer = SgtHelper.ExportTextureDialog(generatedTexture, "Ring Lighting");

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
			CachedRing.LightingTex = generatedTexture;
		}

		[ContextMenu("Remove Texture")]
		public void RemoveTexture()
		{
			if (CachedRing.LightingTex == generatedTexture)
			{
				cachedRing.LightingTex = null;
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
					generatedTexture = SgtHelper.CreateTempTexture2D("Ring LightingTex (Generated)", width, 1, format);

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
			var back     = Mathf.Pow(       u,  backPower) * backStrength;
			var front    = Mathf.Pow(1.0f - u, frontPower);
			var lighting = baseStrength;

			lighting = Mathf.Lerp(lighting, 1.0f, back );
			lighting = Mathf.Lerp(lighting, 1.0f, front);
			lighting = SgtHelper.Saturate(lighting);

			var color = new Color(lighting, lighting, lighting, 0.0f);

			generatedTexture.SetPixel(x, 0, color);
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtRingLightingTex))]
	public class SgtRingLighting_Editor : SgtEditor<SgtRingLightingTex>
	{
		protected override void OnInspector()
		{
			var dirtyTexture = false;

			BeginError(Any(t => t.Width < 1));
				Draw("width", ref dirtyTexture, "The width of the generated texture. A higher value can result in a smoother transition.");
			EndError();
			Draw("format", ref dirtyTexture, "The format of the generated texture.");

			Separator();

			BeginError(Any(t => t.FrontPower < 0.0f));
				Draw("frontPower", ref dirtyTexture, "How sharp the incoming light scatters forward.");
			EndError();
			BeginError(Any(t => t.BackPower < 0.0f));
				Draw("backPower", ref dirtyTexture, "How sharp the incoming light scatters backward.");
			EndError();

			BeginError(Any(t => t.BackStrength < 0.0f));
				Draw("backStrength", ref dirtyTexture, "The strength of the back scattered light.");
			EndError();
			BeginError(Any(t => t.BackStrength < 0.0f));
				Draw("baseStrength", ref dirtyTexture, "The of the perpendicular scattered light.");
			EndError();

			if (dirtyTexture == true) DirtyEach(t => t.DirtyTexture());
		}
	}
}
#endif