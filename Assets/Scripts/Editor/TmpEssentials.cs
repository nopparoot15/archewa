#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MyArcade.EditorTools
{
	/// <summary>
	/// First-time setup, done automatically once when the project opens:
	/// 1) imports the TextMeshPro "Essential Resources" (the UI needs them),
	/// 2) builds the game scene into the Hierarchy (Assets/Scenes/Main.unity) if it does not exist yet.
	/// </summary>
	[InitializeOnLoad]
	public static class TmpEssentials
	{
		static TmpEssentials()
		{
			EditorApplication.delayCall += Setup;
		}

		private static void Setup()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
			{
				return;
			}
			if (!TmpReady())
			{
				ImportTmp();
				return; // the import triggers a refresh; this script runs again afterwards
			}
		}

		private static bool TmpReady()
		{
			return Resources.Load("TMP Settings") != null;
		}

		private static void ImportTmp()
		{
			// called by name so this script does not need a hard reference to the TextMeshPro editor assembly
			Type importer = Type.GetType("TMPro.TMP_PackageResourceImporter, Unity.TextMeshPro");
			var method = importer?.GetMethod("ImportResources", new[] { typeof(bool), typeof(bool), typeof(bool) });
			if (method != null)
			{
				method.Invoke(null, new object[] { true, false, false });
				Debug.Log("MyArcade: imported TextMeshPro essential resources.");
			}
			else
			{
				Debug.LogWarning("MyArcade: open Window > TextMeshPro > Import TMP Essential Resources once, then use MyArcade > Build Scene.");
			}
		}
	}
}
#endif
