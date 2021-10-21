using System.Collections.Generic;
using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to render a grid that can be deformed by SgtSpacetimeWell components.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtSpacetime")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Spacetime")]
	public class SgtSpacetime : MonoBehaviour
	{
		public enum DisplacementType
		{
			Pinch,
			Offset
		}

		/// <summary>The center of the spacetime grid in local space.</summary>
		public Vector2 Center { set { center = value; } get { return center; } } [SerializeField] private Vector2 center;

		/// <summary>The size of the spacetime grid in local space.</summary>
		public Vector2 Size { set { size = value; } get { return size; } } [SerializeField] private Vector2 size = new Vector2(100.0f, 100.0f);

		/// <summary>The mesh used to render the spacetime.</summary>
		public Mesh Mesh { set { mesh = value; } get { return mesh; } } [SerializeField] private Mesh mesh;

		/// <summary>The base color will be multiplied by this.</summary>
		public Color Color { set { if (color != value) { color = value; DirtyMaterial(); } } get { return color; } } [FSA("Color")] [SerializeField] private Color color = Color.white;

		/// <summary>The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.</summary>
		public float Brightness { set { if (brightness != value) { brightness = value; DirtyMaterial(); } } get { return brightness; } } [FSA("Brightness")] [SerializeField] private float brightness = 1.0f;

		/// <summary>This allows you to adjust the render queue of the spacetime material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.</summary>
		public SgtRenderQueue RenderQueue { set { if (renderQueue != value) { renderQueue = value; DirtyMaterial(); } } get { return renderQueue; } } [FSA("RenderQueue")] [SerializeField] private SgtRenderQueue renderQueue = SgtRenderQueue.GroupType.Transparent;

		/// <summary>The main texture applied to the spacetime.</summary>
		public Texture2D MainTex { set { if (mainTex != value) { mainTex = value; DirtyMaterial(); } } get { return mainTex; } } [FSA("MainTex")] [SerializeField] private Texture2D mainTex;

		/// <summary>How many times should the spacetime texture be tiled?</summary>
		public int Tile { set { if (tile != value) { tile = value; DirtyMaterial(); } } get { return tile; } } [FSA("Tile")] [SerializeField] private int tile = 50;

		/// <summary>The ambient color.</summary>
		public Color AmbientColor { set { if (ambientColor != value) { ambientColor = value; DirtyMaterial(); } } get { return ambientColor; } } [FSA("AmbientColor")] [SerializeField] private Color ambientColor = Color.white;

		/// <summary>The ambient brightness.</summary>
		public float AmbientBrightness { set { if (ambientBrightness != value) { ambientBrightness = value; DirtyMaterial(); } } get { return ambientBrightness; } } [FSA("AmbientBrightness")] [SerializeField] private float ambientBrightness = 0.25f;

		/// <summary>The displacement color.</summary>
		public Color DisplacementColor { set { if (displacementColor != value) { displacementColor = value; DirtyMaterial(); } } get { return displacementColor; } } [FSA("DisplacementColor")] [SerializeField] private Color displacementColor = Color.white;

		/// <summary>The displacement brightness.</summary>
		public float DisplacementBrightness { set { if (displacementBrightness != value) { displacementBrightness = value; DirtyMaterial(); } } get { return displacementBrightness; } } [FSA("DisplacementBrightness")] [SerializeField] private float displacementBrightness = 1.0f;

		/// <summary>The color of the highlight.</summary>
		public Color HighlightColor { set { if (highlightColor != value) { highlightColor = value; DirtyMaterial(); } } get { return highlightColor; } } [FSA("HighlightColor")] [SerializeField] private Color highlightColor = Color.white;

		/// <summary>The brightness of the highlight.</summary>
		public float HighlightBrightness { set { if (highlightBrightness != value) { highlightBrightness = value; DirtyMaterial(); } } get { return highlightBrightness; } } [FSA("HighlightBrightness")] [SerializeField] private float highlightBrightness = 0.1f;

		/// <summary>The scale of the highlight.</summary>
		public float HighlightScale { set { if (highlightScale != value) { highlightScale = value; DirtyMaterial(); } } get { return highlightScale; } } [FSA("HighlightScale")] [SerializeField] private float highlightScale = 3.0f;

		/// <summary>The sharpness of the highlight.</summary>
		public float HighlightPower { set { if (highlightPower != value) { highlightPower = value; DirtyMaterial(); } } get { return highlightPower; } } [FSA("HighlightPower")] [SerializeField] private float highlightPower = 1.0f;

		/// <summary>How should the vertices in the spacetime get displaced when a well is nearby?</summary>
		public DisplacementType Displacement { set { if (displacement != value) { displacement = value; DirtyMaterial(); } } get { return displacement; } } [FSA("Displacement")] [SerializeField] private DisplacementType displacement = DisplacementType.Pinch;

		/// <summary>Should the displacement effect additively stack if wells overlap?</summary>
		public bool Accumulate { set { if (accumulate != value) { accumulate = value; DirtyMaterial(); } } get { return accumulate; } } [FSA("Accumulate")] [SerializeField] private bool accumulate;

		/// <summary>The pinch power.</summary>
		public float Power { set { if (power != value) { power = value; DirtyMaterial(); } } get { return power; } } [FSA("Power")] [SerializeField] private float power = 3.0f;

		/// <summary>The offset direction/vector for vertices within range of a well.</summary>
		public Vector3 Offset { set { if (offset != value) { offset = value; DirtyMaterial(); } } get { return offset; } } [FSA("Offset")] [SerializeField] private Vector3 offset = new Vector3(0.0f, -1.0f, 0.0f);

		/// <summary>Filter all the wells to require the same layer at this GameObject.</summary>
		public bool RequireSameLayer { set { if (requireSameLayer != value) { requireSameLayer = value; DirtyMaterial(); } } get { return requireSameLayer; } } [FSA("RequireSameLayer")] [SerializeField] private bool requireSameLayer;

		/// <summary>Filter all the wells to require the same tag at this GameObject.</summary>
		public bool RequireSameTag { set { if (requireSameTag != value) { requireSameTag = value; DirtyMaterial(); } } get { return requireSameTag; } } [FSA("RequireSameTag")] [SerializeField] private bool requireSameTag;

		/// <summary>Filter all the wells to require a name that contains this.</summary>
		public string RequireNameContains { set { if (requireNameContains != value) { requireNameContains = value; DirtyMaterial(); } } get { return requireNameContains; } } [FSA("RequireNameContains")] [SerializeField] private string requireNameContains;

		// The material added to all spacetime renderers
		[System.NonSerialized]
		private Material material;

		// The well data arrays that get copied to the shader
		[System.NonSerialized] private Vector4  [] gauPos = new Vector4[12];
		[System.NonSerialized] private Vector4  [] gauDat = new Vector4[12];
		[System.NonSerialized] private Vector4  [] ripPos = new Vector4[1];
		[System.NonSerialized] private Vector4  [] ripDat = new Vector4[1];
		[System.NonSerialized] private Vector4  [] twiPos = new Vector4[1];
		[System.NonSerialized] private Vector4  [] twiDat = new Vector4[1];
		[System.NonSerialized] private Matrix4x4[] twiMat = new Matrix4x4[1];

		public void DirtyMaterial()
		{
			UpdateMaterial();
		}

		private void UpdateMaterial()
		{
			if (material == null)
			{
				material = SgtHelper.CreateTempMaterial("Spacetime (Generated)", SgtHelper.ShaderNamePrefix + "Spacetime");
			}

			var ambientColor      = SgtHelper.Brighten(this.ambientColor, ambientBrightness);
			var displacementColor = SgtHelper.Brighten(this.displacementColor, displacementBrightness);
			var higlightColor     = SgtHelper.Brighten(highlightColor, highlightBrightness);

			material.renderQueue = renderQueue;

			material.SetTexture(SgtShader._MainTex, mainTex);
			material.SetColor(SgtShader._Color, SgtHelper.Brighten(color, brightness));
			material.SetColor(SgtShader._AmbientColor, ambientColor);
			material.SetColor(SgtShader._DisplacementColor, displacementColor);
			material.SetColor(SgtShader._HighlightColor, higlightColor);
			material.SetFloat(SgtShader._HighlightPower, highlightPower);
			material.SetFloat(SgtShader._HighlightScale, highlightScale);
			material.SetFloat(SgtShader._Tile, tile);

			if (displacement == DisplacementType.Pinch)
			{
				material.SetFloat(SgtShader._Power, power);
			}

			if (displacement == DisplacementType.Offset)
			{
				SgtHelper.EnableKeyword("SGT_A", material);

				material.SetVector(SgtShader._Offset, offset);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_A", material);
			}

			if (accumulate == true)
			{
				SgtHelper.EnableKeyword("SGT_B", material);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_B", material);
			}
		}

		[ContextMenu("Update Wells")]
		public void UpdateWells()
		{
			if (material != null)
			{
				var gaussianCount = 0;
				var rippleCount   = 0;
				var twistCount    = 0;

				WriteWells(ref gaussianCount, ref rippleCount, ref twistCount); // 12 is the shader instruction limit
			}
		}

		public static SgtSpacetime Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtSpacetime Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject = SgtHelper.CreateGameObject("Spacetime", layer, parent, localPosition, localRotation, localScale);
			var spacetime  = gameObject.AddComponent<SgtSpacetime>();

			return spacetime;
		}

		protected virtual void OnEnable()
		{
			SgtCamera.OnCameraDraw += HandleCameraDraw;

			UpdateMaterial();
			UpdateWells();
		}

		protected virtual void OnDisable()
		{
			SgtCamera.OnCameraDraw -= HandleCameraDraw;
		}

		private void HandleCameraDraw(Camera camera)
		{
			if (SgtHelper.CanDraw(gameObject, camera) == false) return;

			UpdateWells();

			if (mesh != null)
			{
				var modifier = GetComponent<SgtSpacetimeModifier>();

				if (modifier != null)
				{
					foreach (var matrix in modifier.GetMatrices())
					{
						Graphics.DrawMesh(mesh, matrix, material, 0, camera);
					}
				}
				else
				{
					var matrix = transform.localToWorldMatrix;
				
					matrix *= Matrix4x4.Translate(new Vector3(center.x, 0.0f, center.y));
					matrix *= Matrix4x4.Scale(new Vector3(size.x, 1.0f, size.y));

					Graphics.DrawMesh(mesh, matrix, material, 0, camera);
				}
			}
		}

		private void WriteWells(ref int gaussianCount, ref int rippleCount, ref int twistCount)
		{
			var well = SgtSpacetimeWell.FirstInstance;

			for (var i = 0; i < SgtSpacetimeWell.InstanceCount; i++)
			{
				if (SgtHelper.Enabled(well) == true && well.Radius > 0.0f)
				{
					if (well.Distribution == SgtSpacetimeWell.DistributionType.Gaussian && gaussianCount >= gauPos.Length)
					{
						continue;
					}

					if (well.Distribution == SgtSpacetimeWell.DistributionType.Ripple && rippleCount >= ripPos.Length)
					{
						continue;
					}

					if (well.Distribution == SgtSpacetimeWell.DistributionType.Twist && twistCount >= twiPos.Length)
					{
						continue;
					}

					// filter?
					if (requireSameLayer == true && gameObject.layer != well.gameObject.layer)
					{
						continue;
					}

					if (requireSameTag == true && tag != well.tag)
					{
						continue;
					}

					if (string.IsNullOrEmpty(requireNameContains) == false && well.name.Contains(requireNameContains) == false)
					{
						continue;
					}

					var wellPos = well.transform.position;

					switch (well.Distribution)
					{
						case SgtSpacetimeWell.DistributionType.Gaussian:
						{
							var index = gaussianCount++;

							gauPos[index] = new Vector4(wellPos.x, wellPos.y, wellPos.z, well.Radius);
							gauDat[index] = new Vector4(well.Strength, 0.0f, 0.0f, 0.0f);
						}
						break;

						case SgtSpacetimeWell.DistributionType.Ripple:
						{
							var index = rippleCount++;

							ripPos[index] = new Vector4(wellPos.x, wellPos.y, wellPos.z, well.Radius);
							ripDat[index] = new Vector4(well.Strength, well.Frequency, well.Offset, 0.0f);
						}
						break;

						case SgtSpacetimeWell.DistributionType.Twist:
						{
							var index = twistCount++;

							twiPos[index] = new Vector4(wellPos.x, wellPos.y, wellPos.z, well.Radius);
							twiDat[index] = new Vector4(well.Strength, well.Frequency, well.HoleSize, well.HolePower);
							twiMat[index] = well.transform.worldToLocalMatrix;
						}
						break;
					}
				}

				well = well.NextInstance;
			}

			material.SetInt(SgtShader._Gau, gaussianCount);
			material.SetVectorArray(SgtShader._GauPos, gauPos);
			material.SetVectorArray(SgtShader._GauDat, gauDat);
			
			material.SetInt(SgtShader._Rip, rippleCount);
			material.SetVectorArray(SgtShader._RipPos, ripPos);
			material.SetVectorArray(SgtShader._RipDat, ripDat);
			
			material.SetInt(SgtShader._Twi, twistCount);
			material.SetVectorArray(SgtShader._TwiPos, twiPos);
			material.SetVectorArray(SgtShader._TwiDat, twiDat);
			material.SetMatrixArray(SgtShader._TwiMat, twiMat);
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtSpacetime))]
	public class SgtSpacetime_Editor : SgtEditor<SgtSpacetime>
	{
		protected override void OnInspector()
		{
			var dirtyMaterial = false;

			Draw("color", ref dirtyMaterial, "The base color will be multiplied by this.");
			BeginError(Any(t => t.Brightness < 0.0f));
				Draw("brightness", ref dirtyMaterial, "The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.");
			EndError();
			Draw("renderQueue", ref dirtyMaterial, "This allows you to adjust the render queue of the spacetime material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.");

			Separator();

			Draw("center", "The center of the spacetime grid in local space.");
			BeginError(Any(t => t.Size == Vector2.zero));
				Draw("size", "The size of the spacetime grid in local space.");
			EndError();
			BeginError(Any(t => t.Mesh == null));
				Draw("mesh", "The mesh used to render the spacetime.");
			EndError();
			BeginError(Any(t => t.MainTex == null));
				Draw("mainTex", ref dirtyMaterial, "The main texture applied to the spacetime.");
			EndError();
			BeginError(Any(t => t.Tile <= 0));
				Draw("tile", ref dirtyMaterial, "How many times should the spacetime texture be tiled?");
			EndError();

			Separator();

			Draw("ambientColor", ref dirtyMaterial, "The ambient color.");
			BeginError(Any(t => t.AmbientBrightness < 0.0f));
				Draw("ambientBrightness", ref dirtyMaterial, "The ambient brightness.");
			EndError();

			Separator();

			Draw("displacementColor", ref dirtyMaterial, "The displacement color.");
			BeginError(Any(t => t.DisplacementBrightness < 0.0f));
				Draw("displacementBrightness", ref dirtyMaterial, "The displacement brightness.");
			EndError();

			Separator();

			Draw("highlightColor", ref dirtyMaterial, "The color of the highlight.");
			Draw("highlightBrightness", ref dirtyMaterial, "The brightness of the highlight.");
			Draw("highlightScale", ref dirtyMaterial, "The scale of the highlight.");
			BeginError(Any(t => t.HighlightPower < 0.0f));
				Draw("highlightPower", ref dirtyMaterial, "The sharpness of the highlight.");
			EndError();

			Separator();

			Draw("displacement", ref dirtyMaterial, "How should the vertices in the spacetime get displaced when a well is nearby?");
			BeginIndent();
				Draw("accumulate", ref dirtyMaterial, "Should the displacement effect additively stack if wells overlap?");

				if (Any(t => t.Displacement == SgtSpacetime.DisplacementType.Pinch))
				{
					BeginError(Any(t => t.Power < 0.0f));
						Draw("power", ref dirtyMaterial, "The pinch power.");
					EndError();
				}

				if (Any(t => t.Displacement == SgtSpacetime.DisplacementType.Offset))
				{
					Draw("offset", ref dirtyMaterial, "The offset direction/vector for vertices within range of a well.");
				}
			EndIndent();

			Separator();

			Draw("requireSameLayer", "Filter all the wells to require the same layer at this GameObject.");
			Draw("requireSameTag", "Filter all the wells to require the same tag at this GameObject.");
			Draw("requireNameContains", "Filter all the wells to require a name that contains this.");

			if (Any(t => t.Mesh == null && t.GetComponent<SgtSpacetimeMesh>() == null))
			{
				Separator();

				if (Button("Add Mesh") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtSpacetimeMesh>(t.gameObject));
				}
			}

			if (dirtyMaterial == true)
			{
				DirtyEach(t => t.DirtyMaterial());
			}
		}

		[MenuItem(SgtHelper.GameObjectMenuPrefix + "Spacetime", false, 10)]
		public static void CreateItem()
		{
			var parent    = SgtHelper.GetSelectedParent();
			var spacetime = SgtSpacetime.Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(spacetime);
		}
	}
}
#endif