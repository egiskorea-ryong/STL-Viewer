/**
  ******************************************************************************
  * @file    AppMainForm.cs
  * @author  Ali Batuhan KINDAN
  * @date    21.09.2016
  * @brief   This file contains the implementaion of main application form functionality
  ******************************************************************************
  */

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.Threading;
using STL_Tools;
using OpenTK.Graphics.OpenGL;
using BatuGL;
using Mouse_Orbit;
using System.IO;
using System.Collections.Generic;

namespace STLViewer
{
    public partial class AppMainForm : Form
    {
        bool monitorLoaded = false;
        bool moveForm = false;
        int moveOffsetX = 0;
        int moveOffsetY = 0;
        Batu_GL.VAO_TRIANGLES modelVAO = null; // 3d model vertex array object
        private Orbiter orb;
        Vector3 minPos = new Vector3();
        Vector3 maxPos = new Vector3();
        private const float kScaleFactor = 5.0f;

        public AppMainForm()
        {
            /* dot/comma selection for floating point numbers */
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            InitializeComponent();
            orb = new Orbiter();
            GL_Monitor.MouseDown += orb.Control_MouseDownEvent;
            GL_Monitor.MouseUp += orb.Control_MouseUpEvent;
            GL_Monitor.MouseWheel += orb.Control_MouseWheelEvent;
            GL_Monitor.KeyPress += orb.Control_KeyPress_Event;
        }

        private void DrawTimer_Tick(object sender, EventArgs e)
        {
            orb.UpdateOrbiter(MousePosition.X, MousePosition.Y);
            GL_Monitor.Invalidate();
            if (moveForm)
            {
                this.SetDesktopLocation(MousePosition.X - moveOffsetX, MousePosition.Y - moveOffsetY);
            }
        }

        private void GL_Monitor_Load(object sender, EventArgs e)
        {
            GL_Monitor.AllowDrop = true;
            monitorLoaded = true;
            GL.ClearColor(Color.Black);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Batu_GL.Configure(GL_Monitor, Batu_GL.Ortho_Mode.CENTER);
        }

        private void ConfigureBasicLighting(Color modelColor)
        {
            float[] light_1 = new float[] {
            0.2f * modelColor.R / 255.0f,
            0.2f * modelColor.G / 255.0f,
            0.2f * modelColor.B / 255.0f,
            1.0f };
            float[] light_2 = new float[] {
            10.0f * modelColor.R / 255.0f,
            10.0f * modelColor.G / 255.0f,
            10.0f * modelColor.B / 255.0f,
            1.0f };
            float[] specref = new float[] { 
                0.2f * modelColor.R / 255.0f, 
                0.2f * modelColor.G / 255.0f, 
                0.2f * modelColor.B / 255.0f, 
                1.0f };
            float[] specular_0 = new float[] { -1.0f, -1.0f, 1.0f, 1.0f };
            float[] specular_1 = new float[] { 1.0f, -1.0f, 1.0f, 1.0f };
            float[] lightPos_0 = new float[] { 1000f, 1000f, -200.0f, 0.0f };
            float[] lightPos_1 = new float[] { -1000f, 1000f, -200.0f, 0.0f };

            GL.Enable(EnableCap.Lighting);
            /* light 0 */
            GL.Light(LightName.Light0, LightParameter.Ambient, light_1);
            GL.Light(LightName.Light0, LightParameter.Diffuse, light_2);
            GL.Light(LightName.Light0, LightParameter.Specular, specular_0);
            GL.Light(LightName.Light0, LightParameter.Position, lightPos_0);
            GL.Enable(EnableCap.Light0);
            /* light 1 */
            GL.Light(LightName.Light1, LightParameter.Ambient, light_1);
            GL.Light(LightName.Light1, LightParameter.Diffuse, light_2);
            GL.Light(LightName.Light1, LightParameter.Specular, specular_1);
            GL.Light(LightName.Light1, LightParameter.Position, lightPos_1);
            GL.Enable(EnableCap.Light1);
            /*material settings  */
            GL.Enable(EnableCap.ColorMaterial);
            GL.ColorMaterial(MaterialFace.Front, ColorMaterialParameter.AmbientAndDiffuse);
            GL.Material(MaterialFace.Front, MaterialParameter.Specular, specref);
            GL.Material(MaterialFace.Front, MaterialParameter.Shininess, 10);
            GL.Enable(EnableCap.Normalize);
        }

        private void GL_Monitor_Paint(object sender, PaintEventArgs e)
        {
            if (!monitorLoaded)
                return;

            Batu_GL.Configure(GL_Monitor, Batu_GL.Ortho_Mode.CENTER);
            if (modelVAO != null) ConfigureBasicLighting(modelVAO.color);
            GL.Translate(orb.PanX, orb.PanY, 0);
            GL.Rotate(orb.orbitStr.angle, orb.orbitStr.ox, orb.orbitStr.oy, orb.orbitStr.oz);
            GL.Scale(orb.scaleVal * kScaleFactor, orb.scaleVal * kScaleFactor, orb.scaleVal * kScaleFactor); // small multiplication factor to scaling
            GL.Translate(-minPos.x, -minPos.y, -minPos.z);
            GL.Translate(-(maxPos.x - minPos.x) / 2.0f, -(maxPos.y - minPos.y) / 2.0f, -(maxPos.z - minPos.z) / 2.0f);
            if (modelVAO != null) modelVAO.Draw();

            GL_Monitor.SwapBuffers();
        }

        private void ReadSelectedFile(string fileName)
        {
            STLReader stlReader = new STLReader(fileName);
            TriangleMesh[] meshArray = stlReader.ReadFile();
            modelVAO = new Batu_GL.VAO_TRIANGLES();
            modelVAO.parameterArray = STLExport.Get_Mesh_Vertices(meshArray);
            modelVAO.normalArray = STLExport.Get_Mesh_Normals(meshArray);
            modelVAO.color = Color.Crimson;
            minPos = stlReader.GetMinMeshPosition(meshArray);
            maxPos = stlReader.GetMaxMeshPosition(meshArray);
            orb.Reset_Orientation();
            orb.Reset_Pan();
            orb.Reset_Scale();
            if (stlReader.Get_Process_Error())
            { 
                modelVAO = null;
                /* if there is an error, deinitialize the gl monitor to clear the screen */
                Batu_GL.Configure(GL_Monitor, Batu_GL.Ortho_Mode.CENTER);
                GL_Monitor.SwapBuffers();
            }
        }
        public enum ColorType
        {
            Green,
            Red,
            Blue
        }

        // 색상 점을 표현하는 클래스
        public class ColorPoint
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
            public int greenIndex { get; set; }
            public ColorType Color { get; set; }
        }
        private void FileMenuImportBt_Click(object sender, EventArgs e)
        {
            OpenFileDialog newFileDialog = new OpenFileDialog();
            newFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp";

            if (newFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = newFileDialog.FileName;
                Bitmap image = new Bitmap(filePath);
                pictureBox1.Image = image;

                // 이미지를 처리합니다.
                var greenRegions = DetectColorRegions(image, ColorType.Green);
                var colorPoints = new List<ColorPoint>();
                int index = 0;
                foreach (var region in greenRegions)
                {
                    colorPoints.AddRange(DetectAreaColorRegions(image, region, index));
                    index++;
                }

                // 녹색 영역과 색상 점을 이미지에 그립니다.
                DrawDetectedRegions(image, greenRegions, colorPoints);
            }
        }
        private void DrawDetectedRegions(Bitmap image, List<Rectangle> regions, List<ColorPoint> colorPoints)
        {
            using (Graphics g = Graphics.FromImage(image))
            {
                // 녹색 영역을 그립니다.
                using (Pen greenPen = new Pen(Color.Lime, 2))
                {
                    foreach (var region in regions)
                    {
                        g.DrawRectangle(greenPen, region);
                    }
                }

                // 빨강색과 파랑색 점을 그립니다.
                foreach (var point in colorPoints)
                {
                    Brush brush = point.Color == ColorType.Red ? Brushes.Blue : Brushes.Red;
                    g.FillEllipse(brush, point.X - 5, point.Y - 5, 10, 10);
                }
            }

            // 결과 이미지를 PictureBox에 다시 설정합니다.
            pictureBox1.Image = image;
        }

        // 특정 색상 영역을 검출하는 메서드
        private List<Rectangle> DetectColorRegions(Bitmap image, ColorType colorType)
        {
            var regions = new List<Rectangle>();
            bool[,] visited = new bool[image.Width, image.Height];

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    if (!visited[x, y])
                    {
                        Color pixel = image.GetPixel(x, y);
                        if (IsGreen(pixel))
                        {
                            Rectangle region = FloodFill(image, x, y, visited, ColorType.Green);
                            if (region.Width > 5 && region.Height > 5)
                                regions.Add(region);
                        }
                    }
                }
            }

            return regions;
        }
        private List<ColorPoint> DetectAreaColorRegions(Bitmap image, Rectangle rect, int index)
        {
            var regions = new List<(Rectangle, ColorType color)>();
            bool[,] visited = new bool[rect.Right, rect.Bottom];

            for (int x = rect.Left; x < rect.Right; x++)
            {
                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    if (!visited[x, y])
                    {
                        Color pixel = image.GetPixel(x, y);
                        if (IsRed(pixel))
                        {
                            Rectangle region = FloodFill(image, x, y, visited, ColorType.Red);
                            if (region.Width > 10 && region.Height > 10)
                                regions.Add((region, ColorType.Red));
                        }
                        else if (IsBlue(pixel))
                        {
                            Rectangle region = FloodFill(image, x, y, visited, ColorType.Blue);
                            if (region.Width > 10 && region.Height > 10)
                                regions.Add((region, ColorType.Blue));
                        }
                    }
                }
            }

            var points = new List<ColorPoint>();
            foreach (var region in regions)
            {
                int x = (region.Item1.Left + region.Item1.Right) / 2;
                int y = (region.Item1.Top + region.Item1.Bottom) / 2;

                points.Add(new ColorPoint { X = x, Y = y, greenIndex = index, Color = region.Item2 });
            }

            return points;
        }
        // Flood Fill 알고리즘을 사용하여 연속된 영역을 탐지
        private Rectangle FloodFill(Bitmap image, int startX, int startY, bool[,] visited, ColorType colorType)
        {
            Queue<Point> queue = new Queue<Point>();
            queue.Enqueue(new Point(startX, startY));
            visited[startX, startY] = true;

            int minX = startX, minY = startY, maxX = startX, maxY = startY;

            while (queue.Count > 0)
            {
                Point current = queue.Dequeue();
                int x = current.X;
                int y = current.Y;

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);

                foreach (var offset in new[] { new Point(-1, 0), new Point(1, 0), new Point(0, -1), new Point(0, 1) })
                {
                    int newX = x + offset.X;
                    int newY = y + offset.Y;

                    if (newX >= 0 && newX < image.Width && newY >= 0 && newY < image.Height && !visited[newX, newY])
                    {
                        Color pixel = image.GetPixel(newX, newY);
                        if ((colorType == ColorType.Green && IsGreen(pixel)) || (colorType == ColorType.Red && IsRed(pixel)) || (colorType == ColorType.Blue && IsBlue(pixel)))
                        {
                            visited[newX, newY] = true;
                            queue.Enqueue(new Point(newX, newY));
                        }
                    }
                }
            }

            return new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        // 연두색인지 확인하는 메서드
        private bool IsGreen(Color color) => color.G > 240 && color.R < 10 && color.B < 10;
        // 빨강색인지 확인하는 메서드
        private bool IsRed(Color color) => color.R > 240 && color.G < 10 && color.B < 10;
        // 파랑색인지 확인하는 메서드
        private bool IsBlue(Color color) => color.B > 240 && color.R < 10 && color.G < 10;
        private void FileMenuExitBt_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void CloseBt_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void MinimizeBt_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void AppToolBarMStp_MouseDown(object sender, MouseEventArgs e)
        {
            moveForm = true;
            moveOffsetX = MousePosition.X - this.Location.X;
            moveOffsetY = MousePosition.Y - this.Location.Y;
        }

        private void AppToolBarMStp_MouseUp(object sender, MouseEventArgs e)
        {
            moveForm = false;
            moveOffsetX = 0;
            moveOffsetY = 0;
        }

        private void AppToolBarMStp_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized) this.WindowState = FormWindowState.Normal;
            else this.WindowState = FormWindowState.Maximized;
        }

        private void AppTitleLb_MouseDown(object sender, MouseEventArgs e)
        {
            moveForm = true;
            moveOffsetX = MousePosition.X - this.Location.X;
            moveOffsetY = MousePosition.Y - this.Location.Y;
        }

        private void AppTitleLb_MouseUp(object sender, MouseEventArgs e)
        {
            moveForm = false;
            moveOffsetX = 0;
            moveOffsetY = 0;
        }

        private void AppTitleLb_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized) this.WindowState = FormWindowState.Normal;
            else this.WindowState = FormWindowState.Maximized;
        }

        private void MaximizeBt_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized) this.WindowState = FormWindowState.Normal;
            else this.WindowState = FormWindowState.Maximized;
        }

        private void GL_Monitor_DragDrop(object sender, DragEventArgs e)
        {
            var data = e.Data.GetData(DataFormats.FileDrop);
            if (data != null)
            {
                string[] fileNames = data as string[];
                string ext = System.IO.Path.GetExtension(fileNames[0]);
                if (fileNames.Length > 0 && (ext == ".stl" || ext == ".STL" || ext == ".txt" || ext == ".TXT"))
                {
                    ReadSelectedFile(fileNames[0]);
                }
            }
        }

        private void GL_Monitor_DragEnter(object sender, DragEventArgs e)
        {
            // if the extension is not *.txt or *.stl change drag drop effect symbol
            var data = e.Data.GetData(DataFormats.FileDrop);
            if (data != null)
            {
                string[] fileNames = data as string[];
                string ext = System.IO.Path.GetExtension(fileNames[0]);
                if (ext == ".stl" || ext == ".STL" || ext == ".txt" || ext == ".TXT") 
                {
                    e.Effect = DragDropEffects.Copy;
                }
                else
                {
                    e.Effect = DragDropEffects.None;
                }
            }                
        }

        private void HelpMenuHowToUseBt_Click(object sender, EventArgs e)
        {
            AppHowToUseForm newHowToUseForm = new AppHowToUseForm();
            newHowToUseForm.ShowDialog();
        }

        private void HelpMenuAboutBt_Click(object sender, EventArgs e)
        {
            AppAboutForm aboutForm = new AppAboutForm();
            aboutForm.ShowDialog();
        }

        private void FileMenuViewerBt_Click(object sender, EventArgs e)
        {
            pictureBox1.Visible = false;

            string referenceImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "result.stl");
            ReadSelectedFile(referenceImagePath);
        }
    }
}