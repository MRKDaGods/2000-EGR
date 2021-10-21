using UnityEngine;
using UnityEngine.Events;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component monitors the specified <b>SgtFloatingCamera</b> or <b>SgtFloatingObject</b> point for position changes, and outputs the speed of those changes to the <b>OnString</b> event.</summary>
	[ExecuteInEditMode]
	[DefaultExecutionOrder(1000)]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtFloatingSpeedometer")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Floating Speedometer")]
	public class SgtFloatingSpeedometer : MonoBehaviour
	{
		[System.Serializable] public class StringEvent : UnityEvent<string> {}

		public enum UpdateType
		{
			Update,
			FixedUpdate
		}

		/// <summary>The point whose speed will be monitored.</summary>
		public SgtFloatingPoint Point { set { point = value; } get { return point; } } [FSA("Point")] [SerializeField] private SgtFloatingPoint point;

		/// <summary>The format of the speed text.</summary>
		public string Format { set { format = value; } get { return format; } } [FSA("Format")] [SerializeField] private string format = "{0} m/s";

		/// <summary>This allows you to control where in the game loop the speed will be calculated.</summary>
		public UpdateType UpdateIn { set { updateIn = value; } get { return updateIn; } } [FSA("UpdateIn")] [SerializeField] private UpdateType updateIn;

		/// <summary>Each time the speed updates this event will fire, which you can link to update UI text.</summary>
		public StringEvent OnString { get { if (onString == null) onString = new StringEvent(); return onString; } } [SerializeField] private StringEvent onString;

		[System.NonSerialized]
		private SgtFloatingObject cachedObject;

		[System.NonSerialized]
		private SgtPosition expectedPosition;

		[System.NonSerialized]
		private bool expectedPositionSet;

		protected virtual void Update()
		{
			if (updateIn == UpdateType.Update)
			{
				TryUpdate();
			}
		}

		protected virtual void FixedUpdate()
		{
			if (updateIn == UpdateType.FixedUpdate)
			{
				TryUpdate();
			}
		}

		private void TryUpdate()
		{
			if (point != null)
			{
				var currentPosition = point.Position;

				if (expectedPositionSet == false)
				{
					expectedPosition    = currentPosition;
					expectedPositionSet = true;
				}

				var distance = SgtPosition.Distance(ref expectedPosition, ref currentPosition);
				var delta    = SgtHelper.Divide(distance, Time.deltaTime);
				var text     = string.Format(format, System.Math.Round(delta));

				if (onString != null)
				{
					onString.Invoke(text);
				}

				expectedPosition = currentPosition;
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtFloatingSpeedometer))]
	public class SgtFloatingSpeedometer_Editor : SgtEditor<SgtFloatingSpeedometer>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.Point == null));
				Draw("point", "The point whose speed will be monitored.");
			EndError();
			BeginError(Any(t => string.IsNullOrEmpty(t.Format)));
				Draw("format", "The format of the speed text.");
			EndError();
			Draw("updateIn", "This allows you to control where in the game loop the speed will be calculated.");

			Separator();

			Draw("onString");
		}
	}
}
#endif