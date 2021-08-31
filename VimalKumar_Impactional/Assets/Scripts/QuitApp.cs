using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class ControlFlow : MonoBehaviour
{
    public void quitApp()
    {
        Application.Quit();
    }

    public void loginScene()
    {
        SceneManager.LoadScene("LoginScene");
        Debug.Log("Changes Login Scene");
    }

}
