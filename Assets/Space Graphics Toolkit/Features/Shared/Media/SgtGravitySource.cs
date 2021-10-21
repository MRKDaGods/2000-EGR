using System.Collections.Generic;
using UnityEngine;
using FSA = UnityEngine.Serialization.FormerlySerializedAsAttribute;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component allows you to define a gravity source, which can be used to attract Rigidbodyies with the SgtGravityReceiver attached.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtGravitySource")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Gravity Source")]
	public class SgtGravitySource : MonoBehaviour
	{
		/// <summary>The mass of this gravity source.</summary>
		public float Mass { set { mass = value; } get { return mass; } } [FSA("Mass")] [SerializeField] private float mass = 100.0f;

		/// <summary>If you enable this then the Mass setting will be automatically copied from the attached Rigidbody.</summary>
		public bool AutoSetMass { set { autoSetMass = value; } get { return autoSetMass; } } [FSA("AutoSetMass")] [SerializeField] private bool autoSetMass;

		public static LinkedList<SgtGravitySource> Instances { get { return instances; } } [System.NonSerialized] private static LinkedList<SgtGravitySource> instances = new LinkedList<SgtGravitySource>();

		[System.NonSerialized]
		private Rigidbody cachedRigidbody;

		[System.NonSerialized]
		private LinkedListNode<SgtGravitySource> node;

		protected virtual void OnEnable()
		{
			node = instances.AddLast(this);
		}

		protected virtual void OnDisable()
		{
			instances.Remove(node);
		}

		protected virtual void Update()
		{
			if (autoSetMass == true)
			{
				if (cachedRigidbody == null)
				{
					cachedRigidbody = GetComponent<Rigidbody>();
				}

				if (cachedRigidbody != null)
				{
					mass = cachedRigidbody.mass;
				}
			}
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtGravitySource))]
	public class SgtGravitySource_Editor : SgtEditor<SgtGravitySource>
	{
		protected override void OnInspector()
		{
			Draw("mass", "The mass of this gravity source.");
			Draw("autoSetMass", "If you enable this then the Mass setting will be automatically copied from the attached Rigidbody.");
		}
	}
}
#endif