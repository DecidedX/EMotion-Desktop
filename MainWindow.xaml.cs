using EMotion.Client;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using HandyControl.Controls;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Net;
using System.Linq;

namespace EMotion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {

        private ControllerManager? controllerManager;
        private QuickConnectThread? quickConnectThread;
        private ObservableCollection<Item> controllersSource;

        public MainWindow()
        {
            InitializeComponent();
            controllersSource = new ObservableCollection<Item>
            {
                new Item() {Name = "add"}
            };
            try
            {
                controllerManager = new ControllerManager();
                quickConnectThread = new QuickConnectThread(this);
                quickConnectThread.start();
            }
            catch (Exception ex)
            {
                HandyControl.Controls.MessageBox.Show("请先安装VigemBus！");
                Process.Start("explorer", "https://github.com/nefarius/ViGEmBus/releases/tag/v1.22.0");
                Environment.Exit(0);
            }
            controllers.ItemsSource = controllersSource;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (controllers.SelectedIndex != -1)
            {
                if (((Item)controllers.SelectedItem).Name == "add")
                {
                    controllersSource[0] = new Item() { Name = "input" };
                }
                controllers.UnselectAll();
            }
        }

        private async void confirmInput(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBox ipInput = (System.Windows.Controls.TextBox)VisualTreeHelper.GetChild(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent((Button)sender)), 1);
            if (System.Text.RegularExpressions.Regex.IsMatch(ipInput.Text, "((2(5[0-5]|[0-4]\\d))|[0-1]?\\d{1,2})(\\.((2(5[0-5]|[0-4]\\d))|[0-1]?\\d{1,2})){3}") && !containsController(ipInput.Text))
            {
                var d = Dialog.Show<WaitDialog>();
                controllerManager!.createController(ipInput.Text);
                int i = 10;
                while (!controllerManager.getStatus(ipInput.Text).HasValue && i > 0)
                {
                    await Task.Delay(1000);
                    i--;
                }
                if (controllerManager.getStatus(ipInput.Text) ?? false)
                {
                    controllersSource[0] = new Item() { Name = "add"};
                    controllersSource.Add(new Item() { Name = "controller", Value = ipInput.Text});
                }
                else
                {
                    controllerManager!.destoryController(ipInput.Text);
                    Dialog.Show(new RemindDialog("连接失败"));
                }
                d.Close();
            }
            else
            {
                ipInput.SetValue(BorderBrushProperty, Brushes.Red);
            }
        }

        private bool containsController(string ip)
        {
            bool res = false;
            foreach(Item i in controllersSource)
            {
                if (i.Name == "controller" && i.Value == ip) res = true;
            }
            return res;
        }

        private void cancelInput(object sender, RoutedEventArgs e)
        {
            controllersSource[0] = new Item() { Name = "add" };
            controllers.ItemsSource = controllersSource;
        }

        public void showQuickConnect(string ip)
        {
            foreach (Item i in controllersSource)
            {
                if (i.Value == ip) return;
            }
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                controllersSource.Add(new Item() { Name = "quick" , Value = ip });
            }));
        }

        private void quickConnectConfirm(object sender, RoutedEventArgs e)
        {
            Button button = ((Button)sender);
            quickConnectThread!.ackQC(new IPEndPoint(IPAddress.Parse(((Item)button.Tag).Value), QuickConnectThread.PORT));
            confirmInput(sender, e);
            cancelQuickConnect(sender, e);
        }

        private void cancelQuickConnect(object sender, RoutedEventArgs e)
        {
            Button button = ((Button)sender);
            controllersSource.Remove((Item) button.Tag);
        }

        private void disconnectController(object sender, RoutedEventArgs e)
        {
            Button button = ((Button)sender);
            controllerManager!.destoryController(((Item)button.Tag).Value);
            controllersSource.Remove((Item) button.Tag);
        }

        private void switchGamepad(object sender, RoutedEventArgs e)
        {
            RadioButton gamepad = (RadioButton)sender;
            if (controllerManager!.isDS4((string)gamepad.Tag) ^ gamepad.Content.Equals("DS4"))
            {
                controllerManager.switchGamepad((string)gamepad.Tag);
            }
        }
    }
}


