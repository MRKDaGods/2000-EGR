using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to create simple thrusters that can apply forces to Rigidbodies based on their position. You can also use sprites to change the graphics.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtThruster")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Thruster")]
	public class SgtThruster : MonoBehaviour
	{
		/// <summary>How active is this thruster? 0 for off, 1 for max power, -1 for max reverse, etc.</summary>
		public float Throttle { set { throttle = value; } get { return throttle; } } [FSA("Throttle")] [SerializeField] private float throttle;

		/// <summary>The rigidbody you want to apply the thruster forces to</summary>
		public Rigidbody Body { set { body = value; } get { return body; } } [FSA("Rigidbody")] [SerializeField] private Rigidbody body;

		/// <summary>The type of force we want to apply to the Rigidbody.</summary>
		public bool ForceAtPosition { set { forceAtPosition = value; } get { return forceAtPosition; } } [FSA("ForceAtPosition")] [SerializeField] private bool forceAtPosition;

		/// <summary>The force mode used when ading force to the Rigidbody.</summary>
		public ForceMode ForceMode { set { forceMode = value; } get { return forceMode; } } [FSA("ForceMode")] [SerializeField] private ForceMode forceMode = ForceMode.Acceleration;

		/// <summary>The maximum amount of force applied to the rigidbody (when the throttle is -1 or 1).</summary>
		public float ForceMagnitude { set { forceMagnitude = value; } get { return forceMagnitude; } } [FSA("ForceMagnitude")] [SerializeField] private float forceMagnitude = 1.0f;

		/// <summary>Create a child GameObject with a thruster attached</summary>
		public static SgtThruster Create(int layer = 0, Transform parent = null)
		{
			return Create(layer, parent, Vector3.zero, Quaternion.identity, Vector3.one);
		}

		public static SgtThruster Create(int layer, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
		{
			var gameObject = SgtHelper.CreateGameObject("Thruster", layer, parent, localPosition, localRotation, localScale);
			var thruster   = gameObject.AddComponent<SgtThruster>();

			return thruster;
		}

#if UNITY_EDITOR
		[UnityEditor.MenuItem(SgtHelper.GameObjectMenuPrefix + "Thruster", false, 10)]
		public static void CreateMenuItem()
		{
			var parent   = SgtHelper.GetSelectedParent();
			var thruster = Create(parent != null ? parent.gameObject.layer : 0, parent);

			SgtHelper.SelectAndPing(thruster);
		}
#endif

		protected virtual void FixedUpdate()
		{
#if UNITY_EDITOR
			if (Application.isPlaying == false)
			{
				return;
			}
#endif
			// Apply thruster force to rigidbody
			if (body != null)
			{
				var force = transform.forward * forceMagnitude * throttle * Time.fixedDeltaTime;

				if (forceAtPosition == true)
				{
					body.AddForceAtPosition(force, transform.position, forceMode);
				}
				else
				{
					body.AddForce(force, forceMode);
				}
			}
		}

#if UNITY_EDITOR
		protected virtual void OnDrawGizmosSelected()
		{
			var a = transform.position;
			var b = transform.position + transform.forward * forceMagnitude;

			Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
			Gizmos.DrawLine(a, b);

			Gizmos.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
			Gizmos.DrawLine(a, a + (b - a) * throttle);
		}
#endif
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtThruster))]
	public class SgtThruster_Editor : SgtEditor<SgtThruster>
	{
		protected override void OnInspector()
		{
			Draw("throttle", "How active is this thruster? 0 for off, 1 for max power, -1 for max reverse, etc.");
			Draw("body", "The rigidbody you want to apply the thruster forces to");

			if (Any(t => t.Body != null))
			{
				BeginIndent();
					Draw("forceAtPosition", "The type of force we want to apply to the Rigidbody.");
					Draw("forceMode", "The force mode used when ading force to the Rigidbody.");
					Draw("forceMagnitude", "The maximum amount of force applied to the rigidbody (when the throttle is -1 or 1).");
				EndIndent();
			}
		}
	}
}
#endif