using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using CSCore.CoreAudioAPI;

namespace Spotify_Controller
{
    class Program
    {
        private static AudioSessionManager2 GetDefaultAudioSessionManager2(DataFlow dataFlow)
        {
            using (var enumerator = new MMDeviceEnumerator())
            {
                using (var device = enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia))
                {
                    var sessionManager = AudioSessionManager2.FromMMDevice(device);
                    return sessionManager;
                }
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out Int32 lpdwProcessId);
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);
        static bool wePause = false;
        static float threshhold = 0.0f;
        private static Timer newTimer = new Timer(timerCallback, null, 0, 1000);

        static void Main(string[] args)
        {
            Console.WriteLine("Initialized\nSelect A Percentage (1%-100%) to Auto-Mute at:");
            string str = Console.ReadLine();
            threshhold = float.Parse(str) / 100;

            Console.ReadLine();
        }

        private static void timerCallback(Object o)
        {
            bool nonSpotifyAudio = false;

            using (var sessionManager = GetDefaultAudioSessionManager2(DataFlow.Render))
            {
                using (var sessionEnumerator = sessionManager.GetSessionEnumerator())
                {
                    foreach (var session in sessionEnumerator)
                    {
                        using (var audioMeterInformation = session.QueryInterface<AudioMeterInformation>())
                        {
                            float volume = audioMeterInformation.GetPeakValue();

                            using (var audioSessionControl2 = session.QueryInterface<AudioSessionControl2>())
                            {
                                var process = audioSessionControl2.Process;
                                if (process != null)
                                {
                                    int processID = process.Id;

                                    if (process.ProcessName != "Spotify" && volume > threshhold)
                                        nonSpotifyAudio = true;
                                }
                            }
                        }
                    }
                }
            }

            IntPtr targetWindow = FindWindow("Chrome_WidgetWin_0", null);
            if (targetWindow != IntPtr.Zero)
            {
                Int32 pid = 0;
                GetWindowThreadProcessId(targetWindow, out pid);

                Process[] processList = Process.GetProcesses();
                foreach (Process p in processList)
                {
                    if (p.Id == pid)
                    {
                        if (p.ProcessName == "Spotify")
                        {
                            if (nonSpotifyAudio && !p.MainWindowTitle.Contains("Spotify"))
                            {
                                SendMessage(targetWindow, 0x0319, new IntPtr(0), new IntPtr(917504));
                                wePause = true;
                            }
                            else if (!nonSpotifyAudio && p.MainWindowTitle.Contains("Spotify") && wePause == true)
                            {
                                SendMessage(targetWindow, 0x0319, new IntPtr(0), new IntPtr(917504));
                                wePause = false;
                            }

                        }
                    }
                }
            }
        }
    }
}
