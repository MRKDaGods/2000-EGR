using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to warp to the target when clicking a button.
	/// NOTE: The button's OnClick event must be linked to the Click method.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtFloatingWarpButton")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Floating Warp Button")]
	public class SgtFloatingWarpButton : MonoBehaviour
	{
		/// <summary>The point that will be warped to.</summary>
		public SgtFloatingTarget Target { set { target = value; } get { return target; } } [FSA("Target")] [SerializeField] private SgtFloatingTarget target;

		/// <summary>The warp effect that will be used.</summary>
		public SgtFloatingWarp Warp { set { warp = value; } get { return warp; } } [FSA("Warp")] [SerializeField] private SgtFloatingWarp warp;

		public void Click()
		{
			warp.WarpTo(target.CachedPoint.Position, target.WarpDistance);
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtFloatingWarpButton))]
	public class SgtFloatingWarpButton_Editor : SgtEditor<SgtFloatingWarpButton>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.Target == null));
				Draw("target", "The point that will be warped to.");
			EndError();
			BeginError(Any(t => t.Warp == null));
				Draw("warp", "The warp effect that will be used.");
			EndError();
		}
	}
}
#endif