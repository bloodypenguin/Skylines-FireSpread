using System.IO;
using System.Xml.Serialization;

namespace FireSpread
{
    public class ModSettings
    {
        public const string SettingsPath = FileName;
        private const string FileName = "FireSpreadSettings.xml";

        [XmlIgnore]
        private static ModSettings _instance;

        [XmlIgnore]
        public static ModSettings Instance
        {
            get
            {
                if (ModSettings._instance == null)
                    ModSettings.Load();
                return ModSettings._instance;
            }
            set
            {
                ModSettings._instance = value;
            }
        }

        public float FireSpreadModifier { get; set; }

        public float BaseFireSpreadChance { get; set; }

        public float NoWaterFireSpreadAdditional { get; set; }

        public float UneducatedFireSpreadAdditional { get; set; }

        public float IndustrialFireSpreadAdditional { get; set; }

        public float PowerPlantFireSpreadAdditional { get; set; }

        public ModSettings()
        {
            this.FireSpreadModifier = 0.00725f;
            this.BaseFireSpreadChance = 2.5f;
            this.NoWaterFireSpreadAdditional = 7f;
            this.UneducatedFireSpreadAdditional = 1f;
            this.IndustrialFireSpreadAdditional = 10f;
            this.PowerPlantFireSpreadAdditional = 25f;
        }

        public static void Load()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(ModSettings));
            if (!File.Exists(FileName))
            {
                using (FileStream fileStream = File.Create(FileName))
                    xmlSerializer.Serialize((Stream)fileStream, (object)new ModSettings());
            }
            using (FileStream fileStream = File.OpenRead(FileName))
                ModSettings._instance = xmlSerializer.Deserialize((Stream)fileStream) as ModSettings;
        }
    }
}
