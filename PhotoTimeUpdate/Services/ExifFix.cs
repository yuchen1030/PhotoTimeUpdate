using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;

namespace PhotoTimeUpdate.Services
{
    public class ExifFix
    {
        public static List<String> FixExifDateTime(string photoPath, Int64 secs)
        {
            List<string> oriPicInfos = new List<string>();
            List<string> tarPicInfos = new List<string>();
            List<string> tarPicComputes = new List<string>();
            string[] files = new string[0];
            if (File.Exists(photoPath))
            {
                files = new string[] { photoPath };
            }
            else
            {
                files = Directory.GetFiles(photoPath); //获取所有照片路径
            }
            photoPath = photoPath.Trim("/\\".ToCharArray());
            string subFolder = photoPath.Substring(photoPath.LastIndexOf("/") + photoPath.LastIndexOf("\\") + 2);

            int i = 0;
            IFormatProvider culture = new CultureInfo("zh-CN", true);
            foreach (var file in files)
            {
                i++;
                //替代System.Drawing.Image.FromFile(file);
                System.Drawing.Image image = GetImage(file);

                //获取Exif的DateTimeOriginal属性（36867）
                PropertyItem pi = image.GetPropertyItem(36867); //(int)ExifTags.DateTimeOriginal

                //转为时间文本
                string oldTime = Encoding.ASCII.GetString(pi.Value);
                oldTime = oldTime.Replace("\0", "");

                Debug.WriteLine(file + " " + oldTime);
                //时间文本格式化为DateTime
                DateTime time = DateTime.ParseExact(oldTime, "yyyy:MM:dd HH:mm:ss", culture);
                oriPicInfos.Add(i + "," + file + "," + time.ToString("yyyy-MM-dd HH:mm:ss"));

                //得到正确的时间
                DateTime newTime = time.AddSeconds(secs);
                tarPicComputes.Add(newTime.ToString("yyyy-MM-dd HH:mm"));
                tarPicInfos.Add(i + "," + file + "," + newTime.ToString("yyyy-MM-dd HH:mm:ss"));
                //转换为EXIF存储的时间格式
                string newTimeString = newTime.ToString("yyyy:MM:dd HH:mm:ss");
                pi.Value = Encoding.ASCII.GetBytes(newTimeString + "\0");

                //修改DateTimeOriginal属性和其他的时间属性
                image.SetPropertyItem(pi);
                pi.Id = 306; // (int)ExifTags.DateTime; //306
                image.SetPropertyItem(pi);
                pi.Id = 36868; //(int)ExifTags.DateTimeDigitized; //36868
                image.SetPropertyItem(pi);

                //存回文件
                image.Save(file);

                image.Dispose();
            }

            string path0 = Directory.GetCurrentDirectory();
            String res = string.Join(Environment.NewLine, oriPicInfos);
            string oriWhole = System.IO.Path.Combine(path0, subFolder + "_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "_原始图片拍摄时间.csv");
            System.IO.File.WriteAllText(oriWhole, res);

            res = string.Join(Environment.NewLine, tarPicInfos);
            string whole = System.IO.Path.Combine(path0, subFolder + "_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "_修改后图片拍摄时间.csv");
            System.IO.File.WriteAllText(whole, res);

            var result = tarPicComputes.GroupBy(a => a).Select(a => new { key = a.Key, val = a.Count() }).ToList().Select(a => a.key + "：" + a.val).ToList();

            res = string.Join(Environment.NewLine, result);
            whole = System.IO.Path.Combine(path0, subFolder + "_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "_每分钟拍摄图片张数统计.txt");
            System.IO.File.WriteAllText(whole, res);

            List<String> returnMsg = new List<string>();
            returnMsg.Add(path0);
            returnMsg.Add(tarPicComputes.Count().ToString());
            returnMsg.Add("每分钟拍摄图片张数统计如下：" + Environment.NewLine + res);
            return returnMsg;
        }

        public static DateTime GetDateTimeOriginal(System.Drawing.Image image)
        {
            PropertyItem pi = image.GetPropertyItem(36867); //(int)ExifTags.DateTimeOriginal
            string oldTime = Encoding.ASCII.GetString(pi.Value);
            oldTime = oldTime.Replace("\0", "");

            //时间文本格式化为DateTime
            IFormatProvider culture = new CultureInfo("zh-CN", true);
            DateTime time = DateTime.ParseExact(oldTime, "yyyy:MM:dd HH:mm:ss", culture);
            return time;
        }

        public static System.Drawing.Image GetImage(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return null;
                }

                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, bytes.Length);

                    MemoryStream memoryStream = new MemoryStream(bytes);
                    if (memoryStream != null)
                    {
                        return System.Drawing.Image.FromStream(memoryStream);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return null;
        }



        public static string GetTakePicDate(string fileName)
        {
            Encoding ascii = Encoding.ASCII;
            string picDate;
            FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            Image image = Image.FromStream(stream, true, false);
            foreach (PropertyItem p in image.PropertyItems)
            {
                //获取拍摄日期时间
                if (p.Id == 0x9003) // 0x0132 最后更新时间
                {
                    stream.Close();
                    picDate = ascii.GetString(p.Value);
                    picDate = picDate.Replace("\0", "");
                    IFormatProvider culture = new CultureInfo("zh-CN", true);
                    DateTime time = DateTime.ParseExact(picDate, "yyyy:MM:dd HH:mm:ss", culture);
                    if ((!"".Equals(picDate)) && picDate.Length >= 10)
                    {
                        return time.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
            }
            stream.Close();
            return "";
        }




    }
}
