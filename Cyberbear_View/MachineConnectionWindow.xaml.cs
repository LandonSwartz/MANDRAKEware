﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using log4net;
using System.IO;
using Cyberbear_Events.Machine;
using Cyberbear_Events.Machine.GrblArdunio;
using Cyberbear_Events.Machine.LightsArdunio;
using Cyberbear_Events.Machine.CameraControl;
using Cyberbear_Events;

namespace Cyberbear_View
{
    /// <summary>
    /// Interaction logic for MachineConnectionWindow.xaml
    /// </summary>
    public partial class MachineConnectionWindow : Window
    {
        //logger
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private GRBLArdunio gArdunio = GRBLArdunio.Instance;
        private LightsArdunio litArdunio = LightsArdunio.Instance;
        private CameraControl cameraControl = CameraControl.Instance;

        public BitmapImage bi = new BitmapImage(); //image going to be captured



        public MachineConnectionWindow()
        {
            InitializeComponent();

            log.Info("Machine Connection Window Entered");

            Task task = new Task(() => cameraControl.StartVimba());
            task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task.Start();

            //for now will initalize one machine in start  
        }

        /// <summary>
        /// Connects ardunios to form machine
        /// </summary>
        private void Connect_Btn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Connect_Machine();

                //start and stop buttons enabling to be pressed
                StartManualCycleBtn.IsEnabled = true;
                StopManualCycleBtn.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                log.Debug("Failure to connect to serial ports because: " + ex.Message);
            }
        }

        /// <summary>
        /// Connects Grbl and Lights Ardunio and logs the connections
        /// </summary>
        private void Connect_Machine()
        {
            if(gArdunio.Connected == false)
            {
                gArdunio.Connect();
                log.Info("GRBL Ardunio Connected");
            }
            else if (litArdunio.Connected ==false)
            {
                litArdunio.Connect();
                log.Info("Lights Ardunio Connected");
            } 
        }


        //Couldn't figure out how to make more modular so will try in future, works for now
        /// <summary>
        /// Opens all available serial ports for grbl serial port combo box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GrblSerialComboBox_DropDownOpened(object sender, EventArgs e)
        {
            log.Info("Grbl Serial Port Combo Box Opened");

            updateSerialPortComboBox(GrblSerialComboBox);
        }
        /// <summary>
        /// Opens all available serial ports for lights ardunio serial port combo box
        /// </summary>
        private void LightsSerialComboBox_DropDownOpened(object sender, EventArgs e)
        {
            log.Info("Lights Ardunio Serial Port Combo Box Opened");

            updateSerialPortComboBox(LightsSerialComboBox);
        }

        /// <summary>
        /// Updates Combo Box Selection of Serial ports for connecting ardunios
        /// </summary>
        /// <param name="cb">The combobox that is being updated is being passed to the function to get serial ports</param>
        private void updateSerialPortComboBox(ComboBox cb)
        {
            cb.Items.Clear();
            String[] ports = System.IO.Ports.SerialPort.GetPortNames();
            int i = 0;
            int selectedIndex = 0;

            foreach (string port in ports)
            {
                if (string.Equals(cb.Name, "PeripheralSerialPortSelect"))
                {
                    selectedIndex = i;
                }
                if (string.Equals(cb.Name, "SerialPortSelect"))
                {
                    selectedIndex = i;
                }

                cb.Items.Add(port);
                i++;
            }

            string selectPort = ports[i-1];
            UpdateSerialConst(selectPort, cb);

            cb.SelectedIndex = selectedIndex;

            log.Debug("Serial Ports Updated for Combo Box");

            //sees if connection button can be enabled
            Connect_Btn_CanEnable();
        }

        /// <summary>
        /// Updates the Serial const class when port value is changed for connections
        /// </summary>
        /// <param name="comPort">The string name of the comPort selected in the combo box</param>
        /// <param name="cb">The name of the combo box that will have the const updated</param>
        private void UpdateSerialConst(string comPort, ComboBox cb)
        {
            if (string.Equals(cb.Name, "PeripheralSerialPortSelect"))
            {
                SerialConsts.defaultComPortLights = comPort;
                log.Debug("Serial Port for lights is: " + comPort);
            }
            if (string.Equals(cb.Name, "SerialPortSelect"))
            {
                SerialConsts.defaultComPortGrbl = comPort;
                log.Debug("Serial Port for Grbl is: " + comPort);
            }
        }

        /// <summary>
        /// If drop down opened
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            ((ComboBox)sender).Items.Clear();

            foreach (string port in System.IO.Ports.SerialPort.GetPortNames())
                ((ComboBox)sender).Items.Add(port);
        }

        /// <summary>
        /// For when Machine Window Closing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Disconnect_Machine();

            log.Info("Machine Connection Window Closed");
        }

        /// <summary>
        /// Event for start button of MachineConnectionWindow, takes in a hard coded file
        /// and reads the commands line by line and sends to gArdunio property
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartManualCycleBtn_Click(object sender, RoutedEventArgs e)
        {
            cameraControl.ImageAcquiredEvent += CameraControl_ImageAcquiredEvent;

            log.Info("Starting a Manual Cycle");

            // string filePath = @"C:\Users\lsceedlings\Desktop\Lando's Folder\GRBLCommands.txt"; //for first workstation testing
            string filePath = @"C:\Users\sam998\Desktop\GRBLCommands.txt"; //for second workstation testing

            log.Debug("Using the file: " + filePath);

            List<string> lines = File.ReadAllLines(filePath).ToList(); //putting all the lines in a list

            foreach (string line in lines)
            {
                gArdunio.SendLine(line); //sending line to ardunio
                log.Info("G Command Sent: " + line);

                //if line not homing command then take pics
                if(!line.Contains('H'))
                {
                    if(!line.Contains('Y')) //if not moving y axis then take pics
                    {
                        bi = cameraControl.CapSaveImage().Clone(); //capture image
                        bi.Freeze(); //freezes image to avoid need for copying to display and put in other threads
                        //may need to raise event to work but idk
                    }
                    
                }

                System.Threading.Thread.Sleep(1500); //sleep for 1.5 seconds
            }
        }

        /// <summary>
        /// Camera Control registers when the Image Acquired Event is raised
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraControl_ImageAcquiredEvent(object sender, EventArgs e)
        {
            log.Debug("Photo taken by program");
        }

        /// <summary>
        /// Stops manual cycle when started
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopManualCycleBtn_Click(object sender, RoutedEventArgs e)
        {
            //gArdunio.SoftReset();
        }

        /// <summary>
        /// Exception Handler when expections happens
        /// </summary>
        /// <param name="task">The task were the expection occurred is passed to the method
        /// to log the error</param>
        static void ExceptionHandler(Task task)
        {
            var exception = task.Exception;
            log.Error(exception);
        }

        private void CameraList_cb_DropDownOpened(object sender, EventArgs e)
        {
            updateCameraSettingsOptions();
        }

        /// <summary>
        /// Updates Camera Settings Options for combo box, may change in future
        /// </summary>
        /// Commenting out for now because not problem
        private void updateCameraSettingsOptions()
        {
            string csPath = CameraConst.CameraSettingsPath;
            CameraList_cb.Items.Clear();

            DirectoryInfo d = new DirectoryInfo(@"C:\Users\sam998\Desktop\Cyberbear\Cyberbear\Cyberbear_Events\Machine\CameraControl\CameraSettings");//Assuming Test is your Folder
            FileInfo[] Files = d.GetFiles("*.xml"); //Getting Text files with .xml at the end
                                                    //string str = "";

            int i = 0;
            int selectedIndex = i;
            foreach (FileInfo file in Files)
            {
                if (string.Equals(file.Name, csPath))
                {
                    selectedIndex = i;
                }
                CameraList_cb.Items.Add(file);
                i++;
            }
            Dispatcher.Invoke(() =>
            {//this refer to form in WPF application 
                CameraList_cb.SelectedIndex = selectedIndex;
            });

        }

        /// <summary>
        /// Updates Camera instance of new settings chosen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraList_cb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Task task = new Task(() => cameraControl.loadCameraSettings());
            task.ContinueWith(ExceptionHandler, TaskContinuationOptions.OnlyOnFaulted);
            task.Start();
        }

        /// <summary>
        /// Connection button is enabled after both combo boxes filled with serial ports. Method
        /// is raised after closing drop down box in combo box 
        /// </summary>
        private void Connect_Btn_CanEnable()
        {
            //if selected index greater than -1 then something was selected in the combo box
            if(GrblSerialComboBox.SelectedIndex > -1 && LightsSerialComboBox.SelectedIndex > -1)
            {
                Connect_Btn.IsEnabled = true;
                Disconnect_Btn.IsEnabled = true;

                log.Debug("Connection button is enabled");
            }
        }

        private void SaveFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            string folderResult = GetFolderResult();

            if(folderResult != null) //if user chose something
            {
                CameraConst.SaveFolderPath = folderResult;

                SaveFolderPath.Text = folderResult; //set text to folder path
            }
        }

        /// <summary>
        /// Opens dialog window to search for window to save photos t0
        /// </summary>
        /// <returns>String name of Folder path for saving</returns>
        private string GetFolderResult()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                switch (result)
                {
                    case System.Windows.Forms.DialogResult.OK:
                        return dialog.SelectedPath;
                    case System.Windows.Forms.DialogResult.Cancel:
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Disconnects Machines
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Disconnect_Btn_Click(object sender, RoutedEventArgs e)
        {
            Disconnect_Machine();

            Connect_Btn.IsEnabled = false;
            Disconnect_Btn.IsEnabled = false;

            log.Info("Disconnect Button Pressed");
        }

        /// <summary>
        /// When called, will disconnect gardunio and lit ardunio
        /// </summary>
        private void Disconnect_Machine()
        {
            if (gArdunio.Connected == true)
            {
                gArdunio.Disconnect();
                log.Info("Grbl Ardunio Disconnected");
            }
            else if (litArdunio.Connected == true)
            {
                litArdunio.Disconnect();
                log.Info("Lights Ardunio Disconnected");
            }
            else
            {
                cameraControl.ShutdownVimba();
                log.Info("Camera Control Shutdown");
            }

            log.Info("Machine Disconnected");
        }
    }
}
