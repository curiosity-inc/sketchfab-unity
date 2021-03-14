using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SketchfabApiConfig",
    menuName = "Sketchfab/Sketchfab Api Config")]
public class SketchfabApiConfig : ScriptableObject
{
    public string ClientId;
    public string CallbackUrl;
}
