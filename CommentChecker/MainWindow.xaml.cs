using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;

namespace CommentChecker
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        string folderPath;
        List<string> filePathList = new List<string>();
        
        bool stopFlag = false;
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Button_File_Click(object sender, RoutedEventArgs e)
        {
            filePathList = new List<string>();
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                string fileName = ofd.FileName;
                l_filepath.Content = fileName;
                l_folderpath.Content = "";
                filePathList.Add(fileName);
            }
            else
            {
                return;
            }
        }
        private async void Button_Folder_ClickAsync(object sender, RoutedEventArgs e)
        {
            filePathList = new List<string>();

            System.Windows.Forms.FolderBrowserDialog openFileDialog = new System.Windows.Forms.FolderBrowserDialog();
            
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                l_folderpath.Content = folderPath = openFileDialog.SelectedPath;
                l_filepath.Content = "";
            }
            else
            {
                return;
            }

            List<string> extensions = tb_extension.Text.Split(' ').ToList();
            bool? isRecursive = cb_recursive.IsChecked;

            btn_stop.IsEnabled = true;
            await Task.Run(() =>
            {
                FileTraverse(folderPath, extensions, isRecursive);
                stopFlag = false;
            });
            btn_stop.IsEnabled = false;

            l_info.Content = "";

            string allFiles = "";
            foreach (string fileName in filePathList)
            {
                allFiles += fileName + "\n";
            }

            System.Windows.MessageBox.Show(allFiles);
        }


        private void FileTraverse(string folderPath, List<string> extensions, bool? isRecursive)
        {
            if(string.IsNullOrEmpty(folderPath) || stopFlag)
            {
                return;
            }

            DirectoryInfo dir = new DirectoryInfo(folderPath);
            try
            {
                if (!dir.Exists)
                    return;
                DirectoryInfo dirD = dir as DirectoryInfo;
                FileSystemInfo[] files = dirD.GetFileSystemInfos();
                foreach (FileSystemInfo FSys in files)
                {
                    FileInfo fileInfo = FSys as FileInfo;

                    if (fileInfo != null && (extensions.Contains(System.IO.Path.GetExtension(fileInfo.Name)) || extensions.ElementAt(0) == ""))
                    {
                        string fileName = System.IO.Path.Combine(fileInfo.DirectoryName, fileInfo.Name);
                        l_info.Dispatcher.Invoke(() =>
                        {
                            l_info.Content = fileName;
                        });
                        filePathList.Add(fileName);
                    }
                    else if(isRecursive == true)
                    {
                        string pp = FSys.Name;
                        FileTraverse(System.IO.Path.Combine(folderPath, FSys.ToString()), extensions, isRecursive);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
                return;
            }
        }

        private void Button_Stop_Click(object sender, RoutedEventArgs e)
        {
            stopFlag = true;
        }

        private async void Button_Ok_ClickAsync(object sender, RoutedEventArgs e)
        {
            folderPath = "";

            tb_errortext.Text = "";

            bool isIgnoreSpace = cb_ignorespace.IsChecked == true ? true : false;
            bool isTolower = cb_tolower.IsChecked == true ? true : false;
            bool isRegexp = cb_regexp.IsChecked == true ? true : false;

            int ngCount = 0;

            btn_stop.IsEnabled = true;
            await Task.Run(() =>
            {
                foreach (string fileName in filePathList)
                {
                    if (stopFlag)
                    {
                        break;
                    }
                    l_info.Dispatcher.Invoke(() =>
                    {
                        l_info.Content = fileName;
                    });
                    if (!CheckFile(fileName, isIgnoreSpace, isTolower, isRegexp))
                    {
                        ++ngCount;
                    }
                }
                stopFlag = false;
            });
            btn_stop.IsEnabled = false;
            if (ngCount == 0)
            {
                System.Windows.MessageBox.Show("ALL OK");
            }
            else
            {
                System.Windows.MessageBox.Show(ngCount + " FILE(s) NG");
            }

        }
        private bool CheckFile(string fileName, bool isIgnoreSpace, bool isTolower, bool isRegexp)
        {
            if (!File.Exists(fileName))
            {
                string currentErrorText = "找不到文件: " + fileName + "\n\n";
                tb_errortext.Dispatcher.Invoke(() =>
                {
                    tb_errortext.Text += currentErrorText;
                });
                return false;
            }

            int lineNumber;

            string startComment = "";
            string endComment = "";

            bool result = true;

            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                startComment = tb_startcomment.Text;
                endComment = tb_endcomment.Text;
            });

            string fileContent = System.IO.File.ReadAllText(fileName);

            if (isIgnoreSpace)
            {
                fileContent = string.Join(" ", fileContent.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                startComment = string.Join(" ", startComment.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                endComment = string.Join(" ", endComment.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            }
            if (isTolower)
            {
                fileContent = fileContent.ToLowerInvariant();
                startComment = startComment.ToLowerInvariant();
                endComment = endComment.ToLowerInvariant();
            }

            int startIndex = 0;
            int endIndex = 0;
            int errorIndex = 0;
            int lastStartIndex = 0;
            int lastEndIndex = 0;
            Regex regStart = new Regex(startComment);
            Regex regEnd = new Regex(endComment);

            if (!isRegexp)
            {
                startIndex = fileContent.IndexOf(startComment);
                endIndex = fileContent.IndexOf(endComment);
                lastStartIndex = fileContent.LastIndexOf(startComment);
                lastEndIndex = fileContent.LastIndexOf(endComment);
            }
            else
            {
                MatchCollection mcStart = regStart.Matches(fileContent);
                MatchCollection mcEnd = regEnd.Matches(fileContent);
                startIndex = mcStart.Count > 0 ? mcStart[0].Index : -1;
                endIndex = mcEnd.Count > 0 ? mcEnd[0].Index : -1;
                lastStartIndex = mcStart.Count > 0 ? mcStart[mcStart.Count - 1].Index : -1;
                lastEndIndex = mcEnd.Count > 0 ? mcEnd[mcEnd.Count - 1].Index : -1;
            }

            if (startIndex == -1 && endIndex != -1)
            {
                string currentErrorText = fileName + "\n" + "找不到注释头\n\n";
                tb_errortext.Dispatcher.Invoke(() =>
                {
                    tb_errortext.Text += currentErrorText;
                });
                return false;
            }
            if (endIndex == -1 && startIndex != -1)
            {
                string currentErrorText = fileName + "\n" + "找不到注释尾\n\n";
                tb_errortext.Dispatcher.Invoke(() =>
                {
                    tb_errortext.Text += currentErrorText;
                });
                return false;
            }
            if (startIndex > endIndex)
            {
                // startIndex = fileContent.IndexOf(startComment);
                // errorIndex = fileContent.IndexOf(endComment);

                string currentErrorText = fileName + "\n";
                lineNumber = fileContent.Substring(0, startIndex == -1 ? 0 : startIndex).Split('\n').Count();
                currentErrorText += "起始行数: " + lineNumber + "\n";
                lineNumber = fileContent.Substring(0, errorIndex == -1 ? 0 : errorIndex).Split('\n').Count();
                currentErrorText += "错误行数: " + lineNumber + "\n\n";
                tb_errortext.Dispatcher.Invoke(() =>
                {
                    tb_errortext.Text += currentErrorText;
                });
                result = false;
            }
            if (lastStartIndex > lastEndIndex)
            {
                // errorIndex = fileContent.LastIndexOf(startComment);
                // lastEndIndex = fileContent.LastIndexOf(endComment);

                string currentErrorText = fileName + "\n";
                lineNumber = fileContent.Substring(0, errorIndex == -1 ? 0 : errorIndex).Split('\n').Count();
                currentErrorText += "错误行数: " + lineNumber + "\n";
                lineNumber = fileContent.Substring(0, lastEndIndex == -1 ? 0 : lastEndIndex).Split('\n').Count();
                currentErrorText += "结束行数: " + lineNumber + "\n\n";
                tb_errortext.Dispatcher.Invoke(() =>
                {
                    tb_errortext.Text += currentErrorText;
                });
                result = false;
            }

            startIndex = 0;
            endIndex = 0;
            errorIndex = 0;

            bool startFirst = true;
            while (startIndex < fileContent.Count())
            {
                if(stopFlag)
                {
                    return result;
                }

                string currentStartComment = startFirst ? startComment : endComment;
                string currentEndComment = startFirst ? endComment : startComment;

                regStart = new Regex(startFirst ? startComment : endComment);
                regEnd = new Regex(startFirst ? endComment : startComment);

                if (!isRegexp)
                {
                    startIndex = fileContent.IndexOf(currentStartComment, startIndex);
                }
                else
                {
                    Match match = regStart.Match(fileContent, startIndex);
                    startIndex = match.Success ? match.Index : -1;
                }

                if (startIndex == -1)
                {
                    break;
                }

                errorIndex = endIndex = startIndex + 1;
                if (!isRegexp)
                {
                    endIndex = fileContent.IndexOf(currentEndComment, endIndex);
                    errorIndex = fileContent.IndexOf(currentStartComment, errorIndex);
                }
                else
                {
                    Match endMatch = regEnd.Match(fileContent, endIndex);
                    endIndex = endMatch.Success ? endMatch.Index : -1;
                    Match errorMatch = regStart.Match(fileContent, errorIndex);
                    errorIndex = errorMatch.Success ? errorMatch.Index : -1;
                }

                if (errorIndex != -1 && errorIndex < endIndex)
                {
                    string currentErrorText = fileName + "\n";
                    lineNumber = fileContent.Substring(0, startIndex == -1 ? 0 : startIndex).Split('\n').Count();
                    currentErrorText += "起始行数: " + lineNumber + "\n";
                    lineNumber = fileContent.Substring(0, errorIndex == -1 ? 0 : errorIndex).Split('\n').Count();
                    currentErrorText += "错误行数: " + lineNumber + "\n";
                    lineNumber = fileContent.Substring(0, endIndex == -1 ? 0 : endIndex).Split('\n').Count();
                    currentErrorText += "结束行数: " + lineNumber + "\n\n";

                    tb_errortext.Dispatcher.Invoke(() =>
                    {
                        tb_errortext.Text += currentErrorText;
                    });

                    result = false;
                }

                if (endIndex == -1)
                {
                    break;
                }

                startIndex = endIndex;
                startFirst = startFirst == true ? false : true;
            }
            return result;
        }


    }
}
