using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace ConferenceClient
{
	/// <summary>
	/// Interaction logic for WindowSoftwareRegistration.xaml
	/// </summary>
	public partial class WindowSoftwareRegistration : Window
	{
		[DllImport("HardwareID.dll")]
        public static extern String GetHardwareID(bool HDD,
                                                  bool NIC,
                                                  bool CPU,
                                                  bool BIOS,
                                                  string sLicenseCode);
		
		string hardwareID;
        public static string RegCode;
		
		public WindowSoftwareRegistration()
		{
			this.InitializeComponent();
            //RegCode = "Unregistered";
            hardwareID = GetHardwareID(true, true,true, true, "R5LR-S4TQ");
            tbHardwareID.Text = hardwareID;
            tbInstallationCode.Focus();
            //ABE9-6CDE-E3A2-DFB2
			// Insert code required on object creation below this point.
		}

        private  static string  GetMD5(string text)
        {
           UnicodeEncoding UE = new UnicodeEncoding();
           byte[] hashValue;
           byte[] message = UE.GetBytes(text);

           MD5 hashString = new MD5CryptoServiceProvider();
           string hex = "";
 
           hashValue = hashString.ComputeHash(message);
           foreach (byte x in hashValue)
            {
                hex += String.Format("{0:x2}", x);
            }
            return hex;
        }

		private void btnRegister_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			// TODO: Add event handler implementation here.
            try
            {
               
                string hashedHardwareID = GetMD5(hardwareID);
                hashedHardwareID = hashedHardwareID.Substring(1, 10);
                //MessageBox.Show(hashedHardwareID);

                if (tbInstallationCode.Text == hashedHardwareID)
                {
                    RegCode = tbInstallationCode.Text;
                    Properties.Settings.Default.Registration = RegCode;
                    Properties.Settings.Default.Save();
                    this.DialogResult = true;
                }
                else
                {
                    RegCode = "U";
                    Properties.Settings.Default.Registration = RegCode;
                    Properties.Settings.Default.Save();
                    this.DialogResult = false;
                };
                this.Close();
            }
            catch (Exception ex)
            {
            }
		}

        private void buttonContinue_Click(object sender, RoutedEventArgs e)
        {
            RegCode = "U";
            Properties.Settings.Default.Registration = RegCode;
            Properties.Settings.Default.Save();
            this.DialogResult = false;
            this.Close();
        }
	}
}