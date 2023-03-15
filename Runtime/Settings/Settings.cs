using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [CreateAssetMenu(menuName = "MHTTP Portal Settings")]
public class Settings : ScriptableObject
{
    public List<string> PlayerTags = new List<string> { "Player" };
    public string PortalAssetFolder = "Portal Assets/";
    public Mesh DefaultColliderMesh;
}
