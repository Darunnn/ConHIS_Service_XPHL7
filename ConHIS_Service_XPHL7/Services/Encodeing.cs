//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;

//namespace ConHIS_Service_XPHL7.Services
//{
//    internal class Encodeing
//    {
//        public EncodeService(string typeEncode)
//        {
//            string hl7String = "";
//            try
//            {   
                
//                string typeEncode = Encoding.UTF8.GetString(data.Hl7Data);
//                hl7String = typeEncode;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogWarning($"Failed to decode HL7 data with TIS-620: {ex.Message}. Falling back to UTF8.");
//                hl7String = Encoding.UTF8.GetString(data.Hl7Data);
//            }
//        }

//    }
//}
