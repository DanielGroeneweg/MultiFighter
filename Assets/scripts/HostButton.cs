using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

public class HostButton : MonoBehaviour
{
    public TMP_Text ipDisplay;

    public void OnHostButtonClicked()
    {
        StartCoroutine(GetPublicIP());
    }

    IEnumerator GetPublicIP()
    {
        UnityWebRequest www = UnityWebRequest.Get("https://api.ipify.org");
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
            ipDisplay.text = "Your IP: " + www.downloadHandler.text;
        else
            ipDisplay.text = "Failed to get IP";
    }
}
