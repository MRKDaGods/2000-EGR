using UnityEngine;

namespace SpaceGraphicsToolkit
{
	/// <summary>This class allows you to execute an event at a later point in the game loop.</summary>
	[ExecuteInEditMode]
	[HelpURL(SgtHelper.HelpUrlPrefix + "SgtDelay")]
	[AddComponentMenu("")]
	public class SgtDelay : MonoBehaviour
	{
		private static SgtDelay instance;

		private System.Action actions;

		private System.Action actionsCopy;

		public static void Submit(System.Action action)
		{
			if (instance == null)
			{
				var gameObject = new GameObject("SgtDelay");

				gameObject.AddComponent<SgtDelay>();

				gameObject.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
			}

			instance.actions += action;
		}

		private void ExecuteEvents()
		{
			actionsCopy = actions;
			actions     = null;

			actionsCopy.Invoke();
		}

		protected virtual void OnEnable()
		{
			instance = this;

			SgtCamera.OnCameraPreCull += HandleCameraPreCull;
		}

		protected virtual void OnDisable()
		{
			SgtCamera.OnCameraPreCull -= HandleCameraPreCull;
		}

		protected virtual void Update()
		{
			ExecuteEvents();

			DestroyImmediate(gameObject);
		}

		private void HandleCameraPreCull(Camera camera)
		{
			ExecuteEvents();
		}
	}
}