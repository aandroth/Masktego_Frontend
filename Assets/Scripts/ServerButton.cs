using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ServerButton : MonoBehaviour
{
    [SerializeField]
    private string m_text = "";
    public TMP_Text m_ipAddressText;
    public delegate void PutIpIntoBackendDelegate(string t);
    public PutIpIntoBackendDelegate m_putIpIntoBackend;
    public delegate void CallToConnectDelegate();
    public CallToConnectDelegate m_callToConnect;

    public void AssignButtonParameters(string ipText, PutIpIntoBackendDelegate putIp, CallToConnectDelegate callConnect)
    {
        m_text = ipText;
        m_putIpIntoBackend = putIp;
        m_callToConnect = callConnect;
        gameObject.GetComponentInChildren<TMP_Text>().text = m_text;
        gameObject.GetComponent<Button>().onClick.AddListener(() => m_putIpIntoBackend(m_text));
        gameObject.GetComponent<Button>().onClick.AddListener(() => m_callToConnect());
    }

    public void PutTextIntoIpTextPanel(string text = "")
    {
        if(text == "")
            m_ipAddressText.text = m_text;
        else
            m_ipAddressText.text = text;
    }
}
