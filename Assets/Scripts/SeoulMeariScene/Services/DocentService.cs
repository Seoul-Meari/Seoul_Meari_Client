using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DocentService : MonoBehaviour
{
    [SerializeField] private TMP_InputField docentQuestion;
    [SerializeField] private GameObject messageImageObject;
    [SerializeField] private GameObject cameraIcon;

    // Start is called before the first frame update
    public void SendDocentToAI()
    {
        RawImage messageImage = messageImageObject.GetComponent<RawImage>();
        Vector3 gps = GpsService.Instance.CurrentPosition;
        string question = docentQuestion.text;
        Texture2D sourceTex = messageImage.texture as Texture2D;

        if (question.Length > 0 && messageImage.texture != null)
        {
            Texture2D docentImage = new Texture2D(sourceTex.width, sourceTex.height, sourceTex.format, false);
            byte[] jpgBytes = docentImage.EncodeToJPG(80);
            string img_url = System.Convert.ToBase64String(jpgBytes);
            DocentData sendDocent = new DocentData(gps, img_url, question);
            NetworkManager.Instance.PostDocent(sendDocent);

            docentQuestion.text = "";
            messageImage.texture = null;
            messageImageObject.SetActive(false);
            cameraIcon.SetActive(true);
        }
    }
}
