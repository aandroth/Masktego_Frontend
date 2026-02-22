using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static GameController;

public class StartPanel : MonoBehaviour
{
    [SerializeField] Backend m_backend = null;
    public GameObject m_buttonPrefab;
    public GameObject m_buttonParent;
    public float m_buttonOffset;
    [SerializeField] List<GameObject> m_ipButtonsList = new List<GameObject>();
    [SerializeField] string m_ipAddress;

    public delegate void ChangeGameModeDelegate(PLAY_MODE playMode);
    public ChangeGameModeDelegate m_changeGameMode;

    void Start()
    {
        if (m_backend == null)
        {
            m_backend = Backend.Instance;

            if (m_backend == null)
            {
                Debug.Log($"No backend found");
            }
        }
    }
    public void CallBackendForNewServer()
    {
        m_backend?.RequestNewServer();
    }

    public async void StartSoloPlayMode()
    {
        m_backend?.SetServerUrl("localhost");
        await m_backend?.StartWebSocketConnection();
    }

    public async void CallBackendForLocalServerConnect()
    {
        m_backend?.SetServerUrl("localhost");
        await m_backend?.StartWebSocketConnection();
    }

    public async void CallBackendForServerConnect()
    {
        m_changeGameMode(PLAY_MODE.ONLINE);
        await m_backend?.StartWebSocketConnection();
        gameObject.SetActive(false);
    }

    public void CallBackendForServerDisconnect()
    {
        Debug.Log($"Disconnect called");
        m_backend?.CancelConnection();
        gameObject.SetActive(false);
    }
    public void CallBackendForServerKill()
    {
        Debug.Log($"Kill called");
        m_backend?.RequestKillServer();
        CallBackendForServerOptions();
    }

    public void CallBackendForServerOptions()
    {
        Debug.Log($"Find Servers called");
        m_backend?.RequestListOfServers(ParseServerList);
    }

    public void ParseServerList(string[] serverListResult)
    {
        DestroyButtonsInIpAddressPanel();
        if (serverListResult.Length > 0)
        {
            CreateButtonsInIpAddressPanel(serverListResult);
        }
    }

    public void CreateButtonsInIpAddressPanel(string[] servers)
    {
        if (m_backend == null)
        {
            Debug.Log($"No backend found");
            return;
        }
        for (int i = 0; i < servers.Length; ++i)
        {
            GameObject newButton = Instantiate(m_buttonPrefab, m_buttonParent?.transform);
            newButton.GetComponent<ServerButton>().AssignButtonParameters(servers[i], m_backend.SetServerUrl, CallBackendForServerConnect);
            newButton.GetComponentInChildren<TMP_Text>().text = servers[i];
            m_ipButtonsList.Add(newButton);
        }
    }

    public void CreateHostIpAddressButton()
    {
        if (m_backend == null)
        {
            Debug.Log($"No backend found");
            return;
        }
        GameObject hostButton = Instantiate(m_buttonPrefab, m_buttonParent?.transform);
        hostButton.GetComponent<ServerButton>().AssignButtonParameters("localhost", m_backend.SetServerUrl, CallBackendForServerConnect);
        m_ipButtonsList.Insert(0, hostButton);

    }

    public void DestroyButtonsInIpAddressPanel()
    {
        while(m_ipButtonsList.Count > 0)
        {
            Destroy(m_ipButtonsList[0]);
            m_ipButtonsList.RemoveAt(0);
        }
    }
}
