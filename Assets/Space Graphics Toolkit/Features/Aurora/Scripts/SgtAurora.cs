using UnityEngine;
using System.Collections.Generic;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to render an aroura above a planet. The aurora can be set to procedurally animate in the shader.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtAurora")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Aurora")]
	public class SgtAurora : MonoBehaviour
	{
		/// <summary>The base texture tiled along the aurora.</summary>
		public Texture MainTex { set { if (mainTex != value) { mainTex = value; dirtyMaterial = true; } } get { return mainTex; } } [FSA("MainTex")] [SerializeField] private Texture mainTex;

		/// <summary>The base color will be multiplied by this.</summary>
		public Color Color { set { if (color != value) { color = value; dirtyMaterial = true; } } get { return color; } } [FSA("Color")] [SerializeField] private Color color = Color.white;

		/// <summary>The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.</summary>
		public float Brightness { set { if (brightness != value) { brightness = value; dirtyMaterial = true; } } get { return brightness; } } [FSA("Brightness")] [SerializeField] private float brightness = 1.0f;

		/// <summary>This allows you to adjust the render queue of the aurora material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.</summary>
		public SgtRenderQueue RenderQueue { set { if (renderQueue != value) { renderQueue = value; dirtyMaterial = true; } } get { return renderQueue; } } [FSA("RenderQueue")] [SerializeField] private SgtRenderQueue renderQueue = SgtRenderQueue.GroupType.Transparent;

		/// <summary>This allows you to offset the camera distance in world space when rendering the aurora, giving you fine control over the render order.</summary>
		public float CameraOffset { set { cameraOffset = value; } get { return cameraOffset; } } [FSA("CameraOffset")] [SerializeField] private float cameraOffset;

		/// <summary>This allows you to set the random seed used during procedural generation.</summary>
		public int Seed { set { if (seed != value) { seed = value; dirtyMesh = true; } } get { return seed; } } [FSA("Seed")] [SerializeField] [SgtSeed] private int seed;

		/// <summary>The inner radius of the aurora mesh in local space.</summary>
		public float RadiusMin { set { if (radiusMin != value) { radiusMin = value; dirtyMaterial = true; } } get { return radiusMin; } } [FSA("RadiusMin")] [SerializeField] private float radiusMin = 1.0f;

		/// <summary>The inner radius of the aurora mesh in local space.</summary>
		public float RadiusMax { set { if (radiusMax != value) { radiusMax = value; dirtyMaterial = true; } } get { return radiusMax; } } [FSA("RadiusMax")] [SerializeField] private float radiusMax = 1.1f;

		/// <summary>The amount of aurora paths/ribbons.</summary>
		public int PathCount { set { if (pathCount != value) { pathCount = value; dirtyMesh = true; } } get { return pathCount; } } [FSA("PathCount")] [SerializeField] private int pathCount = 8;

		/// <summary>The amount of quads used to build each path.</summary>
		public int PathDetail { set { if (pathDetail != value) { pathDetail = value; dirtyMesh = true; } } get { return pathDetail; } } [FSA("PathDetail")] [SerializeField] private int pathDetail = 100;

		/// <summary>The minimum length of each aurora path.</summary>
		public float PathLengthMin { set { if (pathLengthMin != value) { pathLengthMin = value; dirtyMesh = true; } } get { return pathLengthMin; } } [FSA("PathLengthMin")] [SerializeField] [Range(0.0f, 1.0f)] private float pathLengthMin = 0.1f;

		/// <summary>The maximum length of each aurora path.</summary>
		public float PathLengthMax { set { if (pathLengthMax != value) { pathLengthMax = value; dirtyMesh = true; } } get { return pathLengthMax; } } [FSA("PathLengthMax")] [SerializeField] [Range(0.0f, 1.0f)] private float pathLengthMax = 0.1f;

		/// <summary>The minimum distance between the pole and the aurora path start point.</summary>
		public float StartMin { set { if (startMin != value) { startMin = value; dirtyMesh = true; } } get { return startMin; } } [FSA("StartMin")] [SerializeField] [Range(0.0f, 1.0f)] private float startMin = 0.1f;

		/// <summary>The maximum distance between the pole and the aurora path start point.</summary>
		public float StartMax { set { if (startMax != value) { startMax = value; dirtyMesh = true; } } get { return startMax; } } [FSA("StartMax")] [SerializeField] [Range(0.0f, 1.0f)] private float startMax = 0.5f;

		/// <summary>The probability that the aurora path will begin closer to the pole.</summary>
		public float StartBias { set { if (startBias != value) { startBias = value; dirtyMesh = true; } } get { return startBias; } } [FSA("StartBias")] [SerializeField] private float startBias = 1.0f;

		/// <summary>The probability that the aurora path will start on the northern pole.</summary>
		public float StartTop { set { if (startTop != value) { startTop = value; dirtyMesh = true; } } get { return startTop; } } [FSA("StartTop")] [SerializeField] [Range(0.0f, 1.0f)] private float startTop = 0.5f;

		/// <summary>The amount of waypoints the aurora path will follow based on its length.</summary>
		public int PointDetail { set { if (pointDetail != value) { pointDetail = value; dirtyMesh = true; } } get { return pointDetail; } } [FSA("PointDetail")] [SerializeField] [Range(1, 100)] private int pointDetail = 10;

		/// <summary>The strength of the aurora waypoint twisting.</summary>
		public float PointSpiral { set { if (pointSpiral != value) { pointSpiral = value; dirtyMesh = true; } } get { return pointSpiral; } } [FSA("PointSpiral")] [SerializeField] private float pointSpiral = 1.0f;

		/// <summary>The strength of the aurora waypoint random displacement.</summary>
		public float PointJitter { set { if (pointJitter != value) { pointJitter = value; dirtyMesh = true; } } get { return pointJitter; } } [FSA("PointJitter")] [SerializeField] [Range(0.0f, 1.0f)] private float pointJitter = 1.0f;

		/// <summary>The sharpness of the fading at the start and ends of the aurora paths.</summary>
		public float TrailEdgeFade { set { if (trailEdgeFade != value) { trailEdgeFade = value; dirtyMesh = true; } } get { return trailEdgeFade; } } [FSA("TrailEdgeFade")] [SerializeField] private float trailEdgeFade = 1.0f;

		/// <summary>The amount of times the main texture is tiled based on its length.</summary>
		public float TrailTile { set { if (trailTile != value) { trailTile = value; dirtyMesh = true; } } get { return trailTile; } } [FSA("TrailTile")] [SerializeField] private float trailTile = 30.0f;

		/// <summary>The flatness of the aurora path.</summary>
		public float TrailHeights { set { if (trailHeights != value) { trailHeights = value; dirtyMesh = true; } } get { return trailHeights; } } [FSA("TrailHeights")] [SerializeField] [Range(0.1f, 1.0f)] private float trailHeights = 1.0f;

		/// <summary>The amount of height changes in the aurora path.</summary>
		public int TrailHeightsDetail { set { if (trailHeightsDetail != value) { trailHeightsDetail = value; dirtyMesh = true; } } get { return trailHeightsDetail; } } [FSA("TrailHeightsDetail")] [SerializeField] private int trailHeightsDetail = 10;

		/// <summary>The possible colors given to the top half of the aurora path.</summary>
		public Gradient Colors { get { if (colors == null) colors = new Gradient(); return colors; } } [FSA("Colors")] [SerializeField] private Gradient colors;

		/// <summary>The amount of color changes an aurora path can have based on its length.</summary>
		public int ColorsDetail { set { if (colorsDetail != value) { colorsDetail = value; dirtyMesh = true; } } get { return colorsDetail; } } [FSA("ColorsDetail")] [SerializeField] private int colorsDetail = 10;

		/// <summary>The minimum opacity multiplier of the aurora path colors.</summary>
		public float ColorsAlpha { set { if (colorsAlpha != value) { colorsAlpha = value; dirtyMesh = true; } } get { return colorsAlpha; } } [FSA("ColorsAlpha")] [SerializeField] [Range(0.0f, 1.0f)] private float colorsAlpha = 0.5f;

		/// <summary>The amount of alpha changes in the aurora path.</summary>
		public float ColorsAlphaBias { set { if (colorsAlphaBias != value) { colorsAlphaBias = value; dirtyMesh = true; } } get { return colorsAlphaBias; } } [FSA("ColorsAlphaBias")] [SerializeField] private float colorsAlphaBias = 2.0f;

		/// <summary>Should the aurora fade out when the camera gets near?</summary>
		public bool Near { set { if (near != value) { near = value; dirtyMaterial = true; } } get { return near; } } [FSA("Near")] [SerializeField] private bool near;

		/// <summary>The lookup table used to calculate the fading amount based on the distance, where the left side is used when the camera is near, and the right side is used when the camera is far.</summary>
		public Texture NearTex { set { if (nearTex != value) { nearTex = value; dirtyMaterial = true; } } get { return nearTex; } } [FSA("NearTex")] [SerializeField] private Texture nearTex;

		/// <summary>The distance the fading begins from in world space.</summary>
		public float NearDistance { set { if (nearDistance != value) { nearDistance = value; dirtyMaterial = true; } } get { return nearDistance; } } [FSA("NearDistance")] [SerializeField] private float nearDistance = 2.0f;

		/// <summary>Should the aurora paths animate?</summary>
		public bool Anim { set { if (anim != value) { anim = value; dirtyMaterial = true; } } get { return anim; } } [FSA("Anim")] [SerializeField] private bool anim;

		/// <summary>The current age/offset of the animation.</summary>
		public float AnimOffset { set { animOffset = value; } get { return animOffset; } } [FSA("AnimOffset")] [SerializeField] private float animOffset;

		/// <summary>The speed of the animation.</summary>
		public float AnimSpeed { set { animSpeed = value; } get { return animSpeed; } } [FSA("AnimSpeed")] [SerializeField] private float animSpeed = 1.0f;

		/// <summary>The strength of the aurora path position changes in local space.</summary>
		public float AnimStrength { set { if (animStrength != value) { animStrength = value; dirtyMesh = true; } } get { return animStrength; } } [FSA("AnimStrength")] [SerializeField] private float animStrength = 0.01f;

		/// <summary>The amount of the animation strength changes along the aurora path based on its length.</summary>
		public int AnimStrengthDetail { set { if (animStrengthDetail != value) { animStrengthDetail = value; dirtyMesh = true; } } get { return animStrengthDetail; } } [FSA("AnimStrengthDetail")] [SerializeField] private int animStrengthDetail = 10;

		/// <summary>The maximum angle step between sections of the aurora path.</summary>
		public float AnimAngle { set { if (animAngle != value) { animAngle = value; dirtyMesh = true; } } get { return animAngle; } } [FSA("AnimAngle")] [SerializeField] private float animAngle = 0.01f;

		/// <summary>The amount of the animation angle changes along the aurora path based on its length.</summary>
		public int AnimAngleDetail { set { if (animAngleDetail != value) { animAngleDetail = value; dirtyMesh = true; } } get { return animAngleDetail; } } [FSA("AnimAngleDetail")] [SerializeField] private int animAngleDetail = 10;

		// The material applied to all segments
		[System.NonSerialized]
		private Material material;

		// The meshes applied to the models
		[System.NonSerialized]
		private Mesh mesh;

		[System.NonSerialized]
		private Transform cachedTransform;

		[System.NonSerialized]
		private bool dirtyMesh = true;

		[System.NonSerialized]
		private bool dirtyMaterial = true;

		private static List<Vector3> tempPositions = new List<Vector3>();

		private static List<Vector4> tempCoords0 = new List<Vector4>();

		private static List<Color> tempColors = new List<Color>();

		private static List<Vector3> tempNormals = new List<Vector3>();

		private static List<int> tempIndices = new List<int>();

		private void UpdateMaterial()
		{
			if (material == null)
			{
				material = SgtHelper.CreateTempMaterial("Aurora (Generated)", SgtHelper.ShaderNamePrefix + "Aurora");
			}

			var color = SgtHelper.Premultiply(SgtHelper.Brighten(this.color, brightness));

			material.renderQueue = renderQueue;

			material.SetColor(SgtShader._Color, color);
			material.SetTexture(SgtShader._MainTex, mainTex);
			material.SetFloat(SgtShader._RadiusMin, radiusMin);
			material.SetFloat(SgtShader._RadiusSize, radiusMax - radiusMin);

			SgtHelper.SetTempMaterial(material);

			if (near == true)
			{
				SgtHelper.EnableKeyword("SGT_A"); // Near

				material.SetTexture(SgtShader._NearTex, nearTex);
				material.SetFloat(SgtShader._NearScale, SgtHelper.Reciprocal(nearDistance));
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_A"); // Near
			}

			if (anim == true)
			{
				SgtHelper.EnableKeyword("SGT_B"); // Anim

				material.SetFloat(SgtShader._AnimOffset, animOffset);
			}
			else
			{
				SgtHelper.DisableKeyword("SGT_B"); // Anim
			}
		}

		private void BakeMesh(Mesh mesh)
		{
			mesh.Clear(false);
			mesh.SetVertices(tempPositions);
			mesh.SetUVs(0, tempCoords0);
			mesh.SetColors(tempColors);
			mesh.SetNormals(tempNormals);
			mesh.SetTriangles(tempIndices, 0);

			mesh.bounds = new Bounds(Vector3.zero, Vector3.one * radiusMax * 2.0f);
		}

		private Vector3 GetStart(float angle)
		{
			var distance = Mathf.Lerp(startMin, startMax, Mathf.Pow(Random.value, startBias));

			if (Random.value < startTop)
			{
				return new Vector3(Mathf.Sin(angle) * distance, 1.0f, Mathf.Cos(angle) * distance);
			}
			else
			{
				return new Vector3(Mathf.Sin(angle) * distance, -1.0f, Mathf.Cos(angle) * distance);
			}
		}

		private Vector3 GetNext(Vector3 point, float angle, float speed)
		{
			var noise = Random.insideUnitCircle;

			point.x += Mathf.Sin(angle) * speed;
			point.z += Mathf.Cos(angle) * speed;

			point.x += noise.x * pointJitter;
			point.z += noise.y * pointJitter;

			return Quaternion.Euler(0.0f, pointSpiral, 0.0f) * point;
		}

		private float GetNextAngle(float angle)
		{
			return angle + Random.Range(0.0f, animAngle);
		}

		private float GetNextStrength()
		{
			return Random.Range(-animStrength, animStrength);
		}

		private Color GetNextColor()
		{
			var color = Color.white;

			if (Colors != null)
			{
				color = Colors.Evaluate(Random.value);
			}

			color.a *= Mathf.LerpUnclamped(colorsAlpha, 1.0f, Mathf.Pow(Random.value, colorsAlphaBias));

			return color;
		}

		private float GetNextHeight()
		{
			return Random.Range(0.0f, trailHeights);
		}

		private void Shift<T>(ref T a, ref T b, ref T c, T d, ref float f)
		{
			a  = b;
			b  = c;
			c  = d;
			f -= 1.0f;
		}

		private void AddPath(Mesh mesh, ref int vertexCount)
		{
			var pathLength = Random.Range(pathLengthMin, pathLengthMax);
			var lineCount  = 2 + (int)(pathLength * pathDetail);
			var quadCount  = lineCount - 1;
			var vertices   = quadCount * 2 + 2;

			var angle      = Random.Range(-Mathf.PI, Mathf.PI);
			var speed      = 1.0f / pointDetail;
			var detailStep = 1.0f / pathDetail;
			var pointStep  = detailStep * pointDetail;
			var pointFrac  = 0.0f;
			var pointA     = GetStart(angle + Mathf.PI);
			var pointB     = GetNext(pointA, angle, speed);
			var pointC     = GetNext(pointB, angle, speed);
			var pointD     = GetNext(pointC, angle, speed);
			var coordFrac  = 0.0f;
			var edgeFrac   = -1.0f;
			var edgeStep   = 2.0f / lineCount;
			var coordStep  = detailStep * trailTile;

			var angleA = angle;
			var angleB = GetNextAngle(angleA);
			var angleC = GetNextAngle(angleB);
			var angleD = GetNextAngle(angleC);
			var angleFrac = 0.0f;
			var angleStep = detailStep * animAngleDetail;

			var strengthA    = 0.0f;
			var strengthB    = GetNextStrength();
			var strengthC    = GetNextStrength();
			var strengthD    = GetNextStrength();
			var strengthFrac = 0.0f;
			var strengthStep = detailStep * animStrengthDetail;

			var colorA    = GetNextColor();
			var colorB    = GetNextColor();
			var colorC    = GetNextColor();
			var colorD    = GetNextColor();
			var colorFrac = 0.0f;
			var colorStep = detailStep * colorsDetail;

			var heightA    = GetNextHeight();
			var heightB    = GetNextHeight();
			var heightC    = GetNextHeight();
			var heightD    = GetNextHeight();
			var heightFrac = 0.0f;
			var heightStep = detailStep * trailHeightsDetail;

			for (var i = 0; i < lineCount; i++)
			{
				while (pointFrac >= 1.0f)
				{
					Shift(ref pointA, ref pointB, ref pointC, pointD, ref pointFrac); pointD = GetNext(pointC, angle, speed);
				}

				while (angleFrac >= 1.0f)
				{
					Shift(ref angleA, ref angleB, ref angleC, angleD, ref angleFrac); angleD = GetNextAngle(angleC);
				}

				while (strengthFrac >= 1.0f)
				{
					Shift(ref strengthA, ref strengthB, ref strengthC, strengthD, ref strengthFrac); strengthD = GetNextStrength();
				}

				while (colorFrac >= 1.0f)
				{
					Shift(ref colorA, ref colorB, ref colorC, colorD, ref colorFrac); colorD = GetNextColor();
				}

				while (heightFrac >= 1.0f)
				{
					Shift(ref heightA, ref heightB, ref heightC, heightD, ref heightFrac); heightD = GetNextHeight();
				}

				var point   = SgtHelper.HermiteInterpolate3(pointA, pointB, pointC, pointD, pointFrac);
				var animAng = SgtHelper.HermiteInterpolate(angleA, angleB, angleC, angleD, angleFrac);
				var animStr = SgtHelper.HermiteInterpolate(strengthA, strengthB, strengthC, strengthD, strengthFrac);
				var color   = SgtHelper.HermiteInterpolate(colorA, colorB, colorC, colorD, colorFrac);
				var height  = SgtHelper.HermiteInterpolate(heightA, heightB, heightC, heightD, heightFrac);

				// Fade edges
				color.a *= Mathf.SmoothStep(1.0f, 0.0f, Mathf.Pow(Mathf.Abs(edgeFrac), trailEdgeFade));

				tempCoords0.Add(new Vector4(coordFrac, 0.0f, animAng, animStr));
				tempCoords0.Add(new Vector4(coordFrac, height, animAng, animStr));

				tempPositions.Add(point);
				tempPositions.Add(point);

				tempColors.Add(color);
				tempColors.Add(color);

				pointFrac    += pointStep;
				edgeFrac     += edgeStep;
				coordFrac    += coordStep;
				angleFrac    += angleStep;
				strengthFrac += strengthStep;
				colorFrac    += colorStep;
				heightFrac   += heightStep;
			}

			var vector = tempPositions[1] - tempPositions[0];

			tempNormals.Add(GetNormal(vector, vector));
			tempNormals.Add(GetNormal(vector, vector));

			for (var i = 2; i < lineCount; i++)
			{
				var nextVector = tempPositions[i] - tempPositions[i - 1];

				tempNormals.Add(GetNormal(vector, nextVector));
				tempNormals.Add(GetNormal(vector, nextVector));

				vector = nextVector;
			}

			tempNormals.Add(GetNormal(vector, vector));
			tempNormals.Add(GetNormal(vector, vector));

			for (var i = 0; i < quadCount; i++)
			{
				var offset = vertexCount + i * 2;

				tempIndices.Add(offset + 0);
				tempIndices.Add(offset + 1);
				tempIndices.Add(offset + 2);

				tempIndices.Add(offset + 3);
				tempIndices.Add(offset + 2);
				tempIndices.Add(offset + 1);
			}

			vertexCount += vertices;
		}

		private Vector3 GetNormal(Vector3 a, Vector3 b)
		{
			return Vector3.Cross(a.normalized, b.normalized);
		}

		private void UpdateMesh()
		{
			if (mesh == null)
			{
				mesh = SgtHelper.CreateTempMesh("Aurora Mesh (Generated)");
			}

			SgtHelper.ClearCapacity(tempPositions, 1024);
			SgtHelper.ClearCapacity(tempCoords0, 1024);
			SgtHelper.ClearCapacity(tempIndices, 1024);
			SgtHelper.ClearCapacity(tempColors, 1024);
			SgtHelper.ClearCapacity(tempNormals, 1024);

			if (pathDetail > 0 && pathLengthMin > 0.0f && pathLengthMax > 0.0f)
			{
				var vertexCount = 0;

				SgtHelper.BeginRandomSeed(seed);
				{
					for (var i = 0; i < pathCount; i++)
					{
						AddPath(mesh, ref vertexCount);
					}
				}
				SgtHelper.EndRandomSeed();
			}

			BakeMesh(mesh);
		}

		public static SgtAurora Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtAurora Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject = SgtHelper.CreateGameObject("Aurora", layer, parent, localPosition, localRotation, localScale);
			var aurora     = gameObject.AddComponent<SgtAurora>();

			return aurora;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Aurora", false, 10)]
		public static void CreateMenuItem()
		{
			var parent = SgtHelper.GetSelectedParent();
			var aurora = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(aurora);
		}
#endif

#if UNITY_EDITOR
		protected virtual void Reset()
		{
			if (colors == null)
			{
				colors = new Gradient();

				colors.colorKeys = new GradientColorKey[] { new GradientColorKey(Color.blue, 0.0f), new GradientColorKey(Color.magenta, 1.0f) };
			}
		}
#endif

		protected virtual void OnEnable()
		{
			SgtCamera.OnCameraDraw += HandleCameraDraw;

			cachedTransform = GetComponent<Transform>();
		}

		protected virtual void OnDisable()
		{
			SgtCamera.OnCameraDraw -= HandleCameraDraw;
		}

		protected virtual void OnDestroy()
		{
			SgtHelper.Destroy(mesh);
			SgtHelper.Destroy(material);
		}

		protected virtual void LateUpdate()
		{
			if (dirtyMesh == true)
			{
				dirtyMesh = false; UpdateMesh();
			}

			if (dirtyMaterial == true)
			{
				dirtyMaterial = false; UpdateMaterial();
			}

			if (anim == true)
			{
				if (Application.isPlaying == true)
				{
					animOffset += Time.deltaTime * animSpeed;
				}

				if (material != null)
				{
					material.SetFloat(SgtShader._AnimOffset, animOffset);
				}
			}
		}

#if UNITY_EDITOR
		protected virtual void Start()
		{
			// Upgrade scene
			// NOTE: This must be done in Start because when done in OnEnable this fails to dirty the scene
			SgtHelper.DestroyOldGameObjects(transform, "Aurora Model");
		}
#endif

		protected virtual void OnDidApplyAnimationProperties()
		{
			dirtyMesh     = true;
			dirtyMaterial = true;
		}

#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			dirtyMesh     = true;
			dirtyMaterial = true;
		}
#endif

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			Gizmos.matrix = transform.localToWorldMatrix;

			Gizmos.DrawWireSphere(Vector3.zero, radiusMin);

			Gizmos.DrawWireSphere(Vector3.zero, radiusMax);
		}
#endif

		private void HandleCameraDraw(Camera camera)
		{
			if (SgtHelper.CanDraw(gameObject, camera) == false) return;

			var matrix = cachedTransform.localToWorldMatrix;

			if (cameraOffset != 0.0f)
			{
				var direction = Vector3.Normalize(camera.transform.position - cachedTransform.position);

				matrix = Matrix4x4.Translate(direction * cameraOffset) * matrix;
			}

			Graphics.DrawMesh(mesh, matrix, material, gameObject.layer, camera);
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtAurora))]
	public class SgtAurora_Editor : SgtEditor<SgtAurora>
	{
		protected override void OnInspector()
		{
			Draw("color", "The base color will be multiplied by this.");
			Draw("brightness", "The Color.rgb values are multiplied by this, allowing you to quickly adjust the overall brightness.");
			Draw("renderQueue", "This allows you to adjust the render queue of the aurora material. You can normally adjust the render queue in the material settings, but since this material is procedurally generated your changes will be lost.");
			Draw("cameraOffset", "This allows you to offset the camera distance in world space when rendering the aurora, giving you fine control over the render order."); // Updated automatically

			Separator();

			BeginError(Any(t => t.MainTex == null));
				Draw("mainTex", "The base texture tiled along the aurora.");
			EndError();
			Draw("seed", "This allows you to set the random seed used during procedural generation.");
			BeginError(Any(t => t.RadiusMin >= t.RadiusMax));
				Draw("radiusMin", "The inner radius of the aurora mesh in local space.");
				Draw("radiusMax", "The outer radius of the aurora mesh in local space.");
			EndError();

			Separator();

			BeginError(Any(t => t.PathCount < 1));
				Draw("pathCount", "The amount of aurora paths/ribbons.");
			EndError();
			BeginError(Any(t => t.PathDetail < 1));
				Draw("pathDetail", "The amount of quads used to build each path.");
			EndError();
			BeginError(Any(t => t.PathLengthMin > t.PathLengthMax));
				Draw("pathLengthMin", "The minimum length of each aurora path.");
				Draw("pathLengthMax", "The maximum length of each aurora path.");
			EndError();

			Separator();

			BeginError(Any(t => t.StartMin > t.StartMax));
				Draw("startMin", "The minimum distance between the pole and the aurora path start point.");
				Draw("startMax", "The maximum distance between the pole and the aurora path start point.");
			EndError();
			BeginError(Any(t => t.StartBias < 1.0f));
				Draw("startBias", "The probability that the aurora path will begin closer to the pole.");
			EndError();
			Draw("startTop", "The probability that the aurora path will start on the northern pole.");

			Separator();

			Draw("pointDetail", "The amount of waypoints the aurora path will follow based on its length.");
			Draw("pointSpiral", "The strength of the aurora waypoint twisting.");
			Draw("pointJitter", "The strength of the aurora waypoint random displacement.");

			Separator();

			Draw("trailTile", "The amount of times the main texture is tiled based on its length.");
			BeginError(Any(t => t.TrailEdgeFade < 1.0f));
				Draw("trailEdgeFade", "The sharpness of the fading at the start and ends of the aurora paths.");
			EndError();
			Draw("trailHeights", "The flatness of the aurora path.");
			BeginError(Any(t => t.TrailHeightsDetail < 1));
				Draw("trailHeightsDetail", "The amount of height changes in the aurora path.");
			EndError();

			Separator();

			Draw("colors", "The possible colors given to the top half of the aurora path.");
			BeginError(Any(t => t.ColorsDetail < 1));
				Draw("colorsDetail", "The amount of color changes an aurora path can have based on its length.");
			EndError();
			Draw("colorsAlpha", "The minimum opacity multiplier of the aurora path colors.");
			Draw("colorsAlphaBias", "The amount of alpha changes in the aurora path.");

			Separator();

			Draw("near", "Should the aurora fade out when the camera gets near?");

			if (Any(t => t.Near == true))
			{
				BeginIndent();
					BeginError(Any(t => t.NearTex == null));
						Draw("nearTex", "The lookup table used to calculate the fading amount based on the distance, where the left side is used when the camera is near, and the right side is used when the camera is far.");
					EndError();
					BeginError(Any(t => t.NearDistance < 0.0f));
						Draw("nearDistance", "The distance the fading begins from in world space.");
					EndError();
				EndIndent();
			}

			Separator();

			Draw("anim", "Should the aurora paths animate?");

			if (Any(t => t.Anim == true))
			{
				BeginIndent();
					Draw("animOffset", "The current age/offset of the animation."); // Updated automatically
					BeginError(Any(t => t.AnimSpeed == 0.0f));
						Draw("animSpeed", "The speed of the animation."); // Updated automatically
					EndError();
					Draw("animStrength", "The strength of the aurora path position changes in local space.");
					BeginError(Any(t => t.AnimStrengthDetail < 1));
						Draw("animStrengthDetail", "The amount of the animation strength changes along the aurora path based on its length.");
					EndError();
					Draw("animAngle", "The maximum angle step between sections of the aurora path.");
					BeginError(Any(t => t.AnimAngleDetail < 1));
						Draw("animAngleDetail", "The amount of the animation angle changes along the aurora path based on its length.");
					EndError();
				EndIndent();
			}

			if (Any(t => t.MainTex == null && t.GetComponent<SgtAuroraMainTex>() == null))
			{
				Separator();

				if (Button("Add MainTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtAuroraMainTex>(t.gameObject));
				}
			}

			if (Any(t => t.Near == true && t.NearTex == null && t.GetComponent<SgtAuroraNearTex>() == null))
			{
				Separator();

				if (Button("Add NearTex") == true)
				{
					Each(t => SgtHelper.GetOrAddComponent<SgtAuroraNearTex>(t.gameObject));
				}
			}
		}
	}
}
#endif