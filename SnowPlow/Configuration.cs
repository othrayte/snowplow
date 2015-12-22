using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SnowPlow
{
    [Serializable()]
    [XmlRoot("Configuration")]
    public class Configuration
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [XmlElement("Binary")]
        public List<Binary> Binaries { get; set; }

        public bool HasConfigurationFor(string testContainer)
        {
            return Binaries.Any(binary => binary.File.Equals(testContainer, StringComparison.OrdinalIgnoreCase));
        }

        public Binary ConfigurationFor(string testContainer)
        {
            return Binaries.First(binary => binary.File.Equals(testContainer, StringComparison.OrdinalIgnoreCase));
        }

        public static Configuration Load(FileInfo file)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            return (Configuration)serializer.Deserialize(file.OpenText());
        }

        public static Binary FindConfiguration(FileInfo file)
        {
            do
            {
                string[] extensions = { "*.plow.user", "*.plow" };
                foreach (string extension in extensions)
                {
                    foreach (FileInfo plowFile in directory.EnumerateFiles(extension))
                    {
                        Configuration config = Load(plowFile);
                        if (config.HasConfigurationFor(file.Name))
                            return config.ConfigurationFor(file.Name);
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
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
