using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component marks the attached LeanFloatingPoint component as a warpable target point. This allows you to pick the target using the SgtWarpPin component.</summary>
	[RequireComponent(typeof(SgtFloatingPoint))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtFloatingTarget")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Floating Target")]
	public class SgtFloatingTarget : SgtLinkedBehaviour<SgtFloatingTarget>
	{
		/// <summary>The shorthand name for this warp target.</summary>
		public string WarpName { set { warpName = value; } get { return warpName; } } [FSA("WarpName")] [SerializeField] private string warpName;

		/// <summary>The distance from this SgtFloatingPoint we should warp to, to prevent you warping too close.</summary>
		public SgtLength WarpDistance { set { warpDistance = value; } get { return warpDistance; } } [FSA("WarpDistance")] [SerializeField] private SgtLength warpDistance = 1000.0;

		[System.NonSerialized]
		private SgtFloatingPoint cachedPoint;

		[System.NonSerialized]
		private bool cachedPointSet;

		public SgtFloatingPoint CachedPoint
		{
			get
			{
				if (cachedPointSet == false)
				{
					cachedPoint    = GetComponent<SgtFloatingPoint>();
					cachedPointSet = true;
				}

				return cachedPoint;
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtFloatingTarget))]
	public class SgtWarpTarget_Editor : SgtEditor<SgtFloatingTarget>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => string.IsNullOrEmpty(t.WarpName)));
				Draw("warpName", "The shorthand name for this warp target.");
			EndError();
			BeginError(Any(t => t.WarpDistance < 0.0));
				Draw("warpDistance", "The distance from this SgtFloatingPoint we should warp to, to prevent you warping too close.");
			EndError();
		}
	}
}
#endif