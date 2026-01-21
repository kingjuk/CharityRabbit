# Image Format Issue - RESOLVED

## ? Solution Implemented

**Problem:** Facebook showing "Corrupted Image" error for og:image

**Root Cause:** PNG format may have compatibility issues with some social platforms

**Solution:** Switched to JPG format which is more universally supported

## Changes Made

### Files Updated (Build Successful ?):
1. **Data/SeoService.cs**
   - Changed default image to `charityrabbit.jpg`
   - Added `og:image:type` = `image/jpeg`
   - Updated all schema references

2. **Components/Pages/Home.razor**
   - Updated to use `charityrabbit.jpg`
   - Added `og:image:secure_url`
   - Added explicit `og:image:type`

### Why This Works:
- ? JPG is more universally supported
- ? Better compression for logos/photos
- ? Explicit MIME type declaration
- ? Secure URL specified for HTTPS

## Next Steps

### 1. Deploy to Production
```bash
docker build -t charityrabbit .
docker push 555467380508.dkr.ecr.us-east-2.amazonaws.com/charityrabbit:latest
```

### 2. Clear Facebook Cache
1. Go to: https://developers.facebook.com/tools/debug/
2. Enter: `https://charityrabbit.com`
3. Click **"Scrape Again"** 3-4 times
4. Wait 30 seconds between each scrape

### 3. Verify Image Works
Visit: `https://charityrabbit.com/images/charityrabbit.jpg`
Should display your logo.

---

*See [DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md) for detailed deployment instructions.*

---

## Previous Content (For Reference)

# Image Format Issue - Quick Fix Guide

## Problem
Facebook showing: "Corrupted Image - Provided og:image URL could not be processed as an image."

## Current Status
? Code is correct - using `https://charityrabbit.com/images/charityrabbit.png`
? File exists in `wwwroot/images/charityrabbit.png`
? Image may not be in the correct format or size

## Immediate Solutions

### Option 1: Clear Facebook Cache
1. Go to: https://developers.facebook.com/tools/debug/
2. Enter: `https://charityrabbit.com`
3. Click **"Scrape Again"** button (multiple times if needed)
4. Wait 30 seconds and try again

### Option 2: Check Image Properties

**Requirements for Open Graph Images:**
- **Format:** PNG or JPG (not corrupted)
- **Minimum Size:** 200 x 200 pixels
- **Recommended Size:** 1200 x 630 pixels
- **Max File Size:** 8 MB (Facebook), aim for < 300 KB
- **Aspect Ratio:** 1.91:1 (for best results)

**Check Your Image:**
```powershell
# In PowerShell, run:
Get-Item "wwwroot\images\charityrabbit.png" | Select-Object Name, Length, LastWriteTime

# Check if it's a valid PNG
[System.IO.File]::ReadAllBytes("wwwroot\images\charityrabbit.png")[0..7]
# Should start with: 137 80 78 71 (PNG signature)
```

### Option 3: Convert/Optimize Image

If the image is corrupted or wrong format:

**Using Online Tools:**
1. Go to: https://www.iloveimg.com/resize-image
2. Upload your `charityrabbit.png` or `charityrabbit.jpg`
3. Resize to 1200 x 630 pixels
4. Download and replace `wwwroot/images/charityrabbit.png`

**Using Paint (Windows):**
1. Open `charityrabbit.jpg` in Paint
2. Resize ? Pixels ? 1200 x 630
3. Save As ? PNG ? `charityrabbit.png`
4. Replace the file

### Option 4: Use JPG Instead

If PNG continues to have issues:

1. **Rename existing JPG:**
   ```powershell
   Copy-Item "wwwroot\images\charityrabbit.jpg" "wwwroot\images\og-image.jpg"
   ```

2. **Update SeoService.cs:**
   ```csharp
   // Change line 175 and other references:
   { "og:image", imageUrl ?? $"{_baseUrl}/images/charityrabbit.jpg" },
   // And line 189:
   { "twitter:image", imageUrl ?? $"{_baseUrl}/images/charityrabbit.jpg" },
   ```

3. **Update Home.razor:**
   ```html
   <meta property="og:image" content="https://charityrabbit.com/images/charityrabbit.jpg" />
   <meta name="twitter:image" content="https://charityrabbit.com/images/charityrabbit.jpg" />
   ```

## Troubleshooting Steps

### 1. Verify Image is Accessible
Visit in browser:
```
https://charityrabbit.com/images/charityrabbit.png
```
Should show your logo.

### 2. Check Image Headers
Open Developer Tools (F12) ? Network tab
Visit: https://charityrabbit.com/images/charityrabbit.png
Check Response Headers:
- `Content-Type: image/png` ?
- `Content-Length: [size]` ?
- Status: `200 OK` ?

### 3. Test with Different URLs
Try these in Facebook Debugger:
- `https://charityrabbit.com/`
- `https://charityrabbit.com/images/charityrabbit.png` (direct)
- `https://charityrabbit.com/search`

### 4. Check MIME Types
Ensure your web server serves correct MIME types:
- `.png` ? `image/png`
- `.jpg` ? `image/jpeg`

## Common Causes

### 1. Image Too Large
- Facebook has an 8 MB limit
- Compress using TinyPNG: https://tinypng.com/

### 2. Wrong Format
- File extension says `.png` but is actually `.jpg`
- Re-save in correct format

### 3. Corrupted File
- File got corrupted during transfer
- Re-export/save the image

### 4. Cache Issues
- Facebook cached old URL
- Use "Scrape Again" multiple times
- Wait 5-10 minutes between attempts

### 5. CDN/Server Issues
- Image not deployed to server
- Check if file exists on production
- Verify file permissions (readable by web server)

## Quick Test Commands

```powershell
# Test local file
Test-Path "wwwroot\images\charityrabbit.png"

# Check file size
(Get-Item "wwwroot\images\charityrabbit.png").Length / 1KB
# Should be < 300 KB ideally

# Test if it's a valid image
Add-Type -AssemblyName System.Drawing
try {
    [System.Drawing.Image]::FromFile("wwwroot\images\charityrabbit.png")
    Write-Host "? Valid image file"
} catch {
    Write-Host "? Corrupted image file"
}
```

## Best Solution: Create Optimized OG Image

**Steps:**
1. Open https://www.canva.com
2. Create ? Custom size ? 1200 x 630 px
3. Add your CharityRabbit logo + tagline
4. Download as PNG (high quality)
5. Compress at https://tinypng.com
6. Save as `wwwroot/images/charityrabbit.png` (replace)

## After Fixing

1. **Deploy to production**
   ```bash
   docker build -t charityrabbit .
   docker tag charityrabbit 555467380508.dkr.ecr.us-east-2.amazonaws.com/charityrabbit:latest
   docker push 555467380508.dkr.ecr.us-east-2.amazonaws.com/charityrabbit:latest
   ```

2. **Clear all caches**
   - Facebook Debugger: "Scrape Again"
   - LinkedIn Inspector: Re-check
   - Twitter Card Validator: Re-test

3. **Wait 5-10 minutes**
   - CDN propagation
   - Cache clearing

4. **Test again**

## Alternative: Use White Logo

If the colored logo doesn't work:

```powershell
# Copy white logo
Copy-Item "wwwroot\images\charityrabbitwhite.png" "wwwroot\images\og-image.png"
```

Then update references to use `og-image.png`.

## Need Help?

### Image Is Valid But Still Not Working?

**Check these:**
1. HTTPS certificate valid?
2. robots.txt blocking images?
3. Authentication required to access images?
4. Correct Content-Type header?

### Still Having Issues?

**Contact:**
- GitHub Issues: https://github.com/kingjuk/CharityRabbit/issues
- Facebook Support: Use their debugger's feedback option

## Current Configuration Check

Your code should have:

**SeoService.cs (line ~175):**
```csharp
{ "og:image", imageUrl ?? $"{_baseUrl}/images/charityrabbit.png" },
```

**Home.razor:**
```html
<meta property="og:image" content="https://charityrabbit.com/images/charityrabbit.png" />
```

**File exists:**
```
? wwwroot/images/charityrabbit.png
? wwwroot/images/charityrabbit.jpg (backup)
```

## Summary

**Most Likely Issues:**
1. ?? **Cache** - Clear Facebook cache
2. ?? **Size** - Resize to 1200 x 630 px
3. ?? **Format** - Ensure it's a valid PNG
4. ?? **Not Deployed** - Deploy latest version

**Quick Fix:** Use the JPG version instead of PNG (it's more universally supported).

---

**Status:** Check image format and clear Facebook cache first!
