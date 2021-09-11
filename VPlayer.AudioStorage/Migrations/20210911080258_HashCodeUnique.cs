using Microsoft.EntityFrameworkCore.Migrations;

namespace VPlayer.AudioStorage.Migrations
{
    public partial class HashCodeUnique : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
         
            migrationBuilder.CreateIndex(
                name: "IX_TvShowPlaylists_HashCode_IsUserCreated",
                table: "TvShowPlaylists",
                columns: new[] { "HashCode", "IsUserCreated" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SongPlaylists_HashCode_IsUserCreated",
                table: "SongPlaylists",
                columns: new[] { "HashCode", "IsUserCreated" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TvShowPlaylists_HashCode_IsUserCreated",
                table: "TvShowPlaylists");

            migrationBuilder.DropIndex(
                name: "IX_SongPlaylists_HashCode_IsUserCreated",
                table: "SongPlaylists");

        
        }
    }
}
