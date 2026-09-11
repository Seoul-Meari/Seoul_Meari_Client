using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneChangeButton : MonoBehaviour
{
    public string sceneName;

    public void ChangeScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}