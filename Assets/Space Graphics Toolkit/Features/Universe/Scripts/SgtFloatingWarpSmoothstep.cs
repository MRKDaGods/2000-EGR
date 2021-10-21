using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component will smoothly warp to the target, where the speed will slow down near the start of the travel, and near the end.</summary>
	[DefaultExecutionOrder(100)]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtFloatingWarpSmoothstep")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Floating Warp Smoothstep")]
	public class SgtFloatingWarpSmoothstep : SgtFloatingWarp
	{
		/// <summary>Seconds it takes to complete a warp.</summary>
		public double WarpTime { set { warpTime = value; } get { return warpTime; } } [FSA("WarpTime")] [SerializeField] private double warpTime = 10.0;

		/// <summary>Warp smoothstep iterations.</summary>
		public int Smoothness { set { smoothness = value; } get { return smoothness; } } [FSA("Smoothness")] [SerializeField] private int smoothness = 3;

		/// <summary>Currently warping?</summary>
		public bool Warping { set { warping = value; } get { return warping; } } [FSA("Warping")] [SerializeField] private bool warping;

		/// <summary>Current warp progress in seconds.</summary>
		public double Progress { set { progress = value; } get { return progress; } } [FSA("Progress")] [SerializeField] private double progress;

		/// <summary>Start position of the warp.</summary>
		public SgtPosition StartPosition { set { startPosition = value; } get { return startPosition; } } [FSA("StartPosition")] [SerializeField] private SgtPosition startPosition;

		/// <summary>Target position of the warp.</summary>
		public SgtPosition TargetPosition { set { targetPosition = value; } get { return targetPosition; } } [FSA("TargetPosition")] [SerializeField] private SgtPosition targetPosition;

		public override bool CanAbortWarp
		{
			get
			{
				return warping;
			}
		}

		public override void WarpTo(SgtPosition position)
		{
			warping        = true;
			progress       = 0.0;
			startPosition  = point.Position;
			targetPosition = position;
		}

		public override void AbortWarp()
		{
			warping = false;
		}

		protected virtual void Update()
		{
			if (warping == true)
			{
				progress += Time.deltaTime;

				if (progress > warpTime)
				{
					progress = warpTime;
				}

				var bend = SmoothStep(progress / warpTime, smoothness);

				if (point != null)
				{
					point.Position = SgtPosition.Lerp(ref startPosition, ref targetPosition, bend);
				}

				if (progress >= warpTime)
				{
					warping = false;
				}
			}
		}

		private static double SmoothStep(double m, int n)
		{
			for (int i = 0 ; i < n ; i++)
			{
				m = m * m * (3.0 - 2.0 * m);
			}

			return m;
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtFloatingWarpSmoothstep))]
	public class SgtFloatingWarpSmoothstep_Editor : SgtFloatingWarp_Editor<SgtFloatingWarpSmoothstep>
	{
		protected override void OnInspector()
		{
			base.OnInspector();

			Separator();

			BeginError(Any(t => t.WarpTime < 0.0));
				Draw("warpTime", "Seconds it takes to complete a warp.");
			EndError();
			BeginError(Any(t => t.Smoothness < 1));
				Draw("smoothness", "Warp smoothstep iterations.");
			EndError();

			Separator();

			BeginDisabled();
				Draw("warping", "Currently warping?");
				Draw("progress", "Current warp progress in seconds.");
				Draw("startPosition", "Start position of the warp.");
				Draw("targetPosition", "Target position of the warp.");
			EndDisabled();
		}
	}
}
#endif