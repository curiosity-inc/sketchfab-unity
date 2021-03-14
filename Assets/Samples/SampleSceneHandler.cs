using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Curiosity.Sketchfab;

public class SampleSceneHandler : MonoBehaviour
{
    public void OnLoginButtonClicked()
    {
        SketchfabApi.Instance.Login();
    }
}
