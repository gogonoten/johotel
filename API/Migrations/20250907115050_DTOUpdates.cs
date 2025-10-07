using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    public partial class DTOUpdates : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // If an older single-column index on Bookings(RoomId) exists, drop it safely (Postgres).
            migrationBuilder.Sql(@"DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM pg_indexes WHERE schemaname = 'public' AND indexname = 'IX_Bookings_RoomId') THEN
        EXECUTE 'DROP INDEX public.""IX_Bookings_RoomId""';
    END IF;
END$$;");

            // Repartition existing rows: 301–360 → Family, 361–400 → Suite.
            migrationBuilder.Sql(@"UPDATE ""Rooms"" SET ""Type"" = 'Family' WHERE ""RoomNumber"" BETWEEN 301 AND 360;");
            migrationBuilder.Sql(@"UPDATE ""Rooms"" SET ""Type"" = 'Suite'  WHERE ""RoomNumber"" BETWEEN 361 AND 400;");

            // Unique index on RoomNumber (prevents duplicates).
            migrationBuilder.CreateIndex(
                name: "IX_Rooms_RoomNumber",
                table: "Rooms",
                column: "RoomNumber",
                unique: true);

            // Composite index for booking overlap lookups.
            migrationBuilder.CreateIndex(
                name: "IX_Bookings_RoomId_CheckIn_CheckOut",
                table: "Bookings",
                columns: new[] { "RoomId", "CheckIn", "CheckOut" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Optionally revert repartition: set all 301–400 back to Standard.
            migrationBuilder.Sql(@"UPDATE ""Rooms"" SET ""Type"" = 'Standard' WHERE ""RoomNumber"" BETWEEN 301 AND 400;");

            // Drop the indexes created in Up.
            migrationBuilder.DropIndex(
                name: "IX_Rooms_RoomNumber",
                table: "Rooms");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_RoomId_CheckIn_CheckOut",
                table: "Bookings");

            // Restore the old RoomId-only index if you previously had it (optional).
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Bookings_RoomId"" ON ""Bookings"" (""RoomId"");");
        }
    }
}
