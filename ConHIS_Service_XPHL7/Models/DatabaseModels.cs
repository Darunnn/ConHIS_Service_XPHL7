using System;

namespace ConHIS_Service_XPHL7.Models
{
    public class DrugDispenseipd
    {
        public int DrugDispenseipdId { get; set; }
        public int PrescId { get; set; }
        public string DrugRequestMsgType { get; set; }
        public byte[] Hl7Data { get; set; }
        public DateTime DrugDispenseDatetime { get; set; }
        public char RecieveStatus { get; set; } = 'N';
        public DateTime? RecieveStatusDatetime { get; set; }
        public string RecieveOrderType { get; set; }
    }

  
}
