using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json; // Used for saving and loading settings
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace Tor_Proxy
{
	// A simple class to hold the settings data
	public class UserSettings
	{
		public string TorPath { get; set; } = "";
		public string ProxyCount { get; set; } = "5";
	}

	public partial class MainWindow : Window
	{
		private readonly List<Process> _runningTorProcesses = new List<Process>();
		private readonly string _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "settings.json");

		public MainWindow()
		{
			InitializeComponent();
			LoadSettings(); // Load settings when the application starts
		}

		#region Window and UI Events

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			SaveSettings(); // Save settings when the application closes
			StopAllTorProcesses();
		}

		private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
		private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
		private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

		#endregion

		#region Settings Logic (JSON-based)

		private void LoadSettings()
		{
			try
			{
				if (File.Exists(_settingsFilePath))
				{
					string json = File.ReadAllText(_settingsFilePath);
					var settings = JsonSerializer.Deserialize<UserSettings>(json);
					if (settings != null)
					{
						TorPathBox.Text = settings.TorPath;
						ProxyCountBox.Text = settings.ProxyCount;
					}
				}
			}
			catch (Exception ex)
			{
				Log($"Could not load settings: {ex.Message}");
			}
		}

		private void SaveSettings()
		{
			try
			{
				var settings = new UserSettings
				{
					TorPath = TorPathBox.Text,
					ProxyCount = ProxyCountBox.Text
				};
				string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
				File.WriteAllText(_settingsFilePath, json);
			}
			catch (Exception ex)
			{
				Log($"Could not save settings: {ex.Message}");
			}
		}

		#endregion

		#region Drag-Drop and Browse Logic

		private void TorPathBox_PreviewDragOver(object sender, DragEventArgs e)
		{
			e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
			e.Handled = true;
		}

		private void TorPathBox_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				var files = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (files != null && files.Length > 0 && Path.GetFileName(files[0]).Equals("tor.exe", StringComparison.OrdinalIgnoreCase))
				{
					TorPathBox.Text = files[0];
					Log($"تم تحديد مسار Tor: {files[0]}");
				}
				else
				{
					Log("خطأ: الرجاء إلقاء ملف tor.exe فقط.");
				}
			}
		}

		private void BrowseButton_Click(object sender, RoutedEventArgs e)
		{
			var openFileDialog = new OpenFileDialog
			{
				Title = "Select tor.exe",
				Filter = "Tor Executable (tor.exe)|tor.exe",
				CheckFileExists = true
			};

			if (openFileDialog.ShowDialog() == true)
			{
				TorPathBox.Text = openFileDialog.FileName;
				Log($"تم تحديد مسار Tor: {openFileDialog.FileName}");
			}
		}

		#endregion

		#region Core Logic (Start/Stop)

		private async void StartTorInstances_Click(object sender, RoutedEventArgs e)
		{
			if (!File.Exists(TorPathBox.Text))
			{
				Log("خطأ فادح: مسار ملف tor.exe المحدد غير موجود.");
				return;
			}
			if (!int.TryParse(ProxyCountBox.Text, out int proxyCount) || proxyCount <= 0)
			{
				Log("خطأ فادح: عدد النسخ يجب أن يكون رقمًا صحيحًا أكبر من صفر.");
				return;
			}

			string torPath = TorPathBox.Text;

			SetUIState(true);
			LogBox.Text = ""; // Clear the log box
			Log($"بدء عملية التحضير لتشغيل {proxyCount} نسخة...");

			await Task.Run(() =>
			{
				Log("جاري البحث عن وإغلاق أي عمليات Tor قديمة...");
				StopAllTorProcesses();
				Log("تم إغلاق العمليات القديمة بنجاح.");

				string torDirectory = Path.GetDirectoryName(torPath);

				if (string.IsNullOrEmpty(torDirectory))
				{
					Log("خطأ فادح: لم يتمكن البرنامج من تحديد المجلد الموجود به tor.exe.");
					SetUIState(false);
					return;
				}

				string baseDir = Path.Combine(torDirectory, "TorData");
				Log($"سيتم استخدام المجلد التالي للبيانات: {baseDir}");

				try
				{
					Directory.CreateDirectory(baseDir);
				}
				catch (Exception ex)
				{
					Log($"خطأ فادح أثناء إنشاء مجلد البيانات: {ex.Message}. جرب تشغيل البرنامج كمسؤول.");
					SetUIState(false);
					return;
				}

				Log("بدء تشغيل نسخ Tor...");
				for (int i = 0; i < proxyCount; i++)
				{
					int port = 9050 + i;
					string dataDir = Path.Combine(baseDir, $"Data_{port}");
					Directory.CreateDirectory(dataDir);

					var startInfo = new ProcessStartInfo
					{
						FileName = torPath,
						Arguments = $"--SOCKSPort {port} --DataDirectory \"{dataDir}\" --ControlPort {9151 + i}",
						WorkingDirectory = torDirectory,
						CreateNoWindow = true,
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						StandardOutputEncoding = Encoding.UTF8,
						StandardErrorEncoding = Encoding.UTF8
					};

					try
					{
						var process = new Process { StartInfo = startInfo };

						process.OutputDataReceived += (sender, args) => { if (args.Data != null) Log($"[Tor Output]: {args.Data}"); };
						process.ErrorDataReceived += (sender, args) => { if (args.Data != null) Log($"[Tor ERROR]: {args.Data}"); };

						process.Start();

						process.BeginOutputReadLine();
						process.BeginErrorReadLine();

						_runningTorProcesses.Add(process);
						Log($"تم إرسال أمر التشغيل للنسخة {i + 1} على المنفذ {port}.");
					}
					catch (Exception ex)
					{
						Log($"!!! فشل فادح عند محاولة تشغيل النسخة {i + 1} !!!");
						Log($"السبب: {ex.Message}");
					}
				}
			});

			await Task.Delay(3000);

			_runningTorProcesses.RemoveAll(p => p.HasExited);

			Log($"اكتملت عملية التشغيل. عدد النسخ التي تعمل حاليًا: {_runningTorProcesses.Count}");
			if (_runningTorProcesses.Count > 0)
			{
				ShowProxyListWindow();
			}
			else
			{
				Log("لم تنجح أي نسخة من Tor في البدء. تحقق من رسائل الخطأ أعلاه.");
				SetUIState(false);
			}
		}

		private void StopButton_Click(object sender, RoutedEventArgs e)
		{
			Log("جاري إيقاف جميع عمليات Tor...");
			StopAllTorProcesses();
			Log("تم إيقاف جميع العمليات.");
			SetUIState(false);
		}

		private void StopAllTorProcesses()
		{
			Process[] existingTorProcesses = Process.GetProcessesByName("tor");
			foreach (var process in existingTorProcesses)
			{
				try
				{
					process.Kill();
				}
				catch (Exception ex)
				{
					Log($"Could not stop process {process.Id}: {ex.Message}");
				}
			}
			_runningTorProcesses.Clear();
		}

		#endregion

		#region Helper Methods

		private void ShowProxyListWindow()
		{
			Dispatcher.Invoke(() =>
			{
				var proxyWindow = new Window
				{
					Title = "Generated Proxies",
					Width = 400,
					Height = 450,
					WindowStartupLocation = WindowStartupLocation.CenterOwner,
					Owner = this,
					AllowsTransparency = true,
					WindowStyle = WindowStyle.None,
					Background = Brushes.Transparent
				};

				// Main layout grid
				var mainGrid = new Grid();

				// Acrylic background
				var acrylicBorder = new Border
				{
					CornerRadius = new CornerRadius(10),
					Background = new SolidColorBrush(Color.FromArgb(0xAA, 0x22, 0x22, 0x28))
				};
				acrylicBorder.Effect = new System.Windows.Media.Effects.BlurEffect { Radius = 20 };
				mainGrid.Children.Add(acrylicBorder);

				// Content border
				var contentBorder = new Border
				{
					CornerRadius = new CornerRadius(10),
					BorderThickness = new Thickness(1),
					BorderBrush = new SolidColorBrush(Color.FromArgb(0x44, 0xFF, 0xFF, 0xFF))
				};
				mainGrid.Children.Add(contentBorder);

				var contentGrid = new Grid();
				contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(35) }); // Title bar
				contentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
				contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer
				contentBorder.Child = contentGrid;

				// --- Title Bar ---
				var titleBar = new Grid { Background = Brushes.Transparent };
				titleBar.MouseLeftButtonDown += (s, e) => proxyWindow.DragMove();
				var titleText = new TextBlock { Text = "Generated Proxies", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(15, 0, 0, 0) };
				var closeButton = new Button { Content = "", FontFamily = new FontFamily("Segoe MDL2 Assets"), Width = 35, Height = 30, Foreground = Brushes.White, Background = Brushes.Transparent, BorderThickness = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Right };
				closeButton.Click += (s, e) => proxyWindow.Close();
				titleBar.Children.Add(titleText);
				titleBar.Children.Add(closeButton);
				Grid.SetRow(titleBar, 0);

				// --- Proxy List ---
				var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(10, 0, 10, 0) };
				var stackPanel = new StackPanel();
				scrollViewer.Content = stackPanel;
				Grid.SetRow(scrollViewer, 1);

				var proxyList = new List<string>();
				for (int i = 0; i < _runningTorProcesses.Count; i++)
				{
					string proxy = $"socks5://127.0.0.1:{9050 + i}";
					proxyList.Add(proxy);

					var itemBorder = new Border { CornerRadius = new CornerRadius(4), Margin = new Thickness(5), Padding = new Thickness(10), Background = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)) };
					var itemGrid = new Grid();
					itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
					itemGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

					var proxyTextItem = new TextBlock { Text = proxy, Foreground = Brushes.LightGray, VerticalAlignment = VerticalAlignment.Center, FontFamily = new FontFamily("Consolas") };
					var copyItemButton = new Button { Content = "Copy", FontFamily = new FontFamily("Segoe MDL2 Assets"), ToolTip = "Copy", Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = Brushes.LightGray };

					copyItemButton.Click += async (s, e) => {
						Clipboard.SetText(proxy);
						copyItemButton.Content = "Copied!";
						await Task.Delay(1500);
						copyItemButton.Content = " Copy";
					};

					itemBorder.MouseEnter += (s, e) => itemBorder.Background = new SolidColorBrush(Color.FromArgb(0x44, 0xFF, 0xFF, 0xFF));
					itemBorder.MouseLeave += (s, e) => itemBorder.Background = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF));

					Grid.SetColumn(proxyTextItem, 0);
					Grid.SetColumn(copyItemButton, 1);
					itemGrid.Children.Add(proxyTextItem);
					itemGrid.Children.Add(copyItemButton);
					itemBorder.Child = itemGrid;
					stackPanel.Children.Add(itemBorder);
				}

				// --- Footer (Copy All Button) ---
				var copyAllButton = new Button
				{
					Content = "Copy All Proxies",
					Height = 35,
					Margin = new Thickness(15),
					Background = new SolidColorBrush(Color.FromRgb(0x00, 0x7A, 0xCC)),
					Foreground = Brushes.White,
					BorderThickness = new Thickness(0)
				};
				copyAllButton.Click += async (s, e) =>
				{
					string allProxies = string.Join(Environment.NewLine, proxyList);
					Clipboard.SetText(allProxies);
					copyAllButton.Content = "All Copied!";
					await Task.Delay(1500);
					copyAllButton.Content = "Copy All Proxies";
				};
				Grid.SetRow(copyAllButton, 2);

				contentGrid.Children.Add(titleBar);
				contentGrid.Children.Add(scrollViewer);
				contentGrid.Children.Add(copyAllButton);

				proxyWindow.Content = mainGrid;
				proxyWindow.Show();
			});
		}

		private void SetUIState(bool isRunning)
		{
			Dispatcher.Invoke(() => {
				StartButton.IsEnabled = !isRunning;
				StopButton.IsEnabled = isRunning;
			});
		}

		private void Log(string message)
		{
			Dispatcher.Invoke(() =>
			{
				string timestamp = DateTime.Now.ToString("HH:mm:ss");
				string logEntry = $"[{timestamp}] {message}{Environment.NewLine}";
				LogBox.Text = logEntry + LogBox.Text;
			});
		}

		#endregion
	}
}
