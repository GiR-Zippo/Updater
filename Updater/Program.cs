using System;
using Tao.FreeGlut;
using OpenGL;
using System.Diagnostics;
using SharpMik;
using SharpMik.Player;
using SharpMik.Drivers;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows;
using System.Collections.Generic;

namespace Updater
{
    class Program
    {
        private static int width = 1280, height = 720;
        private static ShaderProgram program;
        private static VBO<Vector3> square;
        private static VBO<uint> squareElements;

        private static Stopwatch timer;
        private static Stopwatch timer2;

        private static Module m_Mod = null;
        private static MikMod m_Player;

        public static string VertexShader = @"
#version 440

in vec3 vertexPosition;

uniform mat4 projection_matrix;
uniform mat4 view_matrix;
uniform mat4 model_matrix;

void main(void)
{
    gl_Position = projection_matrix * view_matrix * model_matrix * vec4(vertexPosition, 0.1);
}
";

        public static string FragmentShader = @"";

        void m_Player_PlayerStateChangeEvent(ModPlayer.PlayerState state)
        {
            //if (state == ModPlayer.PlayerState.kStopped)

        }

        static void Main(string[] args)
        {
            var datas = ReadConfig(false);

            CheckVersion();

            m_Player = new MikMod();
            ModDriver.Mode = (ushort)(ModDriver.Mode | SharpMikCommon.DMODE_NOISEREDUCTION);
            try
            {
                m_Player.Init<NaudioDriver>("");

                DateTime start = DateTime.Now;
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex);
            }
            m_Mod = m_Player.LoadModule(datas.Value);
            string FragmentShader = datas.Key;

            // create an OpenGL window
            Glut.glutInit();
            Glut.glutInitDisplayMode(Glut.GLUT_DEPTH);
            Glut.glutInitWindowSize(width, height);
            Glut.glutCreateWindow("Downloading...");

            Console.WriteLine(Gl.GetString(StringName.ShadingLanguageVersion));

            // provide the Glut callbacks that are necessary for running this tutorial
            Glut.glutIdleFunc(OnRenderFrame);
            Glut.glutDisplayFunc(OnDisplay);
            Glut.glutCloseFunc(OnClose);
            Glut.glutReshapeFunc(on_resize);

            // compile the shader program
            program = new ShaderProgram(VertexShader, FragmentShader);

            // set the view and projection matrix, which are static throughout this tutorial
            program.Use();
            program["projection_matrix"].SetValue(Matrix4.CreatePerspectiveFieldOfView(0.45f, (float)width / height, 0.1f, 1000f));
            program["view_matrix"].SetValue(Matrix4.LookAt(new Vector3(0, 0, 10), Vector3.Zero, new Vector3(0, 1, 0)));
            //program["text"].SetValue(new Vector2(0, 0));
            // create a square
            square = new VBO<Vector3>(new Vector3[] { new Vector3(-3, 3, 0), new Vector3(3, 3, 0), new Vector3(3, -3, 0), new Vector3(-3, -3, 0) });
            squareElements = new VBO<uint>(new uint[] { 0, 1, 2, 3 }, BufferTarget.ElementArrayBuffer);

            m_Player.Play(m_Mod);

            //m_Player.PlayerStateChangeEvent += new ModPlayer.PlayerStateChangedEvent(m_Player_PlayerStateChangeEvent);

            timer = new Stopwatch();
            timer.Start();

            timer2 = new Stopwatch();
            timer2.Start();

            Glut.glutMainLoop();
        }

        private static void on_resize(int w, int h)
        {
            width = w;
            height = h;
            Gl.Viewport(0, 0, w, h);
        }

        private static void OnClose()
        {
            // dispose of all of the resources that were created
            square.Dispose();
            squareElements.Dispose();
            program.DisposeChildren = true;
            program.Dispose();
        }

        private static void OnDisplay()
        {

        }

        private static void OnRenderFrame()
        {
            // set up the OpenGL viewport and clear both the color and depth bits
            Gl.Viewport(0, 0, width, height);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // use our shader program
            Gl.UseProgram(program);

            int w = Glut.glutGet(Glut.GLUT_WINDOW_WIDTH);
            int h = Glut.glutGet(Glut.GLUT_WINDOW_HEIGHT);

            // transform the square
            program["projection_matrix"].SetValue(Matrix4.CreatePerspectiveFieldOfView(0.45f, (float)w / h, 0.1f, 1000f));
            program["model_matrix"].SetValue(Matrix4.CreateTranslation(new Vector3(0, 0, 0)));
            program["resolution"].SetValue(new Vector2(width, height));
            program["time"].SetValue((float)timer.ElapsedMilliseconds);

            //Console.WriteLine(timer.ElapsedMilliseconds);

            // bind the vertex attribute arrays for the square (the easy way)
            Gl.BindBufferToShaderAttribute(square, program, "vertexPosition");
            Gl.BindBuffer(squareElements);

            // draw the square
            Gl.DrawElements(BeginMode.Quads, squareElements.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);

            Glut.glutSwapBuffers();
        }
        
        private static KeyValuePair<string, string> ReadConfig(bool debug)
        {
            string shader = "";
            string modfile = "";

            if (debug)
            {
                modfile = @"Data\grookles_chips.xm";
                shader = File.ReadAllText(@"Data\Shader.frag");
                return new KeyValuePair<string, string>(shader, modfile);
            }

            WebClient Client = new WebClient();
            var x = Client.OpenRead("https://raw.githubusercontent.com/GiR-Zippo/Updater/main/Updater/Data/Tune.info");
            StreamReader sr = new StreamReader(x);
            string file = sr.ReadToEnd();
            x.Close();
            sr.Close();

            var binary = Client.DownloadData("https://github.com/GiR-Zippo/Updater/raw/main/Updater/Data/" + file);
            string result = Path.GetTempPath();
            FileStream fileStream = new FileStream(result + file, FileMode.Create, FileAccess.Write);
            fileStream.Write(binary, 0, binary.Length);
            fileStream.Close();
            modfile = result + file;

            x = Client.OpenRead("https://raw.githubusercontent.com/GiR-Zippo/Updater/main/Updater/Data/Shader.frag");
            sr = new StreamReader(x);
            shader = sr.ReadToEnd();
            x.Close();
            sr.Close();

            return new KeyValuePair<string, string>(shader, modfile);
        }

        /// <summary>
        /// Check the versions
        /// </summary>
        private static void CheckVersion()
        {
            //Check for new versions
            WebClient Client = new WebClient();
            var x = Client.OpenRead("https://raw.githubusercontent.com/GiR-Zippo/BMP_Classic-Test/master/FFBardMusicPlayer/FFBardMusicPlayer.csproj");
            StreamReader sr = new StreamReader(x);
            string profile = sr.ReadToEnd();
            x.Close();
            string gitversion = "0";
            foreach (var t in profile.Split('\n'))
            {
                if (t.Contains("AssemblyVersion"))
                    gitversion = (t.Split('>')[1].Split('<')[0]);
            }

            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo("FFBardMusicPlayer.exe");
                string version = versionInfo.FileVersion;
                Console.WriteLine("Git Version: " + gitversion);
                Console.WriteLine("Installed Version: " + version);
                //Check the versions
                if (version != null)
                {
                    if (version != gitversion)
                    {
                        Process.Start("FFBardMusicPlayer.exe");
                        Environment.Exit(0);
                    }
                }
            }
            catch { }

            Client = new WebClient();
            Client.DownloadDataCompleted += DownloadDataCompleted;
            Client.DownloadDataAsync(new Uri(@"https://github.com/GiR-Zippo/BMP_Classic-Test/releases/download/"+ gitversion + "/FFBardMusicPlayer" + gitversion + ".zip"));
        }

        /// <summary>
        /// download completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void DownloadDataCompleted(object sender, DownloadDataCompletedEventArgs e)
        {
            byte[] raw = e.Result;
            Console.WriteLine(raw.Length + " bytes received");
            using (var zippedStream = new MemoryStream(raw))
            {
                using (var archive = new ZipArchive(zippedStream))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (entry != null)
                        {
                            using (var unzippedEntryStream = entry.Open())
                            {
                                using (FileStream fs = new FileStream(entry.FullName, FileMode.Create, FileAccess.Write))
                                {
                                    unzippedEntryStream.CopyTo(fs);
                                }
                            }
                        }
                    }
                }
            }
            Console.WriteLine("Finished download...");
            Glut.glutSetWindowTitle("Finished Download...");

            MessageBoxResult result = MessageBox.Show("Download finished", "Updater", MessageBoxButton.OKCancel);
            switch (result)
            {
                case MessageBoxResult.OK:
                    Process.Start("FFBardMusicPlayer.exe");
                    Environment.Exit(0);
                    break;
            }
        }
    }
}
