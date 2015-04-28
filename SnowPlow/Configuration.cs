using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SnowPlow
{
    [Serializable()]
    [XmlRoot("Configuration")]
    public class Configuration
    {
        [XmlElement("Binary")]
        public List<Binary> Binaries { get; set; }

        public static Binary FindConfiguration(FileInfo file)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            DirectoryInfo directory = file.Directory;
            do
            {
                foreach (FileInfo plowFile in directory.EnumerateFiles("*.plow.user"))
                {
                    using (StreamReader stream = plowFile.OpenText())
                    {
                        Configuration config = (Configuration)serializer.Deserialize(stream);
                        Binary settings = config.Binaries.Find(binary => binary.File.Equals(file.Name, StringComparison.OrdinalIgnoreCase));
                        if (settings != null) return settings;
                    }
                }
                foreach (FileInfo plowFile in directory.EnumerateFiles("*.plow"))
                {
                    using (StreamReader stream = plowFile.OpenText())
                    {
                        Configuration config = (Configuration)serializer.Deserialize(stream);
                        Binary settings = config.Binaries.Find(binary => binary.File.Equals(file.Name, StringComparison.OrdinalIgnoreCase));
                        if (settings != null) return settings;
                    }
                }
                directory = directory.Parent;
            }
            while (directory != null);
            return null;
        }
    }


    [Serializable()]
    public class Binary
    {
        [XmlAttribute("file")]
        public string File { get; set; }

        [XmlAttribute("enable")]
        public bool Enable { get; set; }

        [XmlElement("EnvVar")]
        public List<EnvVar> EnvVars { get; set; }
    }

    [Serializable()]
    public class EnvVar
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }
}
