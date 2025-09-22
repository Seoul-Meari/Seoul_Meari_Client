using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelController : MonoBehaviour
{
    // Start is called before the first frame update
    bool isPanelShow = false;
    [SerializeField] private GameObject Panel;
    [SerializeField] private GameObject MessageCreateButton;
    [SerializeField] private GameObject DocentButton;

    public void TogglePanel()
    {
        if (isPanelShow)
        {
            isPanelShow = false;
            Panel.SetActive(false);
            MessageCreateButton.SetActive(true);
            DocentButton.SetActive(true);
        }
        else
        {
            isPanelShow = true;
            Panel.SetActive(true);
            MessageCreateButton.SetActive(false);
            DocentButton.SetActive(false);
        }
    }
}
