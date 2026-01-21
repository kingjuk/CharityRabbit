# ?? OG Image Fix - Deployment Checklist

## ? Changes Made

### Files Updated:
1. **Data/SeoService.cs**
   - ? Changed from `charityrabbit.png` to `charityrabbit.jpg`
   - ? Added `og:image:type` = `image/jpeg`
   - ? Updated Organization schema
   - ? Updated Event schema

2. **Components/Pages/Home.razor**
   - ? Changed to `charityrabbit.jpg`
   - ? Added `og:image:secure_url`
   - ? Added `og:image:type`
   - ? Explicit dimensions (1200 x 630)

### Why JPG?
- ? More universally supported than PNG
- ? Better compression for photos/logos
- ? Your JPG file exists and is valid
- ? Fewer format interpretation issues

## ?? Deployment Steps

### 1. Deploy to Production
```bash
# Login to AWS ECR
aws ecr get-login-password --region us-east-2 --profile personal | docker login --username AWS --password-stdin 555467380508.dkr.ecr.us-east-2.amazonaws.com

# Build Docker image
docker build -t charityrabbit .

# Tag with new timestamp
docker tag charityrabbit 555467380508.dkr.ecr.us-east-2.amazonaws.com/charityrabbit:$(Get-Date -Format FileDateTime)

# Push to ECR
docker push 555467380508.dkr.ecr.us-east-2.amazonaws.com/charityrabbit:latest
```

### 2. Verify Image is Accessible
**After deployment, test:**
```
https://charityrabbit.com/images/charityrabbit.jpg
```
Should display your logo.

### 3. Clear All Caches

#### Facebook
1. Go to: https://developers.facebook.com/tools/debug/
2. Enter: `https://charityrabbit.com`
3. Click **"Scrape Again"** 3-4 times
4. Wait 30-60 seconds between scrapes
5. Verify image appears in preview

#### Twitter
1. Go to: https://cards-dev.twitter.com/validator
2. Enter: `https://charityrabbit.com`
3. Preview card should show image

#### LinkedIn
1. Go to: https://www.linkedin.com/post-inspector/
2. Enter: `https://charityrabbit.com`
3. Check preview

### 4. Test Multiple URLs
Test these URLs with Facebook Debugger:
- ? `https://charityrabbit.com/`
- ? `https://charityrabbit.com/search`
- ? `https://charityrabbit.com/goodwork/123` (any event)

## ?? Testing Checklist

### Before Deployment:
- [x] ? Build successful
- [x] ? Code updated to use `.jpg`
- [x] ? JPG file exists locally
- [ ] Deploy to production

### After Deployment:
- [ ] Image accessible at `https://charityrabbit.com/images/charityrabbit.jpg`
- [ ] Facebook Debugger shows image
- [ ] Twitter Card shows image
- [ ] LinkedIn shows image
- [ ] No console errors

### Social Media Preview Check:
- [ ] Facebook: Large image preview visible
- [ ] Twitter: Summary card with image
- [ ] LinkedIn: Image in link preview
- [ ] WhatsApp: Thumbnail shows correctly

## ?? Troubleshooting

### If Image Still Doesn't Show:

#### 1. Check File Deployed
```bash
# SSH to server and check:
ls -lh /app/wwwroot/images/
# Should show charityrabbit.jpg
```

#### 2. Check Response Headers
```bash
curl -I https://charityrabbit.com/images/charityrabbit.jpg
```
Should return:
```
HTTP/2 200
content-type: image/jpeg
content-length: [size]
```

#### 3. Check Image Size
```powershell
# Locally:
(Get-Item "wwwroot\images\charityrabbit.jpg").Length / 1KB
# Should be < 500 KB
```

#### 4. Compress Image (If Needed)
1. Go to: https://tinyjpg.com/
2. Upload `charityrabbit.jpg`
3. Download compressed version
4. Replace file
5. Redeploy

### Common Issues & Solutions:

| Issue | Solution |
|-------|----------|
| 404 Not Found | File not deployed - check Docker image includes wwwroot |
| 403 Forbidden | Check file permissions |
| Still cached | Wait 10 minutes, scrape again |
| Wrong dimensions | Image must be >= 200x200 px |
| File too large | Compress to < 300 KB |

## ?? Expected Results

### Facebook Preview:
```
???????????????????????????????????
?   CharityRabbit Logo Image      ?
?          (1.91:1 ratio)         ?
???????????????????????????????????
? CharityRabbit - Find & Create   ?
? Volunteer Opportunities         ?
???????????????????????????????????
? Discover local volunteer...     ?
? charityrabbit.com               ?
???????????????????????????????????
```

### Twitter Card:
```
???????????????????????????????????
?   CharityRabbit Logo Image      ?
???????????????????????????????????
? CharityRabbit - Find Volunteer  ?
? Opportunities                   ?
? charityrabbit.com               ?
???????????????????????????????????
```

## ?? Timeline

### Immediate (0-5 minutes):
- Build & deploy code
- Verify image is accessible

### Short-term (5-30 minutes):
- Clear Facebook cache
- Test all social platforms
- Verify previews work

### Medium-term (30-60 minutes):
- CDN propagation (if using CDN)
- Cache clearing on all platforms
- Full social media testing

### Long-term (1-24 hours):
- Old cached versions expire
- Search engines re-index
- All users see new image

## ?? Verification Commands

```powershell
# 1. Check local file
Test-Path "wwwroot\images\charityrabbit.jpg"

# 2. Check file size
(Get-Item "wwwroot\images\charityrabbit.jpg").Length / 1KB

# 3. Verify it's a valid JPEG
[System.IO.File]::ReadAllBytes("wwwroot\images\charityrabbit.jpg")[0..1]
# Should be: 255 216 (JPEG signature)

# 4. Test production URL
Invoke-WebRequest -Uri "https://charityrabbit.com/images/charityrabbit.jpg" -Method Head
# Should return: StatusCode: 200
```

## ?? Success Criteria

? **Deployment Successful When:**
1. Image loads at `https://charityrabbit.com/images/charityrabbit.jpg`
2. Facebook Debugger shows no errors
3. Image appears in all social previews
4. No "corrupted image" errors
5. Image dimensions shown correctly (1200 x 630)

## ?? If All Else Fails

### Nuclear Option: Create New Optimized Image

1. **Design new image:**
   - Size: 1200 x 630 px
   - Format: JPG
   - Quality: 80-90%
   - File size: < 200 KB

2. **Use online tool:**
   - Canva: https://www.canva.com
   - Template: Facebook Post (1200 x 630)
   - Add logo + text
   - Export as JPG

3. **Save as:**
   ```
   wwwroot/images/og-image.jpg
   ```

4. **Update references to:**
   ```
   /images/og-image.jpg
   ```

## ?? Support Resources

- **Facebook Debugger:** https://developers.facebook.com/tools/debug/
- **Twitter Validator:** https://cards-dev.twitter.com/validator
- **LinkedIn Inspector:** https://www.linkedin.com/post-inspector/
- **Image Optimizer:** https://tinyjpg.com/
- **Meta Tags Checker:** https://metatags.io/

## ?? Post-Deployment

### Share Your Success:
1. Test share on Facebook
2. Test tweet with link
3. Test LinkedIn post
4. Check analytics for increased engagement

### Monitor:
- Social media click-through rates
- Image load times
- Any error reports

---

**Current Status:** 
- ? Code updated to use JPG
- ? Build successful
- ? Ready for deployment
- ?? Next: Deploy and test

**Estimated Time to Fix:** 5-10 minutes after deployment
