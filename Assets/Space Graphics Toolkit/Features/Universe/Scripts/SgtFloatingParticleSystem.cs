using UnityEngine;

namespace SpaceGraphicsToolkit
{
	/// <summary>This component updates the position of the attached <b>ParticleSystem</b> component when the floating origin system snaps.</summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(ParticleSystem))]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtFloatingParticleSystem")]
	[AddComponentMenu(SgtHelper.ComponentMenuPrefix + "Floating ParticleSystem")]
	public class SgtFloatingParticleSystem : MonoBehaviour
	{
		[System.NonSerialized]
		private ParticleSystem cachedParticleSystem;

		private static ParticleSystem.Particle[] tempParticles;

		protected virtual void OnEnable()
		{
			cachedParticleSystem = GetComponent<ParticleSystem>();

			SgtHelper.OnSnap += HandleSnap;
		}

		protected virtual void OnDisable()
		{
			SgtHelper.OnSnap -= HandleSnap;
		}

		private void HandleSnap(Vector3 delta)
		{
			var count = cachedParticleSystem.main.maxParticles;

			if (tempParticles == null || tempParticles.Length < count)
			{
				tempParticles = new ParticleSystem.Particle[Mathf.Max(1024, count)];
			}

			count = cachedParticleSystem.GetParticles(tempParticles);

			for (var i = 0; i < count; i++)
			{
				tempParticles[i].position += delta;
			}

			cachedParticleSystem.SetParticles(tempParticles, count);
		}
	}
}

#if UNITY_EDITOR
namespace SpaceGraphicsToolkit
{
	using UnityEditor;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(SgtFloatingParticleSystem))]
	public class SgtFloatingParticleSystem_Editor : SgtEditor<SgtFloatingParticleSystem>
	{
		protected override void OnInspector()
		{
		}
	}
}
#endif