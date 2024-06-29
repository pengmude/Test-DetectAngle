using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Markup;

namespace Test_DetectAngle
{
    public partial class Test_JsonString : Form
    {
        public Test_JsonString()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string data = textBox1.Text;

            var pointsByCategory = ExtractCoordinates(data);

            string ss1 = "", ss2 = "", ss3 = "";
            foreach (var category in pointsByCategory)
            {
                //Console.WriteLine($"类别: {category.Key}");
                foreach (var point in category.Value)
                {
                    //Console.WriteLine($"坐标: ({point.X}, {point.Y})");
                    if (category.Key == "缺陷")
                    {
                        ss1 += $"({point.X}, {point.Y}) ";
                    }
                    else if (category.Key == "定位")
                    {
                        ss2 += $"({point.X}, {point.Y}) ";
                    }
                    else if (category.Key == "分类")
                    {
                        ss3 += $"({point.X}, {point.Y}) ";
                    }
                }
            }
            textBox2.Text = ss1;
            textBox3.Text = ss2;
            textBox4.Text = ss3;

        }

        public Dictionary<string, List<System.Drawing.Point>> ExtractCoordinates(string jsonData)
        {
            var result = new Dictionary<string, List<System.Drawing.Point>>();

            // 使用正则表达式匹配括号内的数字
            var regex = new Regex(@"\((\d+),(\d+)\)");

            // 移除JSON字符串中的引号便于处理
            jsonData = jsonData.Replace("'", "").Replace("\"", "");

            // 分割字符串以处理不同类别
            string[] categories = { "缺陷", "定位", "分类" };
            foreach (var category in categories)
            {
                var startIndex = jsonData.IndexOf(category + ":[");
                if (startIndex != -1)
                {
                    startIndex += category.Length + 2; // 跳过类别名和":["

                    var endIndex = jsonData.IndexOf(']', startIndex);
                    if (endIndex != -1)
                    {
                        string content = jsonData.Substring(startIndex, endIndex - startIndex).Trim();

                        // 解析坐标
                        if (!string.IsNullOrEmpty(content))
                        {
                            var matches = regex.Matches(content);
                            var points = new List<System.Drawing.Point>();
                            foreach (Match match in matches)
                            {
                                points.Add(new System.Drawing.Point { X = int.Parse(match.Groups[1].Value), Y = int.Parse(match.Groups[2].Value) });
                            }
                            result[category] = points;
                        }
                        else
                        {
                            result[category] = new List<System.Drawing.Point>(); // 如果没有坐标，添加空列表
                        }
                    }
                }
            }

            return result;
        }
    }
}
