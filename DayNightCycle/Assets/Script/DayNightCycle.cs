
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using SimpleJSON;

public class DayNightCycle : MonoBehaviour
{

    private bool realModeSunReady = false;
    public float latitude, longitude,cycleTime;//cycle time means how many minutes complete a day night cycle
    public bool useRealTime;
    private float sunriseSecond, sunsetSecond,timer, nowTimeSecond;
    private string today;
    private const int DayTimeSecond = 86400;


    private void Start() {
        if (useRealTime)
        {
            GetRealTimeData();
        }
        else
        {
            GetSetTimeData();
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (useRealTime && realModeSunReady)
        {
            if (nowTimeSecond >= DayTimeSecond)
            {
                GetRealTimeData();
            }

            gameObject.transform.rotation = Quaternion.Euler(SunAngle(sunriseSecond, sunsetSecond, nowTimeSecond + timer), -90, 0);
        }
        else if (!useRealTime)
        {
            float timeSpeedConstant = DayTimeSecond / (cycleTime * 60);
            gameObject.transform.rotation = Quaternion.Euler(SunAngle(sunriseSecond, sunsetSecond, timer*timeSpeedConstant), -90, 0);
        }
    }

    void GetSetTimeData()
    {
        sunriseSecond = 21600f;
        sunsetSecond = 64800f;
        nowTimeSecond = 0;
    }

    void GetRealTimeData()
    {
        string nowTimeUrl = "https://www.timeapi.io/api/Time/current/zone?timeZone=Asia/Hong_Kong";
        StartCoroutine(GetJsonData(nowTimeUrl, "Now"));
    }

    IEnumerator GetJsonData(string url, string APIType)
    {
        string[] APITypeList = { "Sun", "Now" };
        using (UnityWebRequest webData = UnityWebRequest.Get(url))
        {
            yield return webData.SendWebRequest();
            if (webData.result == UnityWebRequest.Result.ConnectionError || webData.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.Log("ERROR");
            }
            else if (webData.result == UnityWebRequest.Result.Success)
            {
                JSONNode jsonData = JSON.Parse(System.Text.Encoding.UTF8.GetString(webData.downloadHandler.data));
                if (jsonData == null)
                {
                    Debug.Log("NO DATA");
                }
                else if (APIType == APITypeList[0])
                {
                    //do sun api
                    JSONClass jsonClass = (JSONClass)jsonData["results"];
                    string sunriseInString = jsonClass["sunrise"];
                    string sunsetInString = jsonClass["sunset"];

                    string[] sunrise_hour_minute_seconds = sunriseInString.Split(':');//the 2nd string incule "second PM" need to use indexof(' ') to pick only second
                    sunriseSecond = int.Parse(sunrise_hour_minute_seconds[0]) * 60 * 60 + int.Parse(sunrise_hour_minute_seconds[1]) * 60 + int.Parse(sunrise_hour_minute_seconds[2].Substring(0, sunrise_hour_minute_seconds[2].IndexOf(' ')));
                    if (sunriseInString.Substring(sunriseInString.IndexOf(' ')) == " PM")
                        sunriseSecond += 43200;//12 hour
                    sunriseSecond += 28800;//UTC+8
                    if (sunriseSecond > 86400)
                        sunriseSecond -= 86400;

                    string[] sunsetHourMinuteSecond = sunsetInString.Split(':');//the 2nd string incule "second PM" need to use indexof(' ') to pick only second
                    sunsetSecond = int.Parse(sunsetHourMinuteSecond[0]) * 60 * 60 + int.Parse(sunsetHourMinuteSecond[1]) * 60 + int.Parse(sunsetHourMinuteSecond[2].Substring(0, sunsetHourMinuteSecond[2].IndexOf(' ')));
                    if (sunsetInString.Substring(sunsetInString.IndexOf(' ')) == " PM")
                        sunsetSecond += 43200;//12 hour
                    sunsetSecond += 28800;//UTC+8
                    if (sunsetSecond > 86400)
                        sunsetSecond -= 86400;

                    realModeSunReady = true;
                    timer = 0;
                    //Debug.Log("finish suntime ");
                }
                else if (APIType == APITypeList[1])
                {
                    //do now api
                    float hour = float.Parse(jsonData["hour"]);
                    float minute = float.Parse(jsonData["minute"]);
                    float seconds = float.Parse(jsonData["seconds"]);

                    nowTimeSecond = hour * 60 * 60 + minute * 60 + seconds;
                    today = jsonData["year"] + "-" + jsonData["month"] + "-" + jsonData["day"];
                    string sunriseSunsetUrl = "https://api.sunrisesunset.io/json?lat=" + latitude + "&lng=" + longitude + "&timezone=UTC&date=" + today;
                    StartCoroutine(GetJsonData(sunriseSunsetUrl, "Sun"));
                }
               
            }
        }
    }
    
    float SunAngle(float sunriseSecond,float sunsetSecond,float nowTimeSecond)
    {
        if (nowTimeSecond>=sunriseSecond && nowTimeSecond <= sunsetSecond)
        {
            return (nowTimeSecond-sunriseSecond)*180/(sunsetSecond-sunriseSecond);
        }else if (sunsetSecond < nowTimeSecond && nowTimeSecond< 86400)
        {
            return 180+(nowTimeSecond-sunsetSecond)*90/(DayTimeSecond-sunsetSecond);
        }else
        {   
            return 270+90*nowTimeSecond/sunriseSecond;
        }
    }
    
}

