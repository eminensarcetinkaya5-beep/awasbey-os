using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AwasBeyPCHost
{
    class Program
    {
        private const string SIGNATURE = "By: AwasBey";
        private static HttpListener listener;
        private static string url = "http://localhost:8080/";

        static async Task Main(string[] args)
        {
            Console.Title = $"AwasBey Remote PC Bridge - [{SIGNATURE}]";
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==================================================");
            Console.WriteLine($"   AWASBEY FULL CONTROL PC BRIDGE SERVICE");
            Console.WriteLine($"   Sistem Sahibi: {SIGNATURE}");
            Console.WriteLine("==================================================");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[Status]: Sunucu dinleniyor: {url}");
            Console.ResetColor();

            listener = new HttpListener();
            listener.Prefixes.Add(url);

            try
            {
                listener.Start();
                while (true)
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(context));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Hata]: {ex.Message}");
            }
        }

        private static void HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            // CORS İzinleri (Vercel'den sorunsuz erişim için)
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            if (request.HttpMethod == "OPTIONS")
            {
                response.StatusCode = 200;
                response.Close();
                return;
            }

            string responseJson = "{\"status\":\"ok\", \"by\":\"AwasBey\"}";

            try
            {
                string cmd = request.QueryString["cmd"];
                string pathParam = request.QueryString["path"];

                // 1. Bilgisayarda İstediğin Uygulamayı / Dosyayı Aç
                if (cmd == "launch" && !string.IsNullOrEmpty(pathParam))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = pathParam,
                        UseShellExecute = true
                    });
                    responseJson = $"{{\"result\":\"{pathParam} başlatıldı.\", \"by\":\"AwasBey\"}}";
                }
                // 2. Bağlantı Testi
                else if (cmd == "ping")
                {
                    responseJson = "{\"online\":true, \"message\":\"PC Aktif ve Hazır\", \"by\":\"AwasBey\"}";
                }
                // 3. Bilgisayarı Kapat / Yeniden Başlat
                else if (cmd == "shutdown")
                {
                    Process.Start("shutdown", "/s /t 0");
                    responseJson = "{\"result\":\"PC Kapatılıyor...\"}";
                }
            }
            catch (Exception ex)
            {
                responseJson = $"{{\"error\":\"{ex.Message}\"}}";
            }

            byte[] buffer = Encoding.UTF8.GetBytes(responseJson);
            response.ContentType = "application/json";
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }
}
