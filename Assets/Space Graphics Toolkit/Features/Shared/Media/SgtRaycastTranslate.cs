using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component rotates the current GameObject.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtRaycastTranslate")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Raycast Translate")]
	public class SgtRaycastTranslate : MonoBehaviour
	{
		/// <summary>The target local translation.</summary>
		public Vector3 LocalTarget { set { localTarget = value; } get { return localTarget; } } [FSA("LocalTarget")] [SerializeField] private Vector3 localTarget = Vector3.back;

		/// <summary>The GameObject layers we will raycast against.</summary>
		public LayerMask Layers { set { layers = value; } get { return layers; } } [FSA("Layers")] [SerializeField] private LayerMask layers = Physics.DefaultRaycastLayers;

		/// <summary>The radius of the raycast, to prevent surface penetration by cameras.</summary>
		public float Radius { set { radius = value; } get { return radius; } } [FSA("Radius")] [SerializeField] private float radius = 0.1f;

		protected virtual void Update()
		{
			var distance01 = GetDistance01();

			transform.localPosition = localTarget * distance01;
		}

		private float GetDistance01()
		{
#if UNITY_EDITOR
			if (Application.isPlaying == false)
			{
				return 1.0f;
			}
#endif
			var parent     = transform.parent;
			var distance01 = 1.0f;

			if (parent != null)
			{
				var pointA = parent.position;
				var pointB = parent.TransformPoint(localTarget);
				var pointD = pointB - pointA;
				var pointM = pointD.magnitude;

				if (pointM > 0.0f)
				{
					var hit = default(RaycastHit);

					if (Physics.SphereCast(pointA, radius, pointD, out hit, pointM, layers) == true)
					{
						distance01 = hit.distance / pointM;
					}
				}
			}

			return distance01;
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtRaycastTranslate))]
	public class SgtRaycastTranslate_Editor : SgtEditor<SgtRaycastTranslate>
	{
		protected override void OnInspector()
		{
			Draw("localTarget", "The target local translation.");
			Draw("layers", "The GameObject layers we will raycast against.");
			Draw("radius", "The radius of the raycast, to prevent surface penetration by cameras.");
		}
	}
}
#endif