

using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test_DetectAngle
{
    class BaseImageOperatorClass
    {
        public static int FindNotchAngle(Mat binaryImage, Mat tempImg, double stepAngle, out Mat outputImg)
        {
            // 确保图像为单通道二值图像
            if (binaryImage.Channels() != 1)
                throw new ArgumentException("The input image must be a single channel binary image.");

            int maxOverlap = 0;
            double bestAngle = 0;
            outputImg = new Mat();
            string ss = "";
            // 旋转并比较
            for (double angle = 0; angle < 360; angle += stepAngle)
            {
                // 旋转模板
                Mat rotatedTemplate = RotateImage(tempImg, (int)angle);

                // 计算重叠区域
                Mat overlap = new Mat();
                Cv2.BitwiseAnd(rotatedTemplate, binaryImage, overlap);

                // 计算并集大小（此处简化为白像素计数）
                int overlapCount = Cv2.CountNonZero(overlap);

                // 更新最大重叠角度
                if (overlapCount >= maxOverlap)
                {
                    maxOverlap = overlapCount;
                    bestAngle = angle;
                    outputImg = overlap;
                }
#if DEBUG
                ss += $"角度：{angle}°, 最大相似像素数目：{overlapCount}\n";
                if(angle == 0)
                    Cv2.ImShow($"0°图像结果：", outputImg);
#endif
            }
            File.WriteAllText("D:\\data.text", ss);

            return (int)bestAngle;
        }

        /// <summary>
        /// 提取图像中圆形物体的外圈圆环
        /// </summary>
        /// <param name="inputImg">输入图像</param>
        /// <param name="tempImg">结果图像</param>
        /// <param name="outerRadius">外圆半径</param>
        /// <param name="innerRadius">内圆半径</param>
        /// <param name="isSaveImage">是否存图</param>
        /// <param name="templateImagePath">存图路径</param>
        /// <returns></returns>
        public static bool FindRing(Mat inputImg, out Mat tempImg, out float innerRadius, out float outerRadius, out Point center, bool isSaveImage = false, string templateImagePath = "")
        {
            Mat srcImage = inputImg.Clone();
            tempImg = new Mat();
            outerRadius = 60;
            innerRadius = 50;
            center = new Point();

            //减小模版数据量，使其为原图的1 / 4
            OpenCvSharp.Size newSize = new OpenCvSharp.Size((int)(srcImage.Width / 4), (int)(srcImage.Height / 4));
            //使用插值方法INTER_LINEAR进行图像缩放，以获得较好的视觉效果
            Cv2.Resize(srcImage, srcImage, newSize, 0, 0, InterpolationFlags.Linear);

            if (srcImage.Channels() != 1)
                srcImage = srcImage.CvtColor(ColorConversionCodes.BGR2GRAY);// 转换为灰度图像
            
            // 进行模糊处理以减少噪声
            var blurredImage = srcImage.Blur(new OpenCvSharp.Size(5, 5));
            
            // 初始化霍夫圆变换参数
            double dp = 2.0;
            int minDist = 200; // 圆之间的最小距离
            double param1 = 50; // Canny边缘函数的高阈值
            double param2 = 100; // 圆心检测阈值.径向累加器阈值
            int minRadius = 138; // 圆的最小半径,原图500
            int maxRadius = 150; // 圆的最大半径，原图700

            // 执行霍夫圆变换
            var circles = blurredImage.HoughCircles(HoughModes.Gradient, dp, minDist, param1, param2, minRadius, maxRadius);

            // 最后找到的一个圆的半径
            float radius = 0;

            // 模版图片
            var circleOnly = new Mat();

            // 如果找到圆，则绘制它们
            if (circles != null)
            {
                foreach (var circle in circles)
                {
                    center = (Point)circle.Center;
                    radius = circle.Radius;

                    // 在原图上画出圆
                    //srcImage.Circle(center, (int)radius, Scalar.Red, 2);

                    //创建一个与原图同样大小的黑色背景图像作为掩码
                    var mask_big = new Mat(srcImage.Size(), MatType.CV_8UC1, Scalar.All(0));

                    //在掩码上画出白色填充的大圆
                    int outerR = (int)radius - 42;
                    outerRadius = outerR;
                    Cv2.Circle(mask_big, center, outerR, Scalar.White, -1);

                    //得到只有圆部分的区域
                    srcImage.CopyTo(circleOnly, mask_big);
                    
                    // 进一步扣去圆的内部细节，保留圆环，为了在后续模版匹配时减少圆内区域对结果的干扰
                    int innerR = (int)radius - 58;
                    innerRadius = innerR;
                    Cv2.Circle(circleOnly, center, innerR, Scalar.All(0), -1);

                    // 以圆心为中心提取包含圆环的正方形，用于模版旋转时尺寸一致
                    int x = (int)(center.X - radius - 50);
                    int y = (int)(center.Y - radius - 50);
                    int width = 100 + 2 * (int)radius;
                    int height = 100 + 2 * (int)radius;
                    Rect cropRect = new Rect(x, y, width, height);
                    var croppedImage = new Mat(circleOnly, cropRect);
                    center = new Point(width / 2, height / 2);
#if DEBUG
                    MessageBox.Show($"circleOnly Size:({circleOnly.Cols}, {circleOnly.Rows}), croppedImage Size:({croppedImage.Cols}, {croppedImage.Rows})");
#endif
                    // 保存找到的圆
                    if (isSaveImage)
                    {
                        if (templateImagePath == "")
                        {
                            templateImagePath = AppDomain.CurrentDomain.BaseDirectory + "\\template.bmp";
                        }
                        // 保存模版图
                        croppedImage.SaveImage(templateImagePath);
                    }

                    tempImg = croppedImage;
                }
            }
            if (circles.Length == 1)
                return true;
            return false;
        }

        /// <summary>
        /// 在输入图像上绘制一个指定角度范围的圆环
        /// </summary>
        /// <param name="inputImg"></param>
        /// <param name="center"></param>
        /// <param name="innerRadius"></param>
        /// <param name="outerRadius"></param>
        /// <param name="startAngle"></param>
        /// <param name="endAngle"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void CreateTemplateRing(ref Mat inputImg, Point center, float innerRadius, float outerRadius, double startAngle, double endAngle)
        {
            // 参数合法检验：创建的圆环不能超出图像尺寸
            if (center.X + outerRadius > inputImg.Cols || center.Y + outerRadius > inputImg.Rows || center.X < outerRadius || center.Y < outerRadius)
            {
                throw new ArgumentException("绘制圆环参数不合法！");
            }

            // 绘制外圈（先绘制外圈再内圈以去除重叠部分）
            Cv2.Ellipse(inputImg, center, new Size(outerRadius, outerRadius), 0, startAngle, endAngle, Scalar.White, -1);

            // 绘制内圈，以去除中间部分形成圆环
            Cv2.Ellipse(inputImg, center, new Size(innerRadius, innerRadius), 0, startAngle, endAngle, Scalar.Black, -1);
        }
        /// <summary>
        /// 图像二值化
        /// </summary>
        /// <param name="grayImage"></param>
        /// <param name="thresholdValue"></param>
        /// <returns></returns>
        public static Mat Threshold(Mat grayImage, int thresholdValue)
        {
            Mat binaryImage = new Mat();
            Cv2.Threshold(grayImage, binaryImage, thresholdValue, 255, ThresholdTypes.Binary);
            return binaryImage;
        }
        /// <summary>
        /// 旋转图像
        /// </summary>
        /// <param name="srcImage"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Mat RotateImage(Mat srcImage, int angle)
        {
            // 获取图像的宽度和高度
            double width = srcImage.Width;
            double height = srcImage.Height;

            // 计算旋转后图像需要的尺寸
            Size size = new Size((int)Math.Max(width, height), (int)Math.Max(width, height));

            // 获取旋转中心为图像中心
            Point2f center = new Point2f((float)width / 2, (float)height / 2);

            // 计算旋转矩阵
            Mat rotationMatrix = Cv2.GetRotationMatrix2D(center, angle, 1.0);

            // 执行旋转操作
            Mat rotatedImage = new Mat(size, srcImage.Type());
            Cv2.WarpAffine(srcImage, rotatedImage, rotationMatrix, size);

            return rotatedImage;
        }
    }
}
