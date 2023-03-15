using UnityEditor;
using UnityEngine;

public class AddCreatePortalToMenu : MonoBehaviour
{
    [MenuItem("GameObject/MHTTP Portal", false, 10)]
    static void CreateMhttpPortalGameObject(MenuCommand menuCommand)
    {
        UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath(
            "Packages/org.mhttp.mhttp-portal/Runtime/Assets/Portal.prefab",
            typeof(GameObject)
        );


        Debug.Log(prefab);

        GameObject parent = Selection.activeObject as GameObject;

        var newObject = (GameObject) PrefabUtility.InstantiatePrefab(prefab);

        // Ensure it gets reparented if this was a context click (otherwise does nothing)
        GameObjectUtility.SetParentAndAlign(newObject, menuCommand.context as GameObject);
        // Register the creation in the undo system
        Undo.RegisterCreatedObjectUndo(newObject, "Create " + newObject.name);
        Selection.activeObject = newObject;
    }
}
