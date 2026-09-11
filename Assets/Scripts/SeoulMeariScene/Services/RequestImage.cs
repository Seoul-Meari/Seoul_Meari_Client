using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RequestImage : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private MessageInfo messageInfo;
    [SerializeField] private GameObject untilLoadObject;
    
    public void RequestImageByImageKey()
    {
        string imageKey = messageInfo.ImageKey;
        if (imageKey == null)
        {
            Debug.LogError("key is null");
        }
        NetworkManager.Instance.LoadEchoImage(
            imageKey,
            onSuccess: texture =>
            {
                messageInfo.setContentImage(texture);
                untilLoadObject.SetActive(false);
            },
            onError: err =>
            {

            }
            );
    }
}
