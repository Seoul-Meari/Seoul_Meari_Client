using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssetStoreController : MonoBehaviour
{
    [SerializeField] private GameObject OpenAsset;
    [SerializeField] private GameObject AssetStore;
    [SerializeField] private AssetListUI AssetListUI;

    private bool isOpen = false;
    // Start is called before the first frame update

    public void OnClickOpen()
    {
        if (isOpen == false)
        {
            OpenAsset.SetActive(false);
            AssetStore.SetActive(true);
            isOpen = true;

            NetworkManager.Instance.GetAssetMetaData(
                ConfigProvider.Os,
                onSuccess: items =>
                {
                    AssetListUI.Render(items);
                },
                onError: err =>
                {

                }
            );
        }
    }

    public void OnClickClose()
    {
        OpenAsset.SetActive(true);
        AssetStore.SetActive(false);
        isOpen = false;
    }
}
