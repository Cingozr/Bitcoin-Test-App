using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace BlockchainExample.Models
{
    public class BlockChainAPP : DbContext
    {
        public BlockChainAPP() : base("BlockChainAPP") { }

        public DbSet<Wallet> Wallet { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Wallet>().Property(t => t.Balance).HasPrecision(20, 8);
        }
    }
}