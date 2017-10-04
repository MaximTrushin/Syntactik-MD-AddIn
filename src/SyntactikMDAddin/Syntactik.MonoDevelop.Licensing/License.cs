using System.IO;
using System.Reflection;
using Rhino.Licensing;

namespace Syntactik.MonoDevelop.Licensing
{
    public enum Mode
    {
        Demo,
        Full
    }

    public class License
    {
        public Mode RuntimeMode { get; private set; }
        public LicenseValidator Validator { get; }

        public License(string licenseFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string key;
            using (var stream = assembly.GetManifestResourceStream("Syntactik.MonoDevelop.publicKey.xml"))
            using (var reader = new StreamReader(stream))
                key = reader.ReadToEnd();

            Validator = new LicenseValidator(key, licenseFileName);
            Validator.TryLoadingLicenseValuesFromValidatedXml();
        }

        public void ValidateLicense()
        {
            RuntimeMode = Mode.Demo;
            Validator.AssertValidLicense();
            RuntimeMode = Mode.Full;
        }
    }
}
