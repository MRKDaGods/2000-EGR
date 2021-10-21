using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;
using Random = UnityEngine.Random;

namespace SpaceGraphicsToolkit
{
	/// <summary>This class contains some useful features used by most other SGT code.</summary>
	public static partial class SgtHelper
	{
		public delegate void DistanceDelegate(Vector3 worldPosition, ref float distance);

		public delegate void OcclusionDelegate(int layers, Vector4 worldEye, Vector4 worldTgt, ref float occlusion);

		public const string ShaderNamePrefix = "Hidden/Sgt";

		public const string HelpUrlPrefix = "https://bitbucket.org/Darkcoder/space-graphics-toolkit/wiki/";

		public const string ComponentMenuPrefix = "Space Graphics Toolkit/SGT ";

		public const string GameObjectMenuPrefix = "GameObject/Space Graphics Toolkit/";

		public static readonly int QuadsPerMesh = 65000 / 4;

		public static List<Material> tempMaterials = new List<Material>();

		public static event DistanceDelegate OnCalculateDistance;

		/// <summary>This event allows you to register a custom occlusion calculation.</summary>
		public static event OcclusionDelegate OnCalculateOcclusion;

		public static event System.Action<Vector3> OnSnap;

		private static Stack<Random.State> seedStates = new Stack<Random.State>();

		private static Camera[] tempCameraArray = new Camera[128];

		private static List<Camera> tempCameraList = new List<Camera>();

		/// <summary>This will return a non-alloc temporary list of all cameras in the scene.</summary>
		public static List<Camera> GetCameras()
		{
			var count = Camera.GetAllCameras(tempCameraArray);

			tempCameraList.Clear();

			for (var i = 0; i < count; i++)
			{
				tempCameraList.Add(tempCameraArray[i]);
			}

			return tempCameraList;
		}

		public static void InvokeCalculateDistance(Vector3 worldPosition, ref float distance)
		{
			if (OnCalculateDistance != null)
			{
				OnCalculateDistance.Invoke(worldPosition, ref distance);
			}
		}

		public static void InvokeCalculateOcclusion(int layers, Vector4 worldEye, Vector4 worldTgt, ref float occlusion)
		{
			if (OnCalculateOcclusion != null)
			{
				OnCalculateOcclusion.Invoke(layers, worldEye, worldTgt, ref occlusion);
			}
		}

		public static void InvokeSnap(Vector3 delta)
		{
			if (OnSnap != null)
			{
				OnSnap.Invoke(delta);
			}
		}

		public static T GetIndex<T>(ref List<T> list, int index)
		{
			if (list == null)
			{
				list = new List<T>();
			}

			for (var i = list.Count; i <= index; i++)
			{
				list.Add(default(T));
			}

			return list[index];
		}

		public static void ClearCapacity<T>(List<T> list, int minCapacity)
		{
			if (list != null)
			{
				list.Clear();

				if (list.Capacity < minCapacity)
				{
					list.Capacity = minCapacity;
				}
			}
		}

		public static bool Enabled(Behaviour b)
		{
			return b != null && b.enabled == true && b.gameObject.activeInHierarchy == true;
		}

		public static bool CanDraw(GameObject gameObject, Camera camera)
		{
#if UNITY_EDITOR
	#if UNITY_2019_2_OR_NEWER
			if (SceneVisibilityManager.instance.IsHidden(gameObject) == true)
			{
				foreach (SceneView sceneView in SceneView.sceneViews)
				{
					if (sceneView.camera == camera)
					{
						return false;
					}
				}
			}
	#endif

			if (camera.scene.name != null && gameObject.scene != camera.scene)
			{
				return false;
			}
#endif
			return true;
		}

		public static T GetOrAddComponent<T>(GameObject gameObject, bool recordUndo = true)
			where T : Component
		{
			if (gameObject != null)
			{
				var component = gameObject.GetComponent<T>();

				if (component == null) component = AddComponent<T>(gameObject, recordUndo);

				return component;
			}

			return null;
		}

		public static T AddComponent<T>(GameObject gameObject, bool recordUndo = true)
			where T : Component
		{
			if (gameObject != null)
			{
	#if UNITY_EDITOR
				if (Application.isPlaying == true)
				{
					return gameObject.AddComponent<T>();
				}
				else
				{
					if (recordUndo == true)
					{
						return Undo.AddComponent<T>(gameObject);
					}
					else
					{
						return gameObject.AddComponent<T>();
					}
				}
	#else
				return gameObject.AddComponent<T>();
	#endif
			}

			return null;
		}

		public static T Destroy<T>(T o)
			where T : Object
		{
			if (o != null)
			{
	#if UNITY_EDITOR
				if (Application.isPlaying == true)
				{
					Object.Destroy(o);
				}
				else
				{
					Object.DestroyImmediate(o);
				}
	#else
				Object.Destroy(o);
	#endif
			}

			return null;
		}

		public static bool Zero(float v)
		{
			return Mathf.Approximately(v, 0.0f);
		}

		public static float Reciprocal(float v)
		{
			return Zero(v) == false ? 1.0f / v : 0.0f;
		}

		public static float Acos(float v)
		{
			if (v >= -1.0f && v <= 1.0f)
			{
				return Mathf.Acos(v);
			}

			return 0.0f;
		}

		public static Vector3 Reciprocal3(Vector3 xyz)
		{
			xyz.x = Reciprocal(xyz.x);
			xyz.y = Reciprocal(xyz.y);
			xyz.z = Reciprocal(xyz.z);
			return xyz;
		}

		public static float Divide(float a, float b)
		{
			return b != 0.0f ? a / b : 0.0f;
		}

		public static double Divide(double a, double b)
		{
			return b != 0.0 ? a / b : 0.0;
		}

		public static Vector4 NewVector4(Vector3 xyz, float w)
		{
			return new Vector4(xyz.x, xyz.y, xyz.z, w);
		}

		public static float Saturate(float c)
		{
			if (c >= 0.0f && c <= 1.0f)
			{
				return c;
			}

			return c < 0.5f ? 0.0f : 1.0f;
		}

		public static Color Saturate(Color c)
		{
			c.r = Saturate(c.r);
			c.g = Saturate(c.g);
			c.b = Saturate(c.b);
			c.a = Saturate(c.a);

			return c;
		}

		public static Color GetPixel(Cubemap cube, Vector3 p)
		{
			var x = Mathf.Abs(p.x);
			var y = Mathf.Abs(p.y);
			var z = Mathf.Abs(p.z);

			if (x > y)
			{
				if (x > z)
				{
					p *= 1.0f / x;

					if (p.x > 0.0f)
					{
						return GetPixel(cube, CubemapFace.PositiveX, -p.z, -p.y);
					}
					else
					{
						return GetPixel(cube, CubemapFace.NegativeX, p.z, -p.y);
					}
				}
				else
				{
					p *= 1.0f / z;

					if (p.z > 0.0f)
					{
						return GetPixel(cube, CubemapFace.PositiveZ, p.x, -p.y);
					}
					else
					{
						return GetPixel(cube, CubemapFace.NegativeZ, -p.x, -p.y);
					}
				}
			}
			else
			{
				if (y > z)
				{
					p *= 1.0f / y;

					if (p.y > 0.0f)
					{
						return GetPixel(cube, CubemapFace.PositiveY, p.x, p.z);
					}
					else
					{
						return GetPixel(cube, CubemapFace.NegativeY, p.x, -p.z);
					}
				}
				else
				{
					p *= 1.0f / z;

					if (p.z > 0.0f)
					{
						return GetPixel(cube, CubemapFace.PositiveZ, p.x, -p.y);
					}
					else
					{
						return GetPixel(cube, CubemapFace.NegativeZ, -p.x, -p.y);
					}
				}
			}
		}

		public static Color GetPixel(Cubemap cube, CubemapFace face, float h, float v)
		{
			var w = cube.width;
			var s = w + 1;
			var x = (int)((h * 0.5f + 0.5f) * s);
			var y = (int)((v * 0.5f + 0.5f) * s);

			x = Mathf.Clamp(x, 0, w);
			y = Mathf.Clamp(y, 0, w);

			return cube.GetPixel(face, x, y);
		}

		public static T Pop<T>(HashSet<T> collection)
		{
			var first = default(T);

			foreach (var element in collection)
			{
				first = element;

				break;
			}

			collection.Remove(first);

			return first;
		}

		public static float Sharpness(float a, float p)
		{
			if (p >= 0.0f)
			{
				return Mathf.Pow(a, p);
			}
			else
			{
				return 1.0f - Mathf.Pow(1.0f - a, - p);
			}
		}

		public static float CubicInterpolate(float a, float b, float c, float d, float t)
		{
			var tt = t * t;
		
			d = (d - c) - (a - b);
		
			return d * (tt * t) + ((a - b) - d) * tt + (c - a) * t + b;
		}
	
		public static float HermiteInterpolate(float a, float b, float c, float d, float t)
		{
			var tt  = t * t;
			var tt3 = tt * 3.0f;
			var ttt = t * tt;
			var ttt2 = ttt * 2.0f;
			float a0, a1, a2, a3;
		
			var m0 = (c - a) * 0.5f;
			var m1 = (d - b) * 0.5f;
		
			a0  =  ttt2 - tt3 + 1.0f;
			a1  =  ttt  - tt * 2.0f + t;
			a2  =  ttt  - tt;
			a3  = -ttt2 + tt3;
		
			return a0*b + a1*m0 + a2*m1 + a3*c;
		}

		public static Color HermiteInterpolate(Color a, Color b, Color c, Color d, float t)
		{
			var tt  = t * t;
			var tt3 = tt * 3.0f;
			var ttt = t * tt;
			var ttt2 = ttt * 2.0f;
			float a0, a1, a2, a3;
		
			var m0 = (c - a) * 0.5f;
			var m1 = (d - b) * 0.5f;
		
			a0  =  ttt2 - tt3 + 1.0f;
			a1  =  ttt  - tt * 2.0f + t;
			a2  =  ttt  - tt;
			a3  = -ttt2 + tt3;
		
			return a0*b + a1*m0 + a2*m1 + a3*c;
		}

		public static Vector3 HermiteInterpolate3(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
		{
			var tt  = t * t;
			var tt3 = tt * 3.0f;
			var ttt = t * tt;
			var ttt2 = ttt * 2.0f;
			float a0, a1, a2, a3;
		
			var m0 = (c - a) * 0.5f;
			var m1 = (d - b) * 0.5f;
		
			a0  =  ttt2 - tt3 + 1.0f;
			a1  =  ttt  - tt * 2.0f + t;
			a2  =  ttt  - tt;
			a3  = -ttt2 + tt3;
		
			return a0*b + a1*m0 + a2*m1 + a3*c;
		}

		// This gives you the time-independent 't' value for lerp when used for damping
		public static float DampenFactor(float dampen, float deltaTime)
		{
			if (dampen < 0.0f)
			{
				return 1.0f;
			}
	#if UNITY_EDITOR
			if (Application.isPlaying == false)
			{
				return 1.0f;
			}
	#endif
			return 1.0f - Mathf.Exp(-dampen * deltaTime);
		}

		public static float DampenFactor(float dampen, float deltaTime, float linear)
		{
			var factor = DampenFactor(dampen, deltaTime);

			return factor + linear * deltaTime;
		}

		public static int Mod(int a, int b)
		{
			var m = a % b;

			if (m < 0)
			{
				return m + b;
			}

			return m;
		}
	
		public static Bounds NewBoundsFromMinMax(Vector3 min, Vector3 max)
		{
			var bounds = default(Bounds);

			bounds.SetMinMax(min, max);

			return bounds;
		}

		public static Bounds NewBoundsCenter(Bounds b, Vector3 c)
		{
			var x = Mathf.Max(Mathf.Abs(c.x - b.min.x), Mathf.Abs(c.x - b.max.x));
			var y = Mathf.Max(Mathf.Abs(c.y - b.min.z), Mathf.Abs(c.y - b.max.y));
			var z = Mathf.Max(Mathf.Abs(c.z - b.min.z), Mathf.Abs(c.z - b.max.z));

			return new Bounds(c, new Vector3(x, y, z) * 2.0f);
		}

		// This will begin a new need based on a seed and transform it based on a grid cell hash that tries to minimize visible symmetry
		public static int GetRandomSeed(int newSeed, long x, long y, long z)
		{
			var a = 1103515245;
			var b = 12345;

			newSeed = (int)(a * (newSeed + x) + b) % int.MaxValue;
			newSeed = (int)(a * (newSeed + y) + b) % int.MaxValue;
			newSeed = (int)(a * (newSeed + z) + b) % int.MaxValue;

			return newSeed;
		}

		public static void BeginRandomSeed(int newSeed, long x, long y, long z)
		{
			BeginRandomSeed(GetRandomSeed(newSeed, x, y, z));
		}

		public static void BeginRandomSeed(int newSeed)
		{
			seedStates.Push(Random.state);
		
			Random.InitState(newSeed);
		}

		public static void EndRandomSeed()
		{
			Random.state = seedStates.Pop();
		}

		public static Material CreateTempMaterial(string materialName, string shaderName)
		{
			var shader = Shader.Find(shaderName);

			if (shader == null)
			{
				Debug.LogError("Failed to find shader: " + shaderName); return null;
			}

			var material = new Material(shader);

			material.name = materialName;
		
#if UNITY_EDITOR
			material.hideFlags = HideFlags.HideAndDontSave;
#endif

			return material;
		}

		public static float GetMeshRadius(Mesh mesh)
		{
			var min = mesh.bounds.min;
			var max = mesh.bounds.max;
			var avg = Mathf.Abs(min.x) + Mathf.Abs(min.y) + Mathf.Abs(min.z) + Mathf.Abs(max.x) + Mathf.Abs(max.y) + Mathf.Abs(max.z);

			return avg / 6.0f;
		}

		public static Mesh CreateTempMesh(string meshName)
		{
			var mesh = SgtObjectPool<Mesh>.Pop() ?? new Mesh();

			mesh.name = meshName;

	#if UNITY_EDITOR
			mesh.hideFlags = HideFlags.DontSave;
	#endif
			return mesh;
		}
	
		public static Texture2D CreateTempTexture2D(string name, int width, int height, TextureFormat format = TextureFormat.ARGB32, bool mips = false, bool linear = false)
		{
			var texture2D = new Texture2D(width, height, format, mips, linear);

			texture2D.name = name;

#if UNITY_EDITOR
			texture2D.hideFlags = HideFlags.DontSave;
#endif

			return texture2D;
		}

		#pragma warning disable 649
		private static GradientAlphaKey[] tempAlphaKeys = new GradientAlphaKey[2];
		private static GradientColorKey[] tempColorKeys = new GradientColorKey[2];

		public static Gradient CreateGradient(Color color)
		{
			var gradient = new Gradient();

			tempAlphaKeys[0].time = 0.0f; tempAlphaKeys[0].alpha = 1.0f;
			tempAlphaKeys[1].time = 1.0f; tempAlphaKeys[1].alpha = 1.0f;

			tempColorKeys[0].time = 0.0f; tempColorKeys[0].color = color;
			tempColorKeys[1].time = 1.0f; tempColorKeys[1].color = color;

			gradient.SetKeys(tempColorKeys, tempAlphaKeys);

			return gradient;
		}

		public static GameObject CreateGameObject(string name, int layer, Transform parent = null, bool recordUndo = true)
		{
			return CreateGameObject(name, layer, parent, Vector3.zero, Quaternion.identity, Vector3.one, recordUndo);
		}

		public static GameObject CreateGameObject(string name, int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, bool recordUndo = true)
		{
			var gameObject = new GameObject(name);

			gameObject.layer = layer;

			gameObject.transform.SetParent(parent, false);

			gameObject.transform.localPosition = localPosition;
			gameObject.transform.localRotation = localRotation;
			gameObject.transform.localScale    = localScale;

#if UNITY_EDITOR
			if (recordUndo == true)
			{
				Undo.RegisterCreatedObjectUndo(gameObject, undoName);
			}
#endif

			return gameObject;
		}

		// return.x = -PI   .. +PI
		// return.y = -PI/2 .. +PI/2
		public static Vector2 CartesianToPolar(Vector3 xyz)
		{
			var longitude = Mathf.Atan2(xyz.x, xyz.z);
			var latitude  = Mathf.Asin(xyz.y / xyz.magnitude);

			return new Vector2(longitude, latitude);
		}

		// return.x = 0 .. 1
		// return.y = 0 .. 1
		public static Vector2 CartesianToPolarUV(Vector3 xyz)
		{
			var uv = CartesianToPolar(xyz);

			uv.x = Mathf.Repeat(-0.25f - uv.x / (Mathf.PI * 2.0f), 1.0f);
			uv.y = 0.5f + uv.y / Mathf.PI;

			return uv;
		}

		public static Vector4 CalculateSpriteUV(Sprite s)
		{
			var uv = default(Vector4);

			if (s != null)
			{
				var r = s.textureRect;
				var t = s.texture;

				uv.x = Divide(r.xMin, t.width);
				uv.y = Divide(r.yMin, t.height);
				uv.z = Divide(r.xMax, t.width);
				uv.w = Divide(r.yMax, t.height);
			}

			return uv;
		}

		public static void CalculateHorizonThickness(float innerRadius, float middleRadius, float distance, out float innerThickness, out float outerThickness)
		{
			if (distance < innerRadius)
			{
				distance = innerRadius;
			}

			var horizonOuterDistance = Mathf.Sin(Mathf.Acos(innerRadius));
			var horizonDistance      = Mathf.Min(Mathf.Sqrt(distance * distance - innerRadius * innerRadius), horizonOuterDistance);

			outerThickness = horizonDistance + horizonOuterDistance;
			//innerThickness = horizonDistance;

			if (distance < middleRadius)
			{
				distance = middleRadius;
			}

			horizonDistance = Mathf.Min(Mathf.Sqrt(distance * distance - innerRadius * innerRadius), horizonOuterDistance);

			innerThickness = horizonDistance;
		}

		public static void EnableKeyword(string keyword, Material material)
		{
			if (material != null)
			{
				if (material.IsKeywordEnabled(keyword) == false)
				{
					material.EnableKeyword(keyword);
				}
			}
		}

		public static void DisableKeyword(string keyword, Material material)
		{
			if (material != null)
			{
				if (material.IsKeywordEnabled(keyword) == true)
				{
					material.DisableKeyword(keyword);
				}
			}
		}

		public static void Resize<T>(List<T> list, int size)
		{
			if (list.Count > size)
			{
				list.RemoveRange(size, list.Count - size);
			}
			else
			{
				list.Capacity = size;

				for (var i = list.Count; i < size; i++)
				{
					list.Add(default(T));
				}
			}
		}

		public static Texture2D GetReadableTexture(Texture source, int sizeX = -1, int sizeY = -1, TextureFormat format = TextureFormat.RGBA32, bool apply = false, Texture2D tempTexture = null)
		{
			if (sizeX <= 0) sizeX = source.width;
			if (sizeY <= 0) sizeY = source.height;

			var oldActive     = RenderTexture.active;
			var renderTexture = RenderTexture.GetTemporary(sizeX, sizeY, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

			if (tempTexture != null)
			{
				if (tempTexture.width != sizeX || tempTexture.height != sizeY || tempTexture.format != format)
				{
					Object.DestroyImmediate(tempTexture);
				}
			}

			if (tempTexture == null)
			{
				tempTexture = new Texture2D(sizeX, sizeY, format, false);
			}

			Graphics.Blit(source, renderTexture);

			RenderTexture.active = renderTexture;

			tempTexture.ReadPixels(new Rect(0, 0, sizeX, sizeY), 0, 0);

			if (apply == true)
			{
				tempTexture.Apply();
			}

			RenderTexture.active = oldActive;

			RenderTexture.ReleaseTemporary(renderTexture);

			return tempTexture;
		}

		public static int GetStride(TextureFormat format)
		{
			switch (format)
			{
				case TextureFormat.Alpha8: return 1;
				case TextureFormat.RGB24: return 3;
				case TextureFormat.RGBA32: return 4;
				case TextureFormat.ARGB32: return 4;
				case TextureFormat.BGRA32: return 4;
				case TextureFormat.R8: return 1;
			}

			return 0;
		}

		/// <summary>Gets the byte offset of a texture channel based on its format.
		/// Channel = 0 = Red.
		/// Channel = 1 = Green.
		/// Channel = 2 = Blue.
		/// Channel = 3 = Alpha.</summary>
		public static int GetOffset(TextureFormat format, int channel)
		{
			switch (format)
			{
				case TextureFormat.RGB24:
					switch (channel)
					{
						case 0: return 0;
						case 1: return 1;
						case 2: return 2;
					}
				break;
				case TextureFormat.RGBA32:
					switch (channel)
					{
						case 0: return 0;
						case 1: return 1;
						case 2: return 2;
						case 3: return 3;
					}
				break;
				case TextureFormat.ARGB32:
					switch (channel)
					{
						case 0: return 1;
						case 1: return 2;
						case 2: return 3;
						case 3: return 0;
					}
				break;
				case TextureFormat.BGRA32:
					switch (channel)
					{
						case 0: return 2;
						case 1: return 1;
						case 2: return 0;
						case 3: return 3;
					}
				break;
			}

			return 0;
		}

		private static List<Material> materials = new List<Material>();

		public static void AddMaterial(Renderer r, Material m)
		{
			if (r != null && m != null)
			{
				var sms = r.sharedMaterials;

				materials.Clear();

				foreach (var sm in sms)
				{
					if (sm == m)
					{
						return;
					}
				}

				foreach (var sm in sms)
				{
					if (sm != null)
					{
						materials.Add(sm);
					}
				}

				materials.Add(m);

				r.sharedMaterials = materials.ToArray(); materials.Clear();
			}
		}

		// Prevent applying the same shader material twice
		public static void ReplaceMaterial(Renderer r, Material m)
		{
			if (r != null && m != null)
			{
				var sms = r.sharedMaterials;

				foreach (var sm in sms)
				{
					if (sm == m)
					{
						return;
					}
				}

				foreach (var sm in sms)
				{
					if (sm != null)
					{
						if (sm.shader != m.shader)
						{
							materials.Add(sm);
						}
					}
				}

				materials.Add(m);

				r.sharedMaterials = materials.ToArray(); materials.Clear();
			}
		}

		public static void RemoveMaterial(Renderer r, Material m)
		{
			if (r != null)
			{
				var sms = r.sharedMaterials;

				materials.Clear();

				foreach (var sm in sms)
				{
					if (sm != null && sm != m)
					{
						materials.Add(sm);
					}
				}

				r.sharedMaterials = materials.ToArray(); materials.Clear();
			}
		}

		public static float UniformScale(Vector3 scale)
		{
			scale.x = System.Math.Abs(scale.x);
			scale.y = System.Math.Abs(scale.y);
			scale.z = System.Math.Abs(scale.z);

			return Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
		}

		public static Matrix4x4 ShearingZ(Vector2 xy) // Z changes with x/y
		{
			var matrix = Matrix4x4.identity;

			matrix.m20 = xy.x;
			matrix.m21 = xy.y;

			return matrix;
		}

		public static Color Brighten(Color color, float brightness)
		{
			color.r *= brightness;
			color.g *= brightness;
			color.b *= brightness;

			return color;
		}

		public static Color Premultiply(Color color)
		{
			color.r *= color.a;
			color.g *= color.a;
			color.b *= color.a;

			return color;
		}

		public static void SetTempMaterial(Material material)
		{
			tempMaterials.Clear();

			tempMaterials.Add(material);
		}

		public static void SetTempMaterial(Material material1, Material material2)
		{
			tempMaterials.Clear();

			tempMaterials.Add(material1);
			tempMaterials.Add(material2);
		}

		public static void EnableKeyword(string keyword)
		{
			for (var i = tempMaterials.Count - 1; i >= 0; i--)
			{
				var tempMaterial = tempMaterials[i];

				if (tempMaterial != null)
				{
					if (tempMaterial.IsKeywordEnabled(keyword) == false)
					{
						tempMaterial.EnableKeyword(keyword);
					}
				}
			}
		}

		public static void DisableKeyword(string keyword)
		{
			for (var i = tempMaterials.Count - 1; i >= 0; i--)
			{
				var tempMaterial = tempMaterials[i];

				if (tempMaterial != null)
				{
					if (tempMaterial.IsKeywordEnabled(keyword) == true)
					{
						tempMaterial.DisableKeyword(keyword);
					}
				}
			}
		}

		public static void SetMatrix(string key, Matrix4x4 value)
		{
			for (var i = tempMaterials.Count - 1; i >= 0; i--)
			{
				var tempMaterial = tempMaterials[i];

				if (tempMaterial != null)
				{
					tempMaterial.SetMatrix(key, value);
				}
			}
		}

#if UNITY_EDITOR
		public static void DrawSphere(Vector3 center, Vector3 right, Vector3 up, Vector3 forward, int resolution = 32)
		{
			DrawCircle(center, right, up, resolution);
			DrawCircle(center, right, forward, resolution);
			DrawCircle(center, forward, up, resolution);
		}

		public static void DrawCircle(Vector3 center, Vector3 right, Vector3 up, int resolution = 32)
		{
			var step = Reciprocal(resolution);

			for (var i = 0; i < resolution; i++)
			{
				var a = i * step;
				var b = a + step;

				a = a * Mathf.PI * 2.0f;
				b = b * Mathf.PI * 2.0f;

				Gizmos.DrawLine(center + right * Mathf.Sin(a) + up * Mathf.Cos(a), center + right * Mathf.Sin(b) + up * Mathf.Cos(b));
			}
		}

		public static void DrawCircle(Vector3 center, Vector3 axis, float radius, int resolution = 32)
		{
			var rotation = Quaternion.FromToRotation(Vector3.up, axis);
			var right    = rotation * Vector3.right   * radius;
			var forward  = rotation * Vector3.forward * radius;

			DrawCircle(center, right, forward, resolution);
		}
#endif
		public static void UpdateNativeArray<T>(ref NativeArray<T> array, int length)
			where T : struct
		{
			if (array.IsCreated == true && array.Length != length)
			{
				array.Dispose();
			}

			if (array.IsCreated == false)
			{
				array = new NativeArray<T>(length, Allocator.Persistent);
			}
		}
#if UNITY_2019_3_OR_NEWER
		public static NativeArray<float2> ConvertNativeArray(NativeArray<float2> nativeArray)
		{
			return nativeArray;
		}
		public static NativeArray<float3> ConvertNativeArray(NativeArray<float3> nativeArray)
		{
			return nativeArray;
		}
		public static NativeArray<float4> ConvertNativeArray(NativeArray<float4> nativeArray)
		{
			return nativeArray;
		}
		public static NativeArray<int> ConvertNativeArray(NativeArray<int> nativeArray)
		{
			return nativeArray;
		}
		public static NativeArray<Color32> ConvertNativeArray(NativeArray<Color32> nativeArray)
		{
			return nativeArray;
		}
#else
		private static List<Vector2> tempVector2s = new List<Vector2>(1024);
		public static List<Vector2> ConvertNativeArray(NativeArray<float2> nativeArray)
		{
			tempVector2s.Clear(); for (var i = 0; i < nativeArray.Length; i++) tempVector2s.Add(nativeArray[i]); return tempVector2s;
		}
		private static List<Vector3> tempVector3s = new List<Vector3>(1024);
		public static List<Vector3> ConvertNativeArray(NativeArray<float3> nativeArray)
		{
			tempVector3s.Clear(); for (var i = 0; i < nativeArray.Length; i++) tempVector3s.Add(nativeArray[i]); return tempVector3s;
		}
		private static List<Vector4> tempVector4s = new List<Vector4>(1024);
		public static List<Vector4> ConvertNativeArray(NativeArray<float4> nativeArray)
		{
			tempVector4s.Clear(); for (var i = 0; i < nativeArray.Length; i++) tempVector4s.Add(nativeArray[i]); return tempVector4s;
		}
		private static List<int> tempInt32s = new List<int>(1024);
		public static List<int> ConvertNativeArray(NativeArray<int> nativeArray)
		{
			tempInt32s.Clear(); for (var i = 0; i < nativeArray.Length; i++) tempInt32s.Add(nativeArray[i]); return tempInt32s;
		}
		private static List<Color32> tempColors = new List<Color32>(1024);
		public static List<Color32> ConvertNativeArray(NativeArray<Color32> nativeArray)
		{
			tempColors.Clear(); for (var i = 0; i < nativeArray.Length; i++) tempColors.Add(nativeArray[i]); return tempColors;
		}
#endif
	}
}