namespace BlockchainExample.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class recai : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Wallet",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PublicKey = c.String(),
                        PrivateKey = c.String(),
                        TransactionId = c.String(),
                        TransactionUrl = c.String(),
                        Balance = c.Decimal(nullable: false, precision: 20, scale: 8),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Wallet");
        }
    }
}
