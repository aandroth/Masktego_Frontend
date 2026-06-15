using NativeWebSocket;
using System;
using System.Collections;
using System.Net.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using static GameController;

public class Backend : MonoBehaviour
{
    public enum BACKEND_STATE    {
        SOLO,
        STARTING_SERVER,
        JOINING_SERVER
    }
    public BACKEND_STATE state = BACKEND_STATE.STARTING_SERVER;

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
        await StartWebSocketConnection(m_apiWebsocketUrl);
    }

    public async System.Threading.Tasks.Task StartWebSocketConnection(string apiWebsocketUrl = "localhost")
    {
        Debug.Log($"Websocket created with url {apiWebsocketUrl}");
        if (m_connected) await m_webSocket.Close();

        //m_webSocket = new WebSocket(fullUrl);
        m_webSocket = new WebSocket(apiWebsocketUrl);
        Debug.Log($"Websocket created with url {apiWebsocketUrl}");

        m_webSocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
            m_connected = true;
            Debug.Log("Wait for WebSocket connection is over.");

            if (m_connected)
            {
                m_waitingForWeb.SetActive(false);
                _ = SendMessageToWebsocket("on_connection", "on_connection");
            }
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
                    ReceivedMessage_ForConnecting(messageStruct.action, messageStruct.message);
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

        var timeOut = 50000; // 5 seconds
        while (!m_connected && timeOut > 0)
        {
            await Awaitable.WaitForSecondsAsync(1f);
            timeOut -= 1000;
        }
        Debug.Log("Wait for WebSocket connection is over.");

        if (m_connected)
        {
            m_connected = true;
            m_waitingForWeb.SetActive(false);
            await SendMessageToWebsocket("on_connection", "on_connection");
            //m_requestingServer.SetActive(true);
            //_ = RequestNewServer();
        }
    }
    public void ReceivedMessage_ForConnecting(string action, string message = "")
    {
        Debug.Log("ReceivedMessage_ForConnecting");
        Debug.Log($"Action: {action}, Message: {message}");
        switch (action)
        {
            case "on_connection": // Websocket connected
                m_waitingForWeb.SetActive(false);
                switch (state)
                {
                    case BACKEND_STATE.STARTING_SERVER:
                        Debug.Log($"Received on_connection message, so player requesting server");
                        m_requestingServer.SetActive(true);
                        _ = RequestNewServer();
                        break;
                    case BACKEND_STATE.JOINING_SERVER:
                        Debug.Log($"Received on_connection message, so player finding servers");
                        // Show server options
                        break;
                    case BACKEND_STATE.SOLO:
                        Debug.Log($"Received on_connection message, so connected to solo server");
                        break;
                }
                break;
            case "server_name": // Server was created, player joined, and serverName was sent back to us
                Debug.Log($"Server name set to {message}");
                m_joiningServer.SetActive(false);
                m_waitingForOtherPlayer.SetActive(true);
                // Show panel with serverName and waiting for other player message
                break;
            case "Init": // Put aside all connecting panels
                Debug.Log($"Server name set to {message}");
                m_waitingForOtherPlayer.SetActive(false);
                m_waitingPanel.SetActive(false);
                break;
            case "game_over":
                Debug.Log($"game_over, so server_ended");
                // Clean up server data
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



    public void SendMessageToStartPanel(string message)
    {
        if (ReceivedMessageForStartPanel != null)
        {
            ReceivedMessageForStartPanel.Invoke(message, "");
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

    public async System.Threading.Tasks.Task RequestNewServer()
    {
        Debug.Log($"Requesting new server");
        if (!m_connected)
        {
            Debug.LogError($"Not connected to backend, cannot request new server");
            return;
        }
        Debug.Log($"Requesting new server");
        _ = SendMessageToWebsocket("start_server", "start_server");
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


    public void RequestListOfServers(Action<string[]> callbackFn)
    {
        StartCoroutine(RequestListOfServersCoroutine(callbackFn));
    }

    public IEnumerator RequestListOfServersCoroutine(Action<string[]> callbackFn)
    {
        Debug.Log($"Requesting all existing servers at {m_apiWebsocketUrl}/find_servers");
        using (UnityWebRequest serverRequest = UnityWebRequest.Get(m_apiWebsocketUrl + "/find_servers"))
        {
            Debug.Log($"Request made");
            yield return serverRequest.SendWebRequest();
            string errorString = "There was an error? Of course there was an error. Why couldn't it just work!?\n- you, probably";
            switch (serverRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.Log(errorString);
                    Debug.Log(serverRequest.result);
                    callbackFn(new string[] { "localhost" });
                    break;
                case UnityWebRequest.Result.Success:
                    var data = JsonUtility.FromJson<JsonClassList>(serverRequest.downloadHandler.text);
                    Debug.Log($"RequestListOfServers SUCCESS: {(m_serverName)}, {data.body.Length}");
                    callbackFn(data.body);
                    break;
            }
        }
        Debug.Log($"Request finished");
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
        var bytes = System.Text.Encoding.UTF8.GetBytes(pingRequest);
        m_webSocket?.Send(bytes);
    }

    public void SignalPlayerMovedToServer(int playerId, int[][] board)
    {
        if (this == Instance && m_connected)
        {
            //"Action, boardAsString
            //      0,             1
            string readyToServer = $"Player_{playerId}_Moved,{ConvertBoardToString(board)}";

            var bytes = System.Text.Encoding.UTF8.GetBytes(readyToServer);
            m_webSocket?.Send(bytes);
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

            var bytes = System.Text.Encoding.UTF8.GetBytes(readyToServer);
            m_webSocket?.Send(bytes);
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

            var bytes = System.Text.Encoding.UTF8.GetBytes(readyToServer);
            m_webSocket?.Send(bytes);
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

            var bytes = System.Text.Encoding.UTF8.GetBytes(startGameRequest);
            m_webSocket?.Send(bytes);
            Debug.Log($"Request finished");
        }
    }


    public void RequestKillServer()
    {
        if (this == Instance && m_connected)
        {
            StartCoroutine(RequestKillServerCoroutine());
        }
    }

    public IEnumerator RequestKillServerCoroutine()
    {
        if (this == Instance && m_connected)
        {
            string startGameRequest = $"Kill_Game,";

            var bytes = System.Text.Encoding.UTF8.GetBytes(startGameRequest);
            m_webSocket?.Send(bytes);
            Debug.Log($"Request finished");
        }


        Debug.Log($"Requesting kill server at {m_apiWebsocketUrl}/KillGame");
        using (UnityWebRequest serverRequest = UnityWebRequest.Get(m_apiWebsocketUrl + "/KillGame"))
        {
            Debug.Log($"Request made");
            yield return serverRequest.SendWebRequest();
            string errorString = "There was an error? Of course there was an error. Why couldn't it just work!?\n- you, probably";
            switch (serverRequest.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                case UnityWebRequest.Result.ProtocolError:
                    Debug.Log(errorString);
                    Debug.Log(serverRequest.result);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log($"RequestListOfServers SUCCESS: {(m_serverName)}");
                    break;
            }
        }
        Debug.Log($"Request finished");
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
