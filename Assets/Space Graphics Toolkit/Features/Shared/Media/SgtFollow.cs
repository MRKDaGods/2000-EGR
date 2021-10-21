using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This makes the current <b>Transform</b> follow the <b>Target</b> Transform as if it were a child.</summary>
	[ExecuteInEditMode]
	[DefaultExecutionOrder(200)]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtFollow")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Follow")]
	public class SgtFollow : MonoBehaviour
	{
		public enum UpdateType
		{
			Update,
			LateUpdate
		}

		/// <summary>The transform that will be followed.</summary>
		public Transform Target { set { target = value; } get { return target; } } [FSA("Target")] [SerializeField] private Transform target;

		/// <summary>How quickly this Transform follows the target.
		/// -1 = instant.</summary>
		public float Damping { set { damping = value; } get { return damping; } } [FSA("Dampening")] [SerializeField] private float damping = 5.0f;

		/// <summary>Follow the target's rotation too?</summary>
		public bool Rotate { set { rotate = value; } get { return rotate; } } [FSA("Rotate")] [SerializeField] private bool rotate = true;

		/// <summary>Where in the game loop should this component update?</summary>
		public UpdateType FollowIn { set { followIn = value; } get { return followIn; } } [FSA("FollowIn")] [SerializeField] private UpdateType followIn;

		/// <summary>This allows you to specify a positional offset relative to the <b>Target</b>.</summary>
		public Vector3 LocalPosition { set { localPosition = value; } get { return localPosition; } } [FSA("LocalPosition")] [SerializeField] private Vector3 localPosition;

		/// <summary>This allows you to specify a rotational offset relative to the <b>Target</b>.</summary>
		public Vector3 LocalRotation { set { localRotation = value; } get { return localRotation; } } [FSA("LocalRotation")] [SerializeField] private Vector3 localRotation;

		[ContextMenu("UpdatePosition")]
		public void UpdatePosition()
		{
			if (target != null)
			{
				var targetPosition = target.TransformPoint(localPosition);
				var factor         = SgtHelper.DampenFactor(damping, Time.deltaTime);

				transform.position = Vector3.Lerp(transform.position, targetPosition, factor);

				if (rotate == true)
				{
					var targetRotation = target.rotation * Quaternion.Euler(localRotation);

					transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, factor);
				}
			}
		}

		protected virtual void Update()
		{
			if (followIn == UpdateType.Update)
			{
				UpdatePosition();
			}
		}

		protected virtual void LateUpdate()
		{
			if (followIn == UpdateType.LateUpdate)
			{
				UpdatePosition();
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtFollow))]
	public class SgtFollow_Editor : SgtEditor<SgtFollow>
	{
		protected override void OnInspector()
		{
			BeginError(Any(t => t.Target == null));
				Draw("target", "The transform that will be followed.");
			EndError();
			Draw("damping", "How quickly this Transform follows the target.\n\n-1 = instant.");
			Draw("rotate", "Follow the target's rotation too?");
			Draw("followIn", "Where in the game loop should this component update?");

			Separator();

			Draw("localPosition", "This allows you to specify a positional offset relative to the Target transform.");
			Draw("localRotation", "This allows you to specify a rotational offset relative to the Target transform.");
		}
	}
}
#endif