using System;
using UnityEditor.PackageManager;
using UnityEditor.VersionControl;
using UnityEngine;

public class MPU6050 : MonoBehaviour
{
    private UdpServer server;

    public Transform trans;

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
         
    }

    private void OnApplicationQuit()
    {
        server.Dispose();
        server = null;
    }

    void OnMessage(string msg)
    {
        Loom.QueueOnMainThread(() =>
        {
            previousTime = currentTime;        // Previous time is stored before the actual time read
            currentTime = DateTime.Now.Millisecond;            // Current time actual time read
            elapsedTime = (currentTime - previousTime) / 1000;
            elapsedTime = 0.020f;
            Debug.Log("elapsedTime:" + elapsedTime);
            //ifReceive.text += msg+ "\r\n";
            MPUMSG mpumsg = new MPUMSG();
            JsonUtility.FromJsonOverwrite(msg, mpumsg);
            Debug.Log("Temp:" + (mpumsg.Temp / 340.00 + 36.53));
            AccX = mpumsg.AccX / 16384.0f;
            AccY = mpumsg.AccY / 16384.0f;
            AccZ = mpumsg.AccZ / 16384.0f;

            accAngleX = (MathF.Atan(AccY / MathF.Sqrt(MathF.Pow(AccX, 2) + MathF.Pow(AccZ, 2))) * 180 / MathF.PI) - 0.58f;
            accAngleY = (MathF.Atan(-1 * AccX / MathF.Sqrt(MathF.Pow(AccY, 2) + MathF.Pow(AccZ, 2))) * 180 / MathF.PI) + 1.58f;

            GyroX = mpumsg.GyroX / 131.0f;
            GyroY = mpumsg.GyroY / 131.0f;
            GyroZ = mpumsg.GyroZ / 131.0f;

            GyroX = GyroX + 0.56f; // GyroErrorX ~(-0.56)
            GyroY = GyroY - 2f; // GyroErrorY ~(2)
            GyroZ = GyroZ + 0.79f; // GyroErrorZ ~ (-0.8)

            // Currently the raw values are in degrees per seconds, deg/s, so we need to multiply by sendonds (s) to get the angle in degrees
            gyroAngleX = gyroAngleX + GyroX * elapsedTime; // deg/s * s = deg
            gyroAngleY = gyroAngleY + GyroY * elapsedTime;
            yaw = yaw + GyroZ * elapsedTime;
            // Complementary filter - combine acceleromter and gyro angle values
            roll = 0.96f * gyroAngleX + 0.04f * accAngleX;
            pitch = 0.96f * gyroAngleY + 0.04f * accAngleY;
            Debug.Log("yaw:" + yaw + " roll:" + roll + " pitch:" + pitch);
            trans.eulerAngles = new Vector3(yaw, roll, pitch);
        });
    }

    void OnError(string error)
    {
        Loom.QueueOnMainThread(() =>
        {
            Debug.LogError(error);
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