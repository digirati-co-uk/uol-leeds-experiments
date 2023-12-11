﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fedora.ApiModel
{
    public class Transaction
    {
        public Uri Location { get; set; }
        public DateTime Expires { get; set; }
        public bool Expired { get; set; }
        public bool Committed { get; set; }

        public const string HeaderName = "Atomic-ID";
    }
}
