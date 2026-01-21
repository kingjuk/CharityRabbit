using CharityRabbit.Models;
using System.Text;
using System.Text.Json;

namespace CharityRabbit.Data;

public class SeoService
{
    private readonly string _baseUrl = "https://charityrabbit.com";

    public string GetPageTitle(string? pageTitle = null)
    {
        return string.IsNullOrEmpty(pageTitle) 
            ? "CharityRabbit - Find & Create Volunteer Opportunities" 
            : $"{pageTitle} - CharityRabbit";
    }

    public string GetPageDescription(string? pageDescription = null)
    {
        return pageDescription ?? 
            "Discover local volunteer opportunities, create community service events, and track your impact. Join CharityRabbit to make a difference in your community.";
    }

    public string GetCanonicalUrl(string path)
    {
        return $"{_baseUrl}{path}";
    }

    /// <summary>
    /// Generates JSON-LD structured data for Organization
    /// </summary>
    public string GetOrganizationSchema()
    {
        var schema = new
        {
            context = "https://schema.org",
            type = "Organization",
            name = "CharityRabbit",
            url = _baseUrl,
            logo = $"{_baseUrl}/images/logo.png",
            description = "Platform for finding and creating volunteer opportunities",
            sameAs = new[]
            {
                "https://github.com/kingjuk/CharityRabbit"
            },
            contactPoint = new
            {
                type = "ContactPoint",
                contactType = "Support"
            }
        };

        return JsonSerializer.Serialize(schema, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Generates JSON-LD structured data for Event
    /// </summary>
    public string GetEventSchema(GoodWorksModel goodWork)
    {
        object location;
        if (goodWork.IsVirtual)
        {
            location = new { type = "VirtualLocation", url = _baseUrl };
        }
        else
        {
            location = new 
            { 
                type = "Place", 
                name = goodWork.Address ?? string.Empty,
                address = new
                {
                    type = "PostalAddress",
                    addressLocality = goodWork.Address ?? string.Empty
                }
            };
        }

        var schema = new
        {
            context = "https://schema.org",
            type = "Event",
            name = goodWork.Name,
            description = goodWork.Description,
            startDate = goodWork.StartTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
            endDate = goodWork.EndTime?.ToString("yyyy-MM-ddTHH:mm:ss"),
            eventStatus = goodWork.Status == "Active" ? "https://schema.org/EventScheduled" : "https://schema.org/EventCancelled",
            eventAttendanceMode = goodWork.IsVirtual ? "https://schema.org/OnlineEventAttendanceMode" : "https://schema.org/OfflineEventAttendanceMode",
            location = location,
            organizer = new
            {
                type = "Organization",
                name = goodWork.OrganizationName ?? "CharityRabbit",
                url = _baseUrl
            },
            offers = new
            {
                type = "Offer",
                price = "0",
                priceCurrency = "USD",
                availability = goodWork.MaxParticipants.HasValue && goodWork.SignedUpCount >= goodWork.MaxParticipants.Value
                    ? "https://schema.org/SoldOut"
                    : "https://schema.org/InStock"
            }
        };

        return JsonSerializer.Serialize(schema, new JsonSerializerOptions 
        { 
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
    }

    /// <summary>
    /// Generates sitemap XML
    /// </summary>
    public async Task<string> GenerateSitemapXml(List<GoodWorksModel> goodWorks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // Homepage
        AddUrl(sb, "/", priority: "1.0", changefreq: "daily");

        // Static pages
        AddUrl(sb, "/search", priority: "0.9", changefreq: "daily");
        AddUrl(sb, "/profile", priority: "0.7", changefreq: "weekly");
        AddUrl(sb, "/add-good-work", priority: "0.8", changefreq: "monthly");

        // Good works
        foreach (var work in goodWorks.Where(w => w.Status == "Active" && w.Id.HasValue))
        {
            var lastmod = work.LastModifiedDate?.ToString("yyyy-MM-dd") ?? work.CreatedDate.ToString("yyyy-MM-dd");
            AddUrl(sb, $"/goodwork/{work.Id}", priority: "0.8", changefreq: "weekly", lastmod: lastmod);
        }

        sb.AppendLine("</urlset>");
        return sb.ToString();
    }

    private void AddUrl(StringBuilder sb, string path, string priority, string changefreq, string? lastmod = null)
    {
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{_baseUrl}{path}</loc>");
        if (!string.IsNullOrEmpty(lastmod))
        {
            sb.AppendLine($"    <lastmod>{lastmod}</lastmod>");
        }
        sb.AppendLine($"    <changefreq>{changefreq}</changefreq>");
        sb.AppendLine($"    <priority>{priority}</priority>");
        sb.AppendLine("  </url>");
    }

    /// <summary>
    /// Gets Open Graph meta tags
    /// </summary>
    public Dictionary<string, string> GetOpenGraphTags(string title, string description, string? imageUrl = null, string? path = "/")
    {
        return new Dictionary<string, string>
        {
            { "og:title", title },
            { "og:description", description },
            { "og:type", "website" },
            { "og:url", GetCanonicalUrl(path) },
            { "og:site_name", "CharityRabbit" },
            { "og:image", imageUrl ?? $"{_baseUrl}/images/og-image.png" }
        };
    }

    /// <summary>
    /// Gets Twitter Card meta tags
    /// </summary>
    public Dictionary<string, string> GetTwitterCardTags(string title, string description, string? imageUrl = null)
    {
        return new Dictionary<string, string>
        {
            { "twitter:card", "summary_large_image" },
            { "twitter:title", title },
            { "twitter:description", description },
            { "twitter:image", imageUrl ?? $"{_baseUrl}/images/og-image.png" }
        };
    }
}
