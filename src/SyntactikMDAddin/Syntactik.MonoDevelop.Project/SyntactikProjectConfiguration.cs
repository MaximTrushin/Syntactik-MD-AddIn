using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects;

namespace Syntactik.MonoDevelop.Project
{

    [ProjectModelDataItem]
    public class SyntactikProjectConfiguration : ProjectConfiguration
    {
        [ItemProperty("XMLOutputFolder")]
        string _xmlOutputFolder = "";

        public string XMLOutputFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_xmlOutputFolder))
                {

                    _xmlOutputFolder = ParentItem.BaseDirectory + @"\Output\Xml";
                }

                return _xmlOutputFolder;
            }
            set
            {
                _xmlOutputFolder = value ?? string.Empty;
            }
        }
        public SyntactikProjectConfiguration(string id) : base(id)
        {
        }

        public override FilePath OutputDirectory
        {
            get { return this.ParentItem.BaseDirectory + @"\Output\"; }
            set { }
        }
        //public override void CopyFrom(ItemConfiguration configuration)
        //{
        //    base.CopyFrom(configuration);
        //    var conf = configuration as SyntactikProjectConfiguration;
        //    if (conf != null)
        //    {
        //        _xmlOutputFolder = conf._xmlOutputFolder;
        //    }
        //}
    }
   
}