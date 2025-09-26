using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DocentService : MonoBehaviour
{
    [SerializeField] private GameObject questionInputObject;
    [SerializeField] private GameObject picture;
    [SerializeField] private GameObject messageImageObject;
    [SerializeField] private GameObject cameraIcon;
    [SerializeField] private GameObject messageTransferButton;
    [SerializeField] private GameObject messageResetButton;
    [SerializeField] private TextMeshProUGUI questionTMP;
    [SerializeField] private GameObject LoadingObject;
    [SerializeField] private TextMeshProUGUI answerTMP;
    private TMP_InputField questionInput;
    private RawImage messageImage;
    // Start is called before the first frame update

    void Start()
    {
        messageImage = messageImageObject.GetComponent<RawImage>();
        questionInput = questionInputObject.GetComponent<TMP_InputField>();
    }
    public void DocentSendControl()
    {
        SendDocentToAI();
        SetDocentUI();
    }

    private void SendDocentToAI()
    {
        //gps
        Vector3 gps = GpsService.Instance.CurrentPosition;
        //img
        Texture2D sourceTex = messageImage.texture as Texture2D;
        //q
        string question = questionInput.text;

        if (question.Length > 0 && messageImage.texture != null)
        {
            WWWForm docentForm = new WWWForm();
            string gpsString = GpsFormatter.ToGpsString(gps);
            byte[] docentImage = sourceTex.EncodeToJPG(80);

            docentForm.AddBinaryData("img_file", docentImage, "photo.jpg", "image/jpg");
            docentForm.AddField("gps_data", gpsString);
            docentForm.AddField("question", question);

            DocentRes docentResponse;
            messageTransferButton.SetActive(false);
            LoadingObject.SetActive(true);
            NetworkManager.Instance.PostDocent(
                docentForm,
                onSuccess: docentNetworkResponse =>
                {
                    LoadingObject.SetActive(false);
                    messageResetButton.SetActive(true);
                    docentResponse = docentNetworkResponse;
                    answerTMP.text = docentResponse.answer;
                },
                onError: err =>
                {
                    LoadingObject.SetActive(false);
                    messageResetButton.SetActive(true);
                    Debug.Log("Docent Error" + err);
                }
            );
        }
    }

    private void SetDocentUI()
    {
        picture.SetActive(false);
        questionInputObject.SetActive(false);
        questionTMP.text = questionInput.text;
        questionInput.text = "";
    }

    public void ResetMessage()
    {
        messageImage.texture = null;
        messageResetButton.SetActive(false);
        messageImageObject.SetActive(false);
        questionTMP.text = "";
        answerTMP.text = "";
        picture.SetActive(true);
        questionInputObject.SetActive(true);
        messageTransferButton.SetActive(true);
        cameraIcon.SetActive(true);
    }
}
//