using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component modifies the SgtAtmosphere.Height based on camera proximity.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(SgtAtmosphere))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtAtmosphereHeight")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Atmosphere Height")]
	public class SgtAtmosphereHeight : MonoBehaviour
	{
		/// <summary>The minimum distance between the atmosphere center and the camera position in local space.</summary>
		public float DistanceMin { set { distanceMin = value; } get { return distanceMin; } } [FSA("DistanceMin")] [SerializeField] private float distanceMin = 1.1f;

		/// <summary>The maximum distance between the atmosphere center and the camera position in local space.</summary>
		public float DistanceMax { set { distanceMax = value; } get { return distanceMax; } } [FSA("DistanceMax")] [SerializeField] private float distanceMax = 1.2f;

		/// <summary>The SgtAtmosphere.Height value that will be set when at or below DistanceMin.</summary>
		public float HeightClose { set { heightClose = value; } get { return heightClose; } } [FSA("HeightClose")] [SerializeField] private float heightClose = 0.1f;

		/// <summary>The SgtAtmosphere.Height value that will be set when at or above DistanceMax.</summary>
		public float HeightFar { set { heightFar = value; } get { return heightFar; } } [FSA("HeightFar")] [SerializeField] private float heightFar = 0.01f;

		[System.NonSerialized]
		private SgtAtmosphere cachedAtmosphere;

		protected virtual void OnEnable()
		{
			SgtCamera.OnCameraPreCull += PreCull;

			cachedAtmosphere = GetComponent<SgtAtmosphere>();
		}

		protected virtual void OnDisable()
		{
			SgtCamera.OnCameraPreCull -= PreCull;
		}

		private void PreCull(Camera camera)
		{
			if (camera != null)
			{
				var cameraPoint = transform.InverseTransformPoint(camera.transform.position);
				var distance01  = Mathf.InverseLerp(distanceMin, distanceMax, cameraPoint.magnitude);

				cachedAtmosphere.Height = Mathf.Lerp(heightClose, heightFar, distance01);
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtAtmosphereHeight))]
	public class SgtAtmosphereHeight_Editor : SgtEditor<SgtAtmosphereHeight>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.DistanceMin > t.DistanceMax));
				Draw("distanceMin", "The minimum distance between the atmosphere center and the camera position.");
				Draw("distanceMax", "The maximum distance between the atmosphere center and the camera position.");
			EndError();

			Separator();

			Draw("heightClose", "The SgtAtmosphere.Height value that will be set when at or below DistanceMin.");
			Draw("heightFar", "The SgtAtmosphere.Height value that will be set when at or above DistanceMax.");
		}
	}
}
#endif