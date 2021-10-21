using UnityEngine;
using System.Collections.Generic;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to generate the SgtAurora.MainTex field.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtAurora))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtAuroraMainTex")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Aurora MainTex")]
	public class SgtAuroraMainTex : MonoBehaviour
	{
		/// <summary>The width of the generated texture. A higher value can result in a smoother transition. This stores the noise samples.</summary>
		public int Width { set { if (width != value) { width = value; DirtyTexture(); } } get { return width; } } [FSA("Width")] [SerializeField] private int width = 256;

		/// <summary>The height of the generated texture. A higher value can result in a smoother transition. This stores the vertical color samples.</summary>
		public int Height { set { if (height != value) { height = value; DirtyTexture(); } } get { return height; } } [FSA("Height")] [SerializeField] private int height = 64;

		/// <summary>The format of the generated texture.</summary>
		public TextureFormat Format { set { if (format != value) { format = value; DirtyTexture(); } } get { return format; } } [FSA("Format")] [SerializeField] private TextureFormat format = TextureFormat.ARGB32;

		/// <summary>The strength of the noise points.</summary>
		public float NoiseStrength { set { if (noiseStrength != value) { noiseStrength = value; DirtyTexture(); } } get { return noiseStrength; } } [FSA("NoiseStrength")] [SerializeField] [Range(0.0f, 1.0f)] private float noiseStrength = 0.75f;

		/// <summary>The amount of noise points.</summary>
		public int NoisePoints { set { if (noisePoints != value) { noisePoints = value; DirtyTexture(); } } get { return noisePoints; } } [FSA("NoisePoints")] [SerializeField] private int noisePoints = 30;

		/// <summary>The random seed used when generating this texture.</summary>
		public int NoiseSeed { set { if (noiseSeed != value) { noiseSeed = value; DirtyTexture(); } } get { return noiseSeed; } } [FSA("NoiseSeed")] [SerializeField] [SgtSeed] private int noiseSeed;

		/// <summary>The transition style between the top and middle.</summary>
		public SgtEase.Type TopEase { set { if (topEase != value) { topEase = value; DirtyTexture(); } } get { return topEase; } } [FSA("TopEase")] [SerializeField] private SgtEase.Type topEase = SgtEase.Type.Quintic;

		/// <summary>The transition strength between the top and middle.</summary>
		public float TopSharpness { set { if (topSharpness != value) { topSharpness = value; DirtyTexture(); } } get { return topSharpness; } } [FSA("TopSharpness")] [SerializeField] private float topSharpness = 1.0f;

		/// <summary>The point separating the top from bottom.</summary>
		public float MiddlePoint { set { if (middlePoint != value) { middlePoint = value; DirtyTexture(); } } get { return middlePoint; } } [FSA("MiddlePoint")] [SerializeField] [Range(0.0f, 1.0f)] private float middlePoint = 0.25f;

		/// <summary>The base color of the aurora starting from the bottom.</summary>
		public Color MiddleColor { set { if (middleColor != value) { middleColor = value; DirtyTexture(); } } get { return middleColor; } } [FSA("MiddleColor")] [SerializeField] private Color middleColor = Color.green;

		/// <summary>The transition style between the bottom and top of the aurora.</summary>
		public SgtEase.Type MiddleEase { set { if (middleEase != value) { middleEase = value; DirtyTexture(); } } get { return middleEase; } } [FSA("MiddleEase")] [SerializeField] private SgtEase.Type middleEase = SgtEase.Type.Exponential;

		/// <summary>The strength of the color transition between the bottom and top.</summary>
		public float MiddleSharpness { set { if (middleSharpness != value) { middleSharpness = value; DirtyTexture(); } } get { return middleSharpness; } } [FSA("MiddleSharpness")] [SerializeField] private float middleSharpness = 3.0f;

		/// <summary>The transition style between the bottom and middle.</summary>
		public SgtEase.Type BottomEase { set { if (bottomEase != value) { bottomEase = value; DirtyTexture(); } } get { return bottomEase; } } [FSA("BottomEase")] [SerializeField] private SgtEase.Type bottomEase = SgtEase.Type.Exponential;

		/// <summary>The transition strength between the bottom and middle.</summary>
		public float BottomSharpness { set { if (bottomSharpness != value) { bottomSharpness = value; DirtyTexture(); } } get { return bottomSharpness; } } [FSA("BottomSharpness")] [SerializeField] private float bottomSharpness = 1.0f;

		[System.NonSerialized]
		private Texture2D generatedTexture;

		[System.NonSerialized]
		private SgtAurora cachedAurora;

		[System.NonSerialized]
		private bool cachedAuroraSet;

		[System.NonSerialized]
		private static List<float> tempPoints = new List<float>();

		public SgtAurora CachedAurora
		{
			get
			{
				if (cachedAuroraSet == false)
				{
					cachedAurora    = GetComponent<SgtAurora>();
					cachedAuroraSet = true;
				}

				return cachedAurora;
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
		/// Once done, you can remove this component, and set the <b>SgtAurora</b> component's <b>MainTex</b> setting using the exported asset.</summary>
		[ContextMenu("Export Texture")]
		public void ExportTexture()
		{
			var importer = SgtHelper.ExportTextureDialog(generatedTexture, "Aurora MainTex");

			if (importer != null)
			{
				importer.textureCompression  = UnityEditor.TextureImporterCompression.Uncompressed;
				importer.alphaSource         = UnityEditor.TextureImporterAlphaSource.FromInput;
				importer.wrapMode            = TextureWrapMode.Repeat;
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
			CachedAurora.MainTex = generatedTexture;
		}

		[ContextMenu("Remove Texture")]
		public void RemoveTexture()
		{
			if (CachedAurora.MainTex == generatedTexture)
			{
				cachedAurora.MainTex = null;
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
			if (width > 0 && height > 0 && noisePoints > 0)
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
					generatedTexture = SgtHelper.CreateTempTexture2D("Aurora MainTex (Generated)", width, height, format);

					generatedTexture.wrapMode = TextureWrapMode.Repeat;
				}

				SgtHelper.BeginRandomSeed(noiseSeed);
				{
					tempPoints.Clear();

					for (var i = 0; i < noisePoints; i++)
					{
						tempPoints.Add(1.0f - Random.Range(0.0f, noiseStrength));
					}
				}
				SgtHelper.EndRandomSeed();

				var stepU = 1.0f / (width  - 1);
				var stepV = 1.0f / (height - 1);

				for (var y = 0; y < height; y++)
				{
					var v = stepV * y;

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
			var noise      = u * noisePoints;
			var noiseIndex = (int)noise;
			var noiseFrac  = noise % 1.0f;
			var noiseA     = tempPoints[(noiseIndex + 0) % noisePoints];
			var noiseB     = tempPoints[(noiseIndex + 1) % noisePoints];
			var noiseC     = tempPoints[(noiseIndex + 2) % noisePoints];
			var noiseD     = tempPoints[(noiseIndex + 3) % noisePoints];
			var color      = middleColor;

			if (v < middlePoint)
			{
				color.a = SgtEase.Evaluate(bottomEase, SgtHelper.Sharpness(Mathf.InverseLerp(0.0f, middlePoint, v), bottomSharpness));
			}
			else
			{
				color.a = SgtEase.Evaluate(topEase, SgtHelper.Sharpness(Mathf.InverseLerp(1.0f, middlePoint, v), topSharpness));
			}

			var middle = SgtEase.Evaluate(middleEase, SgtHelper.Sharpness(1.0f - v, middleSharpness));

			color.a *= SgtHelper.HermiteInterpolate(noiseA, noiseB, noiseC, noiseD, noiseFrac);

			color.r *= middle * color.a;
			color.g *= middle * color.a;
			color.b *= middle * color.a;
			color.a *= 1.0f - middle;
		
			generatedTexture.SetPixel(x, y, SgtHelper.Saturate(color));
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtAuroraMainTex))]
	public class SgtAuroraMainTex_Editor : SgtEditor<SgtAuroraMainTex>
	{
		protected override void OnInspector()
		{
			var dirtyTexture = false;

			BeginError(Any(t => t.Width < 1));
				Draw("width", ref dirtyTexture, "The width of the generated texture. A higher value can result in a smoother transition. This stores the noise samples.");
			EndError();
			BeginError(Any(t => t.Height < 1));
				Draw("height", ref dirtyTexture, "The height of the generated texture. A higher value can result in a smoother transition. This stores the vertical color samples.");
			EndError();
			Draw("format", ref dirtyTexture, "The format of the generated texture.");

			Separator();

			Draw("noiseStrength", ref dirtyTexture, "The strength of the noise points.");
			BeginError(Any(t => t.NoisePoints <= 0));
				Draw("noisePoints", ref dirtyTexture, "The amount of noise points.");
			EndError();
			Draw("noiseSeed", ref dirtyTexture, "The random seed used when generating this texture.");

			Separator();

			Draw("topEase", ref dirtyTexture, "The transition style between the top and middle.");
			Draw("topSharpness", ref dirtyTexture, "The transition strength between the top and middle.");

			Separator();

			Draw("middlePoint", ref dirtyTexture, "The point separating the top from bottom.");
			Draw("middleColor", ref dirtyTexture, "The base color of the aurora starting from the bottom.");
			Draw("middleEase", ref dirtyTexture, "The transition style between the bottom and top of the aurora.");
			Draw("middleSharpness", ref dirtyTexture, "The strength of the color transition between the bottom and top.");

			Separator();

			Draw("bottomEase", ref dirtyTexture, "The transition style between the bottom and middle.");
			Draw("bottomSharpness", ref dirtyTexture, "The transition strength between the bottom and middle.");

			if (dirtyTexture == true) DirtyEach(t => t.DirtyTexture());
		}
	}
}
#endif