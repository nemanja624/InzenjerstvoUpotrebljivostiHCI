using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Mercedes_Models_CMS.Helpers;
using System.Windows.Media.Imaging;

namespace Mercedes_Models_CMS.Models
{
    [Serializable]
    public class CarModel
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }   
        public string RtfFilePath { get; set; }
        public DateTime DateAdded { get; set; }
        public int HorsePower { get; set; }
        public bool IsSelected { get; set; } = false;

        [XmlIgnore]
        public string? DisplayImagePath => ImagePathResolver.ResolveForDisplay(ImagePath);

        [XmlIgnore]
        public BitmapImage? DisplayImage => ImagePathResolver.LoadBitmapForDisplay(ImagePath);
    }
}
