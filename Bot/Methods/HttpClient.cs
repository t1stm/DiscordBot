using System.Net;
using System.Net.Http;

namespace Bat_Tosho.Methods
{
    public static class HttpClient
    {
        public static System.Net.Http.HttpClient WithCookies()
        {
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(new Cookie("YSC", "DIZwBK2Vq_Y", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("PREF", "tz=Europe.Sofia&f6=40000000&f5=30000", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("SID",
                "BAhafqVRsKH-MfZVxpId1F0OFhNWiyOQ8aJHeKAijXNOo6xt2HmJcKWMUz8bXsU-he0nSA.", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("HSID", "AQNF3MrUdlQQYqkhe", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("SSID", "At2TdZpxStA_TqDlb", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("APISID", "rxRRogLSHeUVz_AH/AAy2fW_5UxKsQ3sPC", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("SAPISID", "1PbHcnU0SapJVRP-/AeTEUU6djJJF0r6ov", "/", "youtube.com"));
            cookieContainer.Add(new Cookie("LOGIN_INFO",
                "AFmmF2swRQIgUh5saMBAsjE76LhWPhlyo0tVSwBLqcjAzN2HMYO-mP8CIQDhma5f9NCYMKsdBjryvMnoTWoXrqyB2XpgheQ_seh-hQ:QUQ3MjNmd014RXdQeUF0eHIySlpBeWZUM21UYjJuVEZPelRmdEt3R05CX1ZPbUk2RjVYZ3dPZXB3bU9MZkxfV3ZzVEh0QkFPN09GVDR1N0dra2JJcktiQ0hibElZdEszUXdGdlZPQlNZNFB1YXo5bjdteG1IdGJUS1k1eWpRWFhXSFdUQWNPaVZJRXhFSllOWU9sV3YtenBNWWhncWJDbkpNTTExTktreDRBejR3NXdSSHJ3cmNNMEFWd290Y3h3VU1FUkpmOHExT3BuaDIyUy11VXV0bnZXdkVSS2FkakIwZw==",
                "/", "youtube.com"));
            cookieContainer.Add(new Cookie("VISITOR_INFO1_LIVE", "qAx2vo_yQS8", "/", "youtube.com"));
            var handler = new HttpClientHandler {CookieContainer = cookieContainer};
            return new System.Net.Http.HttpClient(handler);
        }
    }
}