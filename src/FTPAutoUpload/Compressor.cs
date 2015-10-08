// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Compressor.cs" company="Fujian Start Computer Equipment Co.,Ltd">
//   Copyright © 2012 Start Computer Equipment Co., Ltd All Rights Reserved.
// </copyright>
// <summary>
//   TODO: Update summary.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace FTPAutoUpload
{
    using System;
    using System.Diagnostics;
    using System.IO;


    using SevenZip;

    /// <summary>
    ///     TODO: Update summary.
    /// </summary>
    public class Compressor
    {
        #region Constants

        /// <summary>
        /// The temp file folder path.
        /// </summary>
        private const string TempFileFolderPath = @"C:\TempFileFolderPath";

        #endregion

        #region Static Fields

        /// <summary>
        /// The seven zip 64 dll.
        /// </summary>
        private static readonly string SevenZip64Dll = Path.GetFullPath("7z64.dll");

        /// <summary>
        /// The seven zip dll.
        /// </summary>
        private static readonly string SevenZipDll = Path.GetFullPath("7z.dll");

        #endregion

        #region Properties

        /// <summary>
        /// Gets the log category.
        /// </summary>
        private static string LogCategory
        {
            get
            {
                return "Default Category";
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// 压缩文件
        /// </summary>
        /// <param name="sourceFolderPath">
        /// 要压缩的源文件夹绝对路径
        /// </param>
        /// <param name="target7ZFilePath">
        /// 压缩后的文件存放的绝对路径
        /// </param>
        public static void CompressFileFolder(string sourceFolderPath, string target7ZFilePath)
        {
//            Logger.Write("开始CompressFileFolder", LogCategory);

            // 验证源文件路径
            if (string.IsNullOrEmpty(sourceFolderPath))
            {
                throw new ArgumentException("要压缩的源文件绝对路径为空！");
            }

            // 验证源文件路径是否存在
            if (!Directory.Exists(sourceFolderPath))
            {
                throw new ArgumentException("要压缩的源文件绝对路径不存在！");
            }

            // 设置目标路径
            // 7z文件夹路径
            string file7ZDirPath;
            if (string.IsNullOrEmpty(target7ZFilePath))
            {
                // 如果目标路径为空，则保存在源文件夹同一目录下，以保存时间命名
                file7ZDirPath = Path.Combine(
                    sourceFolderPath, 
                    string.Format("{0}.7z", DateTime.Now.ToString("yyyyMMddHHmmss")));
            }
            else
            {
                file7ZDirPath = target7ZFilePath;
            }

//            Logger.Write(string.Format("CompressFileFolder目标路径：{0}", file7ZDirPath), LogCategory);

            // 拷贝文件到临时目录下（为了防止文件正在被其他进程使用时，无法对该文件直接压缩，所以先拷贝到临时目录下，再压缩临时目录下的文件）
            string tempPath = Path.Combine(TempFileFolderPath, DateTime.Now.ToString("yyyyMMddHHmmss"));
            CopyFileFolder(sourceFolderPath, tempPath);
//            Logger.Write(string.Format("CompressFileFolder将{0}复制到临时文件夹：{1}", sourceFolderPath, tempPath), LogCategory);

            // 压缩
            try
            {
                SevenZipBase.SetLibraryPath(Environment.Is64BitProcess ? SevenZip64Dll : SevenZipDll);
                var cmp = new SevenZipCompressor
                              {
                                  ArchiveFormat = OutArchiveFormat.SevenZip, 
                                  CompressionMethod = CompressionMethod.Default, 
                                  CompressionMode = CompressionMode.Create, 
                                  CompressionLevel = CompressionLevel.Fast, 
                              };

                cmp.CompressDirectory(tempPath, file7ZDirPath);
//                Logger.Write(
//                    string.Format("CompressFileFolder将临时文件夹({0})内容压缩到：{1}", tempPath, file7ZDirPath), 
//                    LogCategory);
            }
            catch (Exception e)
            {
                throw new Exception(string.Format("压缩失败:{0}", e.Message));
            }

            // 删除临时文件
            var tempDirInfo = new DirectoryInfo(tempPath);
            if (tempDirInfo.Exists)
            {
                tempDirInfo.Delete(true);
//                Logger.Write(string.Format("CompressFileFolder删除临时文件夹({0})", tempPath), LogCategory);
            }

//            Logger.Write("结束CompressFileFolder", LogCategory);
        }

        /// <summary>
        /// The connect state.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool ConnectState(string path)
        {
            return ConnectState(path, string.Empty, string.Empty);
        }

        /// <summary>
        /// The connect state.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <param name="userName">
        /// The user name.
        /// </param>
        /// <param name="passWord">
        /// The pass word.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool ConnectState(string path, string userName, string passWord)
        {
            var proc = new Process();
            try
            {
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                string dosLine = @"net use " + path + " /User:" + userName + " " + passWord + " /PERSISTENT:YES";
                proc.StandardInput.WriteLine(dosLine);
                proc.StandardInput.WriteLine("exit");
                while (!proc.HasExited)
                {
                    proc.WaitForExit(1000);
                }
            }
            finally
            {
                proc.Close();
                proc.Dispose();
            }

            return true;
        }

        /// <summary>
        /// 复制文件夹（及文件夹下所有子文件夹和文件）
        /// </summary>
        /// <param name="sourcePath">
        /// 待复制的文件夹路径
        /// </param>
        /// <param name="destinationPath">
        /// 目标路径
        /// </param>
        public static void CopyDirectory(string sourcePath, string destinationPath)
        {
            var info = new DirectoryInfo(sourcePath);
            foreach (FileSystemInfo fsi in info.GetFileSystemInfos())
            {
                string destName = Path.Combine(destinationPath, fsi.Name);

                if (fsi is FileInfo)
                {
                    // 如果是文件，复制文件
                    File.Copy(fsi.FullName, destName);
                }
                else
                {
                    // 如果是文件夹，新建文件夹，递归
                    Directory.CreateDirectory(destName);
                    CopyDirectory(fsi.FullName, destName);
                }
            }
        }

        /// <summary>
        /// 复制文件。
        /// </summary>
        /// <param name="sourceFilePath">
        /// 要复制的源文件路径（包括后缀名）
        /// </param>
        /// <param name="targetFileFolderPath">
        /// 目标路径
        /// </param>
        public static void CopyFile(string sourceFilePath, string targetFileFolderPath)
        {
            if (string.IsNullOrEmpty(sourceFilePath))
            {
                throw new ArgumentException("要拷贝的源文件路径不能为空！");
            }

            var sourceFileInfo = new FileInfo(sourceFilePath);
            if (!sourceFileInfo.Exists)
            {
                throw new ArgumentException("要拷贝的源文件不存在！");
            }

            // 获取文件名
            string name = Path.GetFileNameWithoutExtension(sourceFilePath);
            string extension = Path.GetExtension(sourceFilePath);
            string fullName = string.Format("{0}{1}{2}", name, DateTime.Now.ToString("yyyyMMddHHmmss"), extension);

            string targetFile =
                Path.Combine(
                    string.IsNullOrEmpty(targetFileFolderPath) ? TempFileFolderPath : targetFileFolderPath, 
                    fullName);

            sourceFileInfo.CopyTo(targetFile);
        }

        /// <summary>
        /// 拷贝指定目录下的所有文件到指定文件夹下
        /// </summary>
        /// <param name="sourcePath">
        /// 要拷贝的文件夹
        /// </param>
        /// <param name="targetPath">
        /// 目标文件夹
        /// </param>
        public static void CopyFileFolder(string sourcePath, string targetPath)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentException("要拷贝的源文件夹路径不能为空！");
            }

            var sourcePathInfo = new DirectoryInfo(sourcePath);
            if (!sourcePathInfo.Exists)
            {
                throw new ArgumentException("要拷贝的源文件夹不存在！");
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                targetPath = TempFileFolderPath;
            }

            if (sourcePath.Equals(targetPath))
            {
                throw new ArgumentException("源文件夹路径不能与目标文件夹路径一样！");
            }

            // 创建目标目录
            var targetDirInfo = new DirectoryInfo(targetPath);
            if (!targetDirInfo.Exists)
            {
                Directory.CreateDirectory(targetPath);
            }

            CopyDirectory(sourcePath, targetPath);
        }

        /// <summary>
        /// 将指定文件复制到共享文件夹
        /// </summary>
        /// <param name="sourceFile">
        /// </param>
        /// <param name="targetPath">
        /// </param>
        public static void CopyToShare(string sourceFile, string targetPath)
        {
            if (string.IsNullOrEmpty(sourceFile))
            {
                throw new ArgumentException("要拷贝的源文件路径不能为空！");
            }

            var sourceFileInfo = new FileInfo(sourceFile);
            if (!sourceFileInfo.Exists)
            {
                throw new ArgumentException("要拷贝的源文件不存在！");
            }

            if (string.IsNullOrEmpty(targetPath))
            {
                throw new ArgumentException("共享文件夹路径不能为空！");
            }

            // 获取文件名
            string name = Path.GetFileNameWithoutExtension(sourceFile);
            string extension = Path.GetExtension(sourceFile);
            string fullName = string.Format("{0}{1}{2}", name, DateTime.Now.ToString("yyyyMMddHHmmss"), extension);
            string targetFile = Path.Combine(targetPath, fullName);

            File.Copy(sourceFile, targetFile, true);
        }

        #endregion
    }
}