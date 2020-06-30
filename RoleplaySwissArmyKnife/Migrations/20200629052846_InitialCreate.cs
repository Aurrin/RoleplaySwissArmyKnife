using Microsoft.EntityFrameworkCore.Migrations;

namespace RoleplaySwissArmyKnife.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ControlChannels",
                columns: table => new
                {
                    ControlChannelID = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ResultChannelID = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ControlChannels", x => x.ControlChannelID);
                });

            migrationBuilder.CreateTable(
                name: "InitiativeStates",
                columns: table => new
                {
                    InitiativeStateID = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ChannelID = table.Column<ulong>(nullable: false),
                    PinnedListMessageID = table.Column<ulong>(nullable: false),
                    LastAnnounceMessageID = table.Column<ulong>(nullable: false),
                    CurrentInitiative = table.Column<double>(nullable: false),
                    InInitiative = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiativeStates", x => x.InitiativeStateID);
                });

            migrationBuilder.CreateTable(
                name: "ServerSettings",
                columns: table => new
                {
                    ServerID = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommandPrefix = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServerSettings", x => x.ServerID);
                });

            migrationBuilder.CreateTable(
                name: "InitiativeEntries",
                columns: table => new
                {
                    InitiativeEntryID = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlayerID = table.Column<ulong>(nullable: false),
                    DisplayName = table.Column<string>(nullable: true),
                    Initiative = table.Column<double>(nullable: false),
                    InitiativeStateID = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiativeEntries", x => x.InitiativeEntryID);
                    table.ForeignKey(
                        name: "FK_InitiativeEntries_InitiativeStates_InitiativeStateID",
                        column: x => x.InitiativeStateID,
                        principalTable: "InitiativeStates",
                        principalColumn: "InitiativeStateID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeEntries_InitiativeStateID",
                table: "InitiativeEntries",
                column: "InitiativeStateID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ControlChannels");

            migrationBuilder.DropTable(
                name: "InitiativeEntries");

            migrationBuilder.DropTable(
                name: "ServerSettings");

            migrationBuilder.DropTable(
                name: "InitiativeStates");
        }
    }
}
