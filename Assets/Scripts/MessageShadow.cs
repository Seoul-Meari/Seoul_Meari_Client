using UnityEngine;
using TMPro;

public class MessageShadow : MonoBehaviour
{
    public TMP_Text originalText;
    private TMP_Text shadowText;

    void Start()
    {
        shadowText = Instantiate(originalText, originalText.transform.parent);
        int originalIndex = originalText.transform.GetSiblingIndex();
        shadowText.transform.SetSiblingIndex(originalIndex);


        shadowText.text = originalText.text;
        shadowText.color = new Color(0, 0, 0, 0.5f);
        shadowText.rectTransform.localPosition += new Vector3(0, 0, 1.99f);
    }

    void Update()
    {
        // 원본 텍스트가 바뀌면 그림자도 따라가게
        // shadowText.text = originalText.text;
    }
}