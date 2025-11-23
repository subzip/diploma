using UnityEngine;
using UnityEngine.SceneManagement;

public class StartManager : MonoBehaviour
{
    public void Continue()
    {
       SceneManager.LoadScene("level0");
    }

    public void Exit()
    {
        Application.Quit();
    }
}
