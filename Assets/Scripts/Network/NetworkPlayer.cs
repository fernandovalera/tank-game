using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class NetworkPlayer : NetworkBehaviour
{
    [ClientRpc]
    public void RpcSetCameraTargets(GameObject[] tanks)
    {
        Transform[] targets = new Transform[tanks.Length];

        for (int i = 0; i < tanks.Length; i++)
        {
            targets[i] = tanks[i].transform;
        }

        CameraControl cameraControl = GameObject.Find("CameraRig").GetComponent<CameraControl>();
        cameraControl.SetTargets(targets);
    }

    [ClientRpc]
    public void RpcSetPlayerConnectionInfo(string info)
    {
        Text connectionInfo = GameObject.Find("ConnectionInfo").GetComponent<Text>();
        connectionInfo.text = info;
    }
}
