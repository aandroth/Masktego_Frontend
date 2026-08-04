using NativeWebSocket;
using System;
using System.Collections;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using static GameController;

public class Backend : MonoBehaviour
{
    public enum BACKEND_STATE    {
        NONE,
        SOLO,
        JUST_CONNECTED,
        RESERVING_SERVER,
        STARTING_SERVER,
        JOINING_SERVER
    }
    public BACKEND_STATE state = BACKEND_STATE.NONE;

    public delegate void ReceivedMessageForGameControllerDelegate(string s, string t, string[] p);
    public ReceivedMessageForGameControllerDelegate ReceivedMessageForGameController;
    public delegate void ReceivedMessageForStartPanelDelegate(string s, string t = "");
    public ReceivedMessageForStartPanelDelegate ReceivedMessageForStartPanel;

    public string m_serverName = "";
    public string m_apiWebsocketUrl = "wss://u88pgzj3th.execute-api.us-west-2.amazonaws.com/dev/";
    public string[] m_serverUrlList = new string[0];
    public WebSocket m_webSocket;
    public int m_webSocketConnectionAttemptsToTry = 3;
    public float m_intervalTimeCurr = 0f;
    public float m_intervalTime = 0.3f;
    public bool m_gameInProgress = false;
    public bool m_connected = false;
    public string urlResult;
    private int pings;
    private float pingTiming = 0, pingTimePrev = 0;
    [SerializeField] TMP_Text pingText;

    [SerializeField] GameObject m_waitingPanel;
    [SerializeField] GameObject m_waitingForWeb;
    [SerializeField] GameObject m_requestingServer;
    [SerializeField] GameObject m_joiningServer;
    [SerializeField] GameObject m_waitingForOtherPlayer;
    [SerializeField] GameObject m_serverStarting;
    [SerializeField] GameObject m_failedToConnect;
    [SerializeField] TMP_Text m_serverNameTMP_Text;

    public static Backend Instance { get; private set; }

    public struct MessageStruct
    {
        public string route;
        public string action;
        public string msgType;
        public string message;
    }

    private void Awake()
    {
        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this)
            Destroy(this);
        else
        {
            Instance = this;
        }
    }

    public class JsonClassList
    {
        public string statusCode;
        public string[] body;
    }
    public class JsonClassSingle
    {
        public string statusCode;
        public string body;
    }
    public async void StartServerSequence()
    {
        Debug.Log($"Starting server sequence");
        m_waitingPanel.SetActive(true);
        // Display connecting to websocket message
        m_waitingForWeb.SetActive(true);
        Debug.Log($"StartServerSequence: State = {state}");
        state = BACKEND_STATE.RESERVING_SERVER;
        await StartWebSocketConnection(m_apiWebsocketUrl);
        if (m_connected)
        {
            Debug.Log($"StartServerSequence: State = {state}");
            m_requestingServer.SetActive(true);
        }
        else
        {
            Debug.Log($"Websocket NOT connected, requesting new server failed. Setting state to NONE.");
            state = BACKEND_STATE.NONE;
            m_waitingPanel.SetActive(true);
            m_failedToConnect.SetActive(true);
        }
    }
    public async void JoinServerSequence()
    {
        Debug.Log($"Joining server sequence");
        m_waitingPanel.SetActive(true);
        // Display connecting to websocket message
        m_waitingForWeb.SetActive(true);
        state = BACKEND_STATE.JOINING_SERVER;
        await StartWebSocketConnection(m_apiWebsocketUrl);
        if (m_connected)
        {
            Debug.Log($"JoinServerSequence: State = {state}");
            m_waitingPanel.SetActive(true);
            m_waitingForWeb.SetActive(true);
        }
        else
        {
            Debug.Log($"Websocket NOT connected, joining server failed. Setting state to NONE.");
            state = BACKEND_STATE.NONE;
            m_waitingPanel.SetActive(true);
            m_failedToConnect.SetActive(true);
        }
    }

    public async System.Threading.Tasks.Task StartWebSocketConnection(string apiWebsocketUrl = "localhost")
    {
        Debug.Log($"Websocket about to be created with url {apiWebsocketUrl}");
        if (m_connected) await m_webSocket.Close();

        //m_webSocket = new WebSocket(fullUrl);
        m_webSocket = new WebSocket(apiWebsocketUrl);
        Debug.Log($"Websocket created with url {apiWebsocketUrl}");

        m_webSocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
            m_connected = true;
            Debug.Log("Wait for WebSocket connection is over.");

            m_connected = true;
            m_waitingForWeb.SetActive(false);
            _ = SendMessageToWebsocket("on_connection", "on_connection");
        };

        m_webSocket.OnError += (e) =>
        {
            Debug.Log("Connection error! " + e.ToString());
            Debug.Log("Error! " + e.ToString());
        };

        m_webSocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed! " + e);
            m_connected = false;
            CancelConnection();
            SendServerDataToGameController("Disconnect", "Disconnect", new string[] { "Disconnect" });
        };

        m_webSocket.OnMessage += (bytes) =>
        {
            // getting the message as a string
            var messageStructRaw = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log($"Received message: {messageStructRaw}");
            MessageStruct messageStruct = JsonUtility.FromJson<MessageStruct>(messageStructRaw);
            Debug.Log($"Received msgType: {messageStruct.msgType}, Action: {messageStruct.action}, Message: {messageStruct.message}");
            switch (messageStruct.msgType)
            {
                case "connection":
                    _ = ReceivedMessage_ForConnecting(messageStruct.action, messageStruct.message);
                    break;
                case "gameController":
                    ReceivedMessage_ForGameController(messageStruct.action, messageStruct.message);
                    break;
                default:
                    Debug.Log($"Unknown msgType: {messageStruct.msgType}");
                    break;
            }
        };

        Debug.Log($"Trying to connect to websocket at {m_apiWebsocketUrl}");
        // waiting for messages

        await m_webSocket.Connect();

        float timeToWait = 5f; // seconds
        while (m_webSocket.State == WebSocketState.Connecting && timeToWait > 0)
        {
            Debug.Log("Waiting for websocket to connect...");
            await System.Threading.Tasks.Task.Delay(100);
            timeToWait -= 0.1f;
        }

        if (m_webSocket.State == WebSocketState.Open)
        {
            Debug.Log("Websocket connected successfully.");
            m_connected = true;
        }
        else
        {
            Debug.Log("Websocket NOT connected, requesting new server failed. Setting state to NONE.");
            state = BACKEND_STATE.NONE;
        }
    }

    public async Task ReceivedMessage_ForConnecting(string action, string message = "")
    {
        Debug.Log("ReceivedMessage_ForConnecting");
        Debug.Log($"Action: {action}, Message: {message}");
        switch (action)
        {
            case "on_connection": // Websocket connected
                m_waitingForWeb.SetActive(false);
                switch (state)
                {
                    case BACKEND_STATE.RESERVING_SERVER:
                        Debug.Log($"Received on_connection message, player reserving server");
                        m_requestingServer.SetActive(true);
                        await ReserveNewServer();
                        break;
                    case BACKEND_STATE.JOINING_SERVER:
                        Debug.Log($"Received on_connection message, so player finding servers");
                        _ = SendMessageToWebsocket("list_open_servers", "list_open_servers");
                        break;
                    case BACKEND_STATE.SOLO:
                        Debug.Log($"Received on_connection message, so connected to solo server");
                        break;
                    default:
                        Debug.Log($"Received on_connection message, but state is {state}, so not doing anything");
                        break;
                }
                break;
            case "reserved_server_name": // Server was reserved
                Debug.Log($"Server name set to {message}");
                m_requestingServer.SetActive(false);
                m_waitingForOtherPlayer.SetActive(true);
                m_serverNameTMP_Text.text = message;
                break;


            case "joined_new_server": // player joined, and serverName was sent back to us
                m_waitingForOtherPlayer.SetActive(false);
                m_requestingServer.SetActive(true);
                break;
            case "other_joined": // player joined, and serverName was sent back to us
                m_waitingForOtherPlayer.SetActive(false);
                m_requestingServer.SetActive(true);
                _ = RequestNewServer();
                break;
            case "joined_existing_server": // player joined server, and serverName was sent back to us
                m_waitingForOtherPlayer.SetActive(false);
                m_requestingServer.SetActive(true);
                break;
            case "server_list":
                Debug.Log($"Got serverList: {message}");
                m_waitingPanel.SetActive(false);
                m_waitingForWeb.SetActive(false);
                SendMessageToStartPanel("server_list", message);
                break;
            case "starting_server":
                Debug.Log($"Got server starting message: {message}");
                if (!m_waitingPanel.activeSelf) m_waitingPanel.SetActive(true);
                if(m_waitingForOtherPlayer.activeSelf) m_waitingForOtherPlayer.SetActive(false);
                m_serverStarting.SetActive(true);
                state = BACKEND_STATE.STARTING_SERVER;
                break;
            case "ping": // Ping back to caller
                Debug.Log($"Received ping message: {message}");
                _ = SendMessageToWebsocket("ping", "ping");
                break;
            case "Init": // Put aside all connecting panels
                Debug.Log($"Server has initialized: {message}");
                m_waitingForOtherPlayer.SetActive(false);
                m_waitingPanel.SetActive(false);
                break;
            case "game_over":
                Debug.Log($"game_over, so server_ended");
                // Clean up server data
                break;
            default:
                Debug.Log($"ERROR: Unhandled action at backend: {action}");
                break;
        }
    }

    public void ReceivedMessage_ForGameController(string action, string message)
    {
        string[] playerData = message.Split(',');

        switch (action)
        {
            case "Disconnect":
                SendServerDataToGameController(message, action, playerData);
                CancelConnection();
                break;
            case "Init":
                m_waitingForOtherPlayer.SetActive(false);
                m_waitingPanel.SetActive(false);
                SendServerDataToGameController(message, action, playerData);
                break;
            case "Player_1_Moved":
            case "Board_Update":
            case "Player_Swapped":
            case "Player_1_Turn":
                SendServerDataToGameController(message, action, playerData);
                break;
            case "Ping":
                ServerPing();
                break;
            default:
                Debug.LogWarning($"Unhandled action at backend: {action}");
                break;
        }
    }



    public void SendMessageToStartPanel(string action, string message = "")
    {
        if (ReceivedMessageForStartPanel != null)
        {
            ReceivedMessageForStartPanel.Invoke(action, message);
            Debug.Log("Sent message to StartPanel!");
        }
        else
        {
            Debug.Log("ReceivedMessageForStartPanel is null");
        }
    }

    public async System.Threading.Tasks.Task PingUrl(string url)
    {
        Debug.Log("Performing a ping at " + url);
        UnityWebRequest www = UnityWebRequest.Get($"https://{url}");
        await www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Show results as text
            Debug.Log("Performed a ping at " + url + ", with result " + www.downloadHandler.text);

            // Or retrieve results as binary data
            byte[] results = www.downloadHandler.data;
        }
    }

    public async System.Threading.Tasks.Task ReserveNewServer()
    {
        Debug.Log($"Reserving new server");
        if (!m_connected)
        {
            Debug.LogError($"Not connected to backend, cannot reserve new server");
            return;
        }
        Debug.Log($"Sending message: Reserving new server");
        await SendMessageToWebsocket("reserve_server", "reserve_server");
    }

    public async System.Threading.Tasks.Task RequestNewServer()
    {
        Debug.Log($"Requesting new server");
        if (!m_connected)
        {
            Debug.LogError($"Not connected to backend, cannot request new server");
            return;
        }
        Debug.Log($"Requesting new server");
        await SendMessageToWebsocket("start_server", "start_server");
    }

    public async System.Threading.Tasks.Task JoinServer(string serverName)
    {
        Debug.Log($"Joining server: {serverName}");
        if (!m_connected)
        {
            Debug.LogError($"Not connected to backend, cannot join server");
            m_waitingPanel.SetActive(true);
            m_failedToConnect.SetActive(true);
            return;
        }
        Debug.Log($"Joining server: {serverName}");
        await SendMessageToWebsocket("join_server", serverName);

    }

    public async System.Threading.Tasks.Task SendMessageToWebsocket(string msgType, string message)
    {
        Debug.Log($"Sending message: {message}");
        MessageStruct messageStruct = new MessageStruct
        {
            route = "message",
            action = "message",
            msgType = msgType,
            message = message
        };
        string msgRequest = JsonUtility.ToJson(messageStruct);
        var bytes = System.Text.Encoding.UTF8.GetBytes(msgRequest);
        if(!m_connected || m_webSocket == null)
        {
            Debug.LogError("Cannot send message, not connected or websocket is null");
            return;
        }
        Debug.Log($"Sending: {msgRequest}");
        await m_webSocket?.SendText(msgRequest);
    }
    public async void SendSingleMessageToWebsocket(string message)
    {
        await SendMessageToWebsocket(message, message);
    }


    public async void RequestListOfServers(Action<string[]> callbackFn)
    {
        await SendMessageToWebsocket("request_list_of_servers", "request_list_of_servers");
    }


    public void ServerPing()
    {
        if (pingText == null) return;

        pingTiming += (float)Time.timeAsDouble - pingTimePrev;
        pingTimePrev = (float)Time.timeAsDouble;
        ++pings;
        if (pingTiming > 1)
        {
            pingText.text = $"{pings}";
            pingTiming = 0;
            pings = 0;
        }
    }
    public void PingToServer()
    {
        string pingRequest = $"Ping";
        _ = SendMessageToWebsocket("Ping", "Ping");
    }

    public void SignalPlayerMovedToServer(int playerId, int[][] board)
    {
        if (this == Instance && m_connected)
        {
            //"Action, boardAsString
            //      0,             1
            string readyToServer = $"Player_{playerId}_Moved,{ConvertBoardToString(board)}";

            _ = SendMessageToWebsocket("server_message", readyToServer);
            Debug.Log($"Request finished");
        }
    }

    public void SendBoardChangeToServer(int playerId, Vector2Int pos0, Vector2Int pos1)
    {
        if (this == Instance && m_connected)
        {
            //"Action, playerId, pos0.x | pos0.y, pos1.x | pos1.y
            //      0,        1,               2,               3
            string readyToServer = $"Board_Update,{playerId},{pos0.x}|{pos0.y},{pos1.x}|{pos1.y}";


            _ = SendMessageToWebsocket("server_message", readyToServer);
            Debug.Log($"Request finished");
        }
    }

    public void SendSwapToServer(int playerId, Vector2Int pos0, Vector2Int pos1)
    {
        if (this == Instance && m_connected)
        {
            //"Action, playerId, pos0.x | pos0.y, pos1.x | pos1.y
            //      0,        1,               2,               3
            string readyToServer = $"Player_Swapped,{playerId},{pos0.x}|{pos0.y},{pos1.x}|{pos1.y}";


            _ = SendMessageToWebsocket("server_message", readyToServer);
            Debug.Log($"Request finished");
        }
    }

    public void RequestServerToStartGame()
    {
        if (this == Instance && m_connected)
        {
            //"Action, id
            //      0,  1
            string startGameRequest = $"Start_Game,";

            _ = SendMessageToWebsocket("server_message", startGameRequest);
            Debug.Log($"Request finished");
        }
    }


    public void RequestKillServer()
    {
        if (this == Instance && m_connected)
        {
            //"Action, id
            //      0,  1
            string killGameRequest = $"Kill_Game,";

            _ = SendMessageToWebsocket("server_message", killGameRequest);
            Debug.Log($"Request finished");
        }
    }


    public string ConvertBoardToString(int[][] board)
    {
        string boardAsString = string.Empty;
        for (int i = 0; i < board.Length; i++)
        {
            boardAsString += string.Join('|', board[i]);
            boardAsString += "|";
        }

        return boardAsString.Substring(0, boardAsString.Length - 1); // Remove the trailing "|"
    }

    public void OnDestroy()
    {
        CancelConnection();
    }

    public void ReturnUrlResult(string url = "localhost:3000/hello")
    {
        Debug.Log("ReturnUrlResult");
        StartCoroutine(ReturnUrlResultCoroutine(url));
    }

    IEnumerator ReturnUrlResultCoroutine(string url)
    {
        Debug.Log($"ReturnUrlResultCoroutine to url {url}");
        UnityWebRequest uwr = UnityWebRequest.Get(url);
        yield return uwr.SendWebRequest();

        if (uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error While Sending: " + uwr.error);
            urlResult = uwr.error;
        }
        else
        {
            Debug.Log("Received: " + uwr.downloadHandler.text);
            urlResult = uwr.downloadHandler.text;
        }
        //GetTextData.Invoke(urlResult);
    }

    public void CancelConnection()
    {
        if (this == Instance)
        {
            Debug.Log($"CancelConnection called");
            if (m_webSocket != null && this.m_connected)
            {
                m_webSocket?.CancelConnection();
            }
            m_connected = false;
        }
    }

    public void SendServerDataToGameController(string data, string action, string[] playerData)
    {
        if (Instance.ReceivedMessageForGameController == null)
        {
            Debug.LogError($"ReceivedMessageForGameController is null");
            return;
        }

        Instance.ReceivedMessageForGameController(data, action, playerData);
    }


    public void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (m_connected)
            m_webSocket.DispatchMessageQueue();
#endif
        if (m_connected && m_gameInProgress)
        {
            m_intervalTimeCurr += Time.deltaTime;
            if (m_intervalTimeCurr >= m_intervalTime)
            {
                PingToServer();
            }
        }
    }

    public void SetServerName(string url)
    {
        m_serverName = url;
    }

    public void SetWebsocketUrl(string url)
    {
        m_apiWebsocketUrl = "localhost";
    }

    public void SetServerUrlFull(string url)
    {
        m_apiWebsocketUrl = url;
    }

    public void PlayOnLocalServer()
    {
        m_serverName = "localhost";
        _ = StartWebSocketConnection();
    }
}