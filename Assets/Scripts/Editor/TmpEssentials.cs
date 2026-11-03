#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace MyArcade.EditorTools
{
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
				Debug.Log("imported TextMeshPro essential resources.");
			}
			else
			{
				Debug.LogWarning("open Window > TextMeshPro > Import TMP Essential Resources once.");
			}
		}
	}
}
#endif
