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

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        Loom.Initialize();

        EventCenter.AddListener<string>("Receive", OnMessage);
        EventCenter.AddListener<string>("Error", OnError);

        server = new UdpServer();
        server.Start(16650, "192.168.0.56", 16651);

        //向ESP8266发送本机IP
        server.Send("192.168.0.105");
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
                if (mpumsg.f == 1)
                {
                    fire = true;
                }
                else
                {
                    fire = false;
                }
                //trans.eulerAngles = new Vector3(mpumsg.p, -mpumsg.y, -mpumsg.r);
                trans.eulerAngles = new Vector3(0, -mpumsg.y, 0);
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
}

[Serializable]
public class MPUMSG
{
    public float y;
    public float p;
    public float r;
    public int f;
}