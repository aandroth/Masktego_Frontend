using TMPro;
using UnityEngine;

public class UrlEntry : MonoBehaviour
{
    public TMP_InputField m_ipAddressInput;
    public Backend m_backend;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_ipAddressInput.onValueChanged.AddListener(OnTextChange);
    }

    public void OnTextChange(string newText)
    {
        m_ipAddressInput.text = newText;
        Debug.Log("New text: " + newText);

        if(m_backend != null)
            m_backend.SetServerUrlFull(newText);
    }
}
