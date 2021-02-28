using Emgu.CV;
using Emgu.CV.CvEnum;
using Microsoft.Win32;
using netDxf;
using netDxf.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Drawing;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using Emgu.CV.Structure;

namespace SheetPerforator
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private double ratio;
        private List<int> Exclusions = new List<int>();
        private List<int> ExcludedInterval = new List<int>();


        private ObservableCollection<Circle> _circles = new ObservableCollection<Circle>();
        public ObservableCollection<Circle> Circles
        {
            get => _circles;
            set
            {
                _circles = value;
                OnPropertyChanged();
            }
        }

        private string _circlesCount;
        public string CirclesCount
        {
            get => _circlesCount;
            set
            {
                _circlesCount = value;
                OnPropertyChanged();
            }
        }

        private string _imagePath;
        public string ImagePath
        {
            get => _imagePath;
            set
            {
                _imagePath = value;
                ImageMat = new Mat(_imagePath);
                ImageBitmap = new Bitmap(_imagePath);
                OnPropertyChanged();
            }
        }

        private string _progressLabel = "Processing image pixels...  0/0";
        public string ProgressLabel
        {
            get => _progressLabel;
            set
            {
                _progressLabel = value;
                OnPropertyChanged();
            }
        }

        private string _imageWidth;
        public string ImageWidth
        {
            get => _imageWidth;
            set
            {
                _imageWidth = value;
                OnPropertyChanged();
            }
        }

        private string _imageHeight;
        public string ImageHeight
        {
            get => _imageHeight;
            set
            {
                _imageHeight = value;
                OnPropertyChanged();
            }
        }

        private int _threshold = 1;
        public int Threshold
        {
            get => _threshold;
            set
            {
                _threshold = value;
                OnPropertyChanged();
            }
        }

        private int _currentBarValue = 0;
        public int CurrentBarValue
        {
            get => _currentBarValue;
            set
            {
                _currentBarValue = value;
                OnPropertyChanged();
            }
        }

        private Bitmap _imageBitmap;
        public Bitmap ImageBitmap
        {
            get => _imageBitmap;
            set
            {
                _imageBitmap = value;
                ImageHeight = _imageBitmap.Height.ToString();
                ImageWidth = _imageBitmap.Width.ToString();
                ratio = (double)_imageBitmap.Height / (double)_imageBitmap.Width;
                ImageCanvas.Background = new ImageBrush
                {
                    Stretch = Stretch.Uniform,
                    ImageSource = BitmapToBitmapImage.ToBitmapImage(_imageBitmap)
                };
                OnPropertyChanged();
            }
        }

        private Mat ImageMat { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            CenterWindowOnScreen();
            Title = "Sheet Perforator";
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Jpeg (*.jpeg)|*.jpeg|Jpg (*.jpg)|*.jpg|Png (*.png)|*.png|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
                ImagePath = openFileDialog.FileName;
        }
        private void Process_Click(object sender, RoutedEventArgs e)
        {
            Circles.Clear();
            Exclusions.Clear();
            ExcludedInterval.Clear();

            bool validWidth = int.TryParse(ImageWidth, out int width);
            bool validHeight = int.TryParse(ImageHeight, out int height);

            if (!validWidth || !validHeight)
            {
                MessageBox.Show("Please input valid image dimensions.");
                return;
            }

            CurrentBarValue = 0;
            double pixelCount = 0;

            if (string.IsNullOrEmpty(MinTb.Text) || string.IsNullOrEmpty(MaxTb.Text) || string.IsNullOrEmpty(SpacingTb.Text)
                || string.IsNullOrEmpty(WidthTb.Text) || string.IsNullOrEmpty(HeightTb.Text))
            {
                MessageBox.Show("Please fill all the required fields.");
                return;
            }

            if (!string.IsNullOrEmpty(ExclusionsTb.Text))
            {
                try
                {
                    Exclusions = ExclusionsTb.Text.Split(',').Select(x => int.Parse(x)).ToList();
                }
                catch
                {
                    MessageBox.Show("Specified exclusions not valid.");
                }
            }
            ImageBitmap = (Bitmap)ImageMat.ToBitmap().Clone();
            Mat grayImage = new Mat(ImageMat.Rows, ImageMat.Cols, ImageMat.Depth, ImageMat.NumberOfChannels);
            Image<Bgr, byte> negative = ImageMat.ToImage<Bgr, byte>().Clone();
            negative = negative.Not();
            Mat black = Mat.Zeros(height, width, DepthType.Cv8U, 1);
            Bitmap preview = new Bitmap(black.ToBitmap());
            black.Dispose();
            if (NegativeCb.IsChecked.Value)
            {
                CvInvoke.CvtColor(negative, grayImage, ColorConversion.Bgr2Gray);
            }
            else
            {
                CvInvoke.CvtColor(ImageMat, grayImage, ColorConversion.Bgr2Gray);
            }
            int range = 0;
            float space = 0;
            if (!string.IsNullOrEmpty(MaxTb.Text) && !string.IsNullOrEmpty(MinTb.Text) && !string.IsNullOrEmpty(SpacingTb.Text))
            {
                range = int.Parse(MaxTb.Text) - int.Parse(MinTb.Text) + 1;
                space = float.Parse(SpacingTb.Text);
            }

            List<Tuple<int, int, int>> intervals = new List<Tuple<int, int, int>>();
            for (int i = 0; i < range; i++)
            {
                int value = (255 - Threshold) / range;
                intervals.Add(new Tuple<int, int, int>(i * value + Threshold, i * value + value + Threshold, int.Parse(MinTb.Text) + i));
                if (Exclusions.Where(x => x == (int.Parse(MinTb.Text) + i)).Count() == 0)
                {
                    ExcludedInterval.Add(int.Parse(MinTb.Text) + i);
                }
            }
            intervals[intervals.Count - 1] = new Tuple<int, int, int>(intervals.Last().Item1, 255, intervals.Last().Item3);
            float avg = 0;
            for (int i = 0; i < ExcludedInterval.Count; i++)
            {
                avg += ExcludedInterval[i];
            }
            avg = avg / ExcludedInterval.Count;
            for (int i = 0; i < intervals.Count; i++)
            {
                if (intervals[i].Item2 < Threshold)
                {
                    intervals.Remove(intervals[i]);
                }
                if (intervals[i].Item1 <= Threshold && intervals[i].Item2 > Threshold)
                {
                    intervals[i] = new Tuple<int, int, int>(Threshold, intervals[i].Item2, intervals[i].Item3);
                }
                if (!ExcludedInterval.Contains(intervals[i].Item3))
                {
                    if (intervals[i].Item3 > avg)
                    {
                        intervals[i] = new Tuple<int, int, int>(intervals[i].Item1, intervals[i].Item2, ExcludedInterval.Where(x => x > avg).FirstOrDefault());
                    }
                    else
                    {
                        intervals[i] = new Tuple<int, int, int>(intervals[i].Item1, intervals[i].Item2, ExcludedInterval.Where(x => x <= avg).FirstOrDefault());
                    }
                }
            }
            Task.Run(() =>
            {
                bool withOffset = true;
                Dispatcher.Invoke(() => withOffset = OffsetCb.IsChecked.Value);
                if (withOffset)
                {
                    int odd = 0;
                    for (float i = 0; i < height; i += space)
                    {
                        if (odd > 1)
                        {
                            odd = 0;
                        }
                        if (odd % 2 == 0)
                        {
                            for (float j = 0; j < width; j += space)
                            {
                                int value = (int)grayImage.GetValue((int)i, (int)j);
                                foreach (var item in intervals)
                                {
                                    if (item == intervals.Last())
                                    {
                                        if (value >= item.Item1 && value <= item.Item2)
                                        {
                                            using (Graphics g = Graphics.FromImage(preview))
                                            {
                                                g.FillEllipse(new System.Drawing.SolidBrush(System.Drawing.Color.White), j - (item.Item3 / 2), i - (item.Item3 / 2), item.Item3, item.Item3);
                                            }
                                            Circle circle = new Circle(new Vector2(j, float.Parse(ImageHeight) - i), (double)item.Item3 / 2);
                                            Dispatcher.Invoke(() => Circles.Add(circle));
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (value >= item.Item1 && value < item.Item2)
                                        {
                                            using (Graphics g = Graphics.FromImage(preview))
                                            {
                                                g.FillEllipse(new System.Drawing.SolidBrush(System.Drawing.Color.White), j - (item.Item3 / 2), i - (item.Item3 / 2), item.Item3, item.Item3);
                                            }
                                            Circle circle = new Circle(new Vector2(j, float.Parse(ImageHeight) - i), (double)item.Item3 / 2);
                                            Dispatcher.Invoke(() => Circles.Add(circle));
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (float j = space / 2; j < width; j += space)
                            {
                                int value = (int)grayImage.GetValue((int)i, (int)j);
                                foreach (var item in intervals)
                                {
                                    if (item == intervals.Last())
                                    {
                                        if (value >= item.Item1 && value <= item.Item2)
                                        {
                                            using (Graphics g = Graphics.FromImage(preview))
                                            {
                                                g.FillEllipse(new System.Drawing.SolidBrush(System.Drawing.Color.White), j - (item.Item3 / 2), i - (item.Item3 / 2), item.Item3, item.Item3);
                                            }
                                            Circle circle = new Circle(new Vector2(j, float.Parse(ImageHeight) - i), (double)item.Item3 / 2);
                                            Dispatcher.Invoke(() => Circles.Add(circle));
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        if (value >= item.Item1 && value < item.Item2)
                                        {
                                            using (Graphics g = Graphics.FromImage(preview))
                                            {
                                                g.FillEllipse(new System.Drawing.SolidBrush(System.Drawing.Color.White), j - (item.Item3 / 2), i - (item.Item3 / 2), item.Item3, item.Item3);
                                            }
                                            Circle circle = new Circle(new Vector2(j, float.Parse(ImageHeight) - i), (double)item.Item3 / 2);
                                            Dispatcher.Invoke(() => Circles.Add(circle));
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        pixelCount = i;
                        odd++;
                        Dispatcher.Invoke(() =>
                        {
                            CurrentBarValue = (int)((pixelCount / height) * 100);
                            ProgressLabel = "Processing image pixels...  " + CurrentBarValue + "%";
                        });
                    }
                }
                else
                {
                    for (float i = 0; i < height; i += space)
                    {
                        for (float j = 0; j < width; j += space)
                        {
                            int value = (int)grayImage.GetValue((int)i, (int)j);
                            foreach (var item in intervals)
                            {
                                if (item == intervals.Last())
                                {
                                    if (value >= item.Item1 && value <= item.Item2)
                                    {
                                        using (Graphics g = Graphics.FromImage(preview))
                                        {
                                            g.FillEllipse(new System.Drawing.SolidBrush(System.Drawing.Color.White), j - (item.Item3 / 2), i - (item.Item3 / 2), item.Item3, item.Item3);
                                        }
                                        Circle circle = new Circle(new Vector2(j, float.Parse(ImageHeight) - i), (double)item.Item3 / 2);
                                        Dispatcher.Invoke(() => Circles.Add(circle));
                                        break;
                                    }
                                }
                                else
                                {
                                    if (value >= item.Item1 && value < item.Item2)
                                    {
                                        using (Graphics g = Graphics.FromImage(preview))
                                        {
                                            g.FillEllipse(new System.Drawing.SolidBrush(System.Drawing.Color.White), j - (item.Item3 / 2), i - (item.Item3 / 2), item.Item3, item.Item3);
                                        }
                                        Circle circle = new Circle(new Vector2(j, float.Parse(ImageHeight) - i), (double)item.Item3 / 2);
                                        Dispatcher.Invoke(() => Circles.Add(circle));
                                        break;
                                    }
                                }
                            }
                        }
                        pixelCount = i;
                        Dispatcher.Invoke(() =>
                        {
                            CurrentBarValue = (int)((pixelCount / height) * 100);
                            ProgressLabel = "Processing image pixels...  " + CurrentBarValue + "%";
                        });
                    }
                }
            }).ContinueWith(t =>
            {
                Dispatcher.Invoke(() =>
                {
                    CurrentBarValue = 100;
                    CirclesCount = "Total Perforations: " + Circles.Count;
                    ProgressLabel = "Processing image pixels... 100%";
                    ImageCanvas.Background = new ImageBrush
                    {
                        Stretch = Stretch.Uniform,
                        ImageSource = BitmapToBitmapImage.ToBitmapImage(preview)
                    };
                    ImageBitmap.Dispose();
                    negative.Dispose();
                });
            });
        }
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string file = null;
                DxfDocument dxf = new DxfDocument();
                dxf.AddEntity(Circles.Select(x => x.Clone() as Circle));
                using (var dialog = new System.Windows.Forms.SaveFileDialog())
                {
                    dialog.Filter = "DXF (*.dxf)|";
                    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                    if (result.ToString().Equals("OK"))
                    {
                        file = $"{dialog.FileName}.dxf";
                    }
                }
                if (!string.IsNullOrEmpty(file))
                {
                    dxf.Save(file);
                    MessageBox.Show("Drawing successfully exported.", "Success");
                }
                else
                {
                    MessageBox.Show("Please specify an output path.", "Error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exported file already in use by another process, close the programs using it before exporting again.", "Error");
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                ImagePath = files[0];
            }
        }

        private void WidthTb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }

        private void HeightTb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }

        private void MinTb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }

        private void MaxTb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }

        private void MarginTb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1))
                e.Handled = true;
        }

        private void SpacingTb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1) && !e.Text[e.Text.Length - 1].Equals('.'))
                e.Handled = true;
        }

        private void ExclusionsTb_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!char.IsDigit(e.Text, e.Text.Length - 1) && !e.Text[e.Text.Length - 1].Equals(','))
                e.Handled = true;
        }

        private void MarginTb_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (int.Parse(MarginTb.Text) is int margin)
                {
                    ImageMat = new Mat(_imagePath);
                    try
                    {
                        CvInvoke.Resize(ImageMat, ImageMat, new System.Drawing.Size(int.Parse(ImageWidth), int.Parse(ImageHeight)));
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Error preserving original size.");
                    }
                    if (margin % 2 != 0)
                        margin++;

                    for (int i = 0; i < margin; i++)
                    {
                        Parallel.For(0, ImageMat.Height, j =>
                        {
                            ImageMat.SetDoubleValue(j, i, 0);
                        });
                    }
                    for (int i = ImageMat.Width - 1; i > ImageMat.Width - margin - 1; i--)
                    {
                        Parallel.For(0, ImageMat.Height, j =>
                        {
                            ImageMat.SetDoubleValue(j, i, 0);
                        });
                    }
                    for (int i = 0; i < margin; i++)
                    {
                        Parallel.For(margin, ImageMat.Width - margin, j =>
                        {
                            ImageMat.SetDoubleValue(i, j, 0);
                        });
                    }
                    for (int i = ImageMat.Height - 1; i > ImageMat.Height - margin - 1; i--)
                    {
                        Parallel.For(margin, ImageMat.Width - margin, j =>
                        {
                            ImageMat.SetDoubleValue(i, j, 0);
                        });
                    }
                    //for (int i = margin / 2; i < margin; i++)
                    //{
                    //    ImageMat.SetDoubleValue();
                    //}
                    ImageBitmap = ImageMat.ToBitmap();
                }
            }
            catch (Exception)
            {

            }
        }

        private void WidthTb_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LockRatio.IsChecked.Value)
                {
                    HeightTb.Text = ((int)(int.Parse(WidthTb.Text) * ratio)).ToString();
                }
                try
                {
                    CvInvoke.Resize(ImageMat, ImageMat, new System.Drawing.Size(int.Parse(WidthTb.Text), int.Parse(HeightTb.Text)));
                    ImageBitmap = ImageMat.ToBitmap();
                }
                catch (Exception)
                {

                }
            }
            catch (Exception)
            {

            }
        }

        private void HeightTb_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (LockRatio.IsChecked.Value)
                {
                    WidthTb.Text = ((int)(int.Parse(HeightTb.Text) * (1 / ratio))).ToString();
                }
                try
                {
                    CvInvoke.Resize(ImageMat, ImageMat, new System.Drawing.Size(int.Parse(WidthTb.Text), int.Parse(HeightTb.Text)));
                    ImageBitmap = ImageMat.ToBitmap();
                }
                catch (Exception)
                {

                }
            }
            catch (Exception)
            {

            }
        }
        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }
    }

    public static class MatExtension
    {
        public static dynamic GetValue(this Mat mat, int row, int col)
        {
            var value = CreateElement(mat.Depth);
            Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 1);
            return value[0];
        }

        public static void SetValue(this Mat mat, int row, int col, dynamic value)
        {
            var target = CreateElement(mat.Depth, value);
            Marshal.Copy(target, 0, mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, 1);
        }

        public static double GetDoubleValue(this Mat mat, int row, int col)
        {
            var value = new double[1];
            Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 1);
            return value[0];
        }

        public static void SetDoubleValue(this Mat mat, int row, int col, double value)
        {
            var target = new[] { value };
            Marshal.Copy(target, 0, mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, 1);
        }
        private static dynamic CreateElement(DepthType depthType, dynamic value)
        {
            var element = CreateElement(depthType);
            element[0] = value;
            return element;
        }

        private static dynamic CreateElement(DepthType depthType)
        {
            if (depthType == DepthType.Cv8S)
            {
                return new sbyte[1];
            }
            if (depthType == DepthType.Cv8U)
            {
                return new byte[1];
            }
            if (depthType == DepthType.Cv16S)
            {
                return new short[1];
            }
            if (depthType == DepthType.Cv16U)
            {
                return new ushort[1];
            }
            if (depthType == DepthType.Cv32S)
            {
                return new int[1];
            }
            if (depthType == DepthType.Cv32F)
            {
                return new float[1];
            }
            if (depthType == DepthType.Cv64F)
            {
                return new double[1];
            }
            return new float[1];
        }
    }
}
