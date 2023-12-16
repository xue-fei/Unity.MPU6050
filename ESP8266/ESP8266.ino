/**********************************************************************
项目名称/Project          : 零基础入门学用物联网
程序名称/Program name     : ESP8266WiFiUdp_12
团队/Team                : 太极创客团队 / Taichi-Maker (www.taichi-maker.com)
作者/Author              : 小凯
日期/Date（YYYYMMDD）     : 20200319
程序目的/Purpose          : 
用于演示ESP8266WiFiUdp库中print函数
-----------------------------------------------------------------------
本示例程序为太极创客团队制作的《零基础入门学用物联网》中示例程序。
该教程为对物联网开发感兴趣的朋友所设计和制作。如需了解更多该教程的信息，请参考以下网页：
http://www.taichi-maker.com/homepage/esp8266-nodemcu-iot/iot-c/esp8266-nodemcu-web-client/http-request/
***********************************************************************/
#include <Wire.h>
#include <ArduinoJson.h>
#include <ESP8266WiFi.h>
#include <WiFiUdp.h>
#include "SparkFun_BNO080_Arduino_Library.h"

#define ssid      "401" //这里改成你的设备当前环境下WIFI名字
#define password  "unityioslinux1"          //这里改成你的设备当前环境下WIFI密码
 
#define BTN_1 D4       // 开关按钮
bool btn1_state = false;

WiFiUDP Udp;//实例化WiFiUDP对象
unsigned int localUdpPort = 16651;  // 自定义本地监听端口
unsigned int remoteUdpPort = 16650;  // 自定义远程监听端口
char incomingPacket[255];  // 保存Udp工具发过来的消息
char replyPacket[2048];  //发送的消息,仅支持英文
   
BNO080 myIMU;

void setup()
{ 
  Serial.begin(115200);
  Serial.println();
  Wire.begin();
  if (myIMU.begin() == false)
  {
    Serial.println(F("BNO080 not detected at default I2C address. Check your jumpers and the hookup guide. Freezing..."));
    while (1);
  }
  Wire.setClock(400000); //Increase I2C data rate to 400kHz 
  myIMU.enableRotationVector(16); //Send data update every 50ms

  Serial.printf("正在连接 %s ", ssid);
  WiFi.begin(ssid, password);//连接到wifi
  while (WiFi.status() != WL_CONNECTED)//等待连接
  {
    delay(500);
    Serial.print(".");
  }
  Serial.println("连接成功");
 
  //启动Udp监听服务
  if(Udp.begin(localUdpPort))
  {
    Serial.println("监听成功");
      
    //打印本地的ip地址，在UDP工具中会使用到
    //WiFi.localIP().toString().c_str()用于将获取的本地IP地址转化为字符串   
    Serial.printf("现在收听IP：%s, UDP端口：%d\n", WiFi.localIP().toString().c_str(), localUdpPort);
  }
  else
  {
    Serial.println("监听失败");
  }
  pinMode(BTN_1, INPUT_PULLUP);//开关按钮为输入开启上拉电阻
}
 
void loop()
{
  if (myIMU.dataAvailable() == true)
  {
    float roll = (myIMU.getRoll()) * 180.0 / PI; // Convert roll to degrees
    float pitch = (myIMU.getPitch()) * 180.0 / PI; // Convert pitch to degrees
    float yaw = (myIMU.getYaw()) * 180.0 / PI; // Convert yaw / heading to degrees

    Serial.print(roll, 1);
    Serial.print(F(","));
    Serial.print(pitch, 1);
    Serial.print(F(","));
    Serial.print(yaw, 1);

    DynamicJsonDocument doc(2048);
    // 创建json根节点对象
    JsonObject obj  = doc.to<JsonObject>();
    obj ["roll"] = roll;
    obj ["pitch"] = pitch;
    obj ["yaw"] = yaw; 
    //向udp工具发送消息
    Udp.beginPacket(Udp.remoteIP(), remoteUdpPort);//配置远端ip地址和端口 
    if (digitalRead(BTN_1) == 0)
    {
        obj ["Shoot"] = 1; 
    }
    else
    {
        obj ["Shoot"] = 0; 
    }
    String output;
    serializeJson(doc, replyPacket);    
    Udp.print(replyPacket);//把数据写入发送缓冲区
    Udp.endPacket();//发送数据  

    Serial.println();
  }
  
  
}