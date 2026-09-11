using System.Collections;
using System.Collections.Generic;
using CesiumForUnity;
using Unity.XR.CoreUtils;
using UnityEditor.XR.LegacyInputHelpers;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ForDebug : MonoBehaviour
{
    [SerializeField] private Camera Camera;
    private CesiumGlobeAnchor playerAnchor;

    // Update is called once per frame
    void Start()
    {
        StartCoroutine(UpdateTransform());
    }

    private IEnumerator UpdateTransform()
    {
        playerAnchor = Camera.GetComponent<CesiumGlobeAnchor>();
        while (true)
        {
            Debug.Log(
                $"player GPS: {playerAnchor.longitudeLatitudeHeight.y:F6} , {playerAnchor.longitudeLatitudeHeight.x:F6} , {playerAnchor.longitudeLatitudeHeight.z:F6}");
            yield return new WaitForSeconds(1);
        }
    }
}
