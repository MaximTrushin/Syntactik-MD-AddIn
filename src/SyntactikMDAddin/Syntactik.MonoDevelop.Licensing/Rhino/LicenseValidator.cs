using System;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace Rhino.Licensing
{
    /// <summary>
    /// License validator validates a license file
    /// that can be located on disk.
    /// </summary>
    class LicenseValidator : AbstractLicenseValidator
    {
        public const string COMPUTER_ID = "ComputerId";
        public const string EMAIL = "Email";
        private readonly string licensePath;
        private string inMemoryLicense;

        /// <summary>
        /// Creates a new instance of <seealso cref="LicenseValidator"/>.
        /// </summary>
        /// <param name="publicKey">public key</param>
        /// <param name="licensePath">path to license file</param>
        public LicenseValidator(string publicKey, string licensePath)
            : base(publicKey)
        {
            this.licensePath = licensePath;
        }

        /// <summary>
        /// Creates a new instance of <seealso cref="LicenseValidator"/>.
        /// </summary>
        /// <param name="publicKey">public key</param>
        /// <param name="licensePath">path to license file</param>
        /// <param name="licenseServerUrl">license server endpoint address</param>
        /// <param name="clientId">Id of the license holder</param>
        public LicenseValidator(string publicKey, string licensePath, string licenseServerUrl, Guid clientId)
            : base(publicKey, licenseServerUrl, clientId)
        {
            this.licensePath = licensePath;
        }

        /// <summary>
        /// Gets or Sets the license content
        /// </summary>
        protected override string License
        {
            get
            {
                return inMemoryLicense ?? File.ReadAllText(licensePath);
            }
            set
            {
                try
                {
                    File.WriteAllText(licensePath, value);
                }
                catch (Exception e)
                {
                    inMemoryLicense = value;
                    Log.Warn("Could not write new license value, using in memory model instead.", e);
                }
            }
        }

        private static string GetPlainMachineId()
        {
            string cpuInfo = string.Empty;
            ManagementClass mc = new ManagementClass("win32_processor");
            ManagementObjectCollection moc = mc.GetInstances();

            foreach (ManagementObject mo in moc)
            {
                cpuInfo = mo.Properties["processorID"].Value.ToString();
                break;
            }

            string drive = "C";
            ManagementObject dsk = new ManagementObject(
                @"win32_logicaldisk.deviceid=""" + drive + @":""");
            dsk.Get();
            string volumeSerial = dsk["VolumeSerialNumber"].ToString();

            return string.Format("{0}:{1}", cpuInfo, volumeSerial);
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static string GetMachineId()
        {
            var id = GetPlainMachineId();
            var data = new UTF8Encoding().GetBytes(id);
            SHA256 shaM = new SHA256Managed();
            var hash= shaM.ComputeHash(data);
            var result = ByteArrayToString(hash);
            return result;
        }

        /// <summary>
        /// Validates loaded license
        /// </summary>
        public override void AssertValidLicense()
        {
            if (File.Exists(licensePath) == false)
            {
                Log.WarnFormat("Could not find license file: {0}", licensePath);
                throw new LicenseFileNotFoundException();
            }

            base.AssertValidLicense();

            if (this.LicenseAttributes.ContainsKey(COMPUTER_ID))
            {
                var desired_id = this.LicenseAttributes[COMPUTER_ID];
                if (desired_id != GetMachineId())
                {
                    LicenseAttributes.Clear();
                    throw new LicenseNotFoundException();
                }
            }
        }

        /// <summary>
        /// Removes existing license from the machine.
        /// </summary>
        public override void RemoveExistingLicense()
        {
            File.Delete(licensePath);
        }

        public static string GetMachineName()
        {
            throw new NotImplementedException();
        }
    }
}
