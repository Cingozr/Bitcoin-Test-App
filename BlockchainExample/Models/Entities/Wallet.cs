using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace BlockchainExample.Models
{
    [Table("Wallet")]
    public class Wallet : EntityBase
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }
        public string TransactionId { get; set; }
        public string TransactionUrl { get; set; }
        public decimal Balance { get; set; }
    }
}