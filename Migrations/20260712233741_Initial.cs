using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CharityRabbit.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contacts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    City = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    Zip = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Mission = table.Column<string>(type: "text", nullable: true),
                    Vision = table.Column<string>(type: "text", nullable: true),
                    ContactEmail = table.Column<string>(type: "text", nullable: false),
                    ContactPhone = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    City = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    Country = table.Column<string>(type: "text", nullable: true),
                    ZipCode = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    OrganizationType = table.Column<string>(type: "text", nullable: true),
                    TaxId = table.Column<string>(type: "text", nullable: true),
                    FoundedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LogoUrl = table.Column<string>(type: "text", nullable: true),
                    CoverImageUrl = table.Column<string>(type: "text", nullable: true),
                    FacebookUrl = table.Column<string>(type: "text", nullable: true),
                    TwitterUrl = table.Column<string>(type: "text", nullable: true),
                    InstagramUrl = table.Column<string>(type: "text", nullable: true),
                    LinkedInUrl = table.Column<string>(type: "text", nullable: true),
                    FocusAreas = table.Column<List<string>>(type: "text[]", nullable: false),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastModifiedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "skills",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_friends",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FriendUserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_friends", x => new { x.UserId, x.FriendUserId });
                });

            migrationBuilder.CreateTable(
                name: "user_skills",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    SkillId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_skills", x => new { x.UserId, x.SkillId });
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Phone = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "good_works",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DetailedDescription = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: true),
                    SubCategory = table.Column<string>(type: "text", nullable: true),
                    Address = table.Column<string>(type: "text", nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EstimatedDurationMinutes = table.Column<double>(type: "double precision", nullable: true),
                    EffortLevel = table.Column<string>(type: "text", nullable: true),
                    IsAccessible = table.Column<bool>(type: "boolean", nullable: false),
                    IsVirtual = table.Column<bool>(type: "boolean", nullable: false),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: true),
                    CurrentParticipants = table.Column<int>(type: "integer", nullable: false),
                    MinimumAge = table.Column<int>(type: "integer", nullable: true),
                    FamilyFriendly = table.Column<bool>(type: "boolean", nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    RecurrencePattern = table.Column<string>(type: "text", nullable: true),
                    OrganizationName = table.Column<string>(type: "text", nullable: true),
                    OrganizationWebsite = table.Column<string>(type: "text", nullable: true),
                    ParkingAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    PublicTransitAccessible = table.Column<bool>(type: "boolean", nullable: false),
                    SpecialInstructions = table.Column<string>(type: "text", nullable: true),
                    ImpactDescription = table.Column<string>(type: "text", nullable: true),
                    EstimatedPeopleHelped = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    OutdoorActivity = table.Column<bool>(type: "boolean", nullable: false),
                    WeatherDependent = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    ContactId = table.Column<long>(type: "bigint", nullable: true),
                    LocationId = table.Column<long>(type: "bigint", nullable: true),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_good_works", x => x.Id);
                    table.ForeignKey(
                        name: "FK_good_works_contacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "contacts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_good_works_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "locations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_good_works_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "organization_admins",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    Since = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_admins", x => new { x.UserId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_organization_admins_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organization_admins_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "organization_members",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    JoinedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organization_members", x => new { x.UserId, x.OrganizationId });
                    table.ForeignKey(
                        name: "FK_organization_members_organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_organization_members_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "good_work_skills",
                columns: table => new
                {
                    GoodWorkId = table.Column<long>(type: "bigint", nullable: false),
                    SkillsId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_good_work_skills", x => new { x.GoodWorkId, x.SkillsId });
                    table.ForeignKey(
                        name: "FK_good_work_skills_good_works_GoodWorkId",
                        column: x => x.GoodWorkId,
                        principalTable: "good_works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_good_work_skills_skills_SkillsId",
                        column: x => x.SkillsId,
                        principalTable: "skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "good_work_tags",
                columns: table => new
                {
                    GoodWorkId = table.Column<long>(type: "bigint", nullable: false),
                    TagsId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_good_work_tags", x => new { x.GoodWorkId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_good_work_tags_good_works_GoodWorkId",
                        column: x => x.GoodWorkId,
                        principalTable: "good_works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_good_work_tags_tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_interested",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    GoodWorkId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_interested", x => new { x.UserId, x.GoodWorkId });
                    table.ForeignKey(
                        name: "FK_user_interested_good_works_GoodWorkId",
                        column: x => x.GoodWorkId,
                        principalTable: "good_works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_interested_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_signups",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    GoodWorkId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_signups", x => new { x.UserId, x.GoodWorkId });
                    table.ForeignKey(
                        name: "FK_user_signups_good_works_GoodWorkId",
                        column: x => x.GoodWorkId,
                        principalTable: "good_works",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_signups_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contacts_Email",
                table: "contacts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_good_work_skills_SkillsId",
                table: "good_work_skills",
                column: "SkillsId");

            migrationBuilder.CreateIndex(
                name: "IX_good_work_tags_TagsId",
                table: "good_work_tags",
                column: "TagsId");

            migrationBuilder.CreateIndex(
                name: "IX_good_works_ContactId",
                table: "good_works",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_good_works_CreatedBy",
                table: "good_works",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_good_works_LocationId",
                table: "good_works",
                column: "LocationId");

            migrationBuilder.CreateIndex(
                name: "IX_good_works_OrganizationId",
                table: "good_works",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_good_works_StartTime",
                table: "good_works",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_good_works_Status",
                table: "good_works",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_locations_City_State_Country_Zip",
                table: "locations",
                columns: new[] { "City", "State", "Country", "Zip" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organization_admins_OrganizationId",
                table: "organization_admins",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_organization_members_OrganizationId",
                table: "organization_members",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Slug",
                table: "organizations",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organizations_Status",
                table: "organizations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_skills_Name",
                table: "skills",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_tags_Name",
                table: "tags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_interested_GoodWorkId",
                table: "user_interested",
                column: "GoodWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_user_signups_GoodWorkId",
                table: "user_signups",
                column: "GoodWorkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "good_work_skills");

            migrationBuilder.DropTable(
                name: "good_work_tags");

            migrationBuilder.DropTable(
                name: "organization_admins");

            migrationBuilder.DropTable(
                name: "organization_members");

            migrationBuilder.DropTable(
                name: "user_friends");

            migrationBuilder.DropTable(
                name: "user_interested");

            migrationBuilder.DropTable(
                name: "user_signups");

            migrationBuilder.DropTable(
                name: "user_skills");

            migrationBuilder.DropTable(
                name: "skills");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "good_works");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "contacts");

            migrationBuilder.DropTable(
                name: "locations");

            migrationBuilder.DropTable(
                name: "organizations");
        }
    }
}
