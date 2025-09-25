using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace API.Migrations
{
    public partial class DtoFixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "HashedPassword",
                value: "$2a$11$C2sHsoVgVdP2rzn93K9c2O8u9i4cVtFjYJya0w1PKgJjLgM9bIr96");

            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS ""Bookings""
                  ALTER COLUMN ""CheckIn""  TYPE timestamptz USING (""CheckIn""  AT TIME ZONE 'UTC'),
                  ALTER COLUMN ""CheckOut"" TYPE timestamptz USING (""CheckOut"" AT TIME ZONE 'UTC');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS ""Rooms""
                  ALTER COLUMN ""CreatedAt"" TYPE timestamptz USING (""CreatedAt"" AT TIME ZONE 'UTC'),
                  ALTER COLUMN ""UpdatedAt"" TYPE timestamptz USING (""UpdatedAt"" AT TIME ZONE 'UTC');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS ""Roles""
                  ALTER COLUMN ""CreatedAt"" TYPE timestamptz USING (""CreatedAt"" AT TIME ZONE 'UTC'),
                  ALTER COLUMN ""UpdatedAt"" TYPE timestamptz USING (""UpdatedAt"" AT TIME ZONE 'UTC');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS ""Users""
                  ALTER COLUMN ""CreatedAt"" TYPE timestamptz USING (""CreatedAt"" AT TIME ZONE 'UTC'),
                  ALTER COLUMN ""UpdatedAt"" TYPE timestamptz USING (""UpdatedAt"" AT TIME ZONE 'UTC'),
                  ALTER COLUMN ""LastLogin"" TYPE timestamptz USING (""LastLogin"" AT TIME ZONE 'UTC');
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "HashedPassword",
                value: "$2a$11$VkHJzIzfLRlflsxWdFKsTOpDfGUrvwG5gmbvcFw7WxnYGuSp19RNC");

            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS ""Bookings""
                  ALTER COLUMN ""CheckIn""  TYPE timestamp without time zone USING (""CheckIn""  AT TIME ZONE 'UTC'),
                  ALTER COLUMN ""CheckOut"" TYPE timestamp without time zone USING (""CheckOut"" AT TIME ZONE 'UTC');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS ""Rooms""
                  ALTER COLUMN ""CreatedAt"" TYPE timestamp without time zone USING (""CreatedAt"" AT TIME ZONE 'UTC'),
                  ALTER COLUMN ""UpdatedAt"" TYPE timestamp without time zone USING (""UpdatedAt"" AT TIME ZONE 'UTC');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS ""Roles""
                  ALTER COLUMN ""CreatedAt"" TYPE timestamp without time zone USING (""CreatedAt"" AT TIME ZONE 'UTC'),
                  ALTER COLUMN ""UpdatedAt"" TYPE timestamp without time zone USING (""UpdatedAt"" AT TIME ZONE 'UTC');
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE IF EXISTS ""Users""
                  ALTER COLUMN ""CreatedAt"" TYPE timestamp without time zone USING (""CreatedAt"" AT TIME ZONE 'UTC'),
                  ALTER COLUMN ""UpdatedAt"" TYPE timestamp without time zone USING (""UpdatedAt"" AT TIME ZONE 'UTC'),
                  ALTER COLUMN ""LastLogin"" TYPE timestamp without time zone USING (""LastLogin"" AT TIME ZONE 'UTC');
            ");
        }
    }
}
