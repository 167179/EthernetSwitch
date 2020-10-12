﻿using System.Collections.Generic;
using Lextm.SharpSnmpLib;

namespace EthernetSwitch.Models.SNMP
{
    public class WalkSNMPv1ViewModel
    {
        public string Group { get; set; } = "public";
        public VersionCode VersionCode { get; set; } = VersionCode.V1;
        public string StartObjectId { get; set; } = "1.3.6.1.2.1.1";
        public string IpAddress { get; set; } = "127.0.0.1";
        public int Port { get; set; } = 161;
        public IEnumerable<OID> OIDs { get; set; } = new List<OID>();
        public string Error { get; set; }
    }
}