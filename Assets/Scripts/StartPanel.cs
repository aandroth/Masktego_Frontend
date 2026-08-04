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
    [SerializeField] GameObject m_waitingPanel;
    [SerializeField] GameObject m_waitingForWeb;
    [SerializeField] GameObject m_requestingServer;
    [SerializeField] GameObject m_joiningServer;
    [SerializeField] GameObject m_waitingForOtherPlayer;

    public delegate void ChangeGameModeDelegate(PLAY_MODE playMode);
    public ChangeGameModeDelegate m_changeGameMode;

    public static StartPanel Instance { get; private set; }

    void Awake()
    {
        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this)
            Destroy(this);
        else
        {
            Instance = this;
        }

        if (m_backend == null)
        {
            m_backend = Backend.Instance;

            if (m_backend == null)
            {
                Debug.Log($"No backend found");
            }
        }
    }

    public void OnEnable()
    {
        if(m_backend) 
        {
            m_backend.ReceivedMessageForStartPanel += ReceiveMessageFromBackend;
        }   
    }

    public void OnDisable()
    {
        if(m_backend) 
        {
            m_backend.ReceivedMessageForStartPanel -= ReceiveMessageFromBackend;
        }   
    }


    public void ReceiveMessageFromBackend(string action, string message = "")
    {
        switch (action)
        {
            case "server_list":
                ParseServerList(message.Split(','));
                break;
            default:
                Debug.Log($"Unknown action: {action}");
                break;
        }
    }


    public async void CallBackendForNewServer()
    {
        await m_backend?.StartWebSocketConnection();
        if(m_backend.m_connected)
        {
            m_backend.SendMessage("create_server");
        }
    }

    public async void StartSoloPlayMode()
    {
        await m_backend?.StartWebSocketConnection("localhost");
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
        var startPanelObjects = GameObject.FindObjectsByType<StartPanel>(FindObjectsSortMode.None);
        Debug.Log($"StartPanels found: {startPanelObjects.Length}");

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
            string serverName = servers[i];
            GameObject newButton = Instantiate(m_buttonPrefab, m_buttonParent?.transform);
            newButton.GetComponent<ServerButton>().AssignButtonParameters($"{serverName}", m_backend.SetServerName, () => CallBackendToJoinGame($"{serverName}"));
            newButton.GetComponentInChildren<TMP_Text>().text = serverName;
            m_ipButtonsList.Add(newButton);
        }
    }

    public void CallBackendToJoinGame(string serverName)
    {
        m_changeGameMode(PLAY_MODE.ONLINE);
        _ = m_backend?.JoinServer(serverName);
    }

    //public void CreateHostIpAddressButton()
    //{
    //    if (m_backend == null)
    //    {
    //        Debug.Log($"No backend found");
    //        return;
    //    }
    //    GameObject hostButton = Instantiate(m_buttonPrefab, m_buttonParent?.transform);
    //    hostButton.GetComponent<ServerButton>().AssignButtonParameters("localhost", m_backend.SetServerName, CallBackendForServerConnect);
    //    m_ipButtonsList.Insert(0, hostButton);
    //}

    public void DestroyButtonsInIpAddressPanel()
    {
        while(m_ipButtonsList.Count > 0)
        {
            Destroy(m_ipButtonsList[0]);
            m_ipButtonsList.RemoveAt(0);
        }
    }
}
