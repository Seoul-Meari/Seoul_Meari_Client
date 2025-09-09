using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessagePanelController : MonoBehaviour
{
    // Start is called before the first frame update
    bool isPanelShow = false;
    public GameObject MessagePanel;
    public GameObject MessageCreateButton;

    public void TogglePanel()
    {
        if (isPanelShow)
        {
            isPanelShow = false;
            MessagePanel.SetActive(false);
            MessageCreateButton.SetActive(true);
        }
        else
        {
            isPanelShow = true;
            MessagePanel.SetActive(true);
            MessageCreateButton.SetActive(false);
        }
    }
}
