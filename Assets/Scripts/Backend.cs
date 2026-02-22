using NativeWebSocket;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class Backend : MonoBehaviour
{
    public delegate void ReceivedMessageForGameControllerDelegate(string s, string t, string[] p);
    public ReceivedMessageForGameControllerDelegate ReceivedMessageForGameController;

    public string m_apiGatewayUrl = "https://6yn7am95w0.execute-api.us-west-2.amazonaws.com/dev/";
    public string m_serverUrl = "localhost"; //"18.237.4.137";
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

    public static Backend Instance { get; private set; }


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



    public async System.Threading.Tasks.Task StartWebSocketConnection()
    {
        if (m_connected) await m_webSocket.Close();

        m_webSocket = new WebSocket($"ws://{m_serverUrl}:5000");

        m_webSocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
            m_connected = true;
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
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            if (!message.Contains("Ping"))
                Debug.Log("OnMessage! " + message);
            ReceivedMessage(message);
        };

        Debug.Log($"Trying to connect to websocket at {m_serverUrl}");
        // waiting for messages
        await m_webSocket.Connect();
    }

    public void RequestNewServer(Action fn = null)
    {
        StartCoroutine(RequestNewServerCoroutine(fn));
    }

    public IEnumerator RequestNewServerCoroutine(Action fn = null)
    {
        Debug.Log($"Requesting new server at {m_apiGatewayUrl}/create_server");
        using (UnityWebRequest serverRequest = UnityWebRequest.Get(m_apiGatewayUrl + "/create_server"))
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
                    m_serverUrl = "Bad Result";
                    break;
                case UnityWebRequest.Result.Success:
                    var data = JsonUtility.FromJson<JsonClassSingle>(serverRequest.downloadHandler.text);
                    m_serverUrl = data.body;
                    //m_serverUrl = serverRequest.downloadHandler.text;
                    Debug.Log($"RequestNewServer SUCCESS: {(m_serverUrl)}");
                    break;
            }
        }
        if (fn != null)
            fn.Invoke();
        Debug.Log($"Request finished");
    }


    public void RequestListOfServers(Action<string[]> callbackFn)
    {
        StartCoroutine(RequestListOfServersCoroutine(callbackFn));
    }

    public IEnumerator RequestListOfServersCoroutine(Action<string[]> callbackFn)
    {
        Debug.Log($"Requesting all existing servers at {m_apiGatewayUrl}/find_servers");
        using (UnityWebRequest serverRequest = UnityWebRequest.Get(m_apiGatewayUrl + "/find_servers"))
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
                    Debug.Log($"RequestListOfServers SUCCESS: {(m_serverUrl)}, {data.body.Length}");
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
            ;
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


        Debug.Log($"Requesting kill server at {m_apiGatewayUrl}/KillGame");
        using (UnityWebRequest serverRequest = UnityWebRequest.Get(m_apiGatewayUrl + "/KillGame"))
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
                    Debug.Log($"RequestListOfServers SUCCESS: {(m_serverUrl)}");
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

        return boardAsString.Substring(0, boardAsString.Length-1); // Remove the trailing "|"
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

    public void ReceivedMessage(string raw_data)
    {
        string data = raw_data.Substring(1, raw_data.Length - 2);
        string[] playerData = data.Split(',');
        string action = playerData.Length > 0 ? playerData[0] : "Disconnect";
        if (!action.Contains("Ping"))
            Debug.Log($"Received: {data} with action {action}, playerData.Length: {playerData.Length}");

        switch (action)
        {
            case "Disconnect":
                SendServerDataToGameController(data, action, playerData);
                CancelConnection();
                break;
            case "Init":
            case "Player_1_Moved":
            case "Board_Update":
            case "Player_Swapped":
            case "Player_1_Turn":
                SendServerDataToGameController(data, action, playerData);
                break;
            case "Ping":
                ServerPing();
                break;
            default:
                Debug.LogWarning($"Unhandled action at backend: {action}");
                break;
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

    public void SetServerUrl(string url)
    {
        m_serverUrl = url;
    }

    public void PlayOnLocalServer()
    {
        m_serverUrl = "localhost";
        StartWebSocketConnection();
    }
}
