using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkConnectUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField ipInputField;
    public Button hostButton;
    public Button clientConnectButton;
    public TextMeshProUGUI statusText;

    private void Start()
    {
        hostButton.onClick.AddListener(OnHostClicked);
        clientConnectButton.onClick.AddListener(OnClientConnectClicked);
    }

    private void OnHostClicked()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            statusText.text = "Already hosting or connected.";
            return;
        }

        NetworkManager.Singleton.StartHost();
        statusText.text = "Hosting started!";
    }

    private void OnClientConnectClicked()
    {
        if (NetworkManager.Singleton.IsListening)
        {
            statusText.text = "Already connected or hosting.";
            return;
        }

        string ip = ipInputField.text.Trim();

        if (string.IsNullOrEmpty(ip))
        {
            statusText.text = "Please enter a valid IP address.";
            return;
        }

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, 7777);  // Make sure port matches your host

        statusText.text = $"Connecting to {ip}...";
        NetworkManager.Singleton.StartClient();
    }
}