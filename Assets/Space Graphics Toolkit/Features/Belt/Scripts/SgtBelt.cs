using UnityEngine;
using System.Collections.Generic;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This base class contains the functionality to render an asteroid belt.</summary>
	public abstract class SgtBelt : SgtQuads
	{
		/// <summary>The amount of seconds this belt has been animating for.</summary>
		public float OrbitOffset { set { orbitOffset = value; } get { return orbitOffset; } } [FSA("OrbitOffset")] [SerializeField] private float orbitOffset;

		/// <summary>The animation speed of this belt.</summary>
		public float OrbitSpeed { set { orbitSpeed = value; } get { return orbitSpeed; } } [FSA("OrbitSpeed")] [SerializeField] private float orbitSpeed = 1.0f;

		/// <summary>If you enable this then nearby SgtLight and SgtShadow casters will be found and applied to the lighting calculations.</summary>
		public bool Lit { set { if (lit != value) { lit = value; DirtyMaterial(); } } get { return lit; } } [FSA("Lit")] [SerializeField] private bool lit;

		/// <summary>The look up table associating light angle with surface color. The left side is used on the dark side, the middle is used on the horizon, and the right side is used on the light side.</summary>
		public Texture LightingTex { set { if (lightingTex != value) { lightingTex = value; DirtyMaterial(); } } get { return lightingTex; } } [FSA("LightingTex")] [SerializeField] private Texture lightingTex;

		/// <summary>The belt will always be lit by this amount.</summary>
		public Color AmbientColor { set { if (ambientColor != value) { ambientColor = value; DirtyMaterial(); } } get { return ambientColor; } } [FSA("AmbientColor")] [SerializeField] private Color ambientColor;

		/// <summary>Instead of just tinting the asteroids with the colors, should the RGB values be raised to the power of the color?</summary>
		public bool PowerRgb { set { if (powerRgb != value) { powerRgb = value; DirtyMaterial(); } } get { return powerRgb; } } [FSA("PowerRgb")] [SerializeField] private bool powerRgb;

		public SgtBeltCustom MakeEditableCopy(int layer = 0, Transform parent = null)
		{
			return MakeEditableCopy(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public SgtBeltCustom MakeEditableCopy(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
#if UNITY_EDITOR
			SgtHelper.BeginUndo("Create Editable Belt Copy");
#endif
			var gameObject = SgtHelper.CreateGameObject("Editable Belt Copy", layer, parent, localPosition, localRotation, localScale);
			var customBelt = SgtHelper.AddComponent<SgtBeltCustom>(gameObject, false);
			var asteroids  = customBelt.Asteroids;
			var quadCount  = BeginQuads();

			for (var i = 0; i < quadCount; i++)
			{
				var asteroid = SgtPoolClass<SgtBeltAsteroid>.Pop() ?? new SgtBeltAsteroid();

				NextQuad(ref asteroid, i);

				asteroids.Add(asteroid);
			}

			EndQuads();

			// Copy common settings
			customBelt.Color         = Color;
			customBelt.Brightness    = Brightness;
			customBelt.mainTex       = mainTex;
			customBelt.layout        = layout;
			customBelt.layoutColumns = layoutColumns;
			customBelt.layoutRows    = layoutRows;
			customBelt.layoutRects   = new List<Rect>(layoutRects);
			customBelt.blendMode     = blendMode;
			customBelt.renderQueue   = renderQueue;
			customBelt.orbitOffset   = orbitOffset;
			customBelt.orbitSpeed    = orbitSpeed;
			customBelt.lit           = lit;
			customBelt.lightingTex   = lightingTex;
			customBelt.ambientColor  = ambientColor;
			customBelt.powerRgb      = powerRgb;

			return customBelt;
		}

#if UNITY_EDITOR
		[ContextMenu("Make Editable Copy")]
		public void MakeEditableCopyContext()
		{
			var customBelt = MakeEditableCopy(gameObject.layer, transform.parent, transform.localPosition, transform.localRotation, transform.localScale);

			SgtHelper.SelectAndPing(customBelt);
		}
#endif

		protected override void LateUpdate()
		{
			base.LateUpdate();

			if (Application.isPlaying == true)
			{
				orbitOffset += Time.deltaTime * orbitSpeed;
			}

			material.SetFloat(SgtShader._Age, orbitOffset);

			// Write lights and shadows
			SgtHelper.SetTempMaterial(material);

			var mask   = 1 << gameObject.layer;
			var lights = SgtLight.Find(lit, mask, transform.position);

			SgtShadow.Find(lit, mask, lights);
			SgtShadow.FilterOutRing(transform.position);
			SgtShadow.Write(lit, 2);

			SgtLight.Write(lit, transform.position, transform, null, 1.0f, 2);
		}

		protected override void HandleCameraDraw(Camera camera)
		{
			if (SgtHelper.CanDraw(gameObject, camera) == false) return;

			var properties = shaderProperties.GetProperties(material, camera);
			var sgtCamera  = default(SgtCamera);

			if (SgtCamera.TryFind(camera, ref sgtCamera) == true)
			{
				properties.SetFloat(SgtShader._CameraRollAngle, sgtCamera.RollAngle * Mathf.Deg2Rad);
			}
			else
			{
				properties.SetFloat(SgtShader._CameraRollAngle, 0.0f);
			}

			Graphics.DrawMesh(mesh, transform.localToWorldMatrix, material, gameObject.layer, camera, 0, properties);
		}

		protected override void UpdateMaterial()
		{
			if (material == null)
			{
				material = SgtHelper.CreateTempMaterial("Starfield (Generated)", SgtHelper.ShaderNamePrefix + "Belt");
			}

			base.UpdateMaterial();

			if (blendMode == BlendModeType.Default)
			{
				BuildAlphaTest();
			}

			material.SetFloat(SgtShader._Age, orbitOffset);

			if (lit == true)
			{
				material.SetTexture(SgtShader._LightingTex, lightingTex);
				material.SetColor(SgtShader._AmbientColor, ambientColor);
			}

			if (powerRgb == true)
			{
				SgtHelper.EnableKeyword("SGT_B", material); // PowerRgb
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_B", material); // PowerRgb
			}

			material.SetTexture(SgtShader._LightingTex, lightingTex);
		}

		protected abstract void NextQuad(ref SgtBeltAsteroid quad, int starIndex);

		protected override void BuildMesh(Mesh mesh, int count)
		{
			var positions = new Vector3[count * 4];
			var colors    = new Color[count * 4];
			var normals   = new Vector3[count * 4];
			var tangents  = new Vector4[count * 4];
			var coords1   = new Vector2[count * 4];
			var coords2   = new Vector2[count * 4];
			var indices   = new int[count * 6];
			var maxWidth  = 0.0f;
			var maxHeight = 0.0f;

			for (var i = 0; i < count; i++)
			{
				NextQuad(ref SgtBeltAsteroid.Temp, i);

				var offV     = i * 4;
				var offI     = i * 6;
				var radius   = SgtBeltAsteroid.Temp.Radius;
				var distance = SgtBeltAsteroid.Temp.OrbitDistance;
				var height   = SgtBeltAsteroid.Temp.Height;
				var uv       = tempCoords[SgtHelper.Mod(SgtBeltAsteroid.Temp.Variant, tempCoords.Count)];

				maxWidth  = Mathf.Max(maxWidth , distance + radius);
				maxHeight = Mathf.Max(maxHeight, height   + radius);

				positions[offV + 0] =
				positions[offV + 1] =
				positions[offV + 2] =
				positions[offV + 3] = new Vector3(SgtBeltAsteroid.Temp.OrbitAngle, distance, SgtBeltAsteroid.Temp.OrbitSpeed);

				colors[offV + 0] =
				colors[offV + 1] =
				colors[offV + 2] =
				colors[offV + 3] = SgtBeltAsteroid.Temp.Color;

				normals[offV + 0] = new Vector3(-1.0f,  1.0f, 0.0f);
				normals[offV + 1] = new Vector3( 1.0f,  1.0f, 0.0f);
				normals[offV + 2] = new Vector3(-1.0f, -1.0f, 0.0f);
				normals[offV + 3] = new Vector3( 1.0f, -1.0f, 0.0f);

				tangents[offV + 0] =
				tangents[offV + 1] =
				tangents[offV + 2] =
				tangents[offV + 3] = new Vector4(SgtBeltAsteroid.Temp.Angle / Mathf.PI, SgtBeltAsteroid.Temp.Spin / Mathf.PI, 0.0f, 0.0f);

				coords1[offV + 0] = new Vector2(uv.x, uv.y);
				coords1[offV + 1] = new Vector2(uv.z, uv.y);
				coords1[offV + 2] = new Vector2(uv.x, uv.w);
				coords1[offV + 3] = new Vector2(uv.z, uv.w);

				coords2[offV + 0] =
				coords2[offV + 1] =
				coords2[offV + 2] =
				coords2[offV + 3] = new Vector2(radius, height);

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
			mesh.bounds      = new Bounds(Vector3.zero, new Vector3(maxWidth * 2.0f, maxHeight * 2.0f, maxWidth * 2.0f));
		}

		private void ObserverPreRender(SgtCamera observer)
		{
			if (material != null)
			{
				material.SetFloat(SgtShader._CameraRollAngle, observer.RollAngle * Mathf.Deg2Rad);
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	public abstract class SgtBelt_Editor<T> : SgtQuads_Editor<T>
		where T : SgtBelt
	{
		protected override void DrawMaterial(ref bool updateMaterial)
		{
			Draw("color", ref updateMaterial, "The base color will be multiplied by this.");
			BeginError(Any(t => t.Brightness < 0.0f));
				Draw("brightness", ref updateMaterial, "The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.");
			EndError();
			Draw("blendMode", ref updateMaterial, "The blend mode used to render the material.");
			Draw("renderQueue", ref updateMaterial, "This allows you to adjust the render queue of the belt material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.");
			Draw("orbitOffset", "The amount of seconds this belt has been animating for."); // Updated automatically
			Draw("orbitSpeed", "The animation speed of this belt."); // Updated automatically
		}

		protected void DrawLighting(ref bool updateMaterial)
		{
			Draw("powerRgb", ref updateMaterial, "Instead of just tinting the asteroids with the colors, should the RGB values be raised to the power of the color?");
			Draw("lit", ref updateMaterial, "If you enable this then nearby SgtLight and SgtShadow casters will be found and applied to the lighting calculations.");

			if (Any(t => t.Lit == true))
			{
				if (SgtLight.InstanceCount == 0)
				{
					EditorGUILayout.HelpBox("You need to add the SgtLight component to your scene lights for them to work with SGT.", MessageType.Warning);
				}

				BeginIndent();
					BeginError(Any(t => t.LightingTex == null));
						Draw("lightingTex", ref updateMaterial, "The look up table associating light angle with surface color. The left side is used on the dark side, the middle is used on the horizon, and the right side is used on the light side.");
					EndError();
					Draw("ambientColor", ref updateMaterial, "The belt will always be lit by this amount.");
				EndIndent();
			}

			if (Any(t => t.Lit == true && t.LightingTex == null && t.GetComponent<SgtBeltLightingTex>() == null))
			{
				Separator();

				if (Button("Add LightingTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtBeltLightingTex>(t.gameObject));
				}
			}
		}

		protected override void DrawMainTex(ref bool updateMaterial, ref bool updateMeshesAndModels)
		{
			BeginError(Any(t => t.MainTex == null));
				Draw("mainTex", ref updateMaterial, "The main texture of this material.");
			EndError();
			DrawLayout(ref updateMaterial, ref updateMeshesAndModels);
		}
	}
}
#endif