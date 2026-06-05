using MotherlodeBuddyProject.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

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
        private float mapScaleRatioWidth;
        private float mapScaleRatioHeight;
        private float mapZoomFactor = 1;
        private bool mousePressed;
        private bool calculatePaintMotherlodePositions;
        private float offsetX;
        private float offsetZ;
        private SupportedMaps currentArea;
        private Image currentImageOriginal;
        private Point lastMouseLocation;
        private AreaDatabase areaDatabase;
        private List<Motherlode> motherlodes = new List<Motherlode>();

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
            checkedListBox2.ItemCheck += checkedListBox2_ItemCheck;
            button1.Click += listBox1_SelectedIndexChanged;
            string jsonString = Encoding.UTF8.GetString(Resources.LandmarksJSON);
            areaDatabase = new AreaDatabase();
            areaDatabase.DeserializeLandmarks();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (mapPreviewImageContainer.Image == null) return;

            mapScaleRatioWidth = mapPreviewImageContainer.Width/mapPreviewImageContainer.Height;
            mapScaleRatioHeight = mapPreviewImageContainer.Height/mapPreviewImageContainer.Width;

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
                ClearReferencePointChecklist();
            }

            switch (comboBox1.SelectedIndex)
            {
                case 0:
                    mapPreviewImageContainer.Image = Resources.Map_AreaKurMountains;
                    currentImageOriginal = mapPreviewImageContainer.Image;
                    currentArea = SupportedMaps.AreaKurMountains;
                    break;
                case 1:
                    mapPreviewImageContainer.Image = Resources.Map_AreaDesert1;
                    currentImageOriginal = mapPreviewImageContainer.Image;
                    currentArea = SupportedMaps.AreaDesert1;
                    break;
                case 2:
                    mapPreviewImageContainer.Image = Resources.Map_AreaGazluk;
                    currentImageOriginal = mapPreviewImageContainer.Image;
                    currentArea = SupportedMaps.AreaGazluk;
                    break;
                default:
                    break;
            }
            
            if (listBox1.Items.Count == 0)
                return;
            PopulateReferencePointChecklist();
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
        private void ClearReferencePointChecklist()
        {
            checkedListBox2.Items.Clear();
        }
        private void PopulateReferencePointChecklist()
        {
            if(checkedListBox2.Items.Count == 0)
                ClearReferencePointChecklist();

            foreach (Landmark landmark in areaDatabase.allAreasData[currentArea.ToString()].ToList())
                checkedListBox2.Items.Add(landmark.Name);
        }
        private void mapPreviewImage_MouseUp(object sender, MouseEventArgs e)
        {
            mousePressed = false;
        }
        private void mapPreviewImage_MouseMove(object sender, MouseEventArgs e)
        {
            
        }

        public static Point TranslateImagePointToControl(Point imagePoint, PictureBox pictureBox)
        {
            if (pictureBox.Image == null)
            {
                return imagePoint;
            }

            double scaleWidth = (double)pictureBox.ClientSize.Width / pictureBox.Image.Width;
            double scaleHeight = (double)pictureBox.ClientSize.Height / pictureBox.Image.Height;

            double scale = Math.Min(scaleWidth, scaleHeight);

            double displayWidth = pictureBox.Image.Width * scale;
            double displayHeight = pictureBox.Image.Height * scale;

            double offsetX = (pictureBox.ClientSize.Width - displayWidth) / 2.0;
            double offsetY = (pictureBox.ClientSize.Height - displayHeight) / 2.0;

            int translatedX = (int)Math.Round(imagePoint.X * scale + offsetX);
            int translatedY = (int)Math.Round(imagePoint.Y * scale + offsetY);

            return new Point(translatedX, translatedY);
        }
        private void mapPreviewImage_MouseWheel(object sender, MouseEventArgs e)
        {
            if (mapPreviewImageContainer.Image == null) return;
            return;
            if (e.Delta > 0)
                mapZoomFactor += 0.1f;

            if (e.Delta < 0)
                mapZoomFactor = Math.Max(mapZoomFactor - 0.1f, 1f);

            mapPreviewImageContainer.Invalidate();

        }

        
        private void mapPreviewImage_Paint(object sender, PaintEventArgs e)
        {
            if (mapPreviewImageContainer.Image == null) return;
            RectangleF srcRect = new RectangleF(0, 0, currentImageOriginal.Width * mapScaleRatioWidth, currentImageOriginal.Height * mapScaleRatioHeight);


            e.Graphics.DrawImage(currentImageOriginal, srcRect);
            Pen pointPen = new Pen(Color.Red);
            SolidBrush pointBrush = new SolidBrush(Color.White);
            Font font = new Font("Times New Roman", 11f);

            List<Landmark> landmarks = areaDatabase.allAreasData[currentArea.ToString()].ToList();

            foreach (Landmark landmark in landmarks)
            {
                var (pointXWorld, pointZWorld) = LocationExtractor.ExtractXZ(landmark.WorldLocation);

                Point point = TranslateImagePointToControl(new Point((int)pointXWorld, (int)pointZWorld), mapPreviewImageContainer);

                e.Graphics.DrawEllipse(pointPen, point.X - 1.25f, point.Y - 1.25f, 2.5f, 2.5f);
                e.Graphics.DrawString(landmark.Name, font, pointBrush, point);
            }

            if (calculatePaintMotherlodePositions)
            {
                Pen pen = new Pen(Color.Blue);
                int i = 0;
                foreach(Motherlode motherlode in motherlodes)
                {
                    Point motherlodePosition = CalculateMotherlodeLocation(motherlode);
                    Point positionTranslated = TranslateImagePointToControl(new Point((int)motherlodePosition.X, (int)motherlodePosition.Y), mapPreviewImageContainer);

                    e.Graphics.DrawEllipse(pen, positionTranslated.X - 1.25f, positionTranslated.Y - 1.25f, 2.5f, 2.5f);
                    e.Graphics.DrawString(listBox1.Items[i].ToString(), font, pointBrush, positionTranslated);
                    i++;
                }


            }

            pointPen.Dispose();
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            

            
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1) return;

            int targetCount = (int)numericUpDown1.Value;
            int itemsToAdd = targetCount - motherlodes.Count;
            int itemsToRemove = motherlodes.Count - targetCount;
            if (targetCount > 0 && listBox1.Items.Count == 0)
                PopulateReferencePointChecklist();

            if (motherlodes.Count == 0)
            {
                for (int i = (int)numericUpDown1.Value; i > motherlodes.Count; i--)
                {
                    motherlodes.Add(new Motherlode());
                    listBox1.Items.Add("Motherlode " + i);
                }
                return;
            }

            if (itemsToRemove > 0)
            {
                for(int i = 0; i < itemsToRemove; i++)
                {
                    motherlodes.RemoveAt(motherlodes.Count - 1);
                }
            }
            else if(itemsToAdd > 0)
            {
                Motherlode templateMotherlode = motherlodes[motherlodes.Count - 1];

                for (int i = 0; i < itemsToAdd; i++)
                {
                    Motherlode newMotherlode = new Motherlode();

                    if (templateMotherlode.referencePoint1 != null)
                        newMotherlode.referencePoint1 = templateMotherlode.referencePoint1.Clone();

                    if (templateMotherlode.referencePoint2 != null)
                        newMotherlode.referencePoint2 = templateMotherlode.referencePoint2.Clone();

                    if (templateMotherlode.referencePoint3 != null)
                        newMotherlode.referencePoint3 = templateMotherlode.referencePoint3.Clone();

                    motherlodes.Add(newMotherlode);
                }
            }

            if(motherlodes.Count == 0 && numericUpDown1.Value == 0)
                ClearReferencePointChecklist();

            listBox1.Items.Clear();
            for (int i=1; numericUpDown1.Value + 1 > i; i++) 
                listBox1.Items.Add("Motherlode "+i);
        }
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1)
                listBox1.SelectedIndex = listBox1.Items.Count - 1;

            if (motherlodes[listBox1.SelectedIndex].referencePoint1 == null)
                numericUpDown2.Value = 0;
            else
                numericUpDown2.Value = motherlodes[listBox1.SelectedIndex].referencePoint1.GetDistance();

            if (motherlodes[listBox1.SelectedIndex].referencePoint2 == null)
                numericUpDown3.Value = 0;
            else
                numericUpDown3.Value = motherlodes[listBox1.SelectedIndex].referencePoint2.GetDistance();

            if (motherlodes[listBox1.SelectedIndex].referencePoint3 == null)
                numericUpDown4.Value = 0;
            else
                numericUpDown4.Value = motherlodes[listBox1.SelectedIndex].referencePoint3.GetDistance();
        }

        private void tableLayoutPanel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }
        private void checkedListBox2_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                if (checkedListBox2.CheckedItems.Count >= 3)
                {
                    e.NewValue = CheckState.Unchecked;
                    return;
                }
            }


            foreach(Motherlode motherlode in motherlodes)
            {
                Landmark relatedLandmark = areaDatabase.allAreasData[currentArea.ToString()][e.Index];
                if (e.NewValue == CheckState.Checked)
                {
                    if (motherlode.referencePoint1 == null)
                    {
                        motherlode.referencePoint1 = new Circle(0,relatedLandmark);
                        label6.Text = relatedLandmark.Name.ToString();
                    }
                    else if (motherlode.referencePoint2 == null)
                    {
                        motherlode.referencePoint2 = new Circle(0, relatedLandmark);
                        label7.Text = relatedLandmark.Name.ToString();
                    }
                    else if (motherlode.referencePoint3 == null)
                    {
                        motherlode.referencePoint3 = new Circle(0, relatedLandmark);
                        label8.Text = relatedLandmark.Name.ToString();
                    }
                }
                if (e.NewValue == CheckState.Unchecked)
                {
                    if(motherlode.referencePoint1 != null && motherlode.referencePoint1.GetLandmark().WorldLocation == relatedLandmark.WorldLocation)
                    {
                        motherlode.referencePoint1 = null;
                        label6.Text = "Reference 1";
                    }else if (motherlode.referencePoint2 != null && motherlode.referencePoint2.GetLandmark().WorldLocation == relatedLandmark.WorldLocation)
                    {
                        motherlode.referencePoint2 = null;
                        label7.Text = "Reference 2";
                    }else if (motherlode.referencePoint3 != null && motherlode.referencePoint3.GetLandmark().WorldLocation == relatedLandmark.WorldLocation)
                    {
                        motherlode.referencePoint3 = null;
                        label8.Text = "Reference 3";
                    }
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            if (motherlodes.Count == 0)
                return;
            if (listBox1.SelectedIndex == -1)
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
            if (motherlodes[listBox1.SelectedIndex].referencePoint1 == null)
                return;
            motherlodes[listBox1.SelectedIndex].referencePoint1.SetDistance((int)numericUpDown2.Value);
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            if (motherlodes.Count == 0)
                return;
            if (listBox1.SelectedIndex == -1)
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
            if (motherlodes[listBox1.SelectedIndex].referencePoint2 == null)
                return;
            motherlodes[listBox1.SelectedIndex].referencePoint2.SetDistance((int)numericUpDown3.Value);
        }

        private void numericUpDown4_ValueChanged(object sender, EventArgs e)
        {
            if (motherlodes.Count == 0)
                return;
            if (listBox1.SelectedIndex == -1)
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
            if (motherlodes[listBox1.SelectedIndex].referencePoint3 == null)
                return;
            motherlodes[listBox1.SelectedIndex].referencePoint3.SetDistance((int)numericUpDown4.Value);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(checkedListBox2.CheckedItems.Count < 3)
            {
                MessageBox.Show("Select 3 reference points from the reference points table, then set distances for each reference point.", "Missing reference points!");
                return;
            }
            if (motherlodes.Count == 0)
            {
                MessageBox.Show("Set the motherlode amount, select 3 reference points from the reference points table and set distances for each reference point.", "No motherlodes!"); 
                return;
            }
            
            calculatePaintMotherlodePositions = true;

            mapPreviewImageContainer.Invalidate();
        }

        private void tableLayoutPanel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tableLayoutPanel3_Paint(object sender, PaintEventArgs e)
        {

        }
        public Point CalculateMotherlodeLocation(Motherlode motherlode)
        {
            Point point1 = motherlode.referencePoint1.GetLandmarkLocation();
            Point point2 = motherlode.referencePoint2.GetLandmarkLocation();
            Point point3 = motherlode.referencePoint3.GetLandmarkLocation();
            int distance1 = motherlode.referencePoint1.GetDistance();
            int distance2 = motherlode.referencePoint2.GetDistance();
            int distance3 = motherlode.referencePoint3.GetDistance();

            float a = 2 * point2.X - 2 * point1.X;
            float b = 2 * point2.Y - 2 * point1.Y;
            float c = (float)(Math.Pow(distance1, 2) - Math.Pow(distance2, 2) - Math.Pow(point1.X, 2) + Math.Pow(point2.X, 2) - Math.Pow(point1.Y, 2) + Math.Pow(point2.Y, 2));

            float d = 2 * point3.X - 2 * point1.X;
            float e = 2 * point3.Y - 2 * point1.Y;
            float f = (float)(Math.Pow(distance1, 2) - Math.Pow(distance3, 2) - Math.Pow(point1.X, 2) + Math.Pow(point3.X, 2) - Math.Pow(point1.Y, 2) + Math.Pow(point3.Y, 2));

            float X = (c*e - f*b) / (e*a - b*d);
            float Y = (c*d - a * f) / (b * d - a * e);

            return new Point((int)X, (int)Y);
        }

        private void button4_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("This function will delete ALL motherlodes you added, as well as reset reference points! Choose OK to proceed.", "Caution! Nuclear option!", MessageBoxButtons.OKCancel);
            if (confirmResult == DialogResult.Cancel)
                return;

            numericUpDown1.Value = 0;
            listBox1.Items.Clear();
            checkedListBox2.Items.Clear();
            motherlodes.Clear();

            label6.Text = "Reference 1";
            label7.Text = "Reference 2";
            label8.Text = "Reference 3";

            calculatePaintMotherlodePositions = false;
            mapPreviewImageContainer.Invalidate();
        }
    }
    public class Landmark 
    {
        [JsonPropertyName("Desc")]
        public string Description { get; set; }
        [JsonPropertyName("Name")]
        public string Name { get; set;  }
        [JsonPropertyName("Loc")]
        public string WorldLocation { get; set; }
        [JsonPropertyName("Type")]
        public string Type { get; set; }
    }
    public class AreaDatabase : ISerializable
    {
        public Dictionary<string, List<Landmark>> allAreasData;

        public void DeserializeLandmarks()
        {
            allAreasData = JsonSerializer.Deserialize<Dictionary<string, List<Landmark>>>(Resources.LandmarksJSON);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            return;
        }
    }
    public static class LocationExtractor
    {
        private static readonly Regex XzRegex = new Regex(@"x:\s*([-\d.]+)\s+y:\s*[-\d.]+\s+z:\s*([-\d.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static (double X, double Z) ExtractXZ(string locStr)
        {
            Match match = XzRegex.Match(locStr);

            if (match.Success)
            {
                double x = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                double z = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

                return (x, z);
            }

            throw new FormatException();
        }
    }
    public class Motherlode
    {
        public Circle referencePoint1 { get; set; }
        public Circle referencePoint2 { get; set; }
        public Circle referencePoint3 { get; set; }

        public void ClonePointsFrom(Motherlode sourceMotherlode)
        {
            referencePoint1 = sourceMotherlode.referencePoint1?.Clone();
            referencePoint2 = sourceMotherlode.referencePoint2?.Clone();
            referencePoint3 = sourceMotherlode.referencePoint3?.Clone();
        }
    }
    public class Circle
    {
        private int distanceToMotherlode;
        Landmark referencePoint;

        public Circle(int distance, Landmark referencePoint)
        {
            this.distanceToMotherlode = distance;
            this.referencePoint = referencePoint;
        }

        public Landmark GetLandmark()
        {
            return referencePoint;
        }
        public Point GetLandmarkLocation()
        {
            var(pointXWorld, pointZWorld) = LocationExtractor.ExtractXZ(referencePoint.WorldLocation);


            Point point = new Point((int)pointXWorld, (int)pointZWorld);
            return point;
        }
        public void SetDistance(int distance)
        {
            distanceToMotherlode = distance;
        }
        public int GetDistance()
        {
            return distanceToMotherlode;
        }
        public Circle Clone()
        {
            return new Circle(0, this.referencePoint);
        }
    }
}
