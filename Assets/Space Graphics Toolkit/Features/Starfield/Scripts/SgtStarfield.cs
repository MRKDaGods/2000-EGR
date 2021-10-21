using UnityEngine;
using System.Collections.Generic;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This is the base class for all starfields that store star corner vertices the same point/location and are stretched out in the vertex shader, allowing billboarding in view space, and dynamic resizing.</summary>
	public abstract class SgtStarfield : SgtQuads
	{
		/// <summary>Instead of just tinting the stars with the colors, should the RGB values be raised to the power of the color?</summary>
		public bool PowerRgb { set { if (powerRgb != value) { powerRgb = value; DirtyMaterial(); } } get { return powerRgb; } } [FSA("PowerRgb")] [SerializeField] private bool powerRgb;

		/// <summary>Prevent the quads from being too small on screen?</summary>
		public bool ClampSize { set { if (clampSize != value) { clampSize = value; DirtyMaterial(); } } get { return clampSize; } } [FSA("ClampSize")] [SerializeField] private bool clampSize;

		/// <summary>The minimum size each star can be on screen in pixels. If the star goes below this size, it loses opacity proportional to the amount it would have gone under.</summary>
		public float ClampSizeMin { set { if (clampSizeMin != value) { clampSizeMin = value; DirtyMaterial(); } } get { return clampSizeMin; } } [FSA("ClampSizeMin")] [SerializeField] private float clampSizeMin = 10.0f;

		/// <summary>Should the stars stretch if an observer moves?</summary>
		public bool Stretch { set { if (stretch != value) { stretch = value; DirtyMaterial(); } } get { return stretch; } } [FSA("Stretch")] [SerializeField] private bool stretch;

		/// <summary>The vector of the stretching.</summary>
		public Vector3 StretchVector { set { if (stretchVector != value) { stretchVector = value; DirtyMaterial(); } } get { return stretchVector; } } [FSA("StretchVector")] [SerializeField] private Vector3 stretchVector;

		/// <summary>The scale of the stretching relative to the velocity.</summary>
		public float StretchScale { set { if (stretchScale != value) { stretchScale = value; DirtyMaterial(); } } get { return stretchScale; } } [FSA("StretchScale")] [SerializeField] private float stretchScale = 1.0f;

		/// <summary>When warping with the floating origin system the camera velocity can get too large, this allows you to limit it.</summary>
		public float StretchLimit { set { if (stretchLimit != value) { stretchLimit = value; DirtyMaterial(); } } get { return stretchLimit; } } [FSA("StretchLimit")] [SerializeField] private float stretchLimit = 10000.0f;

		/// <summary>Should the stars fade out when the camera gets near?</summary>
		public bool Near { set { if (near != value) { near = value; DirtyMaterial(); } } get { return near; } } [FSA("Near")] [SerializeField] private bool near;

		/// <summary>The lookup table used to calculate the fading amount based on the distance.</summary>
		public Texture NearTex { set { if (nearTex != value) { nearTex = value; DirtyMaterial(); } } get { return nearTex; } } [FSA("NearTex")] [SerializeField] private Texture nearTex;

		/// <summary>The thickness of the fading effect in world space.</summary>
		public float NearThickness { set { if (nearThickness != value) { nearThickness = value; DirtyMaterial(); } } get { return nearThickness; } } [FSA("Lit")] [SerializeField] private float nearThickness = 2.0f;

		/// <summary>Should the stars pulse in size over time?</summary>
		public bool Pulse { set { if (pulse != value) { pulse = value; DirtyMaterial(); } } get { return pulse; } } [FSA("Pulse")] [SerializeField] private bool pulse;

		/// <summary>The amount of seconds this starfield has been animating.</summary>
		public float PulseOffset { set { if (pulseOffset != value) { pulseOffset = value; DirtyMaterial(); } } get { return pulseOffset; } } [FSA("PulseOffset")] [SerializeField] private float pulseOffset;

		/// <summary>The animation speed of this starfield.</summary>
		public float PulseSpeed { set { if (pulseSpeed != value) { pulseSpeed = value; DirtyMaterial(); } } get { return pulseSpeed; } } [FSA("PulseSpeed")] [SerializeField] private float pulseSpeed = 1.0f;

		public void UpdateNearTex()
		{
			if (material != null)
			{
				material.SetTexture(SgtShader._NearTex, nearTex);
			}
		}

		public SgtStarfieldCustom MakeEditableCopy(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
#if UNITY_EDITOR
			SgtHelper.BeginUndo("Create Editable Starfield Copy");
#endif
			var gameObject      = SgtHelper.CreateGameObject("Editable Starfield Copy", layer, parent, localPosition, localRotation, localScale);
			var customStarfield = SgtHelper.AddComponent<SgtStarfieldCustom>(gameObject, false);
			var stars           = customStarfield.Stars;
			var starCount       = BeginQuads();

			for (var i = 0; i < starCount; i++)
			{
				var star = SgtPoolClass<SgtStarfieldStar>.Pop() ?? new SgtStarfieldStar();

				NextQuad(ref star, i);

				stars.Add(star);
			}

			EndQuads();

			// Copy common settings
			customStarfield.Color             = color;
			customStarfield.Brightness        = brightness;
			customStarfield.MainTex           = mainTex;
			customStarfield.Layout            = layout;
			customStarfield.LayoutColumns     = layoutColumns;
			customStarfield.LayoutRows        = layoutRows;
			customStarfield.layoutRects       = new List<Rect>(layoutRects);
			customStarfield.blendMode         = blendMode;
			customStarfield.renderQueue       = renderQueue;
			customStarfield.powerRgb          = powerRgb;
			customStarfield.clampSize         = clampSize;
			customStarfield.clampSizeMin      = clampSizeMin;
			customStarfield.stretch           = stretch;
			customStarfield.stretchVector     = stretchVector;
			customStarfield.stretchScale      = stretchScale;
			customStarfield.stretchLimit      = stretchLimit;
			customStarfield.near              = near;
			customStarfield.nearTex           = nearTex;
			customStarfield.nearThickness     = nearThickness;
			customStarfield.pulse             = pulse;
			customStarfield.pulseOffset       = pulseOffset;
			customStarfield.pulseSpeed        = pulseSpeed;

			return customStarfield;
		}

		public SgtStarfieldCustom MakeEditableCopy(int layer = 0, Transform parent = null)
		{
			return MakeEditableCopy(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}
#if UNITY_EDITOR
		[ContextMenu("Make Editable Copy")]
		public void MakeEditableCopyContext()
		{
			var customStarfield = MakeEditableCopy(gameObject.layer, transform.parent, transform.localPosition, transform.localRotation, transform.localScale);

			SgtHelper.SelectAndPing(customStarfield);
		}
#endif
		protected override void LateUpdate()
		{
			base.LateUpdate();

			if (pulse == true)
			{
				material.SetFloat(SgtShader._PulseOffset, pulseOffset);
			}

			UpdatePulse();
		}

		protected override void HandleCameraDraw(Camera camera)
		{
			if (SgtHelper.CanDraw(gameObject, camera) == false) return;

			var properties = shaderProperties.GetProperties(material, camera);
			var velocity   = stretchVector;
			var sgtCamera  = default(SgtCamera);

			if (SgtCamera.TryFind(camera, ref sgtCamera) == true)
			{
				properties.SetFloat(SgtShader._CameraRollAngle, sgtCamera.RollAngle * Mathf.Deg2Rad);

				var cameraVelocity = sgtCamera.Velocity;
				var cameraSpeed    = cameraVelocity.magnitude;

				if (cameraSpeed > stretchLimit)
				{
					cameraVelocity = cameraVelocity.normalized * stretchLimit;
				}

				velocity += cameraVelocity * stretchScale;
			}
			else
			{
				properties.SetFloat(SgtShader._CameraRollAngle, 0.0f);
			}

			if (stretch == true)
			{
				properties.SetVector(SgtShader._StretchVector, velocity);
				properties.SetVector(SgtShader._StretchDirection, velocity.normalized);
				properties.SetFloat(SgtShader._StretchLength, velocity.magnitude);
			}
			else
			{
				properties.SetVector(SgtShader._StretchVector, Vector3.zero);
				properties.SetVector(SgtShader._StretchDirection, Vector3.zero);
				properties.SetFloat(SgtShader._StretchLength, 0.0f);
			}

			if (camera.orthographic == true)
			{
				properties.SetFloat(SgtShader._ClampSizeScale, camera.orthographicSize * 0.0025f);
			}
			else
			{
				properties.SetFloat(SgtShader._ClampSizeScale, Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 2.0f);
			}

			HandleDrawMesh(camera, properties);
		}

		protected virtual void HandleDrawMesh(Camera camera, MaterialPropertyBlock properties)
		{
			Graphics.DrawMesh(mesh, transform.localToWorldMatrix, material, gameObject.layer, camera, 0, properties);
		}

		protected override void UpdateMaterial()
		{
			if (material == null)
			{
				material = SgtHelper.CreateTempMaterial("Starfield (Generated)", SgtHelper.ShaderNamePrefix + "Starfield");
			}

			base.UpdateMaterial();

			if (blendMode == SgtQuads.BlendModeType.Default)
			{
				BuildAdditive();
			}

			if (powerRgb == true)
			{
				SgtHelper.EnableKeyword("SGT_B", material); // PowerRgb
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_B", material); // PowerRgb
			}

			if (clampSize == true)
			{
				SgtHelper.EnableKeyword("LIGHT_2", material); // Clamp Size

				material.SetFloat(SgtShader._ClampSizeMin, clampSizeMin);
			}
			else
			{
				SgtHelper.DisableKeyword("LIGHT_2", material); // Clamp Size
			}

			if (stretch == true)
			{
				SgtHelper.EnableKeyword("SGT_C", material); // Stretch
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_C", material); // Stretch
			}

			if (near == true)
			{
				SgtHelper.EnableKeyword("SGT_D", material); // Near

				material.SetTexture(SgtShader._NearTex, nearTex);
				material.SetFloat(SgtShader._NearScale, SgtHelper.Reciprocal(nearThickness));
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_D", material); // Near
			}

			if (pulse == true)
			{
				SgtHelper.EnableKeyword("LIGHT_1", material); // Pulse
			}
			else
			{
				SgtHelper.DisableKeyword("LIGHT_1", material); // Pulse
			}
		}

		protected abstract void NextQuad(ref SgtStarfieldStar quad, int starIndex);

		protected override void BuildMesh(Mesh mesh, int count)
		{
			var positions = new Vector3[count * 4];
			var colors    = new Color[count * 4];
			var normals   = new Vector3[count * 4];
			var tangents  = new Vector4[count * 4];
			var coords1   = new Vector2[count * 4];
			var coords2   = new Vector2[count * 4];
			var indices   = new int[count * 6];
			var minMaxSet = false;
			var min       = default(Vector3);
			var max       = default(Vector3);

			for (var i = 0; i < count; i++)
			{
				NextQuad(ref SgtStarfieldStar.Temp, i);

				var offV     = i * 4;
				var offI     = i * 6;
				var position = SgtStarfieldStar.Temp.Position;
				var radius   = SgtStarfieldStar.Temp.Radius;
				var angle    = Mathf.Repeat(SgtStarfieldStar.Temp.Angle / 180.0f, 2.0f) - 1.0f;
				var uv       = tempCoords[SgtHelper.Mod(SgtStarfieldStar.Temp.Variant, tempCoords.Count)];

				ExpandBounds(ref minMaxSet, ref min, ref max, position, radius);

				positions[offV + 0] =
				positions[offV + 1] =
				positions[offV + 2] =
				positions[offV + 3] = position;

				colors[offV + 0] =
				colors[offV + 1] =
				colors[offV + 2] =
				colors[offV + 3] = SgtStarfieldStar.Temp.Color;

				normals[offV + 0] = new Vector3(-1.0f,  1.0f, angle);
				normals[offV + 1] = new Vector3( 1.0f,  1.0f, angle);
				normals[offV + 2] = new Vector3(-1.0f, -1.0f, angle);
				normals[offV + 3] = new Vector3( 1.0f, -1.0f, angle);

				tangents[offV + 0] =
				tangents[offV + 1] =
				tangents[offV + 2] =
				tangents[offV + 3] = new Vector4(SgtStarfieldStar.Temp.PulseOffset, SgtStarfieldStar.Temp.PulseSpeed, SgtStarfieldStar.Temp.PulseRange, 0.0f);

				coords1[offV + 0] = new Vector2(uv.x, uv.y);
				coords1[offV + 1] = new Vector2(uv.z, uv.y);
				coords1[offV + 2] = new Vector2(uv.x, uv.w);
				coords1[offV + 3] = new Vector2(uv.z, uv.w);

				coords2[offV + 0] = new Vector2(radius,  0.5f);
				coords2[offV + 1] = new Vector2(radius, -0.5f);
				coords2[offV + 2] = new Vector2(radius,  0.5f);
				coords2[offV + 3] = new Vector2(radius, -0.5f);

				indices[offI + 0] = offV + 0;
				indices[offI + 1] = offV + 1;
				indices[offI + 2] = offV + 2;
				indices[offI + 3] = offV + 3;
				indices[offI + 4] = offV + 2;
				indices[offI + 5] = offV + 1;
			}

			mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
			mesh.vertices    = positions;
			mesh.colors      = colors;
			mesh.normals     = normals;
			mesh.tangents    = tangents;
			mesh.uv          = coords1;
			mesh.uv2         = coords2;
			mesh.triangles   = indices;
			mesh.bounds      = SgtHelper.NewBoundsFromMinMax(min, max);
		}

		private void UpdatePulse()
		{
#if UNITY_EDITOR
			if (Application.isPlaying == false)
			{
				return;
			}
#endif
			if (pulse == true)
			{
				pulseOffset += Time.deltaTime * pulseSpeed;
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	public class SgtStarfield_Editor<T> : SgtQuads_Editor<T>
		where T : SgtStarfield
	{
		protected void DrawPointMaterial(ref bool updateMaterial)
		{
			Separator();

			Draw("powerRgb", ref updateMaterial, "Instead of just tinting the stars with the colors, should the RGB values be raised to the power of the color?");
			Draw("clampSize", ref updateMaterial, "Prevent the quads from being too small on screen?");
			if (Any(t => t.ClampSize == true))
			{
				BeginIndent();
					Draw("clampSizeMin", ref updateMaterial, "The minimum size each star can be on screen in pixels. If the star goes below this size, it loses opacity proportional to the amount it would have gone under.");
				EndIndent();
			}

			Draw("stretch", ref updateMaterial, "Should the stars stretch if an observer moves?");

			if (Any(t => t.Stretch == true))
			{
				BeginIndent();
					Draw("stretchVector", ref updateMaterial, "The vector of the stretching.");
					BeginError(Any(t => t.StretchScale < 0.0f));
						Draw("stretchScale", ref updateMaterial, "The scale of the stretching relative to the velocity.");
					EndError();
					BeginError(Any(t => t.StretchLimit <= 0.0f));
						Draw("stretchLimit", "When warping with the floating origin system the camera velocity can get too large, this allows you to limit it.");
					EndError();
				EndIndent();
			}

			Draw("pulse", ref updateMaterial, "Should the stars pulse in size over time?");

			if (Any(t => t.Pulse == true))
			{
				BeginIndent();
					Draw("pulseOffset", "The amount of seconds this starfield has been animating.");
					BeginError(Any(t => t.PulseSpeed == 0.0f));
						Draw("pulseSpeed", "The animation speed of this starfield.");
					EndError();
				EndIndent();
			}

			Draw("near", ref updateMaterial, "Should the stars fade out when the camera gets near?");

			if (Any(t => t.Near == true))
			{
				BeginIndent();
					BeginError(Any(t => t.NearTex == null));
						Draw("nearTex", ref updateMaterial, "The lookup table used to calculate the fading amount based on the distance.");
					EndError();
					BeginError(Any(t => t.NearThickness < 0.0f));
						Draw("nearThickness", ref updateMaterial, "The thickness of the fading effect in world space.");
					EndError();
				EndIndent();
			}

			if (Any(t => t.Near == true && t.NearTex == null && t.GetComponent<SgtStarfieldNearTex>() == null))
			{
				Separator();

				if (Button("Add NearTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtStarfieldNearTex>(t.gameObject));
				}
			}
		}
	}
}
#endif