namespace AuntieDot.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Orders : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.DbOrders",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ShopifyId = c.Long(nullable: false),
                        DisplayId = c.String(),
                        LineItemSummary = c.String(),
                        CustomerName = c.String(),
                        DateCreated = c.DateTimeOffset(nullable: false, precision: 7),
                        IsOpen = c.Boolean(nullable: false),
                        ApplicationUser_Id = c.String(maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.AspNetUsers", t => t.ApplicationUser_Id)
                .Index(t => t.ApplicationUser_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.DbOrders", "ApplicationUser_Id", "dbo.AspNetUsers");
            DropIndex("dbo.DbOrders", new[] { "ApplicationUser_Id" });
            DropTable("dbo.DbOrders");
        }
    }
}
