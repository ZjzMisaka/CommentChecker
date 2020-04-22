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

            await Task.Run(() =>
            {
                FileTraverse(folderPath, extensions, isRecursive);
                stopFlag = false;
            });

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


            int ngCount = 0;

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
                    if (!CheckFile(fileName))
                    {
                        ++ngCount;
                    }
                }
                stopFlag = false;
            });
            if (ngCount == 0)
            {
                System.Windows.MessageBox.Show("ALL OK");
            }
            else
            {
                System.Windows.MessageBox.Show(ngCount + " NG");
            }

        }
        private bool CheckFile(string fileName)
        {
            int lineNumber;

            string startComment = "";
            string endComment = "";

            System.Windows.Application.Current.Dispatcher.Invoke(() => {
                startComment = tb_startcomment.Text;
                endComment = tb_endcomment.Text;
            });

            string fileContent = System.IO.File.ReadAllText(fileName);

            int startIndex = 0;
            int endIndex = 0;
            int errorIndex = 0;

            if (fileContent.IndexOf(startComment) == -1 && fileContent.IndexOf(endComment) != -1)
            {
                string currentErrorText = fileName + "\n" + "找不到注释头\n\n";
                tb_errortext.Dispatcher.Invoke(() =>
                {
                    tb_errortext.Text += currentErrorText;
                });
                return false;
            }
            if (fileContent.IndexOf(endComment) == -1 && fileContent.IndexOf(startComment) != -1)
            {
                string currentErrorText = fileName + "\n" + "找不到注释尾\n\n";
                tb_errortext.Dispatcher.Invoke(() =>
                {
                    tb_errortext.Text += currentErrorText;
                });
                return false;
            }
            if (fileContent.IndexOf(startComment) > fileContent.IndexOf(endComment))
            {
                startIndex = fileContent.IndexOf(startComment);
                errorIndex = fileContent.IndexOf(endComment);

                string currentErrorText = fileName + "\n";
                lineNumber = fileContent.Substring(0, startIndex == -1 ? 0 : startIndex).Split('\n').Count();
                currentErrorText += "起始行数: " + lineNumber + "\n";
                lineNumber = fileContent.Substring(0, errorIndex == -1 ? 0 : errorIndex).Split('\n').Count();
                currentErrorText += "错误行数: " + lineNumber + "\n\n";
                tb_errortext.Dispatcher.Invoke(() =>
                {
                    tb_errortext.Text += currentErrorText;
                });
                return false;
            }
            if (fileContent.LastIndexOf(startComment) > fileContent.LastIndexOf(endComment))
            {
                errorIndex = fileContent.LastIndexOf(startComment);
                endIndex = fileContent.LastIndexOf(endComment);

                string currentErrorText = fileName + "\n";
                lineNumber = fileContent.Substring(0, errorIndex == -1 ? 0 : errorIndex).Split('\n').Count();
                currentErrorText += "错误行数: " + lineNumber + "\n";
                lineNumber = fileContent.Substring(0, endIndex == -1 ? 0 : endIndex).Split('\n').Count();
                currentErrorText += "结束行数: " + lineNumber + "\n\n";
                tb_errortext.Dispatcher.Invoke(() =>
                {
                    tb_errortext.Text += currentErrorText;
                });
                return false;
            }

            startIndex = 0;
            endIndex = 0;
            errorIndex = 0;

            bool startFirst = true;
            while (startIndex < fileContent.Count())
            {
                string currentStartComment = startFirst ? startComment : endComment;
                string currentEndComment = startFirst ? endComment : startComment;

                startIndex = fileContent.IndexOf(currentStartComment, startIndex);

                if (startIndex == -1)
                {
                    break;
                }

                errorIndex = endIndex = startIndex + 1;
                endIndex = fileContent.IndexOf(currentEndComment, endIndex);
                errorIndex = fileContent.IndexOf(currentStartComment, errorIndex);

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
                    
                    return false;
                }

                if (endIndex == -1)
                {
                    break;
                }

                startIndex = endIndex;
                startFirst = startFirst == true ? false : true;
            }
            return true;
        }


    }
}
