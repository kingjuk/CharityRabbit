# Social Media Image Optimization Guide

## Current Images

Your workspace contains the following logo files:
- `wwwroot/images/charityrabbit.jpg` - Original JPG logo
- `wwwroot/images/charityrabbit.png` - PNG version
- `wwwroot/images/charityrabbitwhite.png` - White/transparent version

## Recommended Social Media Image Sizes

### 1. Open Graph (Facebook, LinkedIn)
**Optimal Size:** 1200 x 630 pixels
- **Current:** Using `charityrabbit.png`
- **Format:** PNG or JPG
- **Max File Size:** 8 MB (aim for < 300 KB)
- **Aspect Ratio:** 1.91:1

### 2. Twitter Card
**Optimal Size:** 1200 x 628 pixels (or 800 x 418 minimum)
- **Current:** Using `charityrabbit.png`
- **Format:** PNG, JPG, or WEBP
- **Max File Size:** 5 MB
- **Aspect Ratio:** 1.91:1 or 2:1

### 3. LinkedIn
**Optimal Size:** 1200 x 627 pixels
- Same as Open Graph standard

## Creating an Optimized Social Share Image

### Option 1: Simple Logo with Background
Create a 1200 x 630 image with:
- Background color (brand color or gradient)
- CharityRabbit logo centered
- Tagline: "Find & Create Volunteer Opportunities"
- Include the ?? carrot emoji or icon

### Option 2: Feature Showcase
Create a 1200 x 630 image showing:
- Split design with logo on left
- Key features on right:
  - "Find Local Volunteer Opportunities"
  - "Track Your Impact"
  - "Connect with Your Community"
- CharityRabbit branding

### Option 3: Hero Image
Create a 1200 x 630 image with:
- Background image of volunteers/community
- CharityRabbit logo overlay
- Text: "Make a Difference in Your Community"
- Call-to-action

## Tools for Creating Social Images

### Free Online Tools:
1. **Canva** (https://canva.com)
   - Template: "Facebook Post" (1200 x 630)
   - Free templates available

2. **Adobe Express** (https://express.adobe.com)
   - Social media templates
   - Free tier available

3. **Figma** (https://figma.com)
   - Professional design tool
   - Free for personal use

### Image Optimization Tools:
1. **TinyPNG** (https://tinypng.com)
   - Compress PNG files
   - Maintain quality

2. **Squoosh** (https://squoosh.app)
   - Google's image optimizer
   - Compare formats

3. **ImageOptim** (Mac) or **FileOptimizer** (Windows)
   - Desktop optimization tools

## Implementation Steps

### Step 1: Create the Image
1. Design a 1200 x 630 px image
2. Use your CharityRabbit branding
3. Include clear, readable text
4. Test on various backgrounds (light/dark)

### Step 2: Optimize the Image
1. Export as PNG or high-quality JPG
2. Compress to under 300 KB if possible
3. Ensure text is still readable after compression

### Step 3: Add to Project
```bash
# Save as:
wwwroot/images/og-image.png

# Or create event-specific images:
wwwroot/images/og-image-home.png
wwwroot/images/og-image-event.png
```

### Step 4: Update References
The code has been updated to use `/images/charityrabbit.png` by default.

To use a custom Open Graph image:
```csharp
// In SeoService, pass custom image URL:
var ogTags = _seoService.GetOpenGraphTags(
    "Page Title",
    "Description",
    imageUrl: "https://charityrabbit.com/images/og-image.png"
);
```

## Current Implementation

### Files Updated:
1. **Data/SeoService.cs**
   - Default image: `/images/charityrabbit.png`
   - Added `og:image:alt`, `og:image:width`, `og:image:height`
   - Added `twitter:image:alt`

2. **Components/Pages/Home.razor**
   - Explicit Open Graph tags
   - Image URL: `https://charityrabbit.com/images/charityrabbit.png`
   - Proper dimensions specified

3. **Components/Pages/GoodWorkDetails.razor**
   - Event-specific Open Graph tags
   - Uses default image (can be customized per event)

## Testing Your Social Images

### Test Tools:
1. **Facebook Sharing Debugger**
   ```
   https://developers.facebook.com/tools/debug/
   ```
   - Enter: https://charityrabbit.com
   - Click "Scrape Again" to refresh

2. **Twitter Card Validator**
   ```
   https://cards-dev.twitter.com/validator
   ```
   - Enter: https://charityrabbit.com
   - Preview how it looks

3. **LinkedIn Post Inspector**
   ```
   https://www.linkedin.com/post-inspector/
   ```
   - Enter: https://charityrabbit.com
   - Check preview

4. **Meta Tags Checker**
   ```
   https://metatags.io/
   ```
   - Visual preview of all social platforms

## Best Practices

### Design Guidelines:
1. **Keep it Simple**
   - Clear, bold text
   - High contrast
   - Minimal clutter

2. **Branding**
   - Include logo
   - Use brand colors
   - Consistent typography

3. **Mobile Preview**
   - Text remains readable when small
   - Important elements stay visible

4. **Safe Zones**
   - Keep critical content in center 1000 x 500 area
   - Some platforms crop edges

### Content Guidelines:
1. **Text Overlay**
   - Max 20% text (Facebook rule)
   - Large, readable fonts (min 40-60 px)
   - High contrast with background

2. **Call to Action**
   - "Find Volunteer Opportunities"
   - "Make a Difference Today"
   - "Join CharityRabbit"

3. **Visual Hierarchy**
   - Logo/brand first
   - Main message second
   - Supporting text last

## Quick Win: Use Current Logo

Your current `charityrabbit.png` can work as-is if:
- It's already 1200 x 630 pixels
- Background contrasts well with white
- Logo is clearly visible when small

To check dimensions:
```bash
# On Windows (PowerShell):
Get-ItemProperty wwwroot/images/charityrabbit.png | Select-Object -ExpandProperty Dimensions

# Or right-click file > Properties > Details
```

## Future Enhancements

### Dynamic Images:
Consider generating event-specific images:
```csharp
// Create service to generate images with:
- Event name
- Date/time
- Location
- CharityRabbit branding
```

### Tools for Dynamic Images:
- **ImageSharp** (C# library)
- **Puppeteer Sharp** (screenshot HTML)
- **Third-party APIs** (Cloudinary, Imgix)

## Checklist

- [x] Update SeoService to use charityrabbit.png
- [x] Add proper Open Graph metadata
- [x] Add Twitter Card metadata
- [x] Include image dimensions
- [x] Add alt text for accessibility
- [ ] Create optimized 1200 x 630 social image
- [ ] Test on Facebook Debugger
- [ ] Test on Twitter Card Validator
- [ ] Test on LinkedIn Inspector
- [ ] Optimize file size (< 300 KB)

## Resources

- [Facebook Sharing Best Practices](https://developers.facebook.com/docs/sharing/webmasters)
- [Twitter Card Documentation](https://developer.twitter.com/en/docs/twitter-for-websites/cards/overview/abouts-cards)
- [Open Graph Protocol](https://ogp.me/)
- [Essential Meta Tags](https://css-tricks.com/essential-meta-tags-social-media/)

---

**Current Status:** ? Code is ready. Next step is creating the optimized 1200 x 630 image.
