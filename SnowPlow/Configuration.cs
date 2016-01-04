using EnsureThat;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace SnowPlow
{
    [System.Serializable()]
    [XmlRoot("Configuration")]
    public class Configuration
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [XmlElement("Binary")]
        public List<Container> Containers { get; set; }

        public bool HasConfigurationFor(string testContainer)
        {
            return Containers.Any(container => container.File.Equals(testContainer, System.StringComparison.OrdinalIgnoreCase));
        }

        public Container ConfigurationFor(string testContainer)
        {
            return Containers.First(container => container.File.Equals(testContainer, System.StringComparison.OrdinalIgnoreCase));
        }

        public static Configuration Load(FileInfo file)
        {
            Ensure.That(() => file).IsNotNull();

            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            return (Configuration)serializer.Deserialize(file.OpenText());
        }

        public static Container FindConfiguration(FileInfo testContainer)
        {
            Ensure.That(() => testContainer).IsNotNull();

            DirectoryInfo directory = testContainer.Directory;
            do
            {
                string[] extensions = { "*.plow.user", "*.plow" };
                foreach (string extension in extensions)
                {
                    foreach (FileInfo plowFile in directory.EnumerateFiles(extension))
                    {
                        Configuration config = Load(plowFile);
                        if (config.HasConfigurationFor(testContainer.Name))
                            return config.ConfigurationFor(testContainer.Name);
                    }
                }
                directory = directory.Parent;
            }
            while (directory != null);
            return null;
        }
    }

    [System.Serializable()]
    public class Container
    {
        [XmlAttribute("file")]
        public string File { get; set; }

        [XmlAttribute("enable")]
        public bool Enable { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [XmlElement("EnvVar")]
        public List<EnvironmentVariable> EnvironmentVariables { get; set; }
    }

    [System.Serializable()]
    public class EnvironmentVariable
    {
        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("value")]
        public string Value { get; set; }
    }
}
