using System;
using UnityEngine;

public class MPU6050 : MonoBehaviour
{
    private UdpServer server;

    public Transform trans;
    private bool fire = false;
    public float fireRate = 0.5f;
    private float nextFire = 0.0f;
    public GameObject sphere;

    float AccX;
    float accAngleX;
    float AccY;
    float accAngleY;
    float AccZ;

    float GyroX;
    float GyroY;
    float GyroZ;

    float gyroAngleX;
    float gyroAngleY;

    float roll;
    float pitch;
    float yaw;

    float elapsedTime, currentTime, previousTime;

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;
        Loom.Initialize();

        EventCenter.AddListener<string>("Receive", OnMessage);
        EventCenter.AddListener<string>("Error", OnError);

        server = new UdpServer();
        server.Start(16650, "192.168.0.56", 16651);

        server.Send("192.168.0.105");
    }

    // Update is called once per frame
    void Update()
    {
        if (fire && Time.time > nextFire)
        {
            nextFire = Time.time + fireRate;
            GameObject go = Instantiate(sphere, trans.position, trans.rotation);
            go.SetActive(true);
            go.GetComponent<Rigidbody>().AddForce(trans.forward * 800);
        }
    }

    private void OnApplicationQuit()
    {
        server.Dispose();
        server = null;
    }

    int count = 0;
    float AccErrorX;
    float AccErrorY;
    float GyroErrorX;
    float GyroErrorY;
    float GyroErrorZ;

    void OnMessage(string msg)
    {
        Loom.QueueOnMainThread(() =>
        {
            //previousTime = currentTime;        // Previous time is stored before the actual time read
            //currentTime = DateTime.Now.Millisecond;            // Current time actual time read
            //elapsedTime = (currentTime - previousTime) / 1000;
            elapsedTime = 0.016f;
            Debug.Log("elapsedTime:" + elapsedTime);
            //ifReceive.text += msg+ "\r\n";
            MPUMSG mpumsg = new MPUMSG();
            JsonUtility.FromJsonOverwrite(msg, mpumsg);
            if (mpumsg.Shoot == 1)
            {
                fire = true;
            }
            else
            {
                fire = false;
            }
            Debug.Log("Temp:" + (mpumsg.Temp / 340.00 + 36.53));
            AccX = mpumsg.AccX / 16384.0f;
            AccY = mpumsg.AccY / 16384.0f;
            AccZ = mpumsg.AccZ / 16384.0f;

            accAngleX = (MathF.Atan(AccY / MathF.Sqrt(MathF.Pow(AccX, 2) + MathF.Pow(AccZ, 2))) * 180 / MathF.PI) - 0.58f;
            accAngleY = (MathF.Atan(-1 * AccX / MathF.Sqrt(MathF.Pow(AccY, 2) + MathF.Pow(AccZ, 2))) * 180 / MathF.PI) + 1.58f;

            accAngleX += 3.845675f;
            accAngleY += 1.984601f;

            GyroX = mpumsg.GyroX / 131.0f;
            GyroY = mpumsg.GyroY / 131.0f;
            GyroZ = mpumsg.GyroZ / 131.0f;

            GyroX += 1.553333f;
            GyroY += 3.210216f;
            GyroZ += 0.1432245f;

            if (count < 200)
            {
                count++;
                AccErrorX += accAngleX;
                AccErrorY += accAngleY;

                GyroErrorX += GyroX;
                GyroErrorY += GyroY;
                GyroErrorZ += GyroZ;
            }
            if (count == 200)
            {
                AccErrorX = AccErrorX / 200;
                AccErrorY = AccErrorY / 200;

                GyroErrorX = GyroErrorX / 200;
                GyroErrorY = GyroErrorY / 200;
                GyroErrorZ = GyroErrorZ / 200;

                Debug.LogWarning("AccErrorX:" + AccErrorX
                    + " AccErrorY:" + AccErrorY
                    + " GyroErrorX:" + GyroErrorX
                    + " GyroErrorY:" + GyroErrorY
                    + " GyroErrorZ:" + GyroErrorZ);
                count = 0;
            }

            // Currently the raw values are in degrees per seconds, deg/s, so we need to multiply by sendonds (s) to get the angle in degrees
            gyroAngleX = gyroAngleX + GyroX * elapsedTime; // deg/s * s = deg
            gyroAngleY = gyroAngleY + GyroY * elapsedTime;
            yaw = yaw + GyroZ * elapsedTime;
            // Complementary filter - combine acceleromter and gyro angle values
            roll = 0.96f * gyroAngleX + 0.04f * accAngleX;
            pitch = 0.96f * gyroAngleY + 0.04f * accAngleY;
            Debug.Log("yaw:" + yaw + " roll:" + roll + " pitch:" + pitch);
            trans.eulerAngles = new Vector3(-roll, pitch, yaw);
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
    public float AccX;
    public float AccY;
    public float AccZ;
    public float Temp;
    public float GyroX;
    public float GyroY;
    public float GyroZ;
    public int Shoot;
}