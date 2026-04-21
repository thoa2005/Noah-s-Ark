using UnityEditor;
using UnityEngine;

public class ActiveRagdollSetupEditor : EditorWindow
{
    [MenuItem("Tools/Run Active Ragdoll Setup")]
    public static void RunSetup()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("Player not found!"); return; }

        ActiveRagdollInitialiser initialiser = player.GetComponent<ActiveRagdollInitialiser>();
        if (initialiser == null) initialiser = player.AddComponent<ActiveRagdollInitialiser>();

        // Find the rigs
        Transform physicsRig = player.transform.Find("panda/metarig");
        Transform masterRig = player.transform.Find("panda/MasterMetarig");

        if (physicsRig && masterRig)
        {
            initialiser.physicsRig = physicsRig;
            initialiser.masterRig = masterRig;
            initialiser.FinalSetup();
            Debug.Log("Active Ragdoll Mapping Completed successfully!");
        }
        else
        {
            Debug.LogError("Rigs not found under Player/panda/");
        }
    }
}
