using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace Test_DetectAngle
{
    public partial class FindRotateAngel : Form
    {
        Bitmap bitmap;
        Mat GrayImage;
        Mat BinaryImage;
        public FindRotateAngel()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 点击选择弹窗
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
                bitmap = new Bitmap(openFileDialog1.FileName);
                GrayImage = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
                Mat binaryImage = new Mat();
                binaryImage = BaseImageOperatorClass.Threshold(GrayImage, trackBar1.Value);
                pictureBox1.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binaryImage);
            }
        }
        /// <summary>
        /// 调节二值化阈值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            Mat binaryImage = new Mat();
            binaryImage = BaseImageOperatorClass.Threshold(GrayImage, trackBar1.Value);
            pictureBox1.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(binaryImage);
            label2.Text = trackBar1.Value.ToString();
        }

        /// <summary>
        /// 点击开始检测角度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            // 算法计时开始
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // 使用实时二值化的图像作为查找轮廓的输入图像
            Mat binary = OpenCvSharp.Extensions.BitmapConverter.ToMat((Bitmap)pictureBox1.Image);

            // 提取圆环
            Mat ringImg;
            int angel = -1;
            float outerR, innerR;
            OpenCvSharp.Point center = new OpenCvSharp.Point();
            // 先切割出圆环ROI
            BaseImageOperatorClass.FindRing(binary, out ringImg, out innerR, out outerR, out center);
            // 再创建模版匹配
            if (!ringImg.Empty())
            {
#if DEBUG
                Cv2.ImShow($"待检测的圆环图像({ringImg.Cols},{ringImg.Rows}),大圆R={outerR},小圆r={innerR},Center({center.X},{center.Y})", ringImg);
#endif
                // 创建60°模版圆环
                Mat tmp = new Mat(ringImg.Rows, ringImg.Cols, MatType.CV_8UC1);
                tmp.SetTo(Scalar.Black);    // 一定要给像素灰度值初始化为0，否则像素灰度值有可能是255，就不是全黑图了
                BaseImageOperatorClass.CreateTemplateRing(ref tmp, center, innerR - 10, outerR + 10, -20,20);
#if DEBUG
                Cv2.ImShow($"圆环模版({tmp.Cols}x{tmp.Rows}), 大圆R ={outerR},小圆r ={innerR},Center({center.X},{center.Y})", tmp);
#endif
                // 在圆环图像中检测旋转角度
                Mat res;
                angel = BaseImageOperatorClass.FindNotchAngle(ringImg, tmp, 15, out res);
                // 显示带有轮廓的图像
                pictureBox2.Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(res);
            }

            // 算法计时结束
            stopwatch.Stop();
            TimeSpan ts = stopwatch.Elapsed;
            label3.Text = $"识别角度为：{angel}°，算法耗时：{ts.TotalMilliseconds.ToString("0.00")}ms";
        }
        /// <summary>
        /// 点击保存图片
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            if(saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Mat saveImg = OpenCvSharp.Extensions.BitmapConverter.ToMat((Bitmap)pictureBox1.Image);
                Cv2.ImWrite(saveFileDialog1.FileName, saveImg);
            }
        }
    }
}
