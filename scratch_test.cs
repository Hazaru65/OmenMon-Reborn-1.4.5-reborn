using System;
using System.Xml;

class Program {
    static void Main() {
        try {
            XmlDocument xml = new XmlDocument();
            xml.Load(@"C:\Users\Hazar\Documents\1vibec\OmenMon-Reborn-1.4.0-reborn\Bin\OmenMon.xml");
            string XmlPrefix = "OmenMon/Config/";
            
            XmlNode node1 = xml.SelectSingleNode(XmlPrefix + "FanConstSafetyEnabled");
            if (node1 != null) {
                Console.WriteLine("FanConstSafetyEnabled: " + node1.InnerText);
            } else {
                Console.WriteLine("FanConstSafetyEnabled node not found!");
            }

            XmlNode node2 = xml.SelectSingleNode(XmlPrefix + "FanConstSafetyTemp");
            if (node2 != null) {
                Console.WriteLine("FanConstSafetyTemp: " + node2.InnerText);
            } else {
                Console.WriteLine("FanConstSafetyTemp node not found!");
            }
        } catch (Exception ex) {
            Console.WriteLine(ex.ToString());
        }
    }
}
