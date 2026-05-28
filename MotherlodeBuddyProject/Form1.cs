using MotherlodeBuddyProject.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MotherlodeBuddyProject
{
    //TODO: zrobić aby koordynaty X były w lustrzanym odbiciu, dzięki czemu położenie punktów na mapie (bez skalowania) będzie poprawne
    public enum SupportedMaps
    {
        AreaKurMountains,
        AreaDesert1,
        AreaGazluk
    }
    public partial class Form1 : Form
    {
        private float mapScaleFactorWidth;
        private float mapScaleFactorHeight;
        private float mapZoomFactor = 1;
        private bool mousePressed;
        private float offsetX;
        private float offsetZ;
        private bool mirroredX;
        private bool mirroredZ;
        private SupportedMaps currentArea;
        private Image currentImageOriginal;
        private Point lastMouseLocation;
        private AreaDatabase areaDatabase;


        public Form1()
        {
            InitializeComponent();
            SizeChanged += Form1_SizeChanged;
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            mapPreviewImageContainer.MouseDown += mapPreviewImage_MouseDown;
            mapPreviewImageContainer.MouseMove += mapPreviewImage_MouseMove;
            mapPreviewImageContainer.MouseUp += mapPreviewImage_MouseUp;
            mapPreviewImageContainer.MouseWheel += mapPreviewImage_MouseWheel;
            mapPreviewImageContainer.Paint += mapPreviewImage_Paint;
            string jsonString = Encoding.UTF8.GetString(Resources.LandmarksJSON);
            areaDatabase = new AreaDatabase();
            areaDatabase.DeserializeLandmarks();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (mapPreviewImageContainer.Image == null) return;

            RefreshMapVariables();
            ClampOffset();
            mapPreviewImageContainer.Invalidate();

        }
        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (mapPreviewImageContainer.Image != null)
            {
                mapPreviewImageContainer.Image.Dispose();
                mapPreviewImageContainer.Invalidate();
            }

            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    mapPreviewImageContainer.Image = Resources.Map_AreaKurMountains;
                    currentImageOriginal = mapPreviewImageContainer.Image;
                    currentArea = SupportedMaps.AreaKurMountains;
                    mirroredX = true;
                    break;
                case 1:
                    mapPreviewImageContainer.Image = Resources.Map_AreaDesert1;
                    currentImageOriginal = mapPreviewImageContainer.Image;
                    currentArea = SupportedMaps.AreaDesert1;
                    mirroredZ = true;
                    break;
                case 2:
                    mapPreviewImageContainer.Image = Resources.Map_AreaGazluk;
                    currentImageOriginal = mapPreviewImageContainer.Image;
                    currentArea = SupportedMaps.AreaGazluk;
                    mirroredZ = true;
                    break;
                default:
                    break;
            }
            RefreshMapVariables();
            ClampOffset();
        }
        private void RefreshMapVariables()
        {
            mapScaleFactorWidth = (float)(mapPreviewImageContainer.Width) / (float)(currentImageOriginal.Width);

            mapScaleFactorHeight = (float)(mapPreviewImageContainer.Height) / (float)(currentImageOriginal.Height);

        }
        private void mapPreviewImage_MouseDown(object sender, MouseEventArgs e)
        {
            if (mapPreviewImageContainer.Image == null) return;

            MouseEventArgs mouse = e;
            if (mouse.Button == MouseButtons.Left)
            {
                mousePressed = true;
                lastMouseLocation = mouse.Location;
            }
        }
        private void mapPreviewImage_MouseUp(object sender, MouseEventArgs e)
        {
            mousePressed = false;
        }
        private void mapPreviewImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (mapPreviewImageContainer.Image == null) return;
            MouseEventArgs mouse = e;
            label4.Text = "Mouse location X:"+mouse.Location.X.ToString()+" Y:"+mouse.Location.Y.ToString();
            float xReal;
            float zReal;
            if (mirroredX)
            {
                xReal = mouse.Location.X / mapScaleFactorWidth;
                xReal = currentImageOriginal.Width - xReal;
            }
            else
                xReal = mouse.Location.X / mapScaleFactorWidth;

            if (mirroredZ)
            {
                zReal = mouse.Location.Y / mapScaleFactorHeight;
                zReal = currentImageOriginal.Width - zReal;
            }
            else
                zReal = mouse.Location.Y / mapScaleFactorHeight;


            label5.Text = "World location X:"+xReal.ToString()+" Z:"+ zReal.ToString();

            if (!mousePressed || mapZoomFactor == mapScaleFactorWidth || mapZoomFactor == mapScaleFactorHeight) return;


            int deltaX = mouse.Location.X - lastMouseLocation.X;
            int deltaY = mouse.Location.Y - lastMouseLocation.Y;

            offsetX += deltaX;
            offsetZ += deltaY;
            ClampOffset();


            lastMouseLocation = mouse.Location;
            mapPreviewImageContainer.Invalidate();
        }
        private void ClampOffset()
        {
            float newWidth = currentImageOriginal.Width * mapZoomFactor;
            float newHeight = currentImageOriginal.Height * mapZoomFactor;

            float minX = mapPreviewImageContainer.Width - newWidth;
            float minY = mapPreviewImageContainer.Height - newHeight;

            if (newWidth > mapPreviewImageContainer.Width)
                offsetX = Math.Max(minX, Math.Min(0, offsetX));
            else
                offsetX = (mapPreviewImageContainer.Width - newWidth) / 2;

            if (newHeight > mapPreviewImageContainer.Height)
                offsetZ = Math.Max(minY, Math.Min(0, offsetZ));
            else
                offsetZ = (mapPreviewImageContainer.Height - newHeight) / 2;


        }
        private void mapPreviewImage_MouseWheel(object sender, MouseEventArgs e)
        {
            if (mapPreviewImageContainer.Image == null) return;

            if (e.Delta > 0)
                mapZoomFactor += 0.1f;

            if (e.Delta < 0)
                mapZoomFactor = Math.Max(mapZoomFactor - 0.1f, 1f);

            

            ClampOffset();
            mapPreviewImageContainer.Invalidate();

        }

        //private void mapPreviewImage_Paint(object sender, PaintEventArgs e)
        //{
        //    if (mapPreviewImageContainer.Image == null) return;


        //    float newWidth = currentImageOriginal.Width * mapZoomFactor;
        //    float newHeight = currentImageOriginal.Height * mapZoomFactor;

        //    RectangleF destRect = new RectangleF(offsetX, offsetZ, newWidth, newHeight);
        //    RectangleF srcRect = new RectangleF(0, 0, currentImageOriginal.Width,   currentImageOriginal.Height);


        //    e.Graphics.DrawImage(currentImageOriginal, destRect, srcRect, GraphicsUnit.Pixel);
        //    Brush pointBrush = new SolidBrush(Color.Red);

        //    foreach (var landmark in areaDatabase.allAreasData[currentArea.ToString()].Landmarks)
        //    {
        //        PointF originalPixel = GetWorldPixelCoordinates(landmark.WorldXCoordinate, landmark.WorldZCoordinate);

        //        PointF screenPixel = GetScreenCoordinates(originalPixel, mapZoomFactor, offsetX, offsetZ);

        //        e.Graphics.FillEllipse(pointBrush, originalPixel.X - 5, originalPixel.Y - 5, 10, 10);
        //    }

        //    pointBrush.Dispose();
        //}
        private void mapPreviewImage_Paint(object sender, PaintEventArgs e)
        {
            if (mapPreviewImageContainer.Image == null) return;
            RectangleF srcRect = new RectangleF(0, 0, currentImageOriginal.Width * mapScaleFactorWidth, currentImageOriginal.Height * mapScaleFactorHeight);


            e.Graphics.DrawImage(currentImageOriginal, srcRect);
            Brush pointBrush = new SolidBrush(Color.Red);

            foreach (var landmark in areaDatabase.allAreasData[currentArea.ToString()].Landmarks)
            {
                PointF originalPixel = GetWorldPixelCoordinates(landmark.WorldXCoordinate, landmark.WorldZCoordinate);

                //PointF screenPixel = GetScreenCoordinates(originalPixel, mapZoomFactor, offsetX, offsetZ);

                e.Graphics.FillEllipse(pointBrush, originalPixel.X - 5, originalPixel.Y - 5, 10, 10);
            }

            pointBrush.Dispose();
        }
        public PointF GetWorldPixelCoordinates(float worldX, float worldZ)
        {
            float pixelX = worldX * ((mapPreviewImageContainer.Width) / (currentImageOriginal.Width));
            float pixelZ = worldZ * ((mapPreviewImageContainer.Height) / (currentImageOriginal.Height));

            return new PointF(pixelX, pixelZ);
        }
        public PointF GetScreenCoordinates(PointF originalPixel, float zoomFactor, float viewOffsetX, float viewOffsetY)
        {
            float screenX = (originalPixel.X * zoomFactor) + viewOffsetX;
            float screenY = (originalPixel.Y * zoomFactor) + viewOffsetY;

            return new PointF(screenX, screenY);
        }
        private void placeKnownLandmarks(string areaName, PaintEventArgs e)
        {
            Pen whitePen = new Pen(Brushes.White);
            
        }
    }
    public class Landmark 
    {
        [JsonPropertyName("Name")]
        public string LandmarkName { get; set;  }
        [JsonPropertyName("X")]
        public float WorldXCoordinate { get; set; }
        [JsonPropertyName("Z")]
        public float WorldZCoordinate { get; set; }
    }
    public class Anchor
    {
        [JsonPropertyName("X")]
        public float WorldXCoordinate { get; set; }
        [JsonPropertyName("Z")]
        public float WorldZCoordinate { get; set; }
        [JsonPropertyName("ImageX")]
        public int ImageXCoordinate { get; set; }
        [JsonPropertyName("ImageZ")]
        public int ImageZCoordinate { get; set; }
    }
    public class AreaData
    {
        public List<Landmark> Landmarks { get; set; }
        public List<Anchor> Anchors { get; set; }
    }
    public class AreaDatabase : ISerializable
    {
        public Dictionary<string, AreaData> allAreasData;

        public void DeserializeLandmarks()
        {
            allAreasData = JsonSerializer.Deserialize<Dictionary<string, AreaData>>(Resources.LandmarksJSON);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            return;
        }
    }

}
