using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

#region VLC Streaming
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;
using Onvif.Core;
using Onvif.Core.Client.Camera;
using Onvif.Core.Client.Common;
using Onvif.Core.Client.Camera.Requests;
using Onvif.Core.Client.Ptz;
using System.Threading;
using System.Windows.Threading;
#endregion

namespace CCTVTester
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly int _cameraCount = 6;
        private string[] _rtsp = new string[6];

        // VLC
        private LibVLC _libVLC;
        private MediaPlayer[] _mediaPlayers; //= new MediaPlayer[6];
        private readonly VideoView[] _videoViews; // = new VideoView[6];
        //private bool[] _isPlayerValid; // Track valid state
        private bool _isLibVLCReady = false;

        // Onvif
        private Camera[] _cameras = new Camera[6];
        private Profile[] _profiles = new Profile[6];

        public MainWindow()
        {
            InitializeComponent();

            Task.Run(() =>
            {
                try
                {
                    LibVLCSharp.Shared.Core.Initialize();

                    // Create a single LibVLC instance (shared across all players)
                    _libVLC = new LibVLC(
                        "--clock-synchro=0",       // Disable A/V sync (use only if no audio)
                        "--no-audio",              // Disable audio if not needed
                        "--network-caching=300",   // Reduce network caching
                        "--no-stats",              // Disable stats
                        "--no-sub-autodetect-file" // Don't look for subtitle files
                    );

                    _isLibVLCReady = true;
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                        MessageBox.Show($"Failed to initialize LibVLC: {ex.Message}"));
                }
            });


            _mediaPlayers = new MediaPlayer[_cameraCount];
            _videoViews = new VideoView[_cameraCount];
             _cameras = new Camera[_cameraCount];
            _profiles = new Profile[_cameraCount];
            //_isPlayerValid = new bool[_cameraCount];

            bindVideoView(); // bind video view on the v. begining preview pop up box
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Mark all as invalid
                //for (int i = 0; i < _cameraCount; i++)
                //{
                //    _isPlayerValid[i] = false;
                //}

                disconnectCameras();

                //unbindVideoView();

                // Wait before disposing LibVLC
                System.Threading.Thread.Sleep(200);

                try
                {
                    _libVLC?.Dispose();
                }
                catch (Exception ex2){
                    appendLog(ex2.ToString());
                }
                _libVLC = null;
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        private void appendLog(string msg)
        {
            try
            {
                Dispatcher.Invoke((Action)delegate
                {
                    tbx_log.AppendText(msg + Environment.NewLine);
                    tbx_log.ScrollToEnd();
                });
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        private string[] getRtspUrlDetails(int idx)
        {
            try
            {
                if (idx < 0 || idx >= _rtsp.Count())
                {
                    appendLog($"Wrong index: {idx}");
                    return null;
                }

                string rtsp = _rtsp[idx];
                string user = rtsp.Substring(7).Split(':')[0];
                string pwd = rtsp.Substring(7).Split(':')[1].Split('@')[0];
                string ip_n_port = rtsp.Substring(0, rtsp.Length - 5).Split('@')[1].Split('\\')[0];
                string ip = ip_n_port.Contains(":") ? ip_n_port.Split(':')[0] : "";
                string port = ip_n_port.Contains(":") ? ip_n_port.Split(':')[1] : "";
                appendLog($"#{idx} ({rtsp}) - user: {user} - pwd:{pwd} - ip:{ip} - port:{port}");

                return new string[] { ip, port, user, pwd };
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
            return null;
        }

        private async void PTZButton_Left_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Camera camera = _cameras[0];
                if (camera != null)
                {
                    appendLog("Start to left");
                    await CameraMoveAsync(camera, -0.1f, 0f);
                    await StopAfterDelayAsync(camera, 2000);
                }
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        private async void PTZButton_Right_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Camera camera = _cameras[0];
                if (camera != null)
                {
                    appendLog("Start to right");
                    await CameraMoveAsync(camera, 0.1f, 0f);
                    await StopAfterDelayAsync(camera, 2000);
                }
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        private async void PTZButton_Up_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Camera camera = _cameras[0];
                if (camera != null)
                {
                    appendLog("Start to top");
                    await CameraMoveAsync(camera, 0, 0.1f);
                    await StopAfterDelayAsync(camera, 2000);
                }
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        private async void PTZButton_Down_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Camera camera = _cameras[0];
                if (camera != null)
                {
                    appendLog("Start to down");
                    await CameraMoveAsync(camera, 0, -0.1f);
                    await StopAfterDelayAsync(camera, 2000);
                }
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        private async void PTZButton_In_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Camera camera = _cameras[0];
                if (camera != null)
                {
                    appendLog("Start zoom in");
                    await CameraZoomAsync(camera, 0.3f);
                    await StopAfterDelayAsync(camera, 2000);
                }
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        private async void PTZButton_Out_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Camera camera = _cameras[0];
                if (camera != null)
                {
                    appendLog("Start zoom out");
                    await CameraZoomAsync(camera, -0.3f);
                    await StopAfterDelayAsync(camera, 2000);
                }
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        private async void PTZButton_Reset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Camera camera = _cameras[0];
                if (camera != null)
                {
                    appendLog("Go to home position");

                    if (_profiles[0] == null)
                    {
                        appendLog("profile is null - no action");
                        return;
                    }

                    string token = _profiles[0].token;
                    if (string.IsNullOrEmpty(token))
                    {
                        appendLog("token is null, retrieve again");
                    }
                    else
                    {
                        PTZConfiguration profile = _cameras[0].Profile?.PTZConfiguration;
                        if (profile == null)
                        {
                            appendLog($"profile#0 is null");
                        }
                        else
                            _profiles[0] = _cameras[0].Profile;
                    }

                    PTZSpeed speed = new PTZSpeed { Zoom = new Vector1D { x = 0.5f } };
                    GetPresetsResponse presetList = await camera.Ptz.GetPresetsAsync(token);
                    if (presetList != null && presetList.Preset.Count() > 0)
                    {
                        PTZPreset preset1 = presetList.Preset.First();
                        await camera.Ptz.GotoPresetAsync(token, preset1.token, speed);
                    }
                    else
                    {
                        appendLog($"no preset found, no action");
                    }
                }
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        private void btn_connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                appendLog($"Disconnecting to camera(s) (if any) ...");

                if (!_isLibVLCReady)
                {
                    appendLog($"LibVLC not initialed, please try again.");
                    return;
                }

                disconnectCameras();

                Task task = connectCameraAsync();
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        private void btn_disconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                disconnectCameras();
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        // move function
        private async Task CameraMoveAsync(Camera camera, float toRigth, float toTop)
        {
            try
            {
                var preset = await camera.Ptz.GetPresetsAsync(_profiles[0].token);
                if (preset != null && preset.Preset.Count() > 0)
                {
                    PTZVector vector = new PTZVector { PanTilt = new Vector2D { x = toRigth, y = toTop } };
                    PTZSpeed speed = new PTZSpeed { PanTilt = new Vector2D { x = 0.8f, y = 0.8f } };

                    await camera.MoveAsync(MoveType.Relative, vector, speed);
                }
                else
                {
                    appendLog($"no preset found, no action [1]");
                }
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        // zoom function
        private async Task CameraZoomAsync(Camera camera, float zoom)
        {
            try
            {
                var vector = new PTZVector { Zoom = new Vector1D { x = zoom } };
                var speed = new PTZSpeed { Zoom = new Vector1D { x = 0.8f } };
                await camera.MoveAsync(MoveType.Relative, vector, speed);
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        private async Task StopAfterDelayAsync(Camera camera, int delayMs)
        {
            try
            {
                await Task.Delay(delayMs);
                if (_profiles[0] != null)
                    await camera.Ptz.StopAsync(_profiles[0].token, false, false); // Stop all PTZ movements
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        // start camera
        private async Task connectCameraAsync()
        {
            try
            {
                appendLog($"Connecting to camera(s)...");

                // assign ui textbox url to variables
                _rtsp[0] = tbx_rtsp1.Text.Trim();
                _rtsp[1] = tbx_rtsp2.Text.Trim();
                _rtsp[2] = tbx_rtsp3.Text.Trim();
                _rtsp[3] = tbx_rtsp4.Text.Trim();
                _rtsp[4] = tbx_rtsp5.Text.Trim();
                _rtsp[5] = tbx_rtsp6.Text.Trim();


                for (int i = 0; i < _cameraCount; i++)
                {
                    if (string.IsNullOrEmpty(_rtsp[i]))
                    {
                        appendLog($"rtsp#{i} is empty, will not initiallize");
                        continue;
                    }

                    // Initialize each MediaPlayer and start playback
                    await Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            _mediaPlayers[i] = new MediaPlayer(_libVLC);
                            //_isPlayerValid[i] = true;

                            if (_videoViews[i] != null)
                            {
                                _videoViews[i].MediaPlayer = _mediaPlayers[i];
                            }

                            var media = new Media(_libVLC, new Uri(_rtsp[i]));
                            _mediaPlayers[i].Play(media);

                            appendLog($"Camera {i + 1} started");
                        }
                        catch (Exception ex)
                        {
                            appendLog($"Error playing camera {i + 1}: {ex.Message}");
                            //_isPlayerValid[i] = false;
                        }
                    });


                    //_mediaPlayers[i] = new MediaPlayer(_libVLC);
                    //_videoViews[i].MediaPlayer = _mediaPlayers[i];
                    //var media = new Media(_libVLC, new Uri(_rtsp[i]));
                    //Dispatcher.InvokeAsync(() =>
                    //{
                    //    _mediaPlayers[i].Play();  // Now safe to play
                    //}, DispatcherPriority.Loaded);

                    
                    // Initialize ONVIF Cameras for PTZ
                    string[] rtsp = getRtspUrlDetails(i);
                    var account = new Account(rtsp[0], rtsp[2], rtsp[3]);
                    _cameras[i] = Camera.Create(account, ex2 =>
                    {
                        Dispatcher.Invoke(() =>
                            appendLog($"Exception: Camera {i + 1} connection failed: {ex2.Message}, inner exception:{ex2.InnerException.Message}"));
                    });

                    if (_cameras[i] == null)
                    {
                        appendLog($"Failed to create Camera {i + 1}");
                        continue;
                    }

                    Profile profile = _cameras[i].Profile;
                    if (profile == null)
                    {
                        appendLog($"profile#{i} is null");
                        continue;
                    }
                    _profiles[i] = profile;
                    appendLog($"Profile#{i}: {profile.Name} (Token: {profile.token})");
                }
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        // stop live view and dispose objects
        private void disconnectCameras()
        {
            try
            {
                appendLog($"Disconnecting to camera(s)...");

                for (int i = 0; i < _cameraCount; i++)
                {
                    //_isPlayerValid[i] = false;

                    // Detach from VideoView first
                    if (_videoViews[i] != null)
                    {
                        _videoViews[i].MediaPlayer = null;
                    }


                    // Clean up resources
                    //MediaPlayer player = _mediaPlayers[i];
                    if (_mediaPlayers[i] != null)
                    {
                        // Don't check state, just stop directly
                        try
                        {
                            _mediaPlayers[i].Stop();
                        }
                        catch (ObjectDisposedException)
                        {
                            // Already disposed, ignore
                        }
                        catch (AccessViolationException)
                        {
                            // Already disposed or invalid, ignore
                        }

                        // Wait for stop to complete
                        System.Threading.Thread.Sleep(100);

                        ////player.stop
                        //Dispatcher.Invoke((Action)delegate
                        //{
                        //    //if (player.State == VLCState.Playing || player.State == VLCState.Buffering)
                        //    {
                        //        player.Stop();  // Only if active
                        //    }
                        //    //player.Media.;
                        //});
                        //Thread.Sleep(500);
                        //player?.Dispose();
                        ////player.Media = null;

                        // Dispose
                        _mediaPlayers[i].Dispose();
                        _mediaPlayers[i] = null;
                    }

                    // Dispose cameras (if supported; otherwise, set to null)
                    _cameras[i] = null;
                    _profiles[i] = null;
                }

                appendLog($"Media player disposed");
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

        private void bindVideoView()
        {
            try
            {
                // Map VideoViews (0-indexed)
                _videoViews[0] = VideoView1;
                _videoViews[1] = VideoView2;
                _videoViews[2] = VideoView3;
                _videoViews[3] = VideoView4;
                _videoViews[4] = VideoView5;
                _videoViews[5] = VideoView6;

            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        } 
        
        private void unbindVideoView()
        {
            try
            {
                appendLog($"Dispose video view");

                _videoViews[0].Dispose();
                _videoViews[1].Dispose();
                _videoViews[2].Dispose();
                _videoViews[3].Dispose();
                _videoViews[4].Dispose();
                _videoViews[5].Dispose();
            }
            catch (Exception ex)
            {
                appendLog(ex.ToString());
            }
        }

    }
}
