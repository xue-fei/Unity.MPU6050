using BigRookGames.Weapons;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MPU6050 : MonoBehaviour
{
    private UdpServer server;

    public Transform trans;
    private bool fire = false;
    public float fireRate = 0.1f;
    private float nextFire = 0.0f;
    public GameObject sphere;
    public GunfireController controller;
    public Transform startPoint;

    public Queue<MPUMSG> mpuMsgs = new Queue<MPUMSG>();

    float nowTime;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 30;
        Screen.SetResolution(1920, 1080, true);
        Loom.Initialize();

        EventCenter.AddListener<string>("Receive", OnMessage);
        EventCenter.AddListener<string>("Error", OnError);

        server = new UdpServer();
        server.Start(16650, "192.168.0.56", 16651);

        //向ESP8266发送本机IP
        server.Send("192.168.0.105");
        //server.Send("192.168.0.151");
        nowTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        if (fire && Time.time > nextFire)
        {
            nextFire = Time.time + fireRate;
            controller.FireWeapon();
            GameObject go = Instantiate(sphere, startPoint.position, trans.rotation);
            go.SetActive(true);
            go.GetComponent<Rigidbody>().AddForce(trans.forward * 5000);
        }

        if (Time.time - 3f > nowTime)
        {
            nowTime = Time.time;
            Reset();
        }
    }

    private void OnApplicationQuit()
    {
        server.Dispose();
        server = null;
    }

    Int64 count;
    void OnMessage(string msg)
    {
        Loom.QueueOnMainThread(() =>
        {
            nowTime = Time.time;
            MPUMSG mpumsg = new MPUMSG();
            try
            {
                JsonUtility.FromJsonOverwrite(msg, mpumsg);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                return;
            }
            count++;
            if (count % 2 == 0)
            {
                if (mpumsg.Shoot == 1)
                {
                    fire = true;
                }
                else
                {
                    fire = false;
                }
                //trans.eulerAngles = new Vector3(mpumsg.p, -mpumsg.y, -mpumsg.r);
                //trans.eulerAngles = new Vector3(mpumsg.roll, -mpumsg.yaw, 0);
                trans.rotation = Quaternion.Euler(mpumsg.roll, -mpumsg.yaw, 0);
            }
        });
    }

    void OnError(string error)
    {
        Loom.QueueOnMainThread(() =>
        {
            Debug.LogError(error);
            if (error.Equals("serverSocket == null"))
            {
                fire = false;
            }
        });
    }

    private void Reset()
    {
        trans.eulerAngles = Vector3.zero;
        //向ESP8266发送本机IP
        server.Send("192.168.0.105");
        Debug.LogWarning("Reset");
    }
}

[Serializable]
public class MPUMSG
{
    public float yaw;
    public float pitch;
    public float roll;
    public int Shoot;
}