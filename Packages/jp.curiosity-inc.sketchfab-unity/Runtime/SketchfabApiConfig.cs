using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SketchfabApiConfig",
    menuName = "Sketchfab/Sketchfab Api Config")]
public class SketchfabApiConfig : ScriptableObject
{
    [Tooltip("Client ID specified by sketchfab")]
    public string ClientId;
    [Tooltip("Callback URL starting with http or https")]
    public string CallbackUrl;
    [Tooltip("Callback URL starting with a custom scheme, optional")]
    public string CustomSchemeCallbackUrl;
}
