using UnityEngine;

public class DEV_Testing : MonoBehaviour
{
    [SerializeField] string[] m_serverResponseArray = new string[0];
    [SerializeField] string m_nextServerResponse = "";
    [SerializeField] int m_serverResponseIndex = 0;
    [SerializeField] Backend m_backend = null;
    [SerializeField] StartPanel m_startPanel = null;
    
    
    [SerializeField] int m_serverListCount = 10;

    public void Start()
    {
        if(m_serverResponseIndex < m_serverResponseArray.Length)
            m_nextServerResponse = m_serverResponseArray[m_serverResponseIndex];
    }
    public void SendNextServerResponse()
    {
        m_backend.ReceivedMessage(m_nextServerResponse);
        ++m_serverResponseIndex;
        if (m_serverResponseIndex < m_serverResponseArray.Length)
            m_nextServerResponse = m_serverResponseArray[m_serverResponseIndex];
    }
    public void FillServerList()
    {
        string[] serverList = new string[m_serverListCount];
        for (int i = 0; i < m_serverListCount; ++i)
        {
            serverList[i] = "LOcalHost";
        }
        m_startPanel.ParseServerList(serverList);
    }
}
